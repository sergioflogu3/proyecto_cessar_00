using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Domain.Services;
using System.Security.Claims;

namespace SistemaTickets.Controllers
{
    [Authorize(Roles = "Administrador,Supervisor,Soporte")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
            => _dashboardService = dashboardService;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var role   = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            // M4: RequireValidUserIdFilter ya garantizó que este claim existe y es un entero
            // válido antes de que la acción se ejecute — no hace falta (ni corresponde) un
            // fallback a "0".
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var nombre = User.Claims.LastOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var esAdminOSupervisor = role is "Administrador" or "Supervisor";

            int? tecnicoId = role == "Soporte" ? userId : null;

            var vm = await _dashboardService.GetDashboardAsync(tecnicoId, esAdminOSupervisor, nombre);
            return View(vm);
        }
    }
}
