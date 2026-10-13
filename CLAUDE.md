# CLAUDE.md

Este archivo orienta a Claude Code (claude.ai/code) cuando trabaja con el código de este repositorio.

Lee `docs/constitution.md` y la spec activa (`specs/NNN-*/`) antes de tocar código.

## Qué es

SistemaTickets es una aplicación help-desk de un solo proyecto en ASP.NET Core 8 MVC (lenguaje de dominio en español): ciclo de vida de tickets, inventario de herramientas/repuestos, visitas de campo (visitas técnicas), reportes y gestión de usuarios/roles. No hay suite de tests ni CI configurado todavía (existe un borrador de workflow de CI en `planes/ci.yml`, pero **no** está instalado en `.github/workflows/`).

## Comandos

```bash
# Restaurar + ejecutar (desde la raíz del repo)
dotnet restore SistemaTickets.sln
dotnet run --project SistemaTickets/SistemaTickets.csproj
# por defecto https://localhost:7046 (o similar; ver SistemaTickets/Properties/launchSettings.json)

# Solo compilar
dotnet build SistemaTickets.sln

# Publicar (lo que hace el Dockerfile)
dotnet publish SistemaTickets/SistemaTickets.csproj -c Release -o /app/publish
```

No hay proyectos de test ni configuración de lint: `dotnet build` y la verificación manual con la app en ejecución son las únicas señales de corrección disponibles.

### Docker (app + SQL Server + Azurite)

```bash
chmod +x docker/sql/entrypoint.sh      # la primera vez, Linux/Mac
docker-compose up -d --build           # la primera ejecución crea la BD con docker/sql/init.sql
docker-compose logs -f sqlserver       # esperar ~30-60 s al healthcheck
docker-compose logs -f app
docker-compose down                    # detener
docker-compose down -v                 # detener y borrar volúmenes/BD
```
El servicio `app` solo arranca cuando el healthcheck de `sqlserver` pasa. `azurite` emula Azure Blob Storage en el puerto 10000.

## Arquitectura

Estructura en capas con inversión de dependencias dentro del único proyecto web (`Controllers` → `Domain` → `Infrastructure`, conectadas por completo en `Program.cs`):

- `Controllers/` — solo controladores MVC (no hay controladores de API separados, pero la autenticación JWT también funciona con `Authorization: Bearer` para futuros clientes API/móvil)
- `Domain/Entities` — 19 entidades POCO (Ticket, Usuario, Herramienta, Repuesto, VisitaTecnica, etc.), sin responsabilidades de persistencia
- `Domain/Enums` — p. ej. `RolUsuario`
- `Domain/Repositories` — interfaces de repositorio (`ITicketRepository`, `IUsuarioRepository`, …)
- `Domain/Services` — interfaces de servicio; la lógica de negocio vive en las implementaciones de `Infrastructure/Services`
- `Infrastructure/Persistence` — `NHibernateBootstrap.cs` (configuración de la session factory), `Repositories/` (implementaciones NHibernate), `Maps/` (clases de mapeo FluentNHibernate, una por entidad)
- `Infrastructure/Security` — `JwtHelper`, `EncryptionHelper`
- `Infrastructure/Services` — servicios concretos: `TicketService`, `UsuarioService`, `InventarioService`, `CampoService`, `DashboardService`, `ReporteService`, `EmailService` (SMTP), `AzureBlobStorageService`
- `Models/` — ViewModels agrupados por carpeta de funcionalidad (`Models/Tickets`, `Models/Inventario`, …); las vistas nunca se enlazan directamente a `Domain/Entities` (evita mass-assignment / over-posting)
- `Views/` — vistas Razor, una carpeta por controlador
- `ScriptsDB/` — DDL/DML SQL canónico escrito a mano, que se ejecuta en orden para una BD manual nueva: `users.sql` → `tickets.sql` → `inventario.sql` → `campo_repuestos.sql` → `alter_tickets_prioridad_nullable.sql` → `auditoria_usuarios.sql`

