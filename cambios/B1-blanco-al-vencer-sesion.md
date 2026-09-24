# B1 — Página en blanco al acceder sin sesión válida (en vez de redirigir a login)

**Tipo:** Bug funcional (no es uno de los hallazgos numerados del plan de seguridad) ·
**Estado:** ✅ Resuelto

## Reporte

> "cuando una ruta no esta activo o ya vencio la sesion se queda en blanco la pagina en vez de
> redireccionar al login"

## Causa raíz

En `Program.cs`, el middleware `app.UseStatusCodePages(...)` (el que redirige a `/Users/Login`
cuando la respuesta es 401/403 y el cliente pide HTML) estaba registrado **después** de
`app.UseAuthentication()` / `app.UseAuthorization()`:

```csharp
app.UseAuthentication();
app.UseAuthorization();

app.UseStatusCodePages(async ctx => { /* redirige a /Users/Login */ });
```

En ASP.NET Core, cada `app.Use...()` envuelve al resto del pipeline registrado después de él.
Cuando `UseAuthorization` rechaza una request (usuario no autenticado, rol insuficiente, token
revocado/vencido — cualquier motivo de 401/403), internamente hace un
`ChallengeAsync`/`ForbidAsync` y **retorna sin llamar a `next()`** — corta la ejecución ahí
mismo. Como `UseStatusCodePages` estaba registrado *después*, nunca llegaba a ejecutarse para
esa respuesta: el cliente recibía un 401/403 con el cuerpo vacío, que el navegador renderiza
como una página en blanco, en vez de la redirección a `/Users/Login`.

Esto es un bug preexistente del código original (no algo introducido durante las correcciones
de seguridad de esta conversación) — estaba ahí desde antes de C1. Coincide exactamente con el
reporte: pasa en cualquier ruta protegida sin sesión válida, incluyendo sesión vencida (JWT
expirado), revocada (logout/desactivación — C3) o rol insuficiente.

**No es el mismo bug que arregló el commit `afa5b0d`** ("corregir pantalla en blanco al ocurrir
un error no controlado en login"), que agregó la acción `HomeController.Error()` faltante
(referenciada por `UseExceptionHandler("/Home/Error")`) para **excepciones no controladas**
(errores 500). Este es sobre **401/403 de autenticación/autorización**, un camino de código
completamente distinto.

## Cambio realizado

Se movió `app.UseStatusCodePages(...)` para que quede **antes** de `UseAuthentication()` /
`UseAuthorization()`:

```csharp
app.UseRouting();
app.UseRateLimiter();

app.UseStatusCodePages(async ctx => { /* redirige a /Users/Login */ });

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(...);
```

Así, `UseStatusCodePages` queda envolviendo a `UseAuthentication`/`UseAuthorization`: cuando
estos cortan la ejecución con un 401/403, el control vuelve hacia arriba a través de
`UseStatusCodePages`, que ahora sí puede inspeccionar la respuesta y redirigir.

No se tocó la lógica del handler en sí (sigue chequeando `status == 401 || 403`,
`Accept: text/html`, y que no sea ya `/Users/Login` para evitar loop) — el bug era puramente de
orden de registro del middleware.

## Archivos modificados

- `SistemaTickets/Program.cs` — reordenado `UseStatusCodePages` antes de
  `UseAuthentication`/`UseAuthorization`.
- `README.md`, `AGENTS.md`, `CLAUDE.md` — documentado el requisito de orden, para que no se
  vuelva a romper sin querer en un cambio futuro a este bloque del pipeline.

## Cómo se verificó

Se probó contra el contenedor real de Docker (reconstruido con el fix), no solo se leyó el
código:

```bash
# Sin cookie de sesión:
curl -H "Accept: text/html" http://localhost:8080/Tickets/Index
# → 302 Found, Location: /Users/Login   (antes: 401 en blanco)

# Con un token inválido/vencido en la cookie:
curl -H "Accept: text/html" --cookie "jwt=token-invalido-o-vencido" http://localhost:8080/Tickets/Index
# → 302 Found, Location: /Users/Login

# Ruta con [Authorize(Roles = "Administrador")], sin cookie:
curl -H "Accept: text/html" http://localhost:8080/Users/Index
# → 302 Found, Location: /Users/Login

# /Home/Index (redirige por rol normalmente, pero sin sesión):
curl -H "Accept: text/html" http://localhost:8080/Home/Index
# → 302 Found, Location: /Users/Login
```

Los cuatro casos redirigen correctamente. También se confirmó que `/Users/Login` (la página
pública) y el resto del arranque de la app siguen funcionando sin cambios
(`dotnet build` limpio, contenedor arranca y responde `200` en las rutas públicas).

## Limitaciones conocidas

Ninguna — este era un bug de plomería del pipeline, no una decisión de diseño con trade-offs.
