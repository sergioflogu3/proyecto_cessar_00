using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Models.Reportes
{
    public class ReporteIndexViewModel
    {
        public ReporteFiltro          Filtro      { get; set; } = new();
        public IList<TicketEstado>    Estados     { get; set; } = new List<TicketEstado>();
        public IList<TicketPrioridad> Prioridades { get; set; } = new List<TicketPrioridad>();
        public IList<TicketCategoria> Categorias  { get; set; } = new List<TicketCategoria>();
        public IList<Usuario>         Tecnicos    { get; set; } = new List<Usuario>();
        public IList<Usuario>         Clientes    { get; set; } = new List<Usuario>();
    }
}
