namespace SistemaTickets.Models.Supervisor
{
    public class SeguimientoViewModel
    {
        public List<TecnicoRow>    PorTecnico   { get; set; } = new();
        public List<PrioridadRow>  PorPrioridad { get; set; } = new();
        public List<string>        Estados      { get; set; } = new();
        public int TotalActivos   { get; set; }
        public int SinAsignar     { get; set; }
    }

    public class TecnicoRow
    {
        public string Nombre  { get; set; } = string.Empty;
        public int    Total   { get; set; }
        public Dictionary<string, int> PorEstado { get; set; } = new();
    }

    public class PrioridadRow
    {
        public string Nombre { get; set; } = string.Empty;
        public string Color  { get; set; } = "#6c757d";
        public int    Total  { get; set; }
    }
}
