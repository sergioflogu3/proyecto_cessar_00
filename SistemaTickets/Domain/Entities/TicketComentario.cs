namespace SistemaTickets.Domain.Entities
{
    public class TicketComentario
    {
        public virtual int     Id            { get; set; }
        public virtual Ticket  Ticket        { get; set; } = null!;
        public virtual Usuario Usuario       { get; set; } = null!;
        public virtual string  Contenido     { get; set; } = string.Empty;
        public virtual bool    EsInterno     { get; set; }
        public virtual DateTime FechaCreacion { get; set; }
    }
}
