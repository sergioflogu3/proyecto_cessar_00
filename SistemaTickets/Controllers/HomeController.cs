using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SistemaTickets.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return role switch
            {
                "Administrador" => RedirectToAction(nameof(Administrador)),
                "Supervisor"    => RedirectToAction(nameof(Supervisor)),
                "Soporte"       => RedirectToAction(nameof(Soporte)),
                _               => RedirectToAction(nameof(Usuario))
            };
        }

        [Authorize(Roles = "Administrador")]
        public IActionResult Administrador()
        {
            SetViewBag();
            return View();
        }

        [Authorize(Roles = "Supervisor")]
        public IActionResult Supervisor()
        {
            SetViewBag();
            return View();
        }

        [Authorize(Roles = "Soporte")]
        public IActionResult Soporte()
        {
            SetViewBag();
            return View();
        }

        [Authorize(Roles = "Usuario")]
        public IActionResult Usuario()
        {
            SetViewBag();
            return View();
        }

        private void SetViewBag()
        {
            ViewBag.UserName = User.Claims.LastOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            ViewBag.UserRole = User.FindFirst(ClaimTypes.Role)?.Value;
        }
    }
}
