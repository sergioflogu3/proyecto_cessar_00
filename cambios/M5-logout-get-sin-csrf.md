# M5 — `Logout` por GET sin protección anti-CSRF

**Severidad:** Media · **Estado:** ✅ Resuelto

## Hallazgo original

- **Evidencia:** `UsersController.cs:74-78`.
- **Impacto:** una imagen/enlace externo `<img src=".../Users/Logout">` (o un link) puede cerrar
  la sesión de un usuario sin su consentimiento (CSRF de bajo impacto, pero denegación de
  servicio molesta).
- **Remediación pedida:** convertir el logout a POST con token anti-forgery, o validar
  `SameSite`.

## Decisión: POST + antiforgery (no solo `SameSite`)

La remediación ofrecía dos caminos alternativos. Se eligió el primero (POST + antiforgery) en
vez de solo endurecer `SameSite` en la cookie `jwt`, porque:

- Es la misma protección que ya usa el resto de las acciones que cambian estado en este
  proyecto (`[ValidateAntiForgeryToken]` en `Create`/`Edit`/`Deactivate`/`CambiarPassword`, etc.)
  — consistencia con el patrón existente en vez de introducir un mecanismo nuevo.
- `SameSite=Lax` (el valor actual de la cookie `jwt`, ver `UsersController.Login`) ya bloquea la
  mayoría de los casos cross-site vía `<img>`/`<link>` (esos son "simple" cross-site requests sin
  navegación top-level), pero `Lax` sigue permitiendo navegación top-level (un link normal que el
  usuario hace clic, o un `<form>` con `method="GET"` en otro sitio) — no es una garantía tan
  fuerte como un antiforgery token explícito, y un GET sigue siendo trivial de disparar sin que
  la víctima haga nada consciente.

## Cambios realizados

### 1. Controlador

`Controllers/UsersController.cs` — `Logout` pasó de:

```csharp
[HttpGet]
public IActionResult Logout()
```

a:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Logout()
```

Sigue implícitamente bajo `[Authorize]` (a nivel de clase), sin cambios en la lógica interna
(revocación de `jti` + borrado de cookie — ver C3).

### 2. Vista

`Views/Shared/_Layout.cshtml` — el link del sidebar:

```html
<a asp-controller="Users" asp-action="Logout" class="sidebar-logout">
    <i class="bi bi-box-arrow-right"></i>
    <span>Salir</span>
</a>
```

pasó a un form POST con token, mismo patrón que el modal de "Cambiar contraseña" ya usaba
(`@Html.AntiForgeryToken()`):

```html
<form asp-controller="Users" asp-action="Logout" method="post">
    @Html.AntiForgeryToken()
    <button type="submit" class="sidebar-logout">
        <i class="bi bi-box-arrow-right"></i>
        <span>Salir</span>
    </button>
</form>
```

### 3. CSS

`wwwroot/css/site.css` — se agregó `button.sidebar-logout` (reset de estilos de botón nativo:
`background: none; border: none; width: 100%; text-align: left; cursor: pointer;`), mismo
patrón que ya existía para `button.sidebar-link` (usado por el botón de "Cambiar contraseña").
Sin esto el botón se hubiera visto distinto al link original (fondo, borde y fuente por defecto
del navegador).

## Archivos modificados

- `SistemaTickets/Controllers/UsersController.cs` — `Logout`: `[HttpGet]` → `[HttpPost]` +
  `[ValidateAntiForgeryToken]`.
- `SistemaTickets/Views/Shared/_Layout.cshtml` — link → form POST con antiforgery token.
- `SistemaTickets/wwwroot/css/site.css` — reset de `button.sidebar-logout`.
- `AGENTS.md`, `CLAUDE.md` — nueva entrada en Auth.
- `README.md` — línea nueva en Seguridad.
- `cambios/README.md` — fila M5 en la tabla.

## Cómo se verificó

1. `dotnet build SistemaTickets.sln` — compila limpio (0 errores).
2. Se reconstruyó la imagen de `app` (`docker-compose up -d --build app`).
3. `curl -o /dev/null -w "%{http_code}" http://localhost:8080/Users/Logout` → **405** (Method
   Not Allowed) — confirma que el vector original (un GET simple, disparable desde cualquier
   página externa sin interacción del usuario) ya no existe.
4. `curl -o /dev/null -w "%{http_code}" http://localhost:8080/Users/Login` → **200** (control,
   confirma que el resto de la app sigue funcionando normalmente).
5. Pendiente de una prueba manual en el navegador: click en "Salir" desde el sidebar y confirmar
   que cierra sesión y redirige a `/Users/Login` con normalidad (la lógica interna no cambió,
   pero vale confirmar el form nuevo end-to-end, incluyendo que el botón se vea igual que antes).

## Limitaciones conocidas

- Ninguna funcional — es un cambio de superficie (método HTTP + protección CSRF) sin lógica de
  negocio nueva. El único riesgo real era visual (que el botón no se viera como el link
  original), cubierto por el reset de CSS agregado.
