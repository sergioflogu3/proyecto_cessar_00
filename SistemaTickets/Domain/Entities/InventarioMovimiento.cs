namespace SistemaTickets.Domain.Entities
{
    public class InventarioMovimiento
    {
        public virtual int         Id              { get; set; }
        public virtual Activo?     Activo          { get; set; }
        public virtual Herramienta? Herramienta    { get; set; }
        public virtual string      TipoMovimiento  { get; set; } = string.Empty;
        public virtual Usuario     Usuario         { get; set; } = null!;
        public virtual DateTime    FechaMovimiento { get; set; }
        public virtual string?     Descripcion     { get; set; }
        public virtual string?     ValorAnterior   { get; set; }
        public virtual string?     ValorNuevo      { get; set; }
    }
}
