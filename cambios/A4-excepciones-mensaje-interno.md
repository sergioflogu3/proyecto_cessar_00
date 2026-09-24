# A4 — Excepciones con mensaje interno al cliente

**Severidad:** Alta · **Estado:** ✅ Resuelto

## Hallazgo original

- **Evidencia:** `UsersController.cs:183` (`errors = new[] { ex.Message }`),
  `TicketsController.cs:105` (patrón idéntico en varios controladores AJAX).
- **Impacto:** mensajes de excepción de NHibernate/SqlClient revelan nombres de tablas,
  columnas y a veces fragmentos de SQL → facilita reconocimiento del backend.
- **Remediación pedida:** loggear la excepción completa en el server (`ILogger`) y devolver un
  mensaje genérico al cliente.

## Alcance real del problema

El patrón `catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }`
no estaba solo en los dos lugares de la evidencia — aparecía **13 veces** en 4 controladores:

| Controlador | Acciones afectadas |
|---|---|
| `UsersController` | `Edit` (usa `errors: [...]`), `Deactivate` |
| `TicketsController` | `TomarTicket`, `CambiarEstado`, `AsignarPrioridad`, `Asignar`, `Cerrar`, `AgregarComentario` |
| `InventarioController` | `ActivoBaja`, `AsignarActivo`, `AsignarHerramienta`, `DevolverHerramienta` |
| `CampoController` | `VisitaCambiarEstado`, `AjustarStock`, `RegistrarConsumo` |

## Cambios realizados

En cada uno de los 4 controladores se agregó un helper privado `JsonError(Exception ex, string
accion)` (e inyección de `ILogger<TController>` donde no existía todavía — `UsersController` ya
lo tenía desde A1):

```csharp
private IActionResult JsonError(Exception ex, string accion)
{
    if (ex is InvalidOperationException)
        return Json(new { success = false, message = ex.Message });

    _logger.LogError(ex, "Error inesperado en {Controller}.{Accion}", "...", accion);
    return Json(new { success = false, message = "Ocurrió un error inesperado. Intenta de nuevo más tarde." });
}
```

**Criterio para decidir qué se muestra**: se revisó primero qué excepciones lanza realmente la
capa de servicios (`grep` de `throw new InvalidOperationException` en `Infrastructure/Services`)
— son **~30 sitios**, todos con mensajes fijos y seguros pensados para el usuario final ("Ticket
no encontrado", "Usuario no encontrado", "La herramienta no está disponible para asignación",
etc.), sin interpolar SQL, nombres de columna, ni datos sensibles. Ese es el único tipo de
excepción que se sigue devolviendo tal cual al cliente. Cualquier otra excepción — `NHibernate`,
`SqlException`, `GenericADOException`, errores de conectividad, etc. — se loggea completa con
`ILogger` y al cliente solo le llega el mensaje genérico. Esto preserva el comportamiento actual
de la UI (los mensajes de validación de negocio que ya se mostraban siguen viéndose igual) sin
seguir filtrando detalles de infraestructura cuando algo revienta de verdad.

Los 13 call sites se actualizaron a `return JsonError(ex, nameof(NombreDeLaAccion));`, excepto
`UsersController.Edit`, que devuelve `errors` (array) en vez de `message` (string) — se dejó su
propio bloque `catch` con la misma lógica de "InvalidOperationException pasa, el resto se loggea
y se generaliza", para no cambiar la forma del JSON que ya consume esa vista.

## Archivos modificados

- `SistemaTickets/Controllers/UsersController.cs` — helper `JsonError`, aplicado en
  `Deactivate`; `Edit` con su propio bloque (shape `errors`).
- `SistemaTickets/Controllers/TicketsController.cs` — `ILogger<TicketsController>` + helper
  `JsonError`, aplicado en 6 acciones.
- `SistemaTickets/Controllers/InventarioController.cs` — `ILogger<InventarioController>` +
  helper `JsonError`, aplicado en 4 acciones.
- `SistemaTickets/Controllers/CampoController.cs` — `ILogger<CampoController>` + helper
  `JsonError`, aplicado en 3 acciones.

No se creó una abstracción compartida entre controladores (p. ej. una clase base común): cada
uno ya vive de forma independiente sin un controlador base, y el helper es solo ~8 líneas —
agregar una jerarquía nueva solo para esto era más cambio estructural del que pedía el hallazgo.

## Cómo verificar

1. Cualquier acción AJAX de la lista de arriba que falle por una regla de negocio conocida (p.
   ej. cerrar un ticket que no existe) sigue mostrando el mismo mensaje de siempre
   ("Ticket no encontrado").
2. Si en cambio revienta algo inesperado (por ejemplo, una violación de constraint en la BD),
   el cliente ahora recibe "Ocurrió un error inesperado. Intenta de nuevo más tarde." — nunca el
   mensaje crudo de NHibernate/SqlClient — y el detalle completo de la excepción queda en los
   logs del servidor (`ILogger`, nivel `Error`), identificado por controlador y acción.
3. `dotnet build SistemaTickets.sln` — compila limpio, 0 errores.

## Limitaciones conocidas

- Esto cubre los controladores MVC (que es donde estaba la evidencia y donde vive toda la
  superficie HTTP de la app). No hay una capa de API REST separada en este proyecto.
- Si en el futuro se agrega un nuevo endpoint AJAX con `catch (Exception ex)`, hay que acordarse
  de usar `JsonError` (o el mismo criterio) en vez de volver a exponer `ex.Message` directo —
  esto no está enforced por un analizador estático, solo por convención de código.
