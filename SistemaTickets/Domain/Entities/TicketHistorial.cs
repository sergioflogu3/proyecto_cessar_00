namespace SistemaTickets.Domain.Entities
{
    public class TicketHistorial
    {
        public virtual int      Id             { get; set; }
        public virtual Ticket   Ticket         { get; set; } = null!;
        public virtual Usuario  Usuario        { get; set; } = null!;
        public virtual string   Accion         { get; set; } = string.Empty;
        public virtual string?  ValorAnterior  { get; set; }
        public virtual string?  ValorNuevo     { get; set; }
        public virtual DateTime FechaAccion    { get; set; }
    }
}
