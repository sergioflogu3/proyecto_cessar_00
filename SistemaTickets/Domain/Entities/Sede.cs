namespace SistemaTickets.Domain.Entities
{
    public class Sede
    {
        public virtual int     Id        { get; set; }
        public virtual string  Nombre    { get; set; } = string.Empty;
        public virtual string  Ciudad    { get; set; } = string.Empty;
        public virtual string? Direccion { get; set; }
        public virtual bool    Activa    { get; set; } = true;
    }
}
