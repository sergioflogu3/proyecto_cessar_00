# AGENTS.md — SistemaTickets

Single-project ASP.NET Core 8 MVC app for ticket management. No test suite, no CI, no Docker.

## Quick start

```bash
dotnet restore SistemaTickets.sln
dotnet run --project SistemaTickets/SistemaTickets.csproj
# defaults to https://localhost:7046 (or similar)
```

## Architecture

- **Entry point**: `SistemaTickets/Program.cs`
- **Pattern**: MVC + layered domain/infrastructure folders inside the web project
  - `Controllers/` — MVC controllers (no API controllers)
  - `Domain/Entities`, `Domain/Enums`, `Domain/Repositories`, `Domain/Services`
  - `Infrastructure/Persistence` — NHibernate bootstrap, Fluent mappings, repository implementations
  - `Infrastructure/Security` — JWT helper
  - `Infrastructure/Services` — Azure Blob, email, reports
  - `Models/` — ViewModels organized by feature folder
  - `Views/` — Razor views matching controller names
  - `ScriptsDB/` — Manual SQL Server DDL/DML scripts
- **Default route**: `{controller=Users}/{action=Login}/{id?}`

## Database (SQL Server + NHibernate)

- ORM: NHibernate 5.x with FluentNHibernate mappings in `Infrastructure/Persistence/Maps/`
- Dialect: `MsSql2012Dialect`, driver: `MicrosoftDataSqlClientDriver`
- **Critical**: `appsettings.json` → `NHibernate:UpdateSchema` controls `SchemaUpdate` on startup.
  - `true` = auto-creates/updates tables. Intended **only for first deploy**.
  - Set to `false` after initial schema is created to avoid accidental migrations in production.
- `ConnectionStrings:DefaultConnection` is validated at startup (`ValidateConnectionString` in `Program.cs`): the app refuses to start if it's missing or still a placeholder value. `appsettings.json` only holds placeholders (see `appsettings.json.example`) — set the real value via `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."` (dev) or the `ConnectionStrings__DefaultConnection` env var (prod/Docker).
- **TLS certificate validation on the SQL connection (A5)**: the template connection strings (`appsettings.json[.example]`, README) use `Encrypt=True;TrustServerCertificate=False`, which requires the SQL Server to present a certificate the client actually trusts. `Program.cs` logs a `Warning` (not a hard fail — this flag can be a legitimate choice, unlike a placeholder) if `TrustServerCertificate=true` is present in the connection string outside `Development`. `docker-compose.yml`'s `sqlserver` service uses its own self-signed cert with no custom PKI, so its `app` connection string keeps `TrustServerCertificate=True` deliberately — safe today only because that service also runs with `ASPNETCORE_ENVIRONMENT=Development` (same exception as A3), which suppresses the warning.
- `ScriptsDB/` contains canonical DDL scripts (users, tickets, inventario, campo repuestos). Run these in order if bootstrapping a fresh database manually.

## Auth

- `Jwt:Key` is validated at startup in `Program.cs` (`ValidateJwtKey`): the app refuses to start if the key is missing, shorter than 32 characters, or matches a known placeholder value. Set a real key via `dotnet user-secrets set "Jwt:Key" "..."` (dev) or the `Jwt__Key` env var (prod/Docker) — never a literal value in `appsettings.json`.
- JWT Bearer + cookie dual mode:
  1. Respects `Authorization: Bearer <token>` header if present.
  2. Falls back to `jwt` cookie for browser MVC flows.