Todo el cableado de DI (repositorios, servicios, JWT, autenticación) ocurre en `Program.cs`: ese archivo es el mapa de lo que está registrado y de cómo se ordena el pipeline de autenticación/middleware (redirección HTTPS → archivos estáticos → routing → Authentication → Authorization → redirección por código de estado → rutas MVC).

**Ojo**: las clases de mapeo FluentNHibernate de `Infrastructure/Persistence/Maps/` están bajo el namespace `MiNuevoProyecto.Infrastructure.Persistence.Maps` (herencia de un nombre de proyecto anterior), no bajo `SistemaTickets.*` como el resto del código. `NHibernateBootstrap` las carga con `AddFromAssembly`, así que funcionalmente no rompe nada, pero no "corrijas" el namespace para igualarlo al resto del proyecto sin comprobarlo, y no te sorprendas por la diferencia al buscar clases de mapeo.

### Base de datos (SQL Server + NHibernate)

- ORM: NHibernate 5.7.0 con mapeos FluentNHibernate 3.5.0 (`MsSql2012Dialect`, `MicrosoftDataSqlClientDriver`)
- Ciclo de vida de la sesión: `ISessionFactory` es singleton; `NHibernate.ISession` se abre por petición con `AddScoped(factory => sessionFactory.OpenSession())`: los repositorios comparten una sesión por petición HTTP, sin unidad de trabajo/transacción explícita por llamada a servicio
- **Configuración crítica**: `appsettings.json` → `NHibernate:UpdateSchema` controla si `SchemaUpdate` se ejecuta al arrancar.
  - `true` = crea/actualiza tablas automáticamente; pensado **solo para el primer despliegue**.
  - Ponlo en `false` tras el primer arranque correcto para evitar deriva de esquema no deseada en producción.
- Cadena de conexión: `ConnectionStrings:DefaultConnection` se valida al arrancar (`ValidateConnectionString` en `Program.cs`); la app se niega a iniciar si falta o sigue siendo un placeholder. `appsettings.json` solo contiene un placeholder (ver `appsettings.json.example`); el valor real se define con `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."` en desarrollo, o con la variable de entorno `ConnectionStrings__DefaultConnection` en Docker/producción.
- Validación del certificado TLS: las plantillas usan `Encrypt=True;TrustServerCertificate=False` (requiere un SQL Server con certificado de confianza). `Program.cs` registra (no lanza) una advertencia si `TrustServerCertificate=true` aparece fuera de `Development`; es una comprobación blanda porque, a diferencia de un placeholder, esta opción puede ser legítima según la configuración de certificados del SQL Server real. El `sqlserver` de `docker-compose.yml` usa su propio certificado autofirmado (sin PKI propia), por eso su cadena de conexión mantiene `TrustServerCertificate=True` a propósito; solo es seguro porque ese servicio también corre con `ASPNETCORE_ENVIRONMENT=Development` (misma excepción que el flag `Secure` de la cookie en A3).
- Al consultar con LINQ de NHibernate (`_session.Query<T>()`), carga las relaciones many-to-one con `.Fetch(...)` en pasos separados para evitar joins de producto cartesiano (ver `TicketRepository.GetActivosParaSeguimientoAsync`), y usa `NHibernateUtil.InitializeAsync(...)` para cargar de forma diferida una asociación opcional en lugar de cargarla con eager-fetch.

### Autenticación

