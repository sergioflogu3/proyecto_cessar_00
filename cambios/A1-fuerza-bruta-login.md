# A1 — Sin límite de intentos de login (fuerza bruta)

**Severidad:** Alta · **Estado:** ✅ Resuelto

## Hallazgo original

- **Evidencia:** `Controllers/UsersController.cs:39-49` — `ValidateCredentialsAsync` se
  invocaba sin ningún mecanismo de rate limiting, bloqueo temporal o CAPTCHA; no había registro
  de intentos fallidos.
- **Impacto:** ataque de diccionario/fuerza bruta en línea sin fricción contra las credenciales
  de `Administrador`.
- **Remediación pedida:** rate limiting por IP+usuario (ASP.NET Core `RateLimiter`
  middleware), bloqueo temporal después de N intentos, log de intentos fallidos.

## Cambios realizados

Dos mecanismos complementarios, más logging:

### 1. Rate limiting por IP (`RateLimiter` middleware de ASP.NET Core)

- `Program.cs` → `builder.Services.AddRateLimiter(...)`: política `"login"` con ventana
  deslizante (`SlidingWindowRateLimiterOptions`) partición por IP del cliente — máximo **10
  solicitudes/minuto** por IP. Al superarse, responde `429 Too Many Requests` con header
  `Retry-After`, sin siquiera intentar validar credenciales.
- `app.UseRateLimiter()` agregado al pipeline, después de `UseRouting()` y antes de
  `UseAuthentication()`.
- `UsersController.Login` (POST) tiene el atributo `[EnableRateLimiting("login")]`.

### 2. Bloqueo temporal por usuario tras N intentos fallidos

- Nuevo `ILoginAttemptService` (`Domain/Services`) con implementación en memoria
  `InMemoryLoginAttemptService` (`Infrastructure/Services`), registrado como singleton.
- Bloquea un identificador de login (username/email/teléfono, normalizado a minúsculas) por
  **15 minutos** después de **5 intentos fallidos** dentro de una ventana de 15 minutos.
- Se revisa **antes** de llamar a `ValidateCredentialsAsync`: mientras está bloqueado, ni
  siquiera se verifica la contraseña.
- Un login exitoso limpia el contador de ese identificador (`RegisterSuccess`).

### 3. Logging de intentos fallidos

- Cada intento fallido y cada bloqueo se registran vía `ILogger<UsersController>`, incluyendo
  IP y el identificador de login intentado — **nunca la contraseña**.

## Archivos modificados / creados

- `SistemaTickets/Domain/Services/ILoginAttemptService.cs` (nuevo).
- `SistemaTickets/Infrastructure/Services/InMemoryLoginAttemptService.cs` (nuevo).
- `SistemaTickets/Program.cs` — `AddRateLimiter`, política `"login"`, `app.UseRateLimiter()`,
  registro de `ILoginAttemptService`.
- `SistemaTickets/Controllers/UsersController.cs` — `[EnableRateLimiting("login")]`, chequeo de
  bloqueo antes de validar credenciales, registro de fallos/éxitos, inyección de
  `ILoginAttemptService` y `ILogger<UsersController>`.
- `README.md`, `AGENTS.md`, `CLAUDE.md` — documentación de ambos mecanismos.

## Cómo verificar

1. Enviar >10 POST a `/Users/Login` en menos de un minuto desde la misma IP → a partir del
   número 11, respuesta `429` con `Retry-After`.
2. Fallar el login 5 veces seguidas con el mismo usuario (aunque cambie de IP) → el sexto
   intento devuelve "Demasiados intentos fallidos. Intenta de nuevo en N minuto(s)." sin
   siquiera comprobar la contraseña.
3. Revisar logs → cada intento fallido y cada bloqueo aparece con `LogWarning`, IP incluida.

## Limitaciones conocidas (documentadas también en `AGENTS.md`/`CLAUDE.md`)

- Ambos almacenes (rate limiter y bloqueo por usuario) son **en memoria**, mismo caveat que la
  lista de revocación de tokens de C3: se pierden al reiniciar, no se comparten entre
  instancias.
- El bloqueo por usuario es en sí mismo un trade-off de disponibilidad conocido: un atacante
  que conozca un username podría forzar bloqueos repetidos contra esa cuenta rotando de IP.
  Se aceptó porque la alternativa (sin bloqueo) es peor; si se vuelve un problema real, el
  siguiente paso natural sería CAPTCHA u otra señal adicional, explícitamente fuera del alcance
  de esta remediación (que no lo pedía).
