using System.ComponentModel.DataAnnotations;
using SistemaTickets.Infrastructure.Security;

namespace SistemaTickets.Models.Validation
{
    // M2: exige minúscula + mayúscula + número + carácter especial.
    // Igual que NotCommonPasswordAttribute, un valor null/vacío se considera válido acá —
    // eso lo cubre [Required] donde corresponda (en edición, la contraseña es opcional).
    public class PasswordComplexityAttribute : ValidationAttribute
    {
        public PasswordComplexityAttribute()
            : base("La contraseña debe incluir al menos una minúscula, una mayúscula, un número y un carácter especial.")
        {
        }

        public override bool IsValid(object? value)
        {
            if (value is not string password || string.IsNullOrEmpty(password))
                return true;

            return PasswordPolicy.TieneComplejidadSuficiente(password);
        }
    }
}
