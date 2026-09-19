using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Models.Tickets
{
    public class TicketDetailsViewModel
    {
        public Ticket Ticket { get; set; } = null!;

        public IList<TicketEstado>    Estados     { get; set; } = new List<TicketEstado>();
        public IList<TicketPrioridad> Prioridades { get; set; } = new List<TicketPrioridad>();
        public IList<Domain.Entities.Usuario> Agentes { get; set; } = new List<Domain.Entities.Usuario>();

        public bool PuedeEditar          { get; set; }
        public bool PuedeCambiarEstado   { get; set; }
        public bool PuedeAsignar         { get; set; }
        public bool PuedeAsignarPrioridad { get; set; }
        public bool PuedeCerrar          { get; set; }
        public bool PuedeVerInternos     { get; set; }
    }
}
