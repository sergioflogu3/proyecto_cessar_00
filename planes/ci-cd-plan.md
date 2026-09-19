# Plan CI/CD — GitHub Actions (solo CI)

**Estado de la decisión:** CI (build + seguridad), repo **público**, imagen futura a **Docker Hub**, **sin CD** por ahora. Stub de tests se pospone: arrancamos con build/lint hasta abordar la Fase 0.1.

## 1. Objetivo

Automatizar build, verificación y análisis de dependencias en cada PR y push a `main`, dando señal verde/roja antes de merge y sentando las bases para el CD futuro.

## 2. Pipeline: `ci.yml`

El workflow está incluido en esta carpeta como `ci.yml`.
> **⚠️ Para activarlo:** GitHub Actions solo ejecuta workflows desde `.github/workflows/`.
> Mover/copiar `planes/ci.yml` → `.github/workflows/ci.yml` en la raíz del repo.

**Triggers:** `push` a `main`, `pull_request` a `main`. Con `concurrency` para cancelar builds duplicados.

**Jobs:**

1. **build** (ubuntu-latest)
   - `actions/checkout@v4`
   - `actions/setup-dotnet@v4` con .NET 8.x + cache de NuGet
   - `dotnet restore SistemaTickets.sln`
   - `dotnet build SistemaTickets.sln -c Release --no-restore`

2. **format** (opcional, no bloqueante al inicio)
   - `dotnet format SistemaTickets.sln --verify-no-changes` con `continue-on-error: true` hasta normalizar el estilo.

3. **security** (bloqueante)
   - `dotnet list SistemaTickets.sln package --vulnerable --include-transitive` — responde al hallazgo **B2** del plan de seguridad.
   - Política: arranca en modo advertencia (`continue-on-error: true`) 2 semanas, luego se cambia a bloqueante.
   - Reto #16 (repo público): CodeQL gratuito disponible como job opcional de análisis estático C#.

**Secrets necesarios en esta fase:** ninguno (el build no accede a BD ni firma JWT).

## 3. Gobernanza

- Branch protection en `main`: PR requerido + checks `build` y `security` obligatorios antes de merge.
- Badge de estado del workflow en el README (argumento visual fuerte en la defensa).
- Notificación de fallos: la que trae GitHub por defecto (email al committer del commit roto).

## 4. Reservas para el futuro (no se implementan ahora)

- **Fase 0.1:** stub `SistemaTickets.Tests` (xUnit) → job `test` con `dotnet test --collect "XPlat Code Coverage"` y cobertura como artifact.
- **Docker Hub:** job `docker` que solo construye la imagen (sin push) cuando se valide el `Dockerfile` en CI; push con secrets `DOCKERHUB_USERNAME` / `DOCKERHUB_TOKEN` cuando toque.
- **CD:** workflow `deploy.yml` (push a `main`/tags) cuando se decida destino: Azure App Service (Container), servidor propio con docker-compose, o solo publicación a Docker Hub.

## 5. Riesgos / notas

- Sin tests hoy, el CI solo valida compilación — valor limitado al inicio, pero instala la cultura de pipeline desde el primer commit.
- `dotnet list package --vulnerable` puede marcar versiones actuales (NHibernate, SqlClient, QuestPDF); definir cuándo pasar de `warn` a `error`.
- Repo **público**: nunca commitear secretos reales en `appsettings.json` — ver hallazgos **C1/C2** de `problemas-de-seguridad.md`; los secretos irán a GitHub Secrets cuando se implemente CD.
- .NET 8 está en LTS hasta noviembre 2026: planear migración a .NET 9/10 en el pipeline (solo cambia la versión en `setup-dotnet`).
