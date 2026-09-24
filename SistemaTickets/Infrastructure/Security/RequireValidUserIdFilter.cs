using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace SistemaTickets.Infrastructure.Security
{
    // M4: hasta ahora, si el claim NameIdentifier faltaba o no era un entero válido, los
    // controladores caían a `int.Parse(... ?? "0")` y seguían ejecutando la acción con
    // UsuarioId=0 — registros huérfanos en vez de rechazar la petición. Este filtro corre antes
    // de cualquier acción (salvo las marcadas [AllowAnonymous], como Login) y corta con 401 si
    // el claim no está o no parsea a un int, para que ningún controller tenga que resolverlo por
    // su cuenta.
    public class RequireValidUserIdFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any())
                return;

            var claim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claim, out _))
                context.Result = new Microsoft.AspNetCore.Mvc.UnauthorizedResult();
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
