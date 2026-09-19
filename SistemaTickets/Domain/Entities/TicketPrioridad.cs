namespace SistemaTickets.Domain.Entities
{
    public class TicketPrioridad
    {
        public virtual int    Id          { get; set; }
        public virtual string Nombre      { get; set; } = string.Empty;
        public virtual string? Descripcion { get; set; }
        public virtual string Color       { get; set; } = "#6c757d";
        public virtual int    Orden       { get; set; }
        public virtual bool   Activo      { get; set; } = true;
    }
}
