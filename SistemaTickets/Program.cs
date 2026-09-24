using SistemaTickets.Domain.Repositories;
using SistemaTickets.Domain.Services;
using SistemaTickets.Infrastructure.Persistence;
using SistemaTickets.Infrastructure.Services;
using SistemaTickets.Infrastructure.Persistence.Repositories;
using SistemaTickets.Infrastructure.Security;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
namespace SistemaTickets
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // MVC
            // M4: RequireValidUserIdFilter corta con 401 cualquier acción autenticada cuyo
            // claim NameIdentifier falte o no sea un entero válido, antes de que el controller
            // llegue a resolverlo.
            builder.Services.AddControllersWithViews(options =>
                options.Filters.Add<RequireValidUserIdFilter>());

            // A3: HSTS con preload + subdominios + vigencia larga (se aplica vía UseHsts más
            // abajo, que ya está gateado a "fuera de Development").
            builder.Services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            // M6: a diferencia de Email (opcional — EmailService se salta el envío si no está
            // configurado), el almacenamiento de adjuntos no es opcional: todo el flujo de
            // tickets con archivos depende de él. Validar acá, temprano, evita que la primera
            // falla sea una excepción críptica de Azure.Storage.Blobs recién cuando alguien sube
            // un adjunto.
            ValidateAzureBlobStorageConnectionString(builder.Configuration["AzureBlobStorage:ConnectionString"]);

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
            builder.Services.AddSingleton<ITokenRevocationService, InMemoryTokenRevocationService>();
            builder.Services.AddSingleton<ILoginAttemptService, InMemoryLoginAttemptService>();

            // A1: rate limiting por IP para el endpoint de login (mitiga fuerza bruta/diccionario).
            // Se complementa con el bloqueo temporal por usuario de ILoginAttemptService.
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy("login", httpContext =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueLimit = 0
                        }));

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter =
                            ((int)retryAfter.TotalSeconds).ToString();
                    }

                    await context.HttpContext.Response.WriteAsync(
                        "Demasiados intentos de acceso. Intenta de nuevo en unos minutos.", token);
                };
            });

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
                    // A3: exigir HTTPS siempre (no solo fuera de Development). Esta app no usa
                    // Authority/MetadataAddress, así que no afecta el arranque local; deja
                    // explícito que no se acepta una postura insegura "solo en producción".
                    options.RequireHttpsMetadata = true;
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
                        },

                        // C3: rechaza tokens revocados (logout) y usuarios desactivados desde que
                        // se les revisa el siguiente request, en vez de esperar a que expire el JWT.
                        OnTokenValidated = async ctx =>
                        {
                            var jti = ctx.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                            var revocationService = ctx.HttpContext.RequestServices
                                .GetRequiredService<ITokenRevocationService>();

                            if (!string.IsNullOrEmpty(jti) && revocationService.IsRevoked(jti))
                            {
                                ctx.Fail("Token revocado.");
                                return;
                            }

                            var userIdClaim = ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                            if (!int.TryParse(userIdClaim, out var userId))
                            {
                                ctx.Fail("Token inválido.");
                                return;
                            }

                            var usuarioService = ctx.HttpContext.RequestServices
                                .GetRequiredService<IUsuarioService>();
                            var usuario = await usuarioService.GetByIdAsync(userId);

                            if (usuario is null || !usuario.Activo)
                            {
                                ctx.Fail("Usuario inactivo o inexistente.");
                            }
                        }
                    };
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            // A5: TrustServerCertificate=True deshabilita la validación del certificado TLS
            // del SQL Server (habilita MITM en la red). Es aceptable en Development contra un
            // SQL Server local/Docker con certificado autofirmado, pero fuera de Development
            // hace falta un certificado válido y Encrypt=True;TrustServerCertificate=False.
            // No es un fail-fast (a diferencia de C1/C2): a diferencia de un placeholder, este
            // valor puede ser una elección legítima según el SQL Server real, así que solo se
            // advierte en logs en vez de impedir el arranque.
            if (!app.Environment.IsDevelopment() &&
                connectionString.Contains("TrustServerCertificate=true", StringComparison.OrdinalIgnoreCase))
            {
                app.Logger.LogWarning(
                    "ConnectionStrings:DefaultConnection tiene TrustServerCertificate=True fuera de " +
                    "Development. Esto deshabilita la validación del certificado TLS del SQL Server y " +
                    "permite un MITM en la red. Usa un certificado válido para el SQL Server y configura " +
                    "Encrypt=True;TrustServerCertificate=False.");
            }

            // Manejo de errores en producci�n
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseRateLimiter();

            // Redirigir a login si intenta entrar al sitio sin autenticación (o si la sesión
            // venció/fue revocada/el usuario quedó inactivo — todo eso también resulta en 401
            // vía el middleware de autenticación/autorización). Tiene que ir ANTES de
            // UseAuthentication/UseAuthorization: cada "Use..." envuelve al resto del pipeline,
            // y cuando la autorización falla corta la ejecución ahí mismo (Challenge/Forbid) sin
            // llegar a lo que esté registrado después — si este middleware quedara después,
            // nunca se ejecutaría para un 401/403 y la página quedaba en blanco en vez de
            // redirigir.
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

            app.UseAuthentication();
            app.UseAuthorization();

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

        // M6: a diferencia de C1/C2, acá no hay un valor "placeholder" reconocible que detectar
        // — "UseDevelopmentStorage=true" (el valor por defecto committeado, para Azurite/el
        // emulador local) es una cadena de conexión real y funcional, no un placeholder a
        // reemplazar. Lo único que se puede validar en general es que no esté vacía; que sea la
        // cadena correcta para el entorno real (Azurite en dev/Docker, una cuenta real de Azure
        // Storage en producción) queda fuera del alcance de una validación estática.
        private static void ValidateAzureBlobStorageConnectionString(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "AzureBlobStorage:ConnectionString no está configurada. Defínela mediante variable de " +
                    "entorno (AzureBlobStorage__ConnectionString), User Secrets o Azure Key Vault antes de " +
                    "iniciar la aplicación.");
            }
        }
    }
}