using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Domain.Entities;
using SistemaTickets.Domain.Services;
using SistemaTickets.Models.Tickets;
using System.Security.Claims;

namespace SistemaTickets.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly ITicketService  _ticketService;
        private readonly IUsuarioService _usuarioService;
        private readonly IEmailService   _emailService;
        private readonly IStorageService _storageService;

        private const long MaxFileBytes = 10 * 1024 * 1024; // 10 MB

        public TicketsController(
            ITicketService  ticketService,
            IUsuarioService usuarioService,
            IEmailService   emailService,
            IStorageService storageService)
        {
            _ticketService  = ticketService;
            _usuarioService = usuarioService;
            _emailService   = emailService;
            _storageService = storageService;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private int CurrentUserId()
            => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        private string CurrentRole()
            => User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        private bool EsAdminOSupervisor() =>
            CurrentRole() is "Administrador" or "Supervisor";

        private bool EsAdminOSupervisorOSoporte() =>
            CurrentRole() is "Administrador" or "Supervisor" or "Soporte";

        // ── Index ─────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index(
            int page = 1,
            string? search = null,
            int? estadoId = null,
            int? prioridadId = null)
        {
            const int pageSize = TicketIndexViewModel.PageSize;

            var role   = CurrentRole();
            var userId = CurrentUserId();

            int? creadoPorId  = role == "Usuario"  ? userId : null;
            int? asignadoAId  = role == "Soporte"  ? userId : null;

            var (items, total) = await _ticketService.GetPagedAsync(
                page, pageSize, search, estadoId, prioridadId,
                categoriaId: null, creadoPorId, asignadoAId);

            var vm = new TicketIndexViewModel
            {
                Tickets        = items,
                TotalRegistros = total,
                PaginaActual   = page,
                TotalPaginas   = (int)Math.Ceiling((double)total / pageSize),
                Search         = search,
                FiltroEstado   = estadoId,
                FiltroPrioridad = prioridadId,
                Estados        = await _ticketService.GetEstadosAsync(),
                Prioridades    = await _ticketService.GetPrioridadesAsync()
            };

            return View(vm);
        }

        // ── Tickets Nuevos (sin asignar) ──────────────────────────────────────

        [HttpGet]
        [Authorize(Roles = "Administrador,Supervisor,Soporte")]
        public async Task<IActionResult> Nuevos()
        {
            var tickets = await _ticketService.GetTicketsNuevosAsync();
            return View(tickets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Supervisor,Soporte")]
        public async Task<IActionResult> TomarTicket(int ticketId)
        {
            try
            {
                await _ticketService.TomarTicketAsync(ticketId, CurrentUserId());
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── Create ────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = await BuildFormViewModel(null);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TicketFormViewModel model)
        {
            // Validar archivos antes del ModelState general
            if (model.Archivos != null)
            {
                foreach (var f in model.Archivos)
                {
                    if (f.Length > MaxFileBytes)
                        ModelState.AddModelError("Archivos",
                            $"'{f.FileName}' supera el límite de 10 MB.");
                }
            }

            // Normalizar/validar segun el rol: el formulario del rol "Usuario"
            // no expone Prioridad ni Asignado a, asi que se ignora lo que llegue
            // en el POST y la prioridad queda pendiente de triage.
            if (EsAdminOSupervisorOSoporte())
            {
                if (!model.PrioridadId.HasValue)
                    ModelState.AddModelError(nameof(model.PrioridadId),
                        "Selecciona una prioridad.");
            }
            else
            {
                model.PrioridadId = null;
                model.AsignadoAId = null;
            }

            if (!ModelState.IsValid)
            {
                await PopulateFormCatalogs(model);
                return View(model);
            }

            var ticket = new Ticket
            {
                Titulo          = model.Titulo,
                Descripcion     = model.Descripcion,
                Prioridad       = model.PrioridadId.HasValue
                    ? new TicketPrioridad { Id = model.PrioridadId.Value }
                    : null,
                Categoria       = model.CategoriaId.HasValue
                    ? new TicketCategoria { Id = model.CategoriaId.Value }
                    : null,
                AsignadoA       = model.AsignadoAId.HasValue
                    ? new Usuario { Id = model.AsignadoAId.Value }
                    : null,
                FechaVencimiento = model.FechaVencimiento
            };

            var solicitanteId = EsAdminOSupervisorOSoporte() && model.CreadoPorId.HasValue
                ? model.CreadoPorId.Value
                : CurrentUserId();

            var id = await _ticketService.CreateAsync(ticket, solicitanteId);

            // ── Subir adjuntos a Azure Blob Storage ────────────────────────
            if (model.Archivos != null)
            {
                foreach (var file in model.Archivos.Where(f => f.Length > 0))
                {
                    await using var stream = file.OpenReadStream();
                    var blobPath = await _storageService.UploadAsync(
                        stream, file.FileName,
                        file.ContentType ?? "application/octet-stream",
                        id);

                    await _ticketService.AddAdjuntoAsync(new TicketAdjunto
                    {
                        Ticket        = new Ticket { Id = id },
                        NombreArchivo = file.FileName,
                        RutaArchivo   = blobPath,
                        TipoContenido = file.ContentType,
                        TamanioBytes  = file.Length
                    }, CurrentUserId());
                }
            }

            if (model.EnviarCopia)
            {
                var solicitante = await _usuarioService.GetByIdAsync(solicitanteId);
                if (solicitante is not null)
                {
                    await _emailService.SendTicketCopyAsync(
                        id, model.Titulo, model.Descripcion,
                        DateTime.Now, solicitante);
                }
            }

            TempData["Success"] = $"Ticket #{id} creado correctamente.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── Descargar adjunto ─────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> DescargarAdjunto(int id)
        {
            var adjunto = await _ticketService.GetAdjuntoByIdAsync(id);
            if (adjunto is null) return NotFound();

            // Verificar que el usuario puede ver el ticket al que pertenece
            var ticket = await _ticketService.GetByIdAsync(adjunto.Ticket.Id);
            if (ticket is null || !PuedeVerTicket(ticket)) return Forbid();

            var (stream, contentType) = await _storageService.DownloadAsync(adjunto.RutaArchivo);

            return File(stream, contentType, adjunto.NombreArchivo);
        }

        // ── Details ───────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _ticketService.GetByIdAsync(id);
            if (ticket is null) return NotFound();

            if (!PuedeVerTicket(ticket)) return Forbid();

            var role = CurrentRole();

            var vm = new TicketDetailsViewModel
            {
                Ticket              = ticket,
                Estados             = await _ticketService.GetEstadosAsync(),
                Prioridades         = await _ticketService.GetPrioridadesAsync(),
                PuedeEditar         = CurrentRole() == "Administrador",
                PuedeCambiarEstado  = EsAdminOSupervisor() || role == "Soporte",
                PuedeAsignar        = EsAdminOSupervisor(),
                PuedeAsignarPrioridad = EsAdminOSupervisor(),
                PuedeCerrar         = EsAdminOSupervisor() || role == "Soporte",
                PuedeVerInternos    = role != "Usuario"
            };

            if (vm.PuedeAsignar)
            {
                var (agentes, _) = await _usuarioService.GetPagedAsync(1, 200, null, true);
                vm.Agentes = agentes
                    .Where(u => u.Rol is Domain.Enums.RolUsuario.Soporte or Domain.Enums.RolUsuario.Supervisor or Domain.Enums.RolUsuario.Administrador)
                    .ToList();
            }

            return View(vm);
        }

        // ── Edit ──────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var ticket = await _ticketService.GetByIdAsync(id);
            if (ticket is null) return NotFound();
            if (CurrentRole() != "Administrador")
                return Forbid();

            var vm = await BuildFormViewModel(ticket);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TicketFormViewModel model)
        {
            if (CurrentRole() != "Administrador")
                return Forbid();

            if (!ModelState.IsValid)
            {
                await PopulateFormCatalogs(model);
                return View(model);
            }

            var ticket = new Ticket
            {
                Id           = model.Id,
                Titulo       = model.Titulo,
                Descripcion  = model.Descripcion,
                Prioridad    = model.PrioridadId.HasValue
                    ? new TicketPrioridad { Id = model.PrioridadId.Value }
                    : null,
                Categoria    = model.CategoriaId.HasValue
                    ? new TicketCategoria { Id = model.CategoriaId.Value }
                    : null,
                AsignadoA    = model.AsignadoAId.HasValue
                    ? new Usuario { Id = model.AsignadoAId.Value }
                    : null,
                FechaVencimiento = model.FechaVencimiento
            };

            await _ticketService.UpdateAsync(ticket, CurrentUserId());

            TempData["Success"] = "Ticket actualizado correctamente.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        // ── AJAX: Cambiar estado ──────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int ticketId, int estadoId)
        {
            if (!EsAdminOSupervisor() && CurrentRole() != "Soporte")
                return Json(new { success = false, message = "Sin permiso." });
            try
            {
                await _ticketService.ChangeStatusAsync(ticketId, estadoId, CurrentUserId());
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── AJAX: Asignar prioridad ───────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarPrioridad(int ticketId, int prioridadId)
        {
            if (!EsAdminOSupervisor())
                return Json(new { success = false, message = "Sin permiso." });
            try
            {
                await _ticketService.AsignarPrioridadAsync(ticketId, prioridadId, CurrentUserId());
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── AJAX: Asignar ─────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar(int ticketId, int asignadoAId)
        {
            if (!EsAdminOSupervisor())
                return Json(new { success = false, message = "Sin permiso." });
            try
            {
                await _ticketService.AssignAsync(ticketId, asignadoAId, CurrentUserId());
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── AJAX: Cerrar ──────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cerrar(int ticketId)
        {
            if (!EsAdminOSupervisor() && CurrentRole() != "Soporte")
                return Json(new { success = false, message = "Sin permiso." });
            try
            {
                await _ticketService.CloseAsync(ticketId, CurrentUserId());
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── AJAX: Agregar comentario ──────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarComentario(int ticketId, string contenido, bool esInterno = false)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return Json(new { success = false, message = "El contenido no puede estar vacío." });

            var role = CurrentRole();
            if (esInterno && role == "Usuario")
                return Json(new { success = false, message = "Sin permiso para notas internas." });

            try
            {
                var comentario = new TicketComentario
                {
                    Ticket    = new Ticket  { Id = ticketId },
                    Usuario   = new Usuario { Id = CurrentUserId() },
                    Contenido = contenido,
                    EsInterno = esInterno
                };
                await _ticketService.AddCommentAsync(comentario);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── Helpers privados ─────────────────────────────────────────────────

        private bool PuedeVerTicket(Ticket t)
        {
            var role = CurrentRole();
            if (EsAdminOSupervisor()) return true;
            if (role == "Soporte")    return t.AsignadoA?.Id == CurrentUserId();
            return t.CreadoPor.Id == CurrentUserId();
        }

        private async Task<TicketFormViewModel> BuildFormViewModel(Ticket? ticket)
        {
            var prioridades = await _ticketService.GetPrioridadesAsync();
            var categorias  = await _ticketService.GetCategoriasAsync();

            IList<Usuario> agentes = new List<Usuario>();
            if (EsAdminOSupervisor())
            {
                var (all, _) = await _usuarioService.GetPagedAsync(1, 200, null, true);
                agentes = all
                    .Where(u => u.Rol is Domain.Enums.RolUsuario.Soporte
                                      or Domain.Enums.RolUsuario.Supervisor
                                      or Domain.Enums.RolUsuario.Administrador)
                    .ToList();
            }

            IList<Domain.Entities.Usuario> clientes = new List<Domain.Entities.Usuario>();
            if (EsAdminOSupervisorOSoporte())
            {
                var (all, _) = await _usuarioService.GetPagedAsync(1, 500, null, true);
                clientes = all
                    .Where(u => u.Rol == Domain.Enums.RolUsuario.Usuario)
                    .ToList();
            }

            return new TicketFormViewModel
            {
                Id               = ticket?.Id ?? 0,
                Titulo           = ticket?.Titulo ?? string.Empty,
                Descripcion      = ticket?.Descripcion ?? string.Empty,
                PrioridadId      = ticket?.Prioridad?.Id,
                CategoriaId      = ticket?.Categoria?.Id,
                AsignadoAId      = ticket?.AsignadoA?.Id,
                CreadoPorId      = ticket?.CreadoPor?.Id,
                FechaVencimiento = ticket?.FechaVencimiento,
                Prioridades      = prioridades,
                Categorias       = categorias,
                Agentes          = agentes,
                Clientes         = clientes
            };
        }

        private async Task PopulateFormCatalogs(TicketFormViewModel model)
        {
            model.Prioridades = await _ticketService.GetPrioridadesAsync();
            model.Categorias  = await _ticketService.GetCategoriasAsync();

            if (EsAdminOSupervisor())
            {
                var (all, _) = await _usuarioService.GetPagedAsync(1, 200, null, true);
                model.Agentes = all
                    .Where(u => u.Rol is Domain.Enums.RolUsuario.Soporte
                                      or Domain.Enums.RolUsuario.Supervisor
                                      or Domain.Enums.RolUsuario.Administrador)
                    .ToList();
            }

            if (EsAdminOSupervisorOSoporte())
            {
                var (all, _) = await _usuarioService.GetPagedAsync(1, 500, null, true);
                model.Clientes = all
                    .Where(u => u.Rol == Domain.Enums.RolUsuario.Usuario)
                    .ToList();
            }
        }
    }
}
