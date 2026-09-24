# M4 — `CurrentUserId()` con fallback `"0"`

**Severidad:** Media · **Estado:** ✅ Resuelto

## Hallazgo original

- **Evidencia:** `UsersController.cs:194,207`, `TicketsController.cs:34-35` —
  `int.Parse(... ?? "0")`.
- **Impacto:** si el claim falta o está corrupto, se crean registros con `Usuario Id=0` en vez
  de rechazar la petición — datos huérfanos y confusión de trazabilidad.
- **Remediación pedida:** si el claim es inválido, devolver 401/403 y no continuar.

## Alcance real encontrado

El mismo patrón `int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0")` no estaba
solo en los dos sitios citados como evidencia — aparecía **7 veces en 5 controladores**:

| Archivo | Forma |
|---|---|
| `UsersController.cs` | 3 usos inline (`Edit`, `CambiarPassword`, `Deactivate`) |
| `TicketsController.cs` | helper privado `CurrentUserId()` |
| `InventarioController.cs` | helper privado `CurrentUserId()` |
| `CampoController.cs` | helper privado `CurrentUserId()` |
| `DashboardController.cs` | 1 uso inline (`Index`) |

`CurrentUserId()` (donde existe como helper) se llama en decenas de puntos dentro de cada
acción — no solo al principio, sino en medio de la lógica (por ejemplo,
`CampoController.cs:117` compara `visita.Tecnico.Id != CurrentUserId()` después de ya haber
hecho trabajo). Arreglar esto "in place" en cada sitio (parsear y devolver 401 ahí mismo)
hubiera significado reestructurar el flujo de control de una cantidad grande de acciones para
poder cortar con un `return Unauthorized()` a mitad de método, duplicando la misma validación
7 veces.

## Decisión: filtro global en vez de checks por acción

En lugar de tocar cada sitio, se resuelve **una sola vez, antes de que cualquier acción se
ejecute**, con un `IActionFilter` registrado globalmente:

`Infrastructure/Security/RequireValidUserIdFilter.cs` (nuevo):

```csharp
public class RequireValidUserIdFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any())
            return;

        var claim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(claim, out _))
            context.Result = new UnauthorizedResult();
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
```

Registrado en `Program.cs`:

```csharp
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add<RequireValidUserIdFilter>());
```

Por qué esta forma y no otras:

- **Filtro global, no por controlador**: se aplica automáticamente a **todos** los controladores
  del proyecto (incluidos los que hoy no usan `CurrentUserId()`, como `ConfiguracionController`
  o `SupervisorController`), sin tener que acordarse de decorarlos uno por uno ni de mantener
  una lista. Cualquier acción nueva que lea este claim queda cubierta sin código adicional.
- **Respeta `[AllowAnonymous]`**: los filtros de acción corren *después* de autorización pero
  *antes* del cuerpo de la acción, y no distinguen `[AllowAnonymous]` por sí solos — el chequeo
  explícito de `EndpointMetadata` es necesario para no romper `UsersController.Login`/`Logout`
  (las únicas acciones anónimas del proyecto que hoy existen). Verificado en Docker: `GET
  /Users/Login` sigue devolviendo `200` con el filtro activo.
- **Se integra gratis con el fix de B1**: `context.Result = new UnauthorizedResult()` deja el
  status code en 401 igual que un rechazo de `UseAuthorization` — como `UseStatusCodePages` ya
  está registrado *antes* de `UseAuthentication`/`UseAuthorization` (ver `B1-blanco-al-vencer-sesion.md`),
  también envuelve la ejecución de MVC/el filtro, así que una petición HTML sin claim válido
  redirige a `/Users/Login` en vez de mostrar una página en blanco o un 401 crudo.

## Cambios realizados

- `SistemaTickets/Infrastructure/Security/RequireValidUserIdFilter.cs` (nuevo).
- `SistemaTickets/Program.cs` — registrado en `AddControllersWithViews`.
- `SistemaTickets/Controllers/UsersController.cs` (3 sitios: `Edit`, `CambiarPassword`,
  `Deactivate`), `TicketsController.cs`, `InventarioController.cs`, `CampoController.cs`,
  `DashboardController.cs` — el patrón `int.Parse(... ?? "0")` pasó a
  `int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value)`. El `!` (null-forgiving) es
  seguro específicamente porque el filtro ya validó el claim antes en el mismo pipeline de la
  request — si alguna vez se quita el filtro global, este código vuelve a ser inseguro y hay que
  revisarlo de nuevo.
- `AGENTS.md`, `CLAUDE.md` — nueva entrada en Auth.
- `README.md` — línea nueva en Seguridad.
- `cambios/README.md` — fila M4 en la tabla.

## Cómo se verificó

1. `dotnet build SistemaTickets.sln` — compila limpio (0 errores).
2. `grep -rn '?? "0"' Controllers/` — sin resultados, los 7 sitios quedaron migrados.
3. Se reconstruyó la imagen de `app` (`docker-compose up -d --build app`) y se confirmó que
   sigue arrancando sano.
4. `curl -o /dev/null -w "%{http_code}" http://localhost:8080/Users/Login` → `200` — confirma
   que el filtro no bloquea la acción `[AllowAnonymous]`.
5. Pendiente de una prueba manual adicional en el navegador (login + una acción autenticada
   normal, p. ej. cargar `/Tickets/Index`) para confirmar que el flujo común no se ve afectado
   por el filtro — el caso feliz (claim válido) no debería notarse en absoluto.

## Limitaciones conocidas

- El filtro devuelve `401` con cuerpo vacío (`UnauthorizedResult`), no JSON. Para las acciones
  AJAX que esperan `{ success, message }` (la mayoría de los endpoints de escritura), un 401 acá
  haría que el `fetch(...).then(r => r.json())` del cliente falle al parsear — pero este es
  exactamente el camino que M4 pide bloquear (identidad inválida/corrupta), un escenario anómalo
  que no debería ocurrir en uso normal (un JWT válido siempre trae el claim, ver
  `Infrastructure/Security/JwtHelper`), así que no se le agregó manejo especial de JSON.
- No se distingue 401 vs 403 — la remediación aceptaba cualquiera de los dos
  ("devolver 401/403"); se eligió 401 por consistencia con el resto del pipeline de auth
  (`Program.cs`'s `OnTokenValidated` también responde con 401 vía `context.Fail(...)`).
- El filtro corre en cada acción de cada request (`FindFirst` sobre los claims ya materializados
  del `ClaimsPrincipal`, sin I/O) — costo despreciable, no requiere caché ni optimización
  adicional.