- `Jwt:Key` se valida al arrancar (`ValidateJwtKey` en `Program.cs`): la app lanza una excepción y se niega a iniciar si la clave falta, tiene menos de 32 caracteres o coincide con un placeholder conocido. Define una clave real con `dotnet user-secrets set "Jwt:Key" "..."` en desarrollo o con la variable de entorno `Jwt__Key` en producción/Docker (docker-compose la lee de un `.env` local ignorado por git; ver `.env.example`). Nunca subas un valor literal en `appsettings.json`.
- JWT de modo dual: la cabecera `Authorization: Bearer <token>` tiene prioridad; si no está, se usa la cookie `jwt` (lo gestiona el evento `OnMessageReceived` del JWT bearer en `Program.cs`). Así el mismo esquema de autenticación sirve a futuros clientes API y al flujo de navegador con Razor MVC.
- La vigencia del token es `Jwt:ExpireMinutes` (por defecto 30), conectada de verdad a `JwtHelper.CreateToken` y a la expiración de la cookie `jwt` (`UsersController.Login`). Cada token lleva un claim `jti`.
- Revocación en servidor (`OnTokenValidated` en `Program.cs`): rechaza la petición si el `jti` del token está en `ITokenRevocationService` (en memoria, `Infrastructure/Services/InMemoryTokenRevocationService`, singleton) o si el usuario (buscado por el claim `ClaimTypes.NameIdentifier` con `IUsuarioService.GetByIdAsync`) no existe o tiene `Activo == false`. Así la desactivación y el logout surten efecto en la **siguiente petición** del usuario, sin esperar a que expire el JWT. `UsersController.Logout` llama a `ITokenRevocationService.Revoke` con el `jti` actual antes de borrar la cookie. La lista de revocación es solo en memoria (se reinicia con la app y no se comparte entre instancias); pásala a una tabla persistente si eso se vuelve un problema.
- Sesiones solo HTTPS: el flag `Secure` de la cookie `jwt` es `!_environment.IsDevelopment()`: siempre `true` fuera de Development (nunca depende del esquema de la petición actual, para que un proxy mal configurado no pueda degradarlo) y deliberadamente `false` en Development para que `dotnet run --launch-profile http` siga funcionando en local sin certificado de confianza. `RequireHttpsMetadata = true` en el handler JWT bearer (inerte hoy porque la app no tiene `Authority`/dirección de metadatos, pero no hay motivo para dejarlo permisivo). `AddHsts` configura `Preload`/`IncludeSubDomains`/`MaxAge` de 365 días, aplicado con el `UseHsts()` existente (que sigue limitado a no-Development). `Properties/launchSettings.json` lista primero el perfil `https` (el predeterminado de `dotnet run`); HTTPS sigue siendo la configuración local recomendada.
  - En `docker-compose.yml`, `app` corre ahora con `ASPNETCORE_ENVIRONMENT=Development` y se publica directo al host en `${APP_HOST:-127.0.0.1}:${APP_PORT:-8080}:8080` (ambos sobrescribibles en `.env`; por defecto solo enlaza a loopback): eso permite que `http://localhost:8080` mantenga el login. `caddy` sigue terminando TLS en local (autofirmado, `https://localhost:8443`, sin parametrizar) como vía HTTPS. Volver a poner el entorno de `app` en `Production` hace que el puerto HTTP directo deje de mantener el login (igual que un despliegue real); entonces Caddy es la única entrada.
