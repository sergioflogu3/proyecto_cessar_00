using SistemaTickets.Domain.Enums;

namespace SistemaTickets.Domain.Entities
{
    public class Usuario
    {
        public virtual int Id { get; set; }
        public virtual string Username { get; set; } = string.Empty;
        public virtual string PasswordHaseado { get; set; } = string.Empty;
        public virtual string Email { get; set; } = string.Empty;
        public virtual string? NumeroTelefono { get; set; }
        public virtual string Nombre { get; set; } = string.Empty;
        public virtual string Apellido { get; set; } = string.Empty;
        public virtual bool Activo { get; set; }
        public virtual DateTime CreadoEn { get; set; }
        public virtual RolUsuario Rol { get; set; } = RolUsuario.Usuario;
        public virtual string NombreCompleto => $"{Nombre} {Apellido}";
    }
}
