namespace SistemaTickets.Domain.Entities
{
    public class Area
    {
        public virtual int    Id     { get; set; }
        public virtual string Nombre { get; set; } = string.Empty;
        public virtual Sede   Sede   { get; set; } = null!;
        public virtual bool   Activa { get; set; } = true;
    }
}