- Protección contra fuerza bruta en el login: `UsersController.Login` (POST) tiene `[EnableRateLimiting("login")]`, asociado a una política de ventana deslizante por IP (10 peticiones/min, 429 al exceder) registrada con `AddRateLimiter` en `Program.cs`. Aparte, `ILoginAttemptService` (en memoria, `Infrastructure/Services/InMemoryLoginAttemptService`) bloquea un identificador de login durante 15 minutos tras 5 intentos fallidos en una ventana de 15 minutos, comprobado antes de validar credenciales; los fallos y bloqueos se registran con `ILogger<UsersController>`. Ambos almacenes son en memoria y por instancia, con la misma salvedad que la lista de revocación de tokens.
- Roles: `Administrador`, `Supervisor`, `Soporte`, `Usuario`, aplicados con `[Authorize(Roles = "...")]` por controlador/acción.
- Contraseñas con hash BCrypt (`UsuarioService`), nunca en texto plano.
- Sin enumeración de usuarios en el login (M1): `UsersController.Login` muestra un único mensaje genérico (`LoginGenericoError`, "Credenciales inválidas o usuario inactivo.") para cualquier causa de rechazo: login inexistente, contraseña incorrecta o usuario inactivo. `UsuarioService.ValidateCredentialsAsync` también iguala el *tiempo* de respuesta: cuando el login no corresponde a un usuario activo, ejecuta igualmente `BCrypt.Verify` contra un hash ficticio (`DummyPasswordHash`, generado una vez al arrancar a partir de un GUID aleatorio) en lugar de cortocircuitar, de modo que un usuario inexistente no se distinga de una contraseña incorrecta por la rapidez de la respuesta.
- Política de contraseñas (M2): `Infrastructure/Security/PasswordPolicy.MinLength` es 12. `PasswordPolicy.EsContraseñaComun` compara la contraseña con `10k-most-common.txt` de SecLists (MIT), incrustado en el ensamblado (`SistemaTickets.csproj` → `EmbeddedResource`/`LogicalName = SistemaTickets.CommonPasswords.txt`) en lugar de distribuirse como archivo suelto; si alguna vez hay que actualizar o reemplazar esa lista, actualiza también el mapeo `LogicalName`, no basta con cambiar el `.txt` en disco. `PasswordPolicy.TieneComplejidadSuficiente` exige minúscula + mayúscula + dígito + un carácter no alfanumérico. Dos atributos en `Models/Validation/`, `[NotCommonPassword]` y `[PasswordComplexity]`, envuelven esas comprobaciones y se aplican ambos a `UsuarioFormViewModel.Password` / `CambiarPasswordViewModel.NuevoPassword`, junto con `MinimumLength`/`MinLength = 12`. `UsersController.CambiarPassword` devuelve el mensaje de validación real (no un genérico "Datos inválidos.") para que la política sea visible al rechazar; los tres campos de contraseña (`Create.cshtml`, modal de edición en `Index.cshtml`, modal de cambio de contraseña en `_Layout.cshtml`) muestran el requisito de antemano con un texto `form-text`, y el modal de cambio de contraseña ejecuta además las mismas cuatro comprobaciones de complejidad en el cliente (regex) antes del viaje al servidor; el atributo del lado servidor sigue siendo la autoridad.
- Logout por POST (M5): `UsersController.Logout` pasó de `[HttpGet]` a `[HttpPost] [ValidateAntiForgeryToken]`; un GET simple (p. ej. un `<img src="/Users/Logout">` en una página de terceros) podía antes terminar la sesión de un usuario sin su consentimiento. El enlace de logout de la barra lateral en `Views/Shared/_Layout.cshtml` es ahora un `<form method="post">` con `@Html.AntiForgeryToken()` y un `<button type="submit">` (misma clase visual, `sidebar-logout`, ahora dirigida como `button.sidebar-logout` en `site.css` para el reseteo de estilos del navegador).
- Guardia de identidad válida (M4): `Infrastructure/Security/RequireValidUserIdFilter` (registrado globalmente con `AddControllersWithViews(options => options.Filters.Add<RequireValidUserIdFilter>())` en `Program.cs`) se ejecuta antes de cada acción, omite las que llevan `[AllowAnonymous]` (p. ej. `Login`) y devuelve 401 si `ClaimTypes.NameIdentifier` falta o no es un entero válido. Reemplazó el patrón `int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0")` disperso por `UsersController`, `TicketsController`, `InventarioController`, `CampoController` y `DashboardController`; esos puntos ahora hacen un simple `int.Parse(...!.Value)`, seguro porque el filtro ya garantiza un claim válido cuando se ejecuta el cuerpo de la acción.
- Auditoría de cambios de rol/desactivación (M3): `UsersController.Edit`/`Deactivate` pasan el id del administrador que actúa (`ClaimTypes.NameIdentifier`) a `UsuarioService.UpdateAsync`/`DeactivateAsync`, que escriben una fila en `AuditoriaUsuarios` (con `IUsuarioRepository.RegistrarAuditoriaAsync`) cuando `Rol` o `Activo` cambian de verdad: `UsuarioId`/`ModificadoPorId`/`Campo`/`ValorAnterior`/`ValorNuevo`/`FechaCambio`. **Ojo**: captura el valor "anterior" en una variable local *antes* de llamar a `_usuarioRepository.UpdateAsync(...)`; `GetByIdAsync` y el `MergeAsync` posterior resuelven a la *misma* instancia rastreada por la sesión (el identity map de NHibernate), así que leer `existing.Rol` *después* de la actualización ya devuelve el valor nuevo y la comparación siempre parece "sin cambios". Ver `Infrastructure/Services/UsuarioService.cs` para el patrón `rolAnterior`/`estabaActivo`.
- `[ValidateAntiForgeryToken]` en las acciones POST (protección CSRF).
- Las peticiones HTML no autenticadas/prohibidas (401/403) se redirigen a `/Users/Login` con `UseStatusCodePages` en `Program.cs`, en lugar de devolver un código de estado pelado; solo aplica cuando la cabecera `Accept` de la petición pide `text/html`. `UseStatusCodePages` **debe registrarse antes de `UseAuthentication`/`UseAuthorization`**: un middleware envuelve todo lo registrado después, y `UseAuthorization` cortocircuita (Challenge/Forbid, sin llamar a `next()`) cuando rechaza una petición, así que lo registrado después nunca se ejecuta para esa respuesta. Originalmente estaba después de ellos, lo que hacía que un 401/403 (JWT expirado/inválido/revocado, usuario inactivo, rol faltante) produjera una página en blanco en lugar de una redirección; moverlo antes en el pipeline es la corrección real, no la lógica del handler.
- La ruta por defecto es `{controller=Users}/{action=Login}/{id?}`.
- **Adjuntos de tickets (A2)**: `TicketsController.Create` valida cada subida con `Infrastructure/Security/AttachmentValidator`: lista de extensiones permitidas (pdf, jpg/jpeg, png, gif, webp, doc, docx, xls, xlsx, txt, csv) más una comprobación de magic bytes de que el contenido real del archivo coincide con la extensión declarada. El `IFormFile.ContentType` que envía el navegador se descarta; el content-type almacenado/servido es siempre el MIME canónico del validador para esa extensión. `TicketsController.DescargarAdjunto` sirve los adjuntos con `Content-Disposition: attachment` (con `File(..., fileDownloadName)`) y una cabecera explícita `X-Content-Type-Options: nosniff`, de modo que nada se renderiza en línea en el navegador.
- **Sin filtrar detalles de excepciones (A4)**: `UsersController`, `TicketsController`, `InventarioController` y `CampoController` tienen cada uno un helper privado `JsonError(Exception ex, string accion)` respaldado por `ILogger<TController>`, usado en todos los bloques `catch (Exception ex)` de acciones AJAX. Solo `InvalidOperationException` (el tipo que la capa de servicios lanza a propósito, siempre con un mensaje seguro como "Ticket no encontrado") pasa su `.Message` al cliente; cualquier otra cosa se registra completa y el cliente recibe un mensaje genérico. Sigue el mismo patrón en el `catch` de cualquier acción AJAX nueva; no devuelvas `ex.Message` directamente.

