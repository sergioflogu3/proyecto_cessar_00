using SistemaTickets.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace SistemaTickets.Models.Campo
{
    public class VisitaFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La dirección es obligatoria")]
        [StringLength(300)]
        public string Direccion { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de visita es obligatoria")]
        public DateTime FechaVisita { get; set; } = DateTime.Today;

        [StringLength(1000)]
        public string? Descripcion { get; set; }

        [StringLength(1000)]
        public string? Observaciones { get; set; }

        public int? TicketId { get; set; }

        // Catálogos
        public IList<Ticket> Tickets { get; set; } = new List<Ticket>();

        public bool EsEdicion => Id > 0;
    }
}
