# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

SistemaTickets is a single-project ASP.NET Core 8 MVC help-desk app (Spanish domain language): ticket lifecycle, inventory of tools/spare parts, field visits (visitas técnicas), reporting, and user/role management. No test suite, no CI wired up yet (a CI workflow exists as a draft in `planes/ci.yml` but is **not** installed under `.github/workflows/`).

## Commands

```bash
# Restore + run (from repo root)
dotnet restore SistemaTickets.sln
dotnet run --project SistemaTickets/SistemaTickets.csproj
# defaults to https://localhost:7046 (or similar; see SistemaTickets/Properties/launchSettings.json)

# Build only
dotnet build SistemaTickets.sln

# Publish (what Dockerfile does)
dotnet publish SistemaTickets/SistemaTickets.csproj -c Release -o /app/publish
```

There are no test projects and no lint config — `dotnet build` and manual verification via the running app are the only correctness signals available.

### Docker (app + SQL Server + Azurite)

```bash
chmod +x docker/sql/entrypoint.sh      # first time, Linux/Mac
docker-compose up -d --build           # first run creates the DB via docker/sql/init.sql
docker-compose logs -f sqlserver       # wait ~30-60s for healthcheck
docker-compose logs -f app
docker-compose down                    # stop
docker-compose down -v                 # stop and wipe volumes/DB
```
The `app` service only starts once the `sqlserver` healthcheck passes. `azurite` emulates Azure Blob Storage on port 10000.

## Architecture

Layered, dependency-inverted structure inside the single web project (`Controllers` → `Domain` → `Infrastructure`, wired up entirely in `Program.cs`):

- `Controllers/` — MVC controllers only (no separate API controllers, but JWT auth also works over `Authorization: Bearer` for future API/mobile clients)
- `Domain/Entities` — 19 POCO entities (Ticket, Usuario, Herramienta, Repuesto, VisitaTecnica, etc.), no persistence concerns
- `Domain/Enums` — e.g. `RolUsuario`
- `Domain/Repositories` — repository interfaces (`ITicketRepository`, `IUsuarioRepository`, …)
- `Domain/Services` — service interfaces; business logic lives in the `Infrastructure/Services` implementations
- `Infrastructure/Persistence` — `NHibernateBootstrap.cs` (session factory setup), `Repositories/` (NHibernate implementations), `Maps/` (FluentNHibernate mapping classes, one per entity)
- `Infrastructure/Security` — `JwtHelper`, `EncryptionHelper`
- `Infrastructure/Services` — concrete services: `TicketService`, `UsuarioService`, `InventarioService`, `CampoService`, `DashboardService`, `ReporteService`, `EmailService` (SMTP), `AzureBlobStorageService`
- `Models/` — ViewModels grouped by feature folder (`Models/Tickets`, `Models/Inventario`, …); views never bind directly to `Domain/Entities` (avoids mass-assignment / over-posting)
- `Views/` — Razor views, one folder per controller
- `ScriptsDB/` — canonical hand-written SQL DDL/DML, run in order for a fresh manual DB: `users.sql` → `tickets.sql` → `inventario.sql` → `campo_repuestos.sql` → `alter_tickets_prioridad_nullable.sql`

All DI wiring (repositories, services, JWT, auth) happens in `Program.cs` — that file is the map of what's registered and how the auth/middleware pipeline is ordered (HTTPS redirect → static files → routing → Authentication → Authorization → status-code redirect → MVC routes).

**Gotcha**: FluentNHibernate mapping classes in `Infrastructure/Persistence/Maps/` live under namespace `MiNuevoProyecto.Infrastructure.Persistence.Maps` (a leftover from an earlier project name), not `SistemaTickets.*` like the rest of the codebase. `NHibernateBootstrap` loads them via `AddFromAssembly`, so this doesn't break anything functionally, but don't "fix" the namespace to match the rest of the project without checking — and don't be surprised by the mismatch when searching for map classes.

### Database (SQL Server + NHibernate)