### Dependencias externas

- **Azure Blob Storage** (`AzureBlobStorageService`, registrado como `IStorageService`) para los adjuntos de tickets (máx. 10 MB), con configuración en `AzureBlobStorage:*`. Azurite lo emula en local mediante Docker. **M6**: `AzureBlobStorage:ConnectionString` se valida al arrancar (`ValidateAzureBlobStorageConnectionString` en `Program.cs`); la app se niega a iniciar si está vacío. A diferencia de `Jwt:Key`/la cadena de conexión de la BD, aquí no hay comprobación de texto placeholder: el valor por defecto versionado (`UseDevelopmentStorage=true`) es un valor real y funcional para el emulador local, no un placeholder que reemplazar; solo los despliegues reales de producción deben sobrescribirlo (con la variable de entorno `AzureBlobStorage__ConnectionString` o un gestor de secretos, nunca fijo en `appsettings.json`).
- **Email** por SMTP (`EmailService`), con configuración en `Email:*` (valores de Gmail en la configuración de ejemplo). Un fallo aquí no bloquea la creación de tickets: los servicios están aislados tras interfaces. **M6**: a diferencia de la cadena de conexión de blob, esto es realmente opcional: `EmailService` omite el envío (registra en `Debug`) si `Email:SmtpHost` está vacío, así que no hay validación al arrancar. Si configuras credenciales SMTP reales, van por `dotnet user-secrets` (desarrollo) o las variables de entorno `Email__Username`/`Email__Password`/etc. (`docker-compose.yml` las toma del `.env` ignorado por git; ver `.env.example`); nunca fijas en `appsettings.json`.
- **Reportes**: QuestPDF (PDF) y ClosedXML (Excel), manejados desde `ReporteService` / `ReportesController`.