- Role-based: `Administrador`, `Supervisor`, `Soporte`, `Usuario`.
- Unauthenticated HTML requests to protected pages get **redirected to `/Users/Login`** (not 401 JSON).
- `[ValidateAntiForgeryToken]` is used on POSTs.
- **No user-enumeration via login (M1)**: `UsersController.Login` shows a single generic message (`LoginGenericoError`) regardless of whether the login doesn't exist, the password is wrong, or the user is inactive. `UsuarioService.ValidateCredentialsAsync` also equalizes *timing* — when there's no matching active user, it still runs `EncryptionHelper.VerifyPassword` against a dummy BCrypt hash (`DummyPasswordHash`, generated once at class load from a random GUID) instead of returning early, so response latency can't be used to fingerprint valid usernames either.
- **Token lifetime & revocation (C3)**: `Jwt:ExpireMinutes` (default 30) is actually wired into `JwtHelper.CreateToken` and into the `jwt` cookie's `Expires`. Every token carries a `jti` claim. `Program.cs`'s `OnTokenValidated` event rejects a request if the `jti` is in `ITokenRevocationService` (in-memory, `Infrastructure/Services/InMemoryTokenRevocationService`) or if the user looked up by `ClaimTypes.NameIdentifier` is missing/`Activo == false` — so a deactivated user or a stolen-but-logged-out token is rejected on the **next request**, not just at token expiry. `UsersController.Logout` revokes the current token's `jti` before deleting the cookie. Known limitation: the revocation list is in-memory only — it resets on app restart and isn't shared across instances; migrate to a persisted `RevokedTokens` table if deploying with multiple instances or frequent restarts.
- **HTTPS-only sessions (A3)**: the `jwt` cookie (`UsersController.Login`) is set with `Secure = !_environment.IsDevelopment()` — always `true` outside `Development`, never conditional on the current request's scheme, so a misconfigured proxy can't silently downgrade it. In `Development` the flag is `false` on purpose, so `dotnet run --launch-profile http` still works locally without a trusted cert. `AddJwtBearer`'s `RequireHttpsMetadata` is `true` (this app doesn't use `Authority`/metadata discovery, so it's a no-op today, but it removes a permissive flag that had no reason to be `false`). `AddHsts` is configured with `Preload = true`, `IncludeSubDomains = true`, `MaxAge = 365 days` (still only applied outside `Development`, via the existing `app.UseHsts()` call). `Properties/launchSettings.json` lists the `https` profile first (so it's `dotnet run`'s default) — HTTPS is still the recommended way to test locally, HTTP is a deliberate Development-only convenience.
  - `docker-compose.yml`'s `app` service currently runs with `ASPNETCORE_ENVIRONMENT=Development` (changed from `Production`) and publishes directly to the host at `${APP_HOST:-127.0.0.1}:${APP_PORT:-8080}` (both overridable in `.env`) — that combination is what makes `http://localhost:8080` sustain a login. Flip it back to `Production` to simulate a real deployment; at that point the direct HTTP port stops sustaining login and the `caddy` service (self-signed TLS, `https://localhost:8443`, host/port not parameterized) is the only way in.
- **Brute-force protection on login (A1)**: `UsersController.Login` (POST) carries `[EnableRateLimiting("login")]`, backed by a sliding-window policy in `Program.cs` (`AddRateLimiter`) that caps requests per client IP (10/min) and returns 429 when exceeded. Independently, `ILoginAttemptService` (in-memory, `Infrastructure/Services/InMemoryLoginAttemptService`) locks a given login identifier (username/email/phone, case-insensitive) for 15 minutes after 5 failed attempts within a 15-minute window; checked before `ValidateCredentialsAsync` runs. Every failed attempt and every lockout is logged via `ILogger<UsersController>` (IP + login identifier, never the password). Known limitations: both mechanisms are in-memory/per-instance (same caveat as the token revocation list above), and per-username lockout is itself an availability tradeoff — an attacker who knows a username can force repeated lockouts against it from rotating IPs; that's accepted here since the alternative (no lockout) is worse, but worth revisiting (e.g. CAPTCHA) if it becomes a real nuisance.

- **Ticket attachment validation (A2)**: `TicketsController.Create` checks size, extension allow-list, and file-content ("magic bytes") signature via `Infrastructure/Security/AttachmentValidator` before uploading — see `AttachmentValidator.AllowedTypes` for the current whitelist (pdf, jpg/jpeg, png, gif, webp, doc, docx, xls, xlsx, txt, csv). The client-supplied `IFormFile.ContentType` is never trusted or stored; the canonical MIME type returned by the validator is used instead. `TicketsController.DescargarAdjunto` always serves attachments as a forced download (`Content-Disposition: attachment`, via the `File(..., fileDownloadName)` overload) plus an explicit `X-Content-Type-Options: nosniff` header, so a stored file is never rendered inline by the browser regardless of its content-type.
- **No internal exception details to the client (A4)**: `UsersController`, `TicketsController`, `InventarioController`, and `CampoController` each have a private `JsonError(Exception ex, string accion)` helper (backed by `ILogger<TController>`) used in every AJAX `catch (Exception ex)` block. `InvalidOperationException` is the one type the service layer throws deliberately with a safe, user-facing message (e.g. "Ticket no encontrado") — that message passes through as-is. Anything else (NHibernate/SqlClient errors, etc.) is logged in full server-side and the client only gets a generic "Ocurrió un error inesperado" message. If you add a new AJAX action with its own `catch`, use the controller's `JsonError` (or the same InvalidOperationException-vs-everything-else split) instead of returning `ex.Message` directly.

## External dependencies

- **Azure Blob Storage** (`AzureBlobStorageService`) for ticket file attachments. Config: `AzureBlobStorage:ConnectionString` / `ContainerName`.
- **Email** via SMTP (Gmail defaults in config). Config: `Email:*`.
- **Reports**: QuestPDF + ClosedXML.

## Build / run notes

- No test projects exist. Verification is manual or via running the app.
- `libman.json` is present but empty; client libraries are likely checked into `wwwroot/`.
- Standard `dotnet build` / `dotnet run` workflow.

## Safety

- Do not commit real secrets in `appsettings.json`. Placeholder values are present for `Jwt:Key`, `Encryption:Key`, and `ConnectionStrings:DefaultConnection`; `Jwt:Key` and `ConnectionStrings:DefaultConnection` are enforced at startup (`Program.cs`) and `appsettings.json.example` documents the expected shape.
