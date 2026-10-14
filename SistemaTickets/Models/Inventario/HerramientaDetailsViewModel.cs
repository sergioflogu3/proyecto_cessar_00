namespace SistemaTickets.Models.Inventario
{
    public class HerramientaDetailsViewModel
    {
        public int      Id          { get; set; }
        public string   Nombre      { get; set; } = string.Empty;
        public string?  Descripcion { get; set; }
        public string?  Codigo      { get; set; }
        public string   Tipo        { get; set; } = string.Empty;
        public string   Estado      { get; set; } = string.Empty;
        public DateTime CreadoEn    { get; set; }

        public IList<HerramientaAsignacionItem> Asignaciones { get; set; } = new List<HerramientaAsignacionItem>();
        public IList<HerramientaMovimientoItem> Movimientos  { get; set; } = new List<HerramientaMovimientoItem>();
    }

    public class HerramientaAsignacionItem
    {
        public bool      Activa                    { get; set; }
        public DateTime  FechaAsignacion           { get; set; }
        public DateTime? FechaDevolucion           { get; set; }
        public string?   Observaciones             { get; set; }
        public string    TecnicoNombreCompleto     { get; set; } = string.Empty;
        public string    AsignadoPorNombreCompleto { get; set; } = string.Empty;
    }

    public class HerramientaMovimientoItem
    {
        public string   TipoMovimiento        { get; set; } = string.Empty;
        public string?  Descripcion           { get; set; }
        public string?  ValorAnterior         { get; set; }
        public string?  ValorNuevo            { get; set; }
        public DateTime FechaMovimiento       { get; set; }
        public string   UsuarioNombre         { get; set; } = string.Empty;
        public string   UsuarioApellido       { get; set; } = string.Empty;
        public string   UsuarioNombreCompleto { get; set; } = string.Empty;
    }
}
