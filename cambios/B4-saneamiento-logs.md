# B4 — Logs sin política de saneamiento

**Severidad:** Baja · **Estado:** ✅ Resuelto

## Hallazgo original

- **Evidencia:** nivel `Information` global (`appsettings.json:28`); no hay revisión de qué
  queda registrado en logs de excepciones (podrían caer datos de tickets o tokens
  `SaveToken = true`, `Program.cs:64`).
- **Remediación pedida:** revisar qué se loguea; evitar loguear claims/tokens; nivel `Warning`
  en producción para namespace `Microsoft`.

## Parte 1: revisión de qué se loguea

Se revisó cada llamada a `_logger.Log*` del proyecto (`grep -rn "_logger\."`) más el bloque de
eventos de `AddJwtBearer` en `Program.cs` (donde vive `SaveToken = true` y el evento
`OnTokenValidated`, el lugar más obvio para que algo terminara logueando un claim o un token si
alguien agregara un log ahí sin pensarlo).

### El hallazgo real: `NHibernateBootstrap.BuildSessionFactory` tenía `.ShowSql()` fijo, siempre activo

Este fue el hallazgo más importante de B4, y casi se pasa por alto en la primera revisión: `.ShowSql()` está seteado sin condición, en **todos** los entornos —

```csharp
var db = MsSqlConfiguration.MsSql2012
    .ConnectionString(cs)
    .Driver<MicrosoftDataSqlClientDriver>()
    .Dialect<MsSql2012Dialect>()
    .ShowSql();
```

Esto hace que NHibernate escriba **cada sentencia SQL, con los valores reales de cada parámetro
enlazado**, a la salida estándar — título y descripción de tickets, username, cualquier columna
que viaje como parámetro en cualquier INSERT/UPDATE/SELECT. Es literalmente "podrían caer datos
de tickets" del hallazgo original, no una posibilidad hipotética: se puede confirmar con
`docker logs sistickets-app`, aparecen líneas `NHibernate: select ... ;@p0 = 'valor real'`.

**Y no pasa por el saneamiento que se acababa de hacer en la Parte 2** (`Microsoft: Warning` en
producción): NHibernate no enruta su log de SQL a través de `Microsoft.Extensions.Logging` — sin
un logger factory explícito conectado, escribe directo a `Console`. Se nota en el formato: las
líneas de NHibernate son de una sola línea, `NHibernate: <sql>`, mientras que todo lo que pasa
por `ILogger` en este proyecto sale con el formato de dos líneas `info:/warn: Categoria[EventId]`
+ mensaje indentado. Ningún `LogLevel` de `appsettings.json` (ni el `Microsoft: Warning` nuevo)
tiene efecto sobre esto — confirmado empíricamente (ver "Cómo se verificó").

**Fix**: `NHibernateBootstrap.BuildSessionFactory` ahora recibe un parámetro `showSql`, y solo
llama a `.ShowSql()` cuando es `true`. `Program.cs` lo pasa como
`builder.Environment.IsDevelopment()` — se sigue viendo el SQL completo en desarrollo local
(donde hace falta para depurar), y desaparece por completo fuera de `Development` (Docker con
`ASPNETCORE_ENVIRONMENT=Production`, el entorno real de despliegue).

### Resto de la revisión (`_logger.Log*` + eventos de JWT)

