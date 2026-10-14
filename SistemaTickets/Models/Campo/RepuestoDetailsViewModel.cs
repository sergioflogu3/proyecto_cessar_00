namespace SistemaTickets.Models.Campo
{
    public class RepuestoDetailsViewModel
    {
        public int      Id             { get; set; }
        public string   Nombre         { get; set; } = string.Empty;
        public string?  Codigo         { get; set; }
        public string   Categoria      { get; set; } = string.Empty;
        public string   UnidadMedida   { get; set; } = string.Empty;
        public string?  Descripcion    { get; set; }
        public decimal  PrecioUnitario { get; set; }

        public IList<RepuestoStockItem>   Stocks   { get; set; } = new List<RepuestoStockItem>();
        public IList<RepuestoSedeItem>    Sedes    { get; set; } = new List<RepuestoSedeItem>();
        public IList<RepuestoConsumoItem> Consumos { get; set; } = new List<RepuestoConsumoItem>();
    }

    public class RepuestoStockItem
    {
        public string SedeNombre     { get; set; } = string.Empty;
        public int    CantidadMinima { get; set; }
        public int    CantidadActual { get; set; }
        public bool   BajoMinimo     { get; set; }
    }

    public class RepuestoSedeItem
    {
        public int    Id     { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class RepuestoConsumoItem
    {
        public string   TecnicoNombreCompleto { get; set; } = string.Empty;
        public DateTime FechaConsumo          { get; set; }
        public int      Cantidad              { get; set; }
        public string?  Observaciones         { get; set; }
        public int?     VisitaId              { get; set; }
        public int?     TicketId              { get; set; }
    }
}
