using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Domain.Services;
using SistemaTickets.Models.Configuracion;

namespace SistemaTickets.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ConfiguracionController : Controller
    {
        private readonly ITicketService      _svc;
        private readonly IInventarioService  _inv;

        public ConfiguracionController(ITicketService svc, IInventarioService inv)
        {
            _svc = svc;
            _inv = inv;
        }

        private async Task<ConfiguracionViewModel> BuildVMAsync(string tab)
            => new()
            {
                TabActiva   = tab,
                Categorias  = await _svc.GetAllCategoriasAdminAsync(),
                Prioridades = await _svc.GetAllPrioridadesAdminAsync(),
                Estados     = await _svc.GetAllEstadosAdminAsync(),
                ActivoTipos = await _inv.GetAllActivoTiposAdminAsync(),
                Sedes       = await _inv.GetAllSedesAdminAsync(),
                Areas       = await _inv.GetAllAreasAdminAsync()
            };

        [HttpGet]
        public async Task<IActionResult> Index(string tab = "categorias")
            => View(await BuildVMAsync(tab));

        // ── Categorías ────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearCategoria(string nombre, string? descripcion)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                TempData["Error"] = "El nombre es obligatorio.";
                return RedirectToAction(nameof(Index), new { tab = "categorias" });
            }

            await _svc.CrearCategoriaAsync(nombre.Trim(), descripcion?.Trim());
            TempData["Success"] = $"Categoría \"{nombre.Trim()}\" creada.";
            return RedirectToAction(nameof(Index), new { tab = "categorias" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarCategoria(int id, string nombre, string? descripcion)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                TempData["Error"] = "El nombre es obligatorio.";
                return RedirectToAction(nameof(Index), new { tab = "categorias" });
            }

            await _svc.EditarCategoriaAsync(id, nombre.Trim(), descripcion?.Trim());
            TempData["Success"] = "Categoría actualizada.";
            return RedirectToAction(nameof(Index), new { tab = "categorias" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCategoria(int id)
        {
            await _svc.ToggleCategoriaAsync(id);
            return RedirectToAction(nameof(Index), new { tab = "categorias" });
        }

        // ── Prioridades ───────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearPrioridad(string nombre, string color, int orden, string? descripcion)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                TempData["Error"] = "El nombre es obligatorio.";
                return RedirectToAction(nameof(Index), new { tab = "prioridades" });
            }

            await _svc.CrearPrioridadAsync(nombre.Trim(), color, orden, descripcion?.Trim());
            TempData["Success"] = $"Prioridad \"{nombre.Trim()}\" creada.";
            return RedirectToAction(nameof(Index), new { tab = "prioridades" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPrioridad(int id, string nombre, string color, int orden, string? descripcion)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                TempData["Error"] = "El nombre es obligatorio.";
                return RedirectToAction(nameof(Index), new { tab = "prioridades" });
            }

            await _svc.EditarPrioridadAsync(id, nombre.Trim(), color, orden, descripcion?.Trim());
            TempData["Success"] = "Prioridad actualizada.";
            return RedirectToAction(nameof(Index), new { tab = "prioridades" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePrioridad(int id)
        {
            await _svc.TogglePrioridadAsync(id);
            return RedirectToAction(nameof(Index), new { tab = "prioridades" });
        }

        // ── Estados ───────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarEstado(int id, string color, int orden)
        {
            await _svc.EditarEstadoAsync(id, color, orden);
            TempData["Success"] = "Estado actualizado.";
            return RedirectToAction(nameof(Index), new { tab = "estados" });
        }

        // ── Tipos de Activo ───────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearActivoTipo(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                TempData["Error"] = "El nombre es obligatorio.";
                return RedirectToAction(nameof(Index), new { tab = "activotipos" });
            }
            await _inv.CrearActivoTipoAsync(nombre.Trim());
            TempData["Success"] = $"Tipo \"{nombre.Trim()}\" creado.";
            return RedirectToAction(nameof(Index), new { tab = "activotipos" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarActivoTipo(int id, string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                TempData["Error"] = "El nombre es obligatorio.";
                return RedirectToAction(nameof(Index), new { tab = "activotipos" });
            }
            await _inv.EditarActivoTipoAsync(id, nombre.Trim());
            TempData["Success"] = "Tipo actualizado.";
            return RedirectToAction(nameof(Index), new { tab = "activotipos" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActivoTipo(int id)
        {
            await _inv.ToggleActivoTipoAsync(id);
            return RedirectToAction(nameof(Index), new { tab = "activotipos" });
        }

        // ── Sedes ─────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearSede(string nombre, string ciudad, string? direccion)
        {
            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(ciudad))
            {
                TempData["Error"] = "Nombre y ciudad son obligatorios.";
                return RedirectToAction(nameof(Index), new { tab = "sedes" });
            }
            await _inv.CrearSedeAsync(nombre.Trim(), ciudad.Trim(), direccion?.Trim());
            TempData["Success"] = $"Sede \"{nombre.Trim()}\" creada.";
            return RedirectToAction(nameof(Index), new { tab = "sedes" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarSede(int id, string nombre, string ciudad, string? direccion)
        {
            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(ciudad))
            {
                TempData["Error"] = "Nombre y ciudad son obligatorios.";
                return RedirectToAction(nameof(Index), new { tab = "sedes" });
            }
            await _inv.EditarSedeAsync(id, nombre.Trim(), ciudad.Trim(), direccion?.Trim());
            TempData["Success"] = "Sede actualizada.";
            return RedirectToAction(nameof(Index), new { tab = "sedes" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSede(int id)
        {
            await _inv.ToggleSedeAsync(id);
            return RedirectToAction(nameof(Index), new { tab = "sedes" });
        }

        // ── Áreas ─────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearArea(string nombre, int sedeId)
        {
            if (string.IsNullOrWhiteSpace(nombre) || sedeId <= 0)
            {
                TempData["Error"] = "Nombre y sede son obligatorios.";
                return RedirectToAction(nameof(Index), new { tab = "areas" });
            }
            await _inv.CrearAreaAsync(nombre.Trim(), sedeId);
            TempData["Success"] = $"Área \"{nombre.Trim()}\" creada.";
            return RedirectToAction(nameof(Index), new { tab = "areas" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarArea(int id, string nombre, int sedeId)
        {
            if (string.IsNullOrWhiteSpace(nombre) || sedeId <= 0)
            {
                TempData["Error"] = "Nombre y sede son obligatorios.";
                return RedirectToAction(nameof(Index), new { tab = "areas" });
            }
            await _inv.EditarAreaAsync(id, nombre.Trim(), sedeId);
            TempData["Success"] = "Área actualizada.";
            return RedirectToAction(nameof(Index), new { tab = "areas" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleArea(int id)
        {
            await _inv.ToggleAreaAsync(id);
            return RedirectToAction(nameof(Index), new { tab = "areas" });
        }
    }
}