- **Ningún código loguea el JWT crudo, un volcado de claims, ni una contraseña.**
  `Program.cs`'s `OnMessageReceived`/`OnTokenValidated` no tienen ningún `_logger.Log*` —
  solo hacen `ctx.Fail(...)` con mensajes fijos ("Token revocado.", "Usuario inactivo o
  inexistente."), sin interpolar el token ni el claim.
- `IdentityModelEventSource.ShowPII` (el flag de Microsoft.IdentityModel que, si se activa,
  hace que los logs internos de validación de tokens incluyan el contenido real de claims/tokens
  en vez de un placeholder `"[PII of type ... is hidden]"`) **nunca se toca en el código** —
  sigue en su valor por defecto (`false`), que es el seguro.
- `UsersController.Login` loguea el **login intentado** (username/email/teléfono) y la IP en
  los intentos fallidos (`LogWarning`, ver A1) — deliberado, es lo que permite detectar fuerza
  bruta; nunca loguea la contraseña. No es un hallazgo nuevo de B4, ya estaba revisado como
  parte de A1.
- **Un hallazgo real**: `EmailService.SendTicketCopyAsync` logueaba el correo **desencriptado**
  del solicitante en texto plano, a nivel `Information`:
  ```csharp
  _logger.LogInformation("Copia de ticket #{TicketId} enviada a {Email}.", ticketId, toEmail);
  ```
  Ese email se guarda cifrado en la base específicamente para protegerlo (`EncryptionHelper`) —
  desencriptarlo solo para mandarlo y después dejarlo en texto plano en los logs de la
  aplicación deshace ese cuidado. Corregido para loguear `solicitante.Id` (alcanza para
  correlacionar en logs sin volver a exponer el correo):
  ```csharp
  _logger.LogInformation(
      "Copia de ticket #{TicketId} enviada al solicitante (UsuarioId {UsuarioId}).",
      ticketId, solicitante.Id);
  ```

## Parte 2: `Microsoft` a `Warning` en producción

El `appsettings.json` base ya tenía `"Microsoft.AspNetCore": "Warning"` — pero eso solo cubre
ese sub-namespace. Cosas como `Microsoft.Hosting.Lifetime` (los mensajes de arranque:
"Now listening on...", "Application started"...) o `Microsoft.IdentityModel.*` (validación de
tokens) caen fuera de `Microsoft.AspNetCore` y seguían heredando `"Default": "Information"`.

Se agregó `SistemaTickets/appsettings.Production.json` (nuevo, mismo mecanismo estándar de
ASP.NET Core que ya usa `appsettings.Development.json` — se carga automáticamente cuando
`ASPNETCORE_ENVIRONMENT=Production`, sobre el `appsettings.json` base):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

**Por qué un archivo nuevo en vez de editar el `appsettings.json` base**: la remediación pide
específicamente "Warning **en producción**" — no en todos los entornos. `appsettings.Development.json`
ya tenía (desde antes de este cambio) `"Microsoft.AspNetCore": "Warning"`, así que el
comportamiento en desarrollo/Docker (que hoy corre con `ASPNETCORE_ENVIRONMENT=Development`, ver
A3) queda exactamente igual que antes — no se tocó. Un archivo `appsettings.Production.json`
nuevo, aislado, es el mecanismo menos invasivo para lograr el efecto solo donde se pidió.

## Archivos modificados / creados

- `SistemaTickets/Infrastructure/Persistence/NHibernateBootstrap.cs` — `ShowSql()` condicionado
  a un nuevo parámetro `showSql` (antes: siempre activo).
- `SistemaTickets/Program.cs` — pasa `showSql: builder.Environment.IsDevelopment()` a
  `BuildSessionFactory`.
- `SistemaTickets/appsettings.Production.json` (nuevo) — `Logging:LogLevel:Microsoft: "Warning"`.
- `SistemaTickets/Infrastructure/Services/EmailService.cs` — ya no loguea el email
  desencriptado, loguea `solicitante.Id`.
- `AGENTS.md`, `CLAUDE.md` — nueva entrada documentando ambos mecanismos y la revisión hecha.
- `README.md` — línea nueva en Seguridad.
- `cambios/README.md` — fila B4 en la tabla.

## Cómo se verificó

1. `dotnet build SistemaTickets.sln` — compila limpio (0 errores).
2. Se cambió temporalmente `docker-compose.yml`'s `app.environment.ASPNETCORE_ENVIRONMENT` de
   `Development` a `Production`, se reconstruyó (`docker-compose up -d --build app`) y se
   comparó el comportamiento contra el contenedor real en ambos entornos:
   - **`Microsoft: Warning`** — en Development aparecían `info: Microsoft.Hosting.Lifetime[14]
     Now listening on...`, `Application started`, `Hosting environment: Development`,
     `Content root path: /app`; con `Production` esas cuatro líneas `info:` desaparecen por
     completo, solo quedan los `warn:` (incluida la advertencia de A5 sobre
     `TrustServerCertificate=True`, esperada al salir de `Development`, ya cubierta por A5).
   - **Eco de SQL** — se generó un intento de login (`curl` con token antiforgery real, contra
     `POST /Users/Login`, credenciales inexistentes a propósito — solo hace falta que dispare la
     consulta a `Usuarios`, no que el login tenga éxito) en cada entorno:
     - **Development**: `docker logs sistickets-app | grep -c "^NHibernate:"` → **1** (la
       consulta se logueó completa, con el valor del parámetro).
     - **Production** (mismo intento de login, mismo código): → **0** — ninguna línea de SQL,
       confirmando que `showSql: false` corta el eco por completo, y que no dependía del nivel
       `Microsoft: Warning` (que de todas formas no lo hubiera filtrado, según el análisis de
       arriba).
3. Se revirtió `docker-compose.yml` a `ASPNETCORE_ENVIRONMENT=Development` (con `sed`, y se
   verificó con `grep` que quedó igual que antes de esta verificación) y se reconstruyó de
   nuevo — se confirmó `GET /Users/Login` → `200` y que el eco de SQL vuelve a aparecer en
   Development (nada se rompió en el flujo de desarrollo/Docker habitual).

## Limitaciones conocidas

- No se probó el envío real de un correo (no hay credenciales SMTP en este entorno, mismo
  límite que M6) — el cambio en `EmailService` es un cambio de log, no de lógica de envío, así
  que el riesgo de regresión es bajo, pero no se ejecutó ese camino end-to-end.
- El nivel `Warning` en producción para `Microsoft.*`, y `showSql: false` fuera de Development,
  reducen ruido/riesgo pero también reducen visibilidad operativa (menos detalle si algo falla a
  nivel framework o hace falta ver una consulta puntual en producción). Es el trade-off que
  pedía explícitamente la remediación; si hace falta ese detalle para diagnosticar un problema
  puntual, se puede reactivar temporalmente sin volver a este hallazgo.
- No se auditaron logs propios de `Azure.Storage.Blobs`/`QuestPDF`/`ClosedXML` — no se encontró
  ninguna configuración en este proyecto que los active explícitamente a un nivel verboso, así
  que no se consideraron parte del alcance real de este hallazgo (a diferencia del `ShowSql()`
  de NHibernate, que sí estaba prendido y sí es código de este proyecto).
