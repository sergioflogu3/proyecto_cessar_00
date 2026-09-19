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
- Connection string lives in `appsettings.json` (`DefaultConnection`).
- `ScriptsDB/` contains canonical DDL scripts (users, tickets, inventario, campo repuestos). Run these in order if bootstrapping a fresh database manually.

## Auth

- JWT Bearer + cookie dual mode:
  1. Respects `Authorization: Bearer <token>` header if present.
  2. Falls back to `jwt` cookie for browser MVC flows.
- Role-based: `Administrador`, `Supervisor`, `Soporte`, `Usuario`.
- Unauthenticated HTML requests to protected pages get **redirected to `/Users/Login`** (not 401 JSON).
- `[ValidateAntiForgeryToken]` is used on POSTs.

## External dependencies

- **Azure Blob Storage** (`AzureBlobStorageService`) for ticket file attachments. Config: `AzureBlobStorage:ConnectionString` / `ContainerName`.
- **Email** via SMTP (Gmail defaults in config). Config: `Email:*`.
- **Reports**: QuestPDF + ClosedXML.

## Build / run notes

- No test projects exist. Verification is manual or via running the app.
- `libman.json` is present but empty; client libraries are likely checked into `wwwroot/`.
- Standard `dotnet build` / `dotnet run` workflow.

## Safety

- Do not commit real secrets in `appsettings.json`. Placeholder keys are present for `Jwt:Key` and `Encryption:Key`.
