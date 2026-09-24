using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Domain.Entities;
using SistemaTickets.Domain.Enums;
using SistemaTickets.Domain.Services;
using SistemaTickets.Models.Inventario;
using System.Security.Claims;

namespace SistemaTickets.Controllers
{
    [Authorize(Roles = "Administrador,Supervisor,Soporte")]
    public class InventarioController : Controller
    {
        private readonly IInventarioService _inventarioService;
        private readonly IUsuarioService    _usuarioService;
        private readonly ILogger<InventarioController> _logger;

        public InventarioController(
            IInventarioService inventarioService,
            IUsuarioService usuarioService,
            ILogger<InventarioController> logger)
        {
            _inventarioService = inventarioService;
            _usuarioService    = usuarioService;
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

            _logger.LogError(ex, "Error inesperado en InventarioController.{Accion}", accion);
            return Json(new { success = false, message = "Ocurrió un error inesperado. Intenta de nuevo más tarde." });
        }

        private int CurrentUserId()
            => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        private string CurrentRole()
            => User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        private bool EsAdmin() => CurrentRole() == "Administrador";
        private bool EsAdminOSupervisor() => CurrentRole() is "Administrador" or "Supervisor";

        // ====================================================================
        // ACTIVOS
        // ====================================================================

        [HttpGet]
        public async Task<IActionResult> Activos(
            int page = 1, string? search = null,
            string? tipo = null, string? estado = null)
        {
            const int pageSize = ActivoIndexViewModel.PageSize;
            var (items, total) = await _inventarioService.GetActivosPagedAsync(
                page, pageSize, search, tipo, estado);

            var vm = new ActivoIndexViewModel
            {
                Activos        = items,
                TotalRegistros = total,
                PaginaActual   = page,
                TotalPaginas   = (int)Math.Ceiling((double)total / pageSize),
                Search         = search,
                FiltroTipo     = tipo,
                FiltroEstado   = estado,
                TiposActivo    = await _inventarioService.GetActivoTiposAsync()
            };
            return View(vm);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ActivoCreate()
        {
            var vm = await BuildActivoForm(null);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ActivoCreate(ActivoFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateActivoForm(model);
                return View(model);
            }
            var activo = MapToActivo(model);
            var id = await _inventarioService.CreateActivoAsync(activo, CurrentUserId());
            TempData["Success"] = $"Activo #{id} registrado correctamente.";
            return RedirectToAction(nameof(ActivoDetails), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> ActivoDetails(int id)
        {
            var activo = await _inventarioService.GetActivoByIdAsync(id);
            if (activo is null) return NotFound();
            return View(activo);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ActivoEdit(int id)
        {
            var activo = await _inventarioService.GetActivoByIdAsync(id);
            if (activo is null) return NotFound();
            var vm = await BuildActivoForm(activo);
            return View("ActivoCreate", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ActivoEdit(ActivoFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateActivoForm(model);
                return View("ActivoCreate", model);
            }
            var activo = MapToActivo(model);
            await _inventarioService.UpdateActivoAsync(activo, CurrentUserId());
            TempData["Success"] = "Activo actualizado correctamente.";
            return RedirectToAction(nameof(ActivoDetails), new { id = model.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ActivoBaja(int id)
        {
            try
            {
                await _inventarioService.BajaActivoAsync(id, CurrentUserId());
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return JsonError(ex, nameof(ActivoBaja));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Supervisor")]
        public async Task<IActionResult> AsignarActivo(int activoId, int usuarioId)
        {
            try
            {
                await _inventarioService.AsignarActivoAsync(activoId, usuarioId, CurrentUserId());
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return JsonError(ex, nameof(AsignarActivo));
            }
        }

        // ====================================================================
        // HERRAMIENTAS
        // ====================================================================

        [HttpGet]
        public async Task<IActionResult> Herramientas(
            int page = 1, string? search = null,
            string? tipo = null, string? estado = null)
        {
            const int pageSize = HerramientaIndexViewModel.PageSize;

            // Soporte solo ve sus herramientas asignadas
            if (CurrentRole() == "Soporte")
            {
                var asignaciones = await _inventarioService.GetHerramientasByTecnicoAsync(CurrentUserId());
                var vm2 = new HerramientaIndexViewModel
                {
                    Herramientas   = asignaciones.Select(a => a.Herramienta).ToList(),
                    TotalRegistros = asignaciones.Count,
                    PaginaActual   = 1,
                    TotalPaginas   = 1
                };
                return View(vm2);
            }

            var (items, total) = await _inventarioService.GetHerramientasPagedAsync(
                page, pageSize, search, tipo, estado);

            var vm = new HerramientaIndexViewModel
            {
                Herramientas   = items,
                TotalRegistros = total,
                PaginaActual   = page,
                TotalPaginas   = (int)Math.Ceiling((double)total / pageSize),
                Search         = search,
                FiltroTipo     = tipo,
                FiltroEstado   = estado
            };
            return View(vm);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador,Supervisor")]
        public IActionResult HerramientaCreate()
            => View(new HerramientaFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Supervisor")]
        public async Task<IActionResult> HerramientaCreate(HerramientaFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var herramienta = new Herramienta
            {
                Nombre      = model.Nombre,
                Descripcion = model.Descripcion,
                Codigo      = model.Codigo,
                Tipo        = model.Tipo
            };
            var id = await _inventarioService.CreateHerramientaAsync(herramienta, CurrentUserId());
            TempData["Success"] = $"Herramienta #{id} registrada correctamente.";
            return RedirectToAction(nameof(HerramientaDetails), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> HerramientaDetails(int id)
        {
            var h = await _inventarioService.GetHerramientaByIdAsync(id);
            if (h is null) return NotFound();
            return View(h);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador,Supervisor")]
        public async Task<IActionResult> HerramientaEdit(int id)
        {
            var h = await _inventarioService.GetHerramientaByIdAsync(id);
            if (h is null) return NotFound();
            return View("HerramientaCreate", new HerramientaFormViewModel
            {
                Id          = h.Id,
                Nombre      = h.Nombre,
                Descripcion = h.Descripcion,
                Codigo      = h.Codigo,
                Tipo        = h.Tipo
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Supervisor")]
        public async Task<IActionResult> HerramientaEdit(HerramientaFormViewModel model)
        {
            if (!ModelState.IsValid) return View("HerramientaCreate", model);

            await _inventarioService.UpdateHerramientaAsync(new Herramienta
            {
                Id          = model.Id,
                Nombre      = model.Nombre,
                Descripcion = model.Descripcion,
                Codigo      = model.Codigo,
                Tipo        = model.Tipo
            }, CurrentUserId());

            TempData["Success"] = "Herramienta actualizada correctamente.";
            return RedirectToAction(nameof(HerramientaDetails), new { id = model.Id });
        }

        // ── AJAX: Asignar herramienta ─────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Supervisor")]
        public async Task<IActionResult> AsignarHerramienta(
            int herramientaId, int tecnicoId, string? observaciones)
        {
            try
            {
                await _inventarioService.AsignarHerramientaAsync(
                    herramientaId, tecnicoId, CurrentUserId(), observaciones);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return JsonError(ex, nameof(AsignarHerramienta));
            }
        }

        // ── AJAX: Devolver herramienta ────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Supervisor")]
        public async Task<IActionResult> DevolverHerramienta(int herramientaId, string? observaciones)
        {
            try
            {
                await _inventarioService.DevolverHerramientaAsync(
                    herramientaId, CurrentUserId(), observaciones);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return JsonError(ex, nameof(DevolverHerramienta));
            }
        }

        // ── AJAX: Lista técnicos para asignar ────────────────────────────────

        [HttpGet]
        [Authorize(Roles = "Administrador,Supervisor")]
        public async Task<IActionResult> GetTecnicos()
        {
            var (usuarios, _) = await _usuarioService.GetPagedAsync(1, 200, null, true);
            var tecnicos = usuarios
                .Where(u => u.Rol is RolUsuario.Soporte or RolUsuario.Supervisor or RolUsuario.Administrador)
                .Select(u => new { id = u.Id, nombre = u.NombreCompleto })
                .ToList();
            return Json(tecnicos);
        }

        // ── AJAX: Areas por sede ──────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetAreasBySede(int sedeId)
        {
            var areas = await _inventarioService.GetAreasAsync(sedeId);
            return Json(areas.Select(a => new { id = a.Id, nombre = a.Nombre }));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private async Task<ActivoFormViewModel> BuildActivoForm(Activo? activo)
        {
            var tipos    = await _inventarioService.GetActivoTiposAsync();
            var sedes    = await _inventarioService.GetSedesAsync();
            var areas    = await _inventarioService.GetAreasAsync();
            var (usuarios, _) = await _usuarioService.GetPagedAsync(1, 200, null, true);

            return new ActivoFormViewModel
            {
                Id               = activo?.Id ?? 0,
                Nombre           = activo?.Nombre ?? string.Empty,
                Descripcion      = activo?.Descripcion,
                Tipo             = activo?.Tipo ?? string.Empty,
                NumeroSerie      = activo?.NumeroSerie,
                Marca            = activo?.Marca,
                Modelo           = activo?.Modelo,
                Estado           = activo?.Estado ?? "Disponible",
                SedeId           = activo?.Sede?.Id,
                AreaId           = activo?.Area?.Id,
                AsignadoAId      = activo?.AsignadoA?.Id,
                FechaAdquisicion = activo?.FechaAdquisicion,
                FechaGarantia    = activo?.FechaGarantia,
                TiposActivo      = tipos,
                Sedes            = sedes,
                Areas            = areas,
                Usuarios         = usuarios.ToList()
            };
        }

        private async Task PopulateActivoForm(ActivoFormViewModel model)
        {
            model.TiposActivo = await _inventarioService.GetActivoTiposAsync();
            model.Sedes       = await _inventarioService.GetSedesAsync();
            model.Areas       = await _inventarioService.GetAreasAsync();
            var (usuarios, _) = await _usuarioService.GetPagedAsync(1, 200, null, true);
            model.Usuarios    = usuarios.ToList();
        }

        private static Activo MapToActivo(ActivoFormViewModel model) => new()
        {
            Id               = model.Id,
            Nombre           = model.Nombre,
            Descripcion      = model.Descripcion,
            Tipo             = model.Tipo,
            NumeroSerie      = model.NumeroSerie,
            Marca            = model.Marca,
            Modelo           = model.Modelo,
            Estado           = model.Estado,
            Sede             = model.SedeId.HasValue ? new Sede { Id = model.SedeId.Value } : null,
            Area             = model.AreaId.HasValue ? new Area { Id = model.AreaId.Value } : null,
            AsignadoA        = model.AsignadoAId.HasValue ? new Usuario { Id = model.AsignadoAId.Value } : null,
            FechaAdquisicion = model.FechaAdquisicion,
            FechaGarantia    = model.FechaGarantia
        };
    }
}
