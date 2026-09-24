using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Domain.Entities;
using SistemaTickets.Domain.Services;
using SistemaTickets.Models.Campo;
using System.Security.Claims;

namespace SistemaTickets.Controllers
{
    [Authorize(Roles = "Administrador,Supervisor,Soporte")]
    public class CampoController : Controller
    {
        private readonly ICampoService   _campoService;
        private readonly ITicketService  _ticketService;
        private readonly IInventarioService _inventarioService;
        private readonly ILogger<CampoController> _logger;

        public CampoController(
            ICampoService campoService,
            ITicketService ticketService,
            IInventarioService inventarioService,
            ILogger<CampoController> logger)
        {
            _campoService       = campoService;
            _ticketService      = ticketService;
            _inventarioService  = inventarioService;
            _logger             = logger;
        }

        // A4: las InvalidOperationException las lanza a propósito la capa de servicios con un
        // mensaje pensado para mostrarse al usuario; cualquier otra excepción (NHibernate,
        // SqlClient, etc.) se loggea completa acá y al cliente solo le llega un mensaje
        // genérico, para no filtrar detalles internos del backend.
        private IActionResult JsonError(Exception ex, string accion)
        {
            if (ex is InvalidOperationException)
                return Json(new { success = false, message = ex.Message });

            _logger.LogError(ex, "Error inesperado en CampoController.{Accion}", accion);
            return Json(new { success = false, message = "Ocurrió un error inesperado. Intenta de nuevo más tarde." });
        }

        private int CurrentUserId()
            => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        private string CurrentRole()
            => User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        private bool EsAdminOSupervisor() => CurrentRole() is "Administrador" or "Supervisor";

        // ====================================================================
        // VISITAS TÉCNICAS
        // ====================================================================

