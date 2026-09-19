using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Domain.Entities;
using SistemaTickets.Domain.Services;
using SistemaTickets.Infrastructure.Security;
using SistemaTickets.Models.Auth;
using SistemaTickets.Models.Users;
using System.Security.Claims;

namespace SistemaTickets.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IConfiguration  _configuration;

        public UsersController(IUsuarioService usuarioService, IConfiguration configuration)
        {
            _usuarioService = usuarioService;
            _configuration  = configuration;
        }

        // ── Auth ─────────────────────────────────────────────────────────────

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var isValid = await _usuarioService.ValidateCredentialsAsync(model.Login, model.Password);

            if (!isValid)
            {
                ModelState.AddModelError(string.Empty, "Credenciales inválidas o usuario inactivo.");
                return View(model);
            }

            var user = await _usuarioService.GetByLoginAsync(model.Login);
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "No se encontró el usuario.");
                return View(model);
            }

            var token = JwtHelper.CreateToken(
                user.Id.ToString(), user.Username, user.Email,
                user.Rol, user.NombreCompleto, _configuration);

            Response.Cookies.Append("jwt", token, new CookieOptions
            {
                HttpOnly  = true,
                Secure    = HttpContext.Request.IsHttps,
                SameSite  = SameSiteMode.Lax,
                Path      = "/",
                Expires   = DateTimeOffset.UtcNow.AddHours(2)
            });

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            return RedirectToAction(nameof(Login));
        }

        // ── Gestión de usuarios (solo Administrador) ─────────────────────────

        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, string? search = null, bool? activo = null)
        {
            const int pageSize = UsuarioIndexViewModel.PageSize;

            var (items, total) = await _usuarioService.GetPagedAsync(page, pageSize, search, activo);

            var vm = new UsuarioIndexViewModel
            {
                Usuarios       = items,
                TotalRegistros = total,
                PaginaActual   = page,
                TotalPaginas   = (int)Math.Ceiling((double)total / pageSize),
                Search         = search,
                FiltroActivo   = activo
            };

            return View(vm);
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public IActionResult Create() => View(new UsuarioFormViewModel());

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError(nameof(model.Password), "La contraseña es requerida");

            if (!ModelState.IsValid) return View(model);

            var usuario = new Usuario
            {
                Username       = model.Username,
                Email          = model.Email,
                NumeroTelefono = model.NumeroTelefono,
                Nombre         = model.Nombre,
                Apellido       = model.Apellido,
                Rol            = model.Rol
            };

            await _usuarioService.CreateAsync(usuario, model.Password!);

            TempData["Success"] = $"Usuario {model.Username} creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> GetForEdit(int id)
        {
            var user = await _usuarioService.GetByIdAsync(id);
            if (user is null) return NotFound();

            return Json(new
            {
                id             = user.Id,
                username       = user.Username,
                email          = user.Email,
                numeroTelefono = user.NumeroTelefono ?? string.Empty,
                nombre         = user.Nombre,
                apellido       = user.Apellido,
                rol            = (int)user.Rol
            });
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UsuarioFormViewModel model)
        {
            // Contraseña no obligatoria en edición
            ModelState.Remove(nameof(model.Password));

            if (!ModelState.IsValid)
                return Json(new { success = false, errors = ModelState.Values
                    .SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

            try
            {
                var usuario = new Usuario
                {
                    Id             = model.Id,
                    Username       = model.Username,
                    Email          = model.Email,
                    NumeroTelefono = model.NumeroTelefono,
                    Nombre         = model.Nombre,
                    Apellido       = model.Apellido,
                    Rol            = model.Rol
                };

                await _usuarioService.UpdateAsync(usuario, model.Password);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errors = new[] { ex.Message } });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPassword(CambiarPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Datos inválidos." });

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var ok = await _usuarioService.CambiarPasswordAsync(userId, model.PasswordActual, model.NuevoPassword);

            return ok
                ? Json(new { success = true,  message = "Contraseña cambiada correctamente." })
                : Json(new { success = false, message = "La contraseña actual no es correcta." });
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var currentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (id == currentId)
                return Json(new { success = false, message = "No puedes inactivar tu propia cuenta." });

            try
            {
                await _usuarioService.DeactivateAsync(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
