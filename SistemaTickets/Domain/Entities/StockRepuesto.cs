namespace SistemaTickets.Domain.Entities
{
    public class StockRepuesto
    {
        public virtual int      Id                 { get; set; }
        public virtual Repuesto Repuesto           { get; set; } = null!;
        public virtual Sede     Sede               { get; set; } = null!;
        public virtual int      CantidadActual     { get; set; }
        public virtual int      CantidadMinima     { get; set; }
        public virtual DateTime FechaActualizacion { get; set; }

        public virtual bool BajoMinimo => CantidadActual < CantidadMinima;
    }
}
