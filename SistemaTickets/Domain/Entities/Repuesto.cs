namespace SistemaTickets.Domain.Entities
{
    public class Repuesto
    {
        public virtual int      Id             { get; set; }
        public virtual string   Nombre         { get; set; } = string.Empty;
        public virtual string?  Descripcion    { get; set; }
        public virtual string?  Codigo         { get; set; }
        public virtual string   Categoria      { get; set; } = string.Empty;
        public virtual string   UnidadMedida   { get; set; } = "Unidad";
        public virtual decimal  PrecioUnitario { get; set; }
        public virtual bool     Activo         { get; set; } = true;
        public virtual DateTime CreadoEn       { get; set; }

        public virtual IList<StockRepuesto>   Stocks   { get; set; } = new List<StockRepuesto>();
        public virtual IList<ConsumoRepuesto> Consumos { get; set; } = new List<ConsumoRepuesto>();

        public static readonly string[] Categorias    = { "Hardware", "Cable", "Periférico", "Consumible", "Otro" };
        public static readonly string[] UnidadesMedida = { "Unidad", "Caja", "Metro", "Litro" };
    }
}
