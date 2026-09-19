using System.ComponentModel.DataAnnotations;
using SistemaTickets.Domain.Enums;

namespace SistemaTickets.Models.Users
{
    public class UsuarioFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
        [Display(Name = "Nombre de usuario")]
        public string Username { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mínimo 6 caracteres")]
        [Display(Name = "Contraseña")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [StringLength(120)]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        [StringLength(30)]
        [Display(Name = "Teléfono")]
        public string? NumeroTelefono { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(80)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es requerido")]
        [StringLength(80)]
        [Display(Name = "Apellido")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es requerido")]
        [Display(Name = "Rol")]
        public RolUsuario Rol { get; set; } = RolUsuario.Usuario;

        public bool EsEdicion => Id > 0;
    }
}
