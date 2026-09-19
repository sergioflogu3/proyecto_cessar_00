namespace SistemaTickets.Domain.Entities
{
    public class ConsumoRepuesto
    {
        public virtual int           Id            { get; set; }
        public virtual Repuesto      Repuesto      { get; set; } = null!;
        public virtual Ticket?       Ticket        { get; set; }
        public virtual VisitaTecnica? Visita       { get; set; }
        public virtual Usuario       Tecnico       { get; set; } = null!;
        public virtual int           Cantidad      { get; set; }
        public virtual DateTime      FechaConsumo  { get; set; }
        public virtual string?       Observaciones { get; set; }
    }
}