        [HttpGet]
        public async Task<IActionResult> Visitas(
            int page = 1, string? search = null, string? estado = null)
        {
            const int pageSize = VisitaIndexViewModel.PageSize;

            // Soporte solo ve sus propias visitas
            int? tecnicoId = CurrentRole() == "Soporte" ? CurrentUserId() : null;

            var (items, total) = await _campoService.GetVisitasPagedAsync(
                page, pageSize, search, estado, tecnicoId);

            var vm = new VisitaIndexViewModel
            {
                Visitas        = items,
                TotalRegistros = total,
                PaginaActual   = page,
                TotalPaginas   = (int)Math.Ceiling((double)total / pageSize),
                Search         = search,
                FiltroEstado   = estado
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> VisitaCreate()
        {
            var vm = await BuildVisitaForm(null);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VisitaCreate(VisitaFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateVisitaForm(model);
                return View(model);
            }

            var visita = new VisitaTecnica
            {
                FechaVisita   = model.FechaVisita.ToUniversalTime(),
                Direccion     = model.Direccion,
                Descripcion   = model.Descripcion,
                Observaciones = model.Observaciones,
                Ticket        = model.TicketId.HasValue
                    ? new Ticket { Id = model.TicketId.Value } : null
            };

            var id = await _campoService.CreateVisitaAsync(visita, CurrentUserId());
            TempData["Success"] = $"Visita #{id} programada correctamente.";
            return RedirectToAction(nameof(VisitaDetails), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> VisitaDetails(int id)
        {
            var visita = await _campoService.GetVisitaByIdAsync(id);
            if (visita is null) return NotFound();

            if (CurrentRole() == "Soporte" && visita.Tecnico.Id != CurrentUserId())
                return Forbid();

            return View(visita);
        }

        [HttpGet]
        public async Task<IActionResult> VisitaEdit(int id)
        {
            var visita = await _campoService.GetVisitaByIdAsync(id);
            if (visita is null) return NotFound();
            if (CurrentRole() == "Soporte" && visita.Tecnico.Id != CurrentUserId()) return Forbid();

            var vm = await BuildVisitaForm(visita);
            return View("VisitaCreate", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VisitaEdit(VisitaFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateVisitaForm(model);
                return View("VisitaCreate", model);
            }

            var visita = new VisitaTecnica
            {
                Id            = model.Id,
                FechaVisita   = model.FechaVisita.ToUniversalTime(),
                Direccion     = model.Direccion,
                Descripcion   = model.Descripcion,
                Observaciones = model.Observaciones,
                Ticket        = model.TicketId.HasValue
                    ? new Ticket { Id = model.TicketId.Value } : null
            };

            await _campoService.UpdateVisitaAsync(visita);
            TempData["Success"] = "Visita actualizada correctamente.";
            return RedirectToAction(nameof(VisitaDetails), new { id = model.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VisitaCambiarEstado(int visitaId, string estado)
        {
            try
            {
                await _campoService.CambiarEstadoVisitaAsync(visitaId, estado);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return JsonError(ex, nameof(VisitaCambiarEstado));
            }
        }

        // ====================================================================
        // REPUESTOS
        // ====================================================================

        [HttpGet]
        public async Task<IActionResult> Repuestos(
            int page = 1, string? search = null, string? categoria = null)
        {
            const int pageSize = RepuestoIndexViewModel.PageSize;
            var (items, total) = await _campoService.GetRepuestosPagedAsync(
                page, pageSize, search, categoria);

            var alertas = await _campoService.GetStockBajoMinimoAsync();

            var vm = new RepuestoIndexViewModel
            {
                Repuestos       = items,
                StockAlerta     = alertas,
                TotalRegistros  = total,
                PaginaActual    = page,
                TotalPaginas    = (int)Math.Ceiling((double)total / pageSize),
                Search          = search,
                FiltroCategoria = categoria
            };
            return View(vm);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador,Supervisor")]
        public IActionResult RepuestoCreate()
            => View(new RepuestoFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Supervisor")]
        public async Task<IActionResult> RepuestoCreate(RepuestoFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var repuesto = MapToRepuesto(model);
            var id = await _campoService.CreateRepuestoAsync(repuesto);
            TempData["Success"] = $"Repuesto #{id} registrado correctamente.";
            return RedirectToAction(nameof(RepuestoDetails), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> RepuestoDetails(int id)
        {
            var repuesto = await _campoService.GetRepuestoByIdAsync(id);
            if (repuesto is null) return NotFound();

            var stocks = await _campoService.GetStockByRepuestoAsync(id);
            ViewBag.Stocks = stocks;
            ViewBag.Sedes  = await _inventarioService.GetSedesAsync();
            return View(repuesto);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador,Supervisor")]
        public async Task<IActionResult> RepuestoEdit(int id)
        {
            var repuesto = await _campoService.GetRepuestoByIdAsync(id);
            if (repuesto is null) return NotFound();
            return View("RepuestoCreate", new RepuestoFormViewModel
            {
                Id             = repuesto.Id,
                Nombre         = repuesto.Nombre,
                Descripcion    = repuesto.Descripcion,
                Codigo         = repuesto.Codigo,
                Categoria      = repuesto.Categoria,
                UnidadMedida   = repuesto.UnidadMedida,
                PrecioUnitario = repuesto.PrecioUnitario
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Supervisor")]
        public async Task<IActionResult> RepuestoEdit(RepuestoFormViewModel model)
        {
            if (!ModelState.IsValid) return View("RepuestoCreate", model);
            await _campoService.UpdateRepuestoAsync(MapToRepuesto(model));
            TempData["Success"] = "Repuesto actualizado.";
            return RedirectToAction(nameof(RepuestoDetails), new { id = model.Id });
        }

        // ── AJAX: Ajustar stock ───────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Supervisor")]
        public async Task<IActionResult> AjustarStock(
            int repuestoId, int sedeId, int cantidad, bool esEntrada)
        {
            if (cantidad <= 0)
                return Json(new { success = false, message = "La cantidad debe ser mayor a 0." });
            try
            {
                await _campoService.AjustarStockAsync(repuestoId, sedeId, cantidad, esEntrada);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return JsonError(ex, nameof(AjustarStock));
            }
        }

        // ── AJAX: Registrar consumo ───────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarConsumo(
            int repuestoId, int cantidad, int? ticketId, int? visitaId,
            int? sedeId, string? observaciones)
        {
            if (cantidad <= 0)
                return Json(new { success = false, message = "La cantidad debe ser mayor a 0." });
            try
            {
                var consumo = new ConsumoRepuesto
                {
                    Repuesto      = new Repuesto      { Id = repuestoId },
                    Tecnico       = new Usuario       { Id = CurrentUserId() },
                    Ticket        = ticketId.HasValue ? new Ticket        { Id = ticketId.Value }  : null,
                    Visita        = visitaId.HasValue ? new VisitaTecnica { Id = visitaId.Value }  : null,
                    Cantidad      = cantidad,
                    Observaciones = observaciones
                };
                await _campoService.RegistrarConsumoAsync(consumo, sedeId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return JsonError(ex, nameof(RegistrarConsumo));
            }
        }

        // ── AJAX: Repuestos disponibles para select ───────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetRepuestosSelect(string? search)
        {
            var (items, _) = await _campoService.GetRepuestosPagedAsync(1, 50, search, null);
            return Json(items.Select(r => new
            {
                id     = r.Id,
                nombre = r.Nombre,
                codigo = r.Codigo ?? "",
                unidad = r.UnidadMedida
            }));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private async Task<VisitaFormViewModel> BuildVisitaForm(VisitaTecnica? v)
        {
            var role   = CurrentRole();
            int? cPorId = role == "Soporte" ? CurrentUserId() : null;
            var (tickets, _) = await _ticketService.GetPagedAsync(
                1, 100, null, null, null, null, cPorId, null);

            return new VisitaFormViewModel
            {
                Id            = v?.Id ?? 0,
                Direccion     = v?.Direccion ?? string.Empty,
                FechaVisita   = v?.FechaVisita.ToLocalTime() ?? DateTime.Today,
                Descripcion   = v?.Descripcion,
                Observaciones = v?.Observaciones,
                TicketId      = v?.Ticket?.Id,
                Tickets       = tickets.Where(t => t.EstaAbierto).ToList()
            };
        }

        private async Task PopulateVisitaForm(VisitaFormViewModel model)
        {
            var role   = CurrentRole();
            int? cPorId = role == "Soporte" ? CurrentUserId() : null;
            var (tickets, _) = await _ticketService.GetPagedAsync(
                1, 100, null, null, null, null, cPorId, null);
            model.Tickets = tickets.Where(t => t.EstaAbierto).ToList();
        }

        private static Repuesto MapToRepuesto(RepuestoFormViewModel m) => new()
        {
            Id             = m.Id,
            Nombre         = m.Nombre,
            Descripcion    = m.Descripcion,
            Codigo         = m.Codigo,
            Categoria      = m.Categoria,
            UnidadMedida   = m.UnidadMedida,
            PrecioUnitario = m.PrecioUnitario
        };
    }
}
