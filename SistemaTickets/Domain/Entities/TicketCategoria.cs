namespace SistemaTickets.Domain.Entities
{
    public class TicketCategoria
    {
        public virtual int    Id          { get; set; }
        public virtual string Nombre      { get; set; } = string.Empty;
        public virtual string? Descripcion { get; set; }
        public virtual bool   Activo      { get; set; } = true;
    }
}
