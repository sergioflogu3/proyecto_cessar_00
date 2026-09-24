using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SistemaTickets.Domain.Entities;
using SistemaTickets.Domain.Services;
using SistemaTickets.Infrastructure.Security;
using SistemaTickets.Models.Auth;
using SistemaTickets.Models.Users;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SistemaTickets.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IConfiguration  _configuration;
        private readonly ITokenRevocationService _tokenRevocationService;
        private readonly ILoginAttemptService _loginAttemptService;
        private readonly ILogger<UsersController> _logger;
        private readonly IWebHostEnvironment _environment;

        public UsersController(
            IUsuarioService usuarioService,
            IConfiguration configuration,
            ITokenRevocationService tokenRevocationService,
            ILoginAttemptService loginAttemptService,
            ILogger<UsersController> logger,
            IWebHostEnvironment environment)
        {
            _usuarioService = usuarioService;
            _configuration  = configuration;
            _tokenRevocationService = tokenRevocationService;
            _loginAttemptService = loginAttemptService;
            _logger = logger;
            _environment = environment;
        }

        private int GetConfiguredExpireMinutes() => _configuration.GetValue<int?>("Jwt:ExpireMinutes") ?? 30;

        // M1: un único mensaje para cualquier motivo de rechazo de login (usuario inexistente,
        // contraseña incorrecta, usuario inactivo) — no debe ser posible distinguir por el
        // mensaje si una cuenta existe o no.
        private const string LoginGenericoError = "Credenciales inválidas o usuario inactivo.";

        // A4: las InvalidOperationException las lanza a propósito la capa de servicios con un
        // mensaje pensado para mostrarse al usuario (p.ej. "Usuario no encontrado"); cualquier
        // otra excepción (NHibernate, SqlClient, etc.) se loggea completa acá y al cliente solo
        // le llega un mensaje genérico, para no filtrar detalles internos del backend.
        private IActionResult JsonError(Exception ex, string accion)
        {
            if (ex is InvalidOperationException)
                return Json(new { success = false, message = ex.Message });

            _logger.LogError(ex, "Error inesperado en UsersController.{Accion}", accion);
            return Json(new { success = false, message = "Ocurrió un error inesperado. Intenta de nuevo más tarde." });
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
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

            // A1: bloqueo temporal por usuario tras varios intentos fallidos, además del
            // rate limiting por IP de la política "login".
            if (_loginAttemptService.IsLockedOut(model.Login, out var retryAfter))
            {
                _logger.LogWarning(
                    "Login bloqueado temporalmente para '{Login}' desde {Ip}. Reintentar en {RetryAfterMinutes} min.",
                    model.Login, ip, Math.Ceiling(retryAfter.TotalMinutes));

                ModelState.AddModelError(string.Empty,
                    $"Demasiados intentos fallidos. Intenta de nuevo en {Math.Ceiling(retryAfter.TotalMinutes)} minuto(s).");
                return View(model);
            }

            var isValid = await _usuarioService.ValidateCredentialsAsync(model.Login, model.Password);

            if (!isValid)
            {
                _loginAttemptService.RegisterFailure(model.Login);
                _logger.LogWarning("Intento de login fallido para '{Login}' desde {Ip}.", model.Login, ip);

                ModelState.AddModelError(string.Empty, LoginGenericoError);
                return View(model);
            }

            _loginAttemptService.RegisterSuccess(model.Login);

            var user = await _usuarioService.GetByLoginAsync(model.Login);
            if (user is null)
            {
                // No debería pasar (ValidateCredentialsAsync ya confirmó que existe y está
                // activo), pero si pasa, el mensaje es el mismo genérico — nunca uno distinto
                // que delate por qué falló.
                _logger.LogWarning(
                    "ValidateCredentialsAsync devolvió true pero GetByLoginAsync no encontró a '{Login}'.",
                    model.Login);
                ModelState.AddModelError(string.Empty, LoginGenericoError);
                return View(model);
            }

            var token = JwtHelper.CreateToken(
                user.Id.ToString(), user.Username, user.Email,
                user.Rol, user.NombreCompleto, _configuration);

            Response.Cookies.Append("jwt", token, new CookieOptions
            {
                HttpOnly  = true,
                // A3: Secure=true en todo entorno real (Docker/producción), sin importar si
                // esta petición en particular llegó por HTTPS — así un proxy mal configurado
                // nunca puede downgradear la cookie en silencio. La única excepción es
                // Development, para poder probar localmente con el perfil "http" sin lidiar
                // con certificados; esto se decide por ASPNETCORE_ENVIRONMENT (fijo por
                // despliegue), nunca por el esquema de la petición entrante.
                Secure    = !_environment.IsDevelopment(),
                SameSite  = SameSiteMode.Lax,
                Path      = "/",
                Expires   = DateTimeOffset.UtcNow.AddMinutes(GetConfiguredExpireMinutes())
            });

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            // C3: además de borrar la cookie, revocamos el JWT server-side (jti) para que
            // no siga siendo válido si alguien lo capturó antes del logout.
            var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (!string.IsNullOrEmpty(jti))
            {
                _tokenRevocationService.Revoke(jti, DateTimeOffset.UtcNow.AddMinutes(GetConfiguredExpireMinutes()));
            }

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
                // A4: mismo criterio que JsonError, pero esta vista espera "errors" (array).
                if (ex is InvalidOperationException)
                    return Json(new { success = false, errors = new[] { ex.Message } });

                _logger.LogError(ex, "Error inesperado en UsersController.{Accion}", nameof(Edit));
                return Json(new { success = false, errors = new[] { "Ocurrió un error inesperado. Intenta de nuevo más tarde." } });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPassword(CambiarPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // M2: mostrar el motivo real (p.ej. longitud mínima, contraseña muy común) en
                // vez de un "Datos inválidos" genérico — la política de contraseñas debe ser
                // visible para quien la está por incumplir, no solo aplicada en silencio.
                var mensaje = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = string.IsNullOrWhiteSpace(mensaje) ? "Datos inválidos." : mensaje });
            }

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
                return JsonError(ex, nameof(Deactivate));
            }
        }
    }
}
