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
- `ScriptsDB/` contains canonical DDL scripts (users, tickets, inventario, campo repuestos). Run these in order if bootstrapping a fresh database manually.

## Auth

- `Jwt:Key` is validated at startup in `Program.cs` (`ValidateJwtKey`): the app refuses to start if the key is missing, shorter than 32 characters, or matches a known placeholder value. Set a real key via `dotnet user-secrets set "Jwt:Key" "..."` (dev) or the `Jwt__Key` env var (prod/Docker) — never a literal value in `appsettings.json`.
- JWT Bearer + cookie dual mode:
  1. Respects `Authorization: Bearer <token>` header if present.
  2. Falls back to `jwt` cookie for browser MVC flows.
- Role-based: `Administrador`, `Supervisor`, `Soporte`, `Usuario`.
- Unauthenticated HTML requests to protected pages get **redirected to `/Users/Login`** (not 401 JSON).
- `[ValidateAntiForgeryToken]` is used on POSTs.
- **Token lifetime & revocation (C3)**: `Jwt:ExpireMinutes` (default 30) is actually wired into `JwtHelper.CreateToken` and into the `jwt` cookie's `Expires`. Every token carries a `jti` claim. `Program.cs`'s `OnTokenValidated` event rejects a request if the `jti` is in `ITokenRevocationService` (in-memory, `Infrastructure/Services/InMemoryTokenRevocationService`) or if the user looked up by `ClaimTypes.NameIdentifier` is missing/`Activo == false` — so a deactivated user or a stolen-but-logged-out token is rejected on the **next request**, not just at token expiry. `UsersController.Logout` revokes the current token's `jti` before deleting the cookie. Known limitation: the revocation list is in-memory only — it resets on app restart and isn't shared across instances; migrate to a persisted `RevokedTokens` table if deploying with multiple instances or frequent restarts.
- **Brute-force protection on login (A1)**: `UsersController.Login` (POST) carries `[EnableRateLimiting("login")]`, backed by a sliding-window policy in `Program.cs` (`AddRateLimiter`) that caps requests per client IP (10/min) and returns 429 when exceeded. Independently, `ILoginAttemptService` (in-memory, `Infrastructure/Services/InMemoryLoginAttemptService`) locks a given login identifier (username/email/phone, case-insensitive) for 15 minutes after 5 failed attempts within a 15-minute window; checked before `ValidateCredentialsAsync` runs. Every failed attempt and every lockout is logged via `ILogger<UsersController>` (IP + login identifier, never the password). Known limitations: both mechanisms are in-memory/per-instance (same caveat as the token revocation list above), and per-username lockout is itself an availability tradeoff — an attacker who knows a username can force repeated lockouts against it from rotating IPs; that's accepted here since the alternative (no lockout) is worse, but worth revisiting (e.g. CAPTCHA) if it becomes a real nuisance.

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
