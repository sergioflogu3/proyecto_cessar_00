using SistemaTickets.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace SistemaTickets.Models.Tickets
{
    public class TicketFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El título es obligatorio")]
        [StringLength(200, ErrorMessage = "Máximo 200 caracteres")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria")]
        public string Descripcion { get; set; } = string.Empty;

        public int? PrioridadId { get; set; }

        public int? CategoriaId { get; set; }

        public int? AsignadoAId { get; set; }

        public int? CreadoPorId { get; set; }

        public DateTime? FechaVencimiento { get; set; }

        public bool EnviarCopia { get; set; } = true;

        // Archivos adjuntos opcionales (máx 10 MB c/u)
        public List<IFormFile>? Archivos { get; set; }

        // Catálogos para los selects
        public IList<TicketPrioridad> Prioridades { get; set; } = new List<TicketPrioridad>();
        public IList<TicketCategoria> Categorias  { get; set; } = new List<TicketCategoria>();
        public IList<Domain.Entities.Usuario> Agentes   { get; set; } = new List<Domain.Entities.Usuario>();
        public IList<Domain.Entities.Usuario> Clientes  { get; set; } = new List<Domain.Entities.Usuario>();

        public bool EsEdicion => Id > 0;
    }
}
