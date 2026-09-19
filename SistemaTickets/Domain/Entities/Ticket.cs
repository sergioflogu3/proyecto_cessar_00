namespace SistemaTickets.Domain.Entities
{
    public class Ticket
    {
        public virtual int    Id                   { get; set; }
        public virtual string Titulo               { get; set; } = string.Empty;
        public virtual string Descripcion          { get; set; } = string.Empty;
        public virtual DateTime  FechaCreacion     { get; set; }
        public virtual DateTime  FechaActualizacion { get; set; }
        public virtual DateTime? FechaCierre        { get; set; }
        public virtual DateTime? FechaVencimiento   { get; set; }

        public virtual TicketEstado    Estado    { get; set; } = null!;
        public virtual TicketPrioridad? Prioridad { get; set; }
        public virtual TicketCategoria? Categoria { get; set; }
        public virtual Usuario          CreadoPor { get; set; } = null!;
        public virtual Usuario?         AsignadoA { get; set; }

        public virtual IList<TicketComentario> Comentarios { get; set; } = new List<TicketComentario>();
        public virtual IList<TicketAdjunto>    Adjuntos    { get; set; } = new List<TicketAdjunto>();
        public virtual IList<TicketHistorial>  Historial   { get; set; } = new List<TicketHistorial>();

        public virtual bool EstaAbierto  => Estado?.Nombre is "Abierto" or "En Progreso" or "Pendiente";
        public virtual bool EstaCerrado  => Estado?.Nombre is "Cerrado" or "Cancelado" or "Resuelto";
    }
}
