namespace SistemaTickets.Models.Dashboard
{
    public class DashboardViewModel
    {
        // KPI cards
        public int TicketsAbiertos       { get; set; }
        public int TicketsCerradosHoy    { get; set; }
        public int TicketsSinAsignar     { get; set; }
        public double TiempoPromedioHoras { get; set; }

        // Charts
        public List<ChartItem> PorEstado    { get; set; } = new();
        public List<ChartItem> PorPrioridad { get; set; } = new();
        public List<ChartItem> PorCategoria { get; set; } = new();
        public List<ChartItem> PorTecnico   { get; set; } = new();
        public List<MesItem>   PorMes       { get; set; } = new();

        // Campo / repuestos
        public List<ChartItem> VisitasPorEstado  { get; set; } = new();
        public int             StockBajoMinimoCount { get; set; }
        public List<string>    StockAlertas      { get; set; } = new();

        // Meta
        public bool    EsAdminOSupervisor { get; set; }
        public string? NombreTecnico      { get; set; }
    }

    public class ChartItem
    {
        public string Label { get; set; } = "";
        public int    Value { get; set; }
        public string Color { get; set; } = "";
    }

    public class MesItem
    {
        public string Label   { get; set; } = "";
        public int    Creados  { get; set; }
        public int    Cerrados { get; set; }
    }
}
