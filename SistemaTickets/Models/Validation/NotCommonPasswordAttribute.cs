using System.ComponentModel.DataAnnotations;
using SistemaTickets.Infrastructure.Security;

namespace SistemaTickets.Models.Validation
{
    // M2: rechaza contraseñas que están en la lista de contraseñas más comunes/filtradas.
    // No valida vacío/nulo a propósito — eso ya lo cubre [Required] donde corresponda, y en
    // los formularios de edición (contraseña opcional) un valor null debe seguir siendo válido.
    public class NotCommonPasswordAttribute : ValidationAttribute
    {
        public NotCommonPasswordAttribute()
            : base("Esa contraseña es demasiado común/fácil de adivinar. Elegí otra.")
        {
        }

        public override bool IsValid(object? value)
        {
            if (value is not string password || string.IsNullOrEmpty(password))
                return true;

            return !PasswordPolicy.EsContraseñaComun(password);
        }
    }
}
