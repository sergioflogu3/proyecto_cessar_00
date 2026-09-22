using SistemaTickets.Domain.Repositories;
using SistemaTickets.Domain.Services;
using SistemaTickets.Infrastructure.Persistence;
using SistemaTickets.Infrastructure.Services;
using SistemaTickets.Infrastructure.Persistence.Repositories;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
namespace SistemaTickets
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // MVC
            builder.Services.AddControllersWithViews();

            // NHibernate
            var connectionString = ValidateConnectionString(builder.Configuration.GetConnectionString("DefaultConnection"));

            // updateSchema: true solo en primer deploy para crear las tablas en Azure SQL
            // Cámbialo a false después del primer arranque exitoso
            var isFirstDeploy = builder.Configuration.GetValue<bool>("NHibernate:UpdateSchema");
            var sessionFactory = NHibernateBootstrap.BuildSessionFactory(
                connectionString,
                updateSchema: isFirstDeploy
            );

            builder.Services.AddSingleton(sessionFactory);
            builder.Services.AddScoped(factory => sessionFactory.OpenSession());

            // DI
            builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            builder.Services.AddScoped<IUsuarioService, UsuarioService>();
            builder.Services.AddScoped<ITicketRepository, TicketRepository>();
            builder.Services.AddScoped<ITicketService, TicketService>();
            builder.Services.AddScoped<IInventarioRepository, InventarioRepository>();
            builder.Services.AddScoped<IInventarioService, InventarioService>();
            builder.Services.AddScoped<ICampoRepository, CampoRepository>();
            builder.Services.AddScoped<ICampoService, CampoService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IReporteRepository, ReporteRepository>();
            builder.Services.AddScoped<IReporteService, ReporteService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddSingleton<IStorageService, AzureBlobStorageService>();

            // JWT
            var jwtSection = builder.Configuration.GetSection("Jwt");
            var jwtKey = ValidateJwtKey(jwtSection["Key"]);
            var jwtIssuer = jwtSection["Issuer"];
            var jwtAudience = jwtSection["Audience"];

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey)
                        ),
                        NameClaimType = System.Security.Claims.ClaimTypes.Name,
                        RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                        ClockSkew = TimeSpan.Zero
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = ctx =>
                        {
                            // Si ya viene por Authorization, lo respeta
                            var auth = ctx.Request.Headers["Authorization"].FirstOrDefault();
                            if (!string.IsNullOrEmpty(auth) &&
                                auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                return Task.CompletedTask;
                            }

                            // Tomar token desde cookie interna del sitio
                            var cookie = ctx.Request.Cookies["jwt"];
                            if (!string.IsNullOrEmpty(cookie))
                            {
                                ctx.Token = cookie;
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            // Manejo de errores en producci�n
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // Redirigir a login si intenta entrar al sitio sin autenticaci�n
            app.UseStatusCodePages(async ctx =>
            {
                var status = ctx.HttpContext.Response.StatusCode;
                var req = ctx.HttpContext.Request;

                var wantsHtml =
                    req.Headers.TryGetValue("Accept", out var acc) &&
                    acc.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);

                if ((status == 401 || status == 403)
                    && wantsHtml
                    && !req.Path.StartsWithSegments("/Users/Login", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.HttpContext.Response.Redirect("/Users/Login");
                }
            });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Users}/{action=Login}/{id?}");
            app.MapControllers();
            // Para Docker/Linux
            //app.Run("http://0.0.0.0:80");

            // Para local sin Docker, si luego quieres probar as�:
            app.Run();
        }

        // C1: la clave JWT nunca debe quedarse en su valor de ejemplo del repo,
        // ni ser demasiado corta para HMAC-SHA256. Si esto falla, la app no arranca.
        private const int MinJwtKeyLength = 32;

        private static readonly string[] KnownPlaceholderJwtKeys =
        {
            "REEMPLAZAR-CON-CLAVE-JWT-SEGURA-MINIMO-32-CARACTERES-AQUI",
            "TU-CLAVE-JWT-SUPER-SEGURA-DE-AL-MENOS-32-CARACTERES"
        };

        private static string ValidateJwtKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException(
                    "Jwt:Key no está configurada. Defínela mediante variable de entorno (Jwt__Key), " +
                    "User Secrets o Azure Key Vault antes de iniciar la aplicación.");
            }

            if (key.Length < MinJwtKeyLength)
            {
                throw new InvalidOperationException(
                    $"Jwt:Key es demasiado corta ({key.Length} caracteres). Debe tener al menos {MinJwtKeyLength} caracteres.");
            }

            var esPlaceholder =
                KnownPlaceholderJwtKeys.Any(p => string.Equals(p, key, StringComparison.OrdinalIgnoreCase)) ||
                key.Contains("REEMPLAZAR", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("CAMBIAR", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("TU-CLAVE", StringComparison.OrdinalIgnoreCase);

            if (esPlaceholder)
            {
                throw new InvalidOperationException(
                    "Jwt:Key todavía tiene un valor de ejemplo/placeholder. Reemplázala por una clave " +
                    "aleatoria real (variable de entorno, User Secrets o Key Vault) antes de iniciar la aplicación.");
            }

            return key;
        }

        // C2: la cadena de conexión no debe quedarse en su valor de ejemplo del repo.
        // Si esto falla, la app no arranca.
        private static readonly string[] PlaceholderConnectionStringMarkers =
        {
            "REEMPLAZAR",
            "PLACEHOLDER",
            "TU-USUARIO",
            "TU-PASSWORD",
            "TU_USUARIO",
            "TU_PASSWORD"
        };

        private static string ValidateConnectionString(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection no está configurada. Defínela mediante variable de " +
                    "entorno (ConnectionStrings__DefaultConnection), User Secrets o Azure Key Vault antes de " +
                    "iniciar la aplicación.");
            }

            var esPlaceholder = PlaceholderConnectionStringMarkers.Any(
                marker => connectionString.Contains(marker, StringComparison.OrdinalIgnoreCase));

            if (esPlaceholder)
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection todavía tiene un valor de ejemplo/placeholder. " +
                    "Reemplázala por credenciales reales (variable de entorno, User Secrets o Key Vault) " +
                    "antes de iniciar la aplicación.");
            }

            return connectionString;
        }
    }
}