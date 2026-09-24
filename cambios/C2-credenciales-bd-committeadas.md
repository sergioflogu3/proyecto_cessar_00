# C2 — Cadena de conexión con credenciales reales committeada

**Severidad:** Crítica · **Estado:** ✅ Resuelto (código) · ⚠️ Acción pendiente del usuario (rotar contraseña)

## Hallazgo original

- **Evidencia:** `SistemaTickets/appsettings.json:3` — `User Id=ariel;Password=ariel123` en texto plano dentro del repositorio.
- **Impacto:** acceso directo a la base de datos por cualquier persona con acceso al código. Con `NHibernate:UpdateSchema=true` además podría alterar el esquema.

## Cambios realizados

1. **Repo limpio**: `SistemaTickets/appsettings.json` ya no contiene `ariel/ariel123`. La
   cadena de conexión quedó con un placeholder consistente con el resto del archivo
   (`REEMPLAZAR-USUARIO-SQL` / `REEMPLAZAR-PASSWORD-SQL`).
2. **Plantilla de referencia**: se creó `SistemaTickets/appsettings.json.example`, copia del
   `appsettings.json` sanitizado, para que quede documentado el formato esperado sin que nadie
   tenga que adivinar qué claves existen.
3. **Validación al arrancar (`SistemaTickets/Program.cs`)** — método `ValidateConnectionString(string? connectionString)`, mismo patrón que `ValidateJwtKey` (C1):
   - Falla si la cadena está vacía/ausente.
   - Falla si contiene marcadores de placeholder (`REEMPLAZAR`, `PLACEHOLDER`, `TU-USUARIO`, `TU-PASSWORD`, `TU_USUARIO`, `TU_PASSWORD`).
   - Se invoca donde antes se leía `connectionString` directamente desde `GetConnectionString("DefaultConnection")`.

## Archivos modificados / creados

- `SistemaTickets/appsettings.json` — placeholder en vez de credenciales reales.
- `SistemaTickets/appsettings.json.example` (nuevo).
- `SistemaTickets/Program.cs` — validación `ValidateConnectionString`.
- `README.md`, `AGENTS.md`, `CLAUDE.md` — documentación del requisito y cómo definir el valor real (User Secrets en dev, variable de entorno en Docker/producción).

## Cómo verificar

```bash
cd SistemaTickets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost;Database=SISTickets;User Id=<usuario>;Password=<password>;Encrypt=True;TrustServerCertificate=False;"
dotnet run --project SistemaTickets.csproj
```

Sin ese secret definido, la app falla al arrancar con un mensaje explícito, igual que con
`Jwt:Key`.

## Lo que NO se automatizó (requiere acción manual del usuario)

1. **No se puso ninguna credencial de SQL Server en User Secrets local**, ni siquiera
   `ariel/ariel123` — hacerlo habría vuelto a propagar una contraseña ya comprometida. Cada
   quien debe correr el comando de arriba con sus propias credenciales reales.
2. **Rotar la contraseña de `ariel` en la instancia real de SQL Server.** El valor `ariel123`
   sigue recuperable en el historial de git (commits `344269c` / `871da36` / `061ba15`) aunque
   ya no aparezca en el archivo actual. Reescribir el historial de git (`git filter-repo`,
   BFG) es una operación destructiva que no se ejecutó sin pedido explícito, y si el repo ya
   se compartió/subió a algún remoto, de todos modos seguiría expuesto ahí. **Rotar la
   contraseña es lo que realmente neutraliza el riesgo**, no basta con limpiar el archivo.
