namespace SistemaTickets.Models.Reportes
{
    public class ReporteFiltro
    {
        public string   Periodo     { get; set; } = "mes";
        public DateTime? Desde      { get; set; }
        public DateTime? Hasta      { get; set; }
        public int?     EstadoId    { get; set; }
        public int?     PrioridadId { get; set; }
        public int?     CategoriaId { get; set; }
        public int?     ClienteId   { get; set; }
        public int?     TecnicoId   { get; set; }
        public int?     TicketId    { get; set; }

        public (DateTime desde, DateTime hasta) ResolverFechas()
        {
            var hoy = DateTime.Now.Date;
            return Periodo switch
            {
                "dia"    => (hoy, hoy.AddDays(1).AddTicks(-1)),
                "semana" => (hoy.AddDays(-6), hoy.AddDays(1).AddTicks(-1)),
                "mes"    => (new DateTime(hoy.Year, hoy.Month, 1),
                             new DateTime(hoy.Year, hoy.Month, 1).AddMonths(1).AddTicks(-1)),
                "anio"   => (new DateTime(hoy.Year, 1, 1),
                             new DateTime(hoy.Year, 12, 31, 23, 59, 59)),
                _        => (Desde ?? hoy.AddDays(-30),
                             Hasta.HasValue ? Hasta.Value.Date.AddDays(1).AddTicks(-1)
                                           : hoy.AddDays(1).AddTicks(-1))
            };
        }
    }
}