- ORM: NHibernate 5.5.2 with FluentNHibernate 3.4.1 mappings (`MsSql2012Dialect`, `MicrosoftDataSqlClientDriver`)
- Session lifecycle: `ISessionFactory` is a singleton; `NHibernate.ISession` is opened per-request via `AddScoped(factory => sessionFactory.OpenSession())` — repositories share one session per HTTP request, no explicit unit-of-work/transaction wrapper per service call
- **Critical config**: `appsettings.json` → `NHibernate:UpdateSchema` controls whether `SchemaUpdate` runs on startup.
  - `true` = auto-creates/updates tables — intended **only for first deploy**.
  - Set to `false` after the first successful startup to avoid unintended schema drift in production.
- Connection string: `ConnectionStrings:DefaultConnection` is validated at startup (`ValidateConnectionString` in `Program.cs`) — the app refuses to start if it's missing or still a placeholder. `appsettings.json` only holds a placeholder (see `appsettings.json.example`); set the real value via `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."` in dev or `ConnectionStrings__DefaultConnection` env var in Docker/prod.
- When querying with NHibernate LINQ (`_session.Query<T>()`), fetch many-to-one relations with `.Fetch(...)` in separate steps to avoid cartesian-product joins (see `TicketRepository.GetActivosParaSeguimientoAsync`), and use `NHibernateUtil.InitializeAsync(...)` to lazily load an optional association instead of eager-fetching it.

### Auth

