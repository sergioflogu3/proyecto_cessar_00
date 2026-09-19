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
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

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
            var jwtKey = jwtSection["Key"]!;
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
    }
}