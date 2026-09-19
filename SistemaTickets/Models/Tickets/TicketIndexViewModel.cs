using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Models.Tickets
{
    public class TicketIndexViewModel
    {
        public IList<Ticket>         Tickets      { get; set; } = new List<Ticket>();
        public IList<TicketEstado>   Estados      { get; set; } = new List<TicketEstado>();
        public IList<TicketPrioridad> Prioridades { get; set; } = new List<TicketPrioridad>();

        public int  TotalRegistros { get; set; }
        public int  PaginaActual   { get; set; }
        public int  TotalPaginas   { get; set; }

        public string? Search       { get; set; }
        public int?    FiltroEstado { get; set; }
        public int?    FiltroPrioridad { get; set; }

        public const int PageSize = 10;

        public int DesdeRegistro => TotalRegistros == 0 ? 0 : (PaginaActual - 1) * PageSize + 1;
        public int HastaRegistro => Math.Min(PaginaActual * PageSize, TotalRegistros);
    }
}
