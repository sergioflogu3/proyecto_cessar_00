namespace SistemaTickets.Domain.Entities
{
    public class TicketAdjunto
    {
        public virtual int     Id             { get; set; }
        public virtual Ticket  Ticket         { get; set; } = null!;
        public virtual Usuario SubidoPor      { get; set; } = null!;
        public virtual string  NombreArchivo  { get; set; } = string.Empty;
        public virtual string  RutaArchivo    { get; set; } = string.Empty;
        public virtual string? TipoContenido  { get; set; }
        public virtual long    TamanioBytes   { get; set; }
        public virtual DateTime FechaSubida   { get; set; }
    }
}
