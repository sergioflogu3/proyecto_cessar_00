# C3 — Token JWT no revocable + desactivación de usuario sin efecto inmediato

**Severidad:** Crítica · **Estado:** ✅ Resuelto

## Hallazgo original

- **Evidencia:** `Controllers/UsersController.cs:74-79` (Logout solo borraba la cookie; el JWT
  seguía vigente), `Program.cs:79` (`ClockSkew = TimeSpan.Zero`, validación solo por fecha),
  `UsersController.Deactivate` (no invalidaba sesiones).
- **Impacto:** un usuario desactivado, o un token robado, seguía siendo aceptado hasta por 2
  horas; el "logout" no invalidaba nada del lado servidor.
- **Remediación pedida:** corto plazo, bajar `ExpireMinutes` a 15-30; mediano plazo, lista de
  revocación (`RevokedTokens` o cache en memoria consultada en `OnTokenValidated`) y validar en
  cada request que el usuario sigue activo (claim `jti` + estado en BD).

## Cambios realizados

### Corto plazo — `ExpireMinutes` conectado y bajado a 30

Se descubrió que `Jwt:ExpireMinutes` **nunca se usaba**: `JwtHelper.CreateToken` tenía
`AddHours(2)` hardcodeado, ignorando el valor de configuración. Se corrigió:

- `JwtHelper.CreateToken` ahora lee `jwtSection.GetValue<int?>("ExpireMinutes") ?? 30` y lo usa
  para `expires`.
- El valor por defecto bajó de `120` a `30` en `appsettings.json`, `appsettings.json.example`,
  `docker-compose.yml` y el ejemplo del `README.md`.
- La cookie `jwt` en `UsersController.Login` también dejó de tener su propio `AddHours(2)`
  hardcodeado; ahora usa el mismo valor configurado (`GetConfiguredExpireMinutes()`).

### Mediano plazo — revocación + verificación de usuario activo por request

- Cada JWT emitido ahora incluye un claim `jti` (`JwtHelper.CreateToken`, `Guid.NewGuid()`).
- Nuevo `ITokenRevocationService` (`Domain/Services`) con implementación en memoria
  `InMemoryTokenRevocationService` (`Infrastructure/Services`): `ConcurrentDictionary<jti,
  expiración>` con purga automática de entradas vencidas. Registrado como singleton.
- `Program.cs` → evento `OnTokenValidated` del middleware JWT Bearer: en **cada request**
  valida (a) que el `jti` del token no esté en la lista de revocación, y (b) que el usuario
  (resuelto por el claim `ClaimTypes.NameIdentifier` vía `IUsuarioService.GetByIdAsync`) siga
  existiendo y con `Activo == true`. Si cualquiera falla, `ctx.Fail(...)` rechaza el request.
- `UsersController.Logout` ahora revoca el `jti` del token actual (con
  `ITokenRevocationService.Revoke`) antes de borrar la cookie — el logout invalida algo real
  del lado servidor.
- `Deactivate` no necesitó cambios propios: como `Activo` ya se revisa en cada request,
  desactivar a alguien lo bloquea en su siguiente petición sin tocar ese endpoint.

## Archivos modificados / creados

- `SistemaTickets/Infrastructure/Security/JwtHelper.cs` — claim `jti` + `ExpireMinutes` real.
- `SistemaTickets/Domain/Services/ITokenRevocationService.cs` (nuevo).
- `SistemaTickets/Infrastructure/Services/InMemoryTokenRevocationService.cs` (nuevo).
- `SistemaTickets/Program.cs` — registro del servicio + evento `OnTokenValidated`.
- `SistemaTickets/Controllers/UsersController.cs` — inyección de `ITokenRevocationService`,
  `Logout` revoca el `jti`, cookie usa `ExpireMinutes` configurado.
- `SistemaTickets/appsettings.json`, `appsettings.json.example`, `docker-compose.yml`,
  `README.md` — `ExpireMinutes` de `120` a `30`.
- `README.md`, `AGENTS.md`, `CLAUDE.md` — documentación del mecanismo.

## Cómo verificar

1. Iniciar sesión → el token expira a los 30 minutos (antes eran 2 horas fijas sin importar la
   config).
2. Hacer logout → el token que tenía la cookie deja de aceptarse inmediatamente (antes seguía
   siendo válido hasta expirar).
3. Un Administrador desactiva a un usuario con sesión activa → en la siguiente petición de ese
   usuario, es rechazado (redirigido a login) aunque su token todavía no haya expirado.

## Limitaciones conocidas (documentadas también en `AGENTS.md`/`CLAUDE.md`)

- La lista de revocación es **en memoria**: se pierde al reiniciar el proceso y no se comparte
  entre instancias. Es una de las dos opciones que planteaba la remediación original ("tabla
  `RevokedTokens` **o** cache en memoria"); si en algún momento se despliega con múltiples
  instancias o reinicios frecuentes, migrar a una tabla persistida sería el siguiente paso.
