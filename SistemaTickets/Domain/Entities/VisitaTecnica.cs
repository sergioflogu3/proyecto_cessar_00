namespace SistemaTickets.Domain.Entities
{
    public class VisitaTecnica
    {
        public virtual int       Id            { get; set; }
        public virtual Ticket?   Ticket        { get; set; }
        public virtual Usuario   Tecnico       { get; set; } = null!;
        public virtual DateTime  FechaVisita   { get; set; }
        public virtual string    Direccion     { get; set; } = string.Empty;
        public virtual string?   Descripcion   { get; set; }
        public virtual string    Estado        { get; set; } = "Programada";
        public virtual string?   Observaciones { get; set; }
        public virtual DateTime  FechaCreacion { get; set; }

        public virtual IList<ConsumoRepuesto> Consumos { get; set; }
            = new List<ConsumoRepuesto>();

        public static readonly string[] Estados =
            { "Programada", "En Curso", "Completada", "Cancelada" };
    }
}
