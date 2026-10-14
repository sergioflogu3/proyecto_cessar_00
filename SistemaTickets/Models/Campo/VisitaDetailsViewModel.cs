namespace SistemaTickets.Models.Campo
{
    public class VisitaDetailsViewModel
    {
        public int      Id                    { get; set; }
        public string   Direccion             { get; set; } = string.Empty;
        public string   Estado                { get; set; } = string.Empty;
        public string?  Descripcion           { get; set; }
        public string?  Observaciones         { get; set; }
        public DateTime FechaVisita           { get; set; }
        public DateTime FechaCreacion         { get; set; }
        public int      TecnicoId             { get; set; }
        public string   TecnicoNombreCompleto { get; set; } = string.Empty;
        public int?     TicketId              { get; set; }

        public IList<string> EstadosPosibles { get; set; } = new List<string>();
        public IList<VisitaConsumoItem> Consumos { get; set; } = new List<VisitaConsumoItem>();
    }

    public class VisitaConsumoItem
    {
        public string   RepuestoNombre        { get; set; } = string.Empty;
        public string?  RepuestoCodigo        { get; set; }
        public string   RepuestoUnidadMedida  { get; set; } = string.Empty;
        public DateTime FechaConsumo          { get; set; }
        public string   TecnicoNombreCompleto { get; set; } = string.Empty;
        public string?  Observaciones         { get; set; }
        public int      Cantidad              { get; set; }
    }
}
