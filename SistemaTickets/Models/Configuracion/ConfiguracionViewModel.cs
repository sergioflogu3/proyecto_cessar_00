using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Models.Configuracion
{
    public class ConfiguracionViewModel
    {
        public string TabActiva { get; set; } = "categorias";

        // Tickets
        public IList<TicketCategoria> Categorias  { get; set; } = new List<TicketCategoria>();
        public IList<TicketPrioridad> Prioridades { get; set; } = new List<TicketPrioridad>();
        public IList<TicketEstado>    Estados      { get; set; } = new List<TicketEstado>();

        // Inventario
        public IList<ActivoTipo> ActivoTipos { get; set; } = new List<ActivoTipo>();
        public IList<Sede>       Sedes       { get; set; } = new List<Sede>();
        public IList<Area>       Areas       { get; set; } = new List<Area>();
    }
}