## Notas de configuración

- `SistemaTickets/appsettings.json` contiene la cadena de conexión, `Jwt:Key` / `Jwt:Issuer` / `Jwt:Audience` / `Jwt:ExpireMinutes`, `Encryption:Key`, `Email:*` y `AzureBlobStorage:*`. `Jwt:Key` y `Encryption:Key` deben tener al menos 32 caracteres. El archivo versionado solo contiene placeholders; `SistemaTickets/appsettings.json.example` documenta la forma esperada.
- Nunca subas secretos reales en `appsettings.json`; `Jwt:Key` y `ConnectionStrings:DefaultConnection` se exigen al arrancar (`Program.cs` lanza una excepción si falta alguno o sigue siendo un placeholder; ver las secciones de Autenticación y Base de datos). En desarrollo, aporta los valores reales con `dotnet user-secrets`; en Docker/producción, con variables de entorno (`Jwt__Key`, `ConnectionStrings__DefaultConnection`, etc.) o un gestor de secretos. `docker-compose.yml` lee `Jwt__Key` de un archivo `.env` local ignorado por git (`.env.example` lo documenta).
- `libman.json` existe pero está vacío: las librerías cliente se incluyen directamente en `wwwroot/lib`, no se restauran con libman.
- **Saneamiento de logs (B4)**: dos correcciones independientes.
  - **Eco de SQL de NHibernate (el hallazgo real)**: `NHibernateBootstrap.BuildSessionFactory` llamaba a `.ShowSql()` incondicionalmente en todos los entornos. NHibernate escribe cada sentencia SQL **con los valores de los parámetros** directamente en `Console` (sin pasar por `Microsoft.Extensions.Logging`, así que ningún `LogLevel` de `appsettings.json`, ni siquiera el override de `Microsoft: Warning` de abajo, lo filtraba nunca). Es decir, títulos/descripciones de tickets, nombres de usuario y cualquier parámetro de consulta acababan en la salida estándar del contenedor en cada petición y en cualquier entorno, incluido uno de producción. Corregido: `BuildSessionFactory` recibe ahora un parámetro `showSql` y solo llama a `.ShowSql()` cuando es `true`; `Program.cs` pasa `builder.Environment.IsDevelopment()`. Verificado empíricamente en Docker: el mismo intento de login (`POST /Users/Login` con credenciales falsas; basta con disparar la consulta a `Usuarios`) produjo 1 línea `NHibernate: ...` con `ASPNETCORE_ENVIRONMENT=Development` y 0 con `=Production`.
  - **Namespace `Microsoft` en `Warning` en producción**: `appsettings.Production.json` (nuevo) fija `Logging:LogLevel:Microsoft` en `Warning`, más amplio que el `Microsoft.AspNetCore: Warning` del `appsettings.json` base (que solo cubre ese subnamespace y sigue aplicando en Development, donde ya era el valor antes de B4 y no cambia). Verificado igual: con `Production` desaparecen las líneas de arranque `info: Microsoft.Hosting.Lifetime[...]` (y cualquier otro log de nivel Information de categoría `Microsoft.*`); solo quedan `warn:` o superior.
  - Ningún código registra el JWT en bruto ni un volcado de claims: se revisó cada llamada `_logger.Log*` del código; el único hallazgo de datos personales fue `EmailService.SendTicketCopyAsync` registrando en `Information` el email descifrado del destinatario en texto plano, corregido para registrar `solicitante.Id` (ver `cambios/B4-saneamiento-logs.md`).
