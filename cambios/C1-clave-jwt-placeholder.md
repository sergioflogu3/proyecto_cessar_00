# C1 — Clave JWT con valor placeholder / predecible

**Severidad:** Crítica · **Estado:** ✅ Resuelto

## Hallazgo original

- **Evidencia:** `SistemaTickets/appsettings.json:6` — `"Key": "REEMPLAZAR-CON-CLAVE-JWT-SEGURA-MINIMO-32-CARACTERES-AQUI"`. `Infrastructure/Security/JwtHelper.cs:19` leía la clave con `jwtSection["Key"]!` sin validar longitud ni que hubiera sido reemplazada. `Program.cs` tampoco la validaba al arrancar.
- **Impacto:** si se desplegaba sin reemplazar la clave (error humano típico), cualquier atacante que hubiera visto el repo podía firmar tokens JWT válidos con rol `Administrador` y tomar control total del sistema.

## Cambios realizados

1. **Validación al arrancar (`SistemaTickets/Program.cs`)** — método privado `ValidateJwtKey(string? key)`:
   - Lanza `InvalidOperationException` (la app no arranca) si la clave está vacía/ausente.
   - Lanza excepción si tiene menos de 32 caracteres.
   - Lanza excepción si coincide con valores de placeholder conocidos (el del repo y el usado en `docker-compose.yml`), o si contiene substrings como `REEMPLAZAR`, `CAMBIAR`, `PLACEHOLDER`, `TU-CLAVE` (heurística para detectar futuros placeholders sin mantener una lista cerrada).
   - Se invoca donde antes se leía `jwtKey` directamente, antes de construir las opciones de `AddJwtBearer`.

2. **Secretos fuera del repositorio**:
   - Se agregó `<UserSecretsId>` al `.csproj` (`SistemaTickets/SistemaTickets.csproj`).
   - `docker-compose.yml` ya no trae la clave hardcodeada: ahora lee `Jwt__Key` desde `${JWT_KEY}` con la sintaxis `${JWT_KEY:?...}` (falla explícitamente si no está definida, en vez de arrancar con un valor inseguro).
   - Se creó `.env.example` documentando la variable `JWT_KEY` (sin valor real) y se agregó `.env` a `.gitignore`.

## Archivos modificados / creados

- `SistemaTickets/Program.cs` — validación `ValidateJwtKey`.
- `SistemaTickets/SistemaTickets.csproj` — `UserSecretsId`.
- `docker-compose.yml` — `Jwt__Key` ahora viene de `${JWT_KEY}`.
- `.env.example` (nuevo).
- `.gitignore` — ignora `.env`.
- `README.md`, `AGENTS.md`, `CLAUDE.md` — documentación del requisito y cómo definir la clave real (User Secrets en dev, variable de entorno en Docker/producción).

## Cómo verificar

```bash
cd SistemaTickets
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)"
dotnet run --project SistemaTickets.csproj   # ahora arranca con una clave real
```

Si se borra el user-secret (o se despliega con `appsettings.json` tal cual viene en el repo,
que solo trae el placeholder), la app **falla al arrancar** con un mensaje explicando qué
variable/secret falta — ese es el comportamiento esperado, no un bug.

## Limitaciones / seguimiento pendiente

Ninguna relevante: la clave se genera con `openssl rand -base64 48` (aleatoria, de longitud
sobrada) y vive únicamente en User Secrets locales / variables de entorno, nunca en el repo.
