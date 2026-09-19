using System.ComponentModel.DataAnnotations;

namespace SistemaTickets.Models.Auth
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El usuario, correo o teléfono es obligatorio.")]
        [Display(Name = "Usuario / Correo / Teléfono")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;
    }
}