- **`AllowedHosts` (B3)**: `"localhost;127.0.0.1"`, no `"*"`. El middleware de filtrado de hosts de ASP.NET Core rechaza (400) cualquier petición cuya cabecera `Host` no coincida con uno de estos, lo que frena la inyección de la cabecera Host en un despliegue mal configurado o expuesto. Estos son los únicos hosts que hoy sirven realmente la app en todos los entornos documentados (`dotnet run`, Docker directo en `localhost:8080`, el proxy inverso de Caddy en `https://localhost:8443`, ya que Caddy reenvía la cabecera `Host` original, que sigue siendo `localhost`). No hay un dominio de producción real documentado en el repo; **si despliegas detrás de un dominio real, `AllowedHosts` debe actualizarse para incluirlo** (separado por punto y coma), o todas las peticiones recibirán 400. Si `APP_HOST` en `.env` se pone en `0.0.0.0` para exponer la app en la LAN (ver `.env.example`), acceder por la IP/nombre de la máquina en la LAN también dará 400 hasta añadir ese host aquí.

## Carencias conocidas (ver `planes/` para los análisis completos)

- Sin tests automatizados, sin pipeline de CI conectado (`planes/ci.yml` es un borrador, no instalado), sin 2FA. `planes/01-problemas-de-seguridad.md`, `planes/02-ci-cd-plan.md` y `planes/03-plan-arquitectura.md` catalogan los problemas de seguridad conocidos y las mejoras propuestas de arquitectura/CI: consúltalos antes de proponer cambios de seguridad o CI/CD para no duplicar ni contradecir el análisis existente.
- **B2 (CVE de dependencias)**: resuelto parcialmente. Las versiones de los paquetes directos se subieron a su último minor/patch (ver `cambios/B2-dependencias-vulnerables.md`) y `dotnet list package --vulnerable --include-transitive` reporta cero vulnerabilidades conocidas a la fecha de escribir esto. La mitad de CI de la remediación (instalar `planes/ci.yml` → `.github/workflows/ci.yml`, o GitHub Dependabot) sigue **sin** instalarse a propósito: el repo tiene un remoto real en GitHub, así que activarla empezaría a ejecutar Actions en cada push/PR; es una decisión que corresponde al usuario, no algo que hacer sin que se pida.
- Specs en `/specs`: solo existe `001-deudaTecnica` (aprobada, pendiente de implementar); el resto del comportamiento aún no está especificado (principio 2 de `docs/constitution.md`).

## Verificación

- Después de cada cambio, verifica con el MCP de Chrome DevTools: levanta la app (`dotnet run --project SistemaTickets/SistemaTickets.csproj`, o Docker en `http://localhost:8080`), abre la vista afectada (la ruta por defecto es `/Users/Login`), prueba la funcionalidad, revisa la consola y comprueba la vista móvil.
- Si el cambio es solo de backend o de documentación y no tiene efecto visible, indícalo en lugar de omitir el paso en silencio.

## Memoria

- Al empezar, lee `MEMORY.md` para conocer el estado del proyecto y las decisiones tomadas.
- Al terminar una tarea, actualízalo: estado actual, decisiones importantes (con su porqué) y errores a evitar.
- Mantenlo breve (máximo ~50 líneas): resume o elimina lo que ya no aporte.
- Si algo se convierte en una regla permanente, propón moverlo a `AGENTS.md` en lugar de dejarlo en la memoria.
- No guardes nunca datos sensibles (claves, tokens, datos personales).

## Límites

- ✅ Siempre: actualizar `MEMORY.md` al terminar cada tarea.
