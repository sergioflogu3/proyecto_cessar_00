namespace SistemaTickets.Models.Inventario
{
    public class ActivoDetailsViewModel
    {
        public int      Id               { get; set; }
        public string   Nombre           { get; set; } = string.Empty;
        public string?  Descripcion      { get; set; }
        public string   Tipo             { get; set; } = string.Empty;
        public string   Estado           { get; set; } = string.Empty;
        public string?  NumeroSerie      { get; set; }
        public string?  Marca            { get; set; }
        public string?  Modelo           { get; set; }
        public string?  SedeNombre       { get; set; }
        public string?  AreaNombre       { get; set; }
        public string?  AsignadoANombreCompleto { get; set; }
        public DateTime? FechaAdquisicion { get; set; }
        public DateTime? FechaGarantia    { get; set; }
        public DateTime  CreadoEn         { get; set; }

        public IList<ActivoMovimientoItem> Movimientos { get; set; } = new List<ActivoMovimientoItem>();
    }

    public class ActivoMovimientoItem
    {
        public string   TipoMovimiento       { get; set; } = string.Empty;
        public string?  Descripcion          { get; set; }
        public string?  ValorAnterior        { get; set; }
        public string?  ValorNuevo           { get; set; }
        public DateTime FechaMovimiento      { get; set; }
        public string   UsuarioNombre        { get; set; } = string.Empty;
        public string   UsuarioApellido      { get; set; } = string.Empty;
        public string   UsuarioNombreCompleto { get; set; } = string.Empty;
    }
}
