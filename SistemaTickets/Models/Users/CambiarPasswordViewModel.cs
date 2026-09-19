using System.ComponentModel.DataAnnotations;

namespace SistemaTickets.Models.Users
{
    public class CambiarPasswordViewModel
    {
        [Required]
        public string PasswordActual { get; set; } = "";

        [Required, MinLength(6)]
        public string NuevoPassword { get; set; } = "";

        [Required, Compare(nameof(NuevoPassword), ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarPassword { get; set; } = "";
    }
}
