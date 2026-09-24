using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Domain.Entities;
using SistemaTickets.Domain.Services;
using SistemaTickets.Infrastructure.Security;
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
        private readonly ILogger<TicketsController> _logger;

        private const long MaxFileBytes = 10 * 1024 * 1024; // 10 MB

        public TicketsController(
            ITicketService  ticketService,
            IUsuarioService usuarioService,
            IEmailService   emailService,
            IStorageService storageService,
            ILogger<TicketsController> logger)
        {
            _ticketService  = ticketService;
            _usuarioService = usuarioService;
            _emailService   = emailService;
            _storageService = storageService;
            _logger = logger;
        }

        // A4: las InvalidOperationException las lanza a propósito la capa de servicios con un
        // mensaje pensado para mostrarse al usuario; cualquier otra excepción (NHibernate,
        // SqlClient, etc.) se loggea completa acá y al cliente solo le llega un mensaje
        // genérico, para no filtrar detalles internos del backend.
        private IActionResult JsonError(Exception ex, string accion)
        {
            if (ex is InvalidOperationException)
                return Json(new { success = false, message = ex.Message });

            _logger.LogError(ex, "Error inesperado en TicketsController.{Accion}", accion);
            return Json(new { success = false, message = "Ocurrió un error inesperado. Intenta de nuevo más tarde." });
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
                return JsonError(ex, nameof(TomarTicket));
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
            // A2: validar archivos antes del ModelState general — tamaño, extensión permitida
            // (lista blanca) y que el contenido real (magic bytes) coincida con la extensión
            // declarada. Los streams válidos se mantienen abiertos para subirlos más abajo,
            // recién después de crear el ticket, sin volver a abrirlos.
            var archivosValidos = new List<(IFormFile File, Stream Stream, string ContentType)>();

            if (model.Archivos != null)
            {
                foreach (var f in model.Archivos.Where(f => f.Length > 0))
                {
                    if (f.Length > MaxFileBytes)
                    {
                        ModelState.AddModelError("Archivos", $"'{f.FileName}' supera el límite de 10 MB.");
                        continue;
                    }

                    var stream = f.OpenReadStream();
                    var contentType = await AttachmentValidator.ValidateAndResolveContentTypeAsync(f.FileName, stream);

                    if (contentType is null)
                    {
                        ModelState.AddModelError("Archivos",
                            $"'{f.FileName}' no es un tipo de archivo permitido, o su contenido no coincide con la extensión.");
                        await stream.DisposeAsync();
                        continue;
                    }

                    archivosValidos.Add((f, stream, contentType));
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
                foreach (var (_, stream, _) in archivosValidos)
                    await stream.DisposeAsync();

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
            // A2: se usa el content-type canónico ya validado por extensión/magic bytes,
            // nunca el que reportó el navegador (file.ContentType).
            foreach (var (file, stream, contentType) in archivosValidos)
            {
                await using var _ = stream;

                var blobPath = await _storageService.UploadAsync(stream, file.FileName, contentType, id);

                await _ticketService.AddAdjuntoAsync(new TicketAdjunto
                {
                    Ticket        = new Ticket { Id = id },
                    NombreArchivo = file.FileName,
                    RutaArchivo   = blobPath,
                    TipoContenido = contentType,
                    TamanioBytes  = file.Length
                }, CurrentUserId());
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

            // A2: nunca renderizar el adjunto inline — siempre forzar descarga, y evitar que
            // el navegador intente "adivinar" el tipo de contenido a partir del contenido.
            Response.Headers.Append("X-Content-Type-Options", "nosniff");

            // El tercer argumento (fileDownloadName) hace que MVC agregue
            // Content-Disposition: attachment; filename="...".
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
                return JsonError(ex, nameof(CambiarEstado));
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
                return JsonError(ex, nameof(AsignarPrioridad));
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
                return JsonError(ex, nameof(Asignar));
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
                return JsonError(ex, nameof(Cerrar));
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
                return JsonError(ex, nameof(AgregarComentario));
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