- `Jwt:Key` is validated at startup (`ValidateJwtKey` in `Program.cs`): the app throws and refuses to start if the key is missing, under 32 characters, or matches a known placeholder value. Set a real key via `dotnet user-secrets set "Jwt:Key" "..."` in dev or the `Jwt__Key` env var in prod/Docker (docker-compose reads it from a local, gitignored `.env` — see `.env.example`) — never commit a literal value in `appsettings.json`.
- Dual-mode JWT: `Authorization: Bearer <token>` header takes priority; if absent, the `jwt` cookie is used instead (handled in the `OnMessageReceived` JWT bearer event in `Program.cs`) — this lets the same auth scheme serve both future API clients and the Razor MVC browser flow.
- Token lifetime is `Jwt:ExpireMinutes` (default 30), actually wired into `JwtHelper.CreateToken` and the `jwt` cookie's expiry (`UsersController.Login`). Every token carries a `jti` claim.
- Server-side revocation (`Program.cs`'s `OnTokenValidated`): rejects the request if the token's `jti` is in `ITokenRevocationService` (in-memory, `Infrastructure/Services/InMemoryTokenRevocationService`, singleton) or if the user (looked up by the `ClaimTypes.NameIdentifier` claim via `IUsuarioService.GetByIdAsync`) is missing or `Activo == false` — this makes deactivation and logout take effect on the user's **next request** instead of waiting for the JWT to expire. `UsersController.Logout` calls `ITokenRevocationService.Revoke` with the current `jti` before deleting the cookie. The revocation list is in-memory only (resets on restart, not shared across instances) — move it to a persisted table if that becomes a problem.
- HTTPS-only sessions: the `jwt` cookie's `Secure` flag is `!_environment.IsDevelopment()` — always `true` outside Development (never conditional on the current request's scheme, so a misconfigured proxy can't downgrade it), and deliberately `false` in Development so `dotnet run --launch-profile http` still works locally without a trusted cert. `RequireHttpsMetadata = true` on the JWT bearer handler (inert today since this app has no `Authority`/metadata address, but no reason to leave it permissive). `AddHsts` sets `Preload`/`IncludeSubDomains`/365-day `MaxAge`, applied via the existing `UseHsts()` (still gated to non-Development). `Properties/launchSettings.json` lists the `https` profile first (the `dotnet run` default — HTTPS is still the recommended local setup).
  - In `docker-compose.yml`, `app` currently runs with `ASPNETCORE_ENVIRONMENT=Development` and is published straight to the host at `${APP_HOST:-127.0.0.1}:${APP_PORT:-8080}:8080` (both overridable in `.env`, default binds only to loopback) — that's what lets `http://localhost:8080` sustain a login. `caddy` still terminates TLS locally (self-signed, `https://localhost:8443`, not parameterized) as the HTTPS path. Switching `app`'s environment back to `Production` makes the direct HTTP port stop sustaining login (matches real-deployment behavior); Caddy is then the only way in.
- Login brute-force protection: `UsersController.Login` (POST) has `[EnableRateLimiting("login")]`, mapped to a per-IP sliding-window policy (10 req/min, 429 on excess) registered via `AddRateLimiter` in `Program.cs`. Separately, `ILoginAttemptService` (in-memory, `Infrastructure/Services/InMemoryLoginAttemptService`) locks a login identifier for 15 minutes after 5 failed attempts in a 15-minute window, checked before credentials are validated; failures and lockouts are logged through `ILogger<UsersController>`. Both stores are in-memory/per-instance, same caveat as the token revocation list.
- Roles: `Administrador`, `Supervisor`, `Soporte`, `Usuario`, enforced via `[Authorize(Roles = "...")]` per controller/action.
- Passwords hashed with BCrypt (`UsuarioService`), never stored in plain text.
- `[ValidateAntiForgeryToken]` on POST actions (CSRF protection).
- Unauthenticated/forbidden HTML requests (401/403) are redirected to `/Users/Login` via `UseStatusCodePages` in `Program.cs`, rather than returning a bare status code — this only applies when the request's `Accept` header wants `text/html`.
- Default route is `{controller=Users}/{action=Login}/{id?}`.
- **Ticket attachments (A2)**: `TicketsController.Create` validates every upload with `Infrastructure/Security/AttachmentValidator` — extension allow-list (pdf, jpg/jpeg, png, gif, webp, doc, docx, xls, xlsx, txt, csv) plus a magic-bytes check that the file's actual content matches the claimed extension. The browser-supplied `IFormFile.ContentType` is discarded; the stored/served content-type is always the validator's canonical MIME for that extension. `TicketsController.DescargarAdjunto` serves attachments with `Content-Disposition: attachment` (via `File(..., fileDownloadName)`) and an explicit `X-Content-Type-Options: nosniff` header, so nothing renders inline in the browser.

### External dependencies

- **Azure Blob Storage** (`AzureBlobStorageService`, registered as `IStorageService`) for ticket attachments (10 MB max), config under `AzureBlobStorage:*`. Azurite emulates this locally via Docker.
- **Email** via SMTP (`EmailService`), config under `Email:*` (Gmail defaults in sample config). A failure here does not block ticket creation — services are isolated behind interfaces.
- **Reports**: QuestPDF (PDF) and ClosedXML (Excel), driven from `ReporteService` / `ReportesController`.

## Configuration notes

- `SistemaTickets/appsettings.json` holds connection string, `Jwt:Key` / `Jwt:Issuer` / `Jwt:Audience` / `Jwt:ExpireMinutes`, `Encryption:Key`, `Email:*`, `AzureBlobStorage:*`. `Jwt:Key` and `Encryption:Key` must be at least 32 characters. The committed file only contains placeholders — `SistemaTickets/appsettings.json.example` documents the expected shape.
- Never commit real secrets in `appsettings.json`; `Jwt:Key` and `ConnectionStrings:DefaultConnection` are enforced at startup (`Program.cs` throws if either is missing or still a placeholder — see the Auth and Database sections above). In dev, supply real values via `dotnet user-secrets`; in Docker/production, via environment variables (`Jwt__Key`, `ConnectionStrings__DefaultConnection`, etc.) or a secret manager. `docker-compose.yml` reads `Jwt__Key` from a local, gitignored `.env` file (`.env.example` documents it).
- `libman.json` exists but is empty — client-side libraries are checked directly into `wwwroot/lib`, not restored via libman.

## Known gaps (see `planes/` for the fuller writeups)

- No automated tests, no CI pipeline currently wired up (`planes/ci.yml` is a draft, not installed), no rate limiting/2FA/JWT revocation. `planes/01-problemas-de-seguridad.md`, `planes/02-ci-cd-plan.md`, and `planes/03-plan-arquitectura.md` catalog known security issues and proposed architecture/CI improvements — consult them before proposing security or CI/CD changes so you don't duplicate or contradict existing analysis.
