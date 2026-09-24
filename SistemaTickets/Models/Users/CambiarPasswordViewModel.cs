using System.ComponentModel.DataAnnotations;
using SistemaTickets.Models.Validation;

namespace SistemaTickets.Models.Users
{
    public class CambiarPasswordViewModel
    {
        [Required]
        public string PasswordActual { get; set; } = "";

        [Required, MinLength(12, ErrorMessage = "La nueva contraseña debe tener al menos 12 caracteres.")]
        [PasswordComplexity]
        [NotCommonPassword]
        public string NuevoPassword { get; set; } = "";

        [Required, Compare(nameof(NuevoPassword), ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarPassword { get; set; } = "";
    }
}
