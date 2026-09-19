using SistemaTickets.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace SistemaTickets.Models.Inventario
{
    public class ActivoFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "El tipo es obligatorio")]
        public string Tipo { get; set; } = string.Empty;

        [StringLength(100)]
        public string? NumeroSerie { get; set; }

        [StringLength(100)]
        public string? Marca { get; set; }

        [StringLength(100)]
        public string? Modelo { get; set; }

        public string Estado { get; set; } = "Disponible";

        public int? SedeId { get; set; }
        public int? AreaId { get; set; }
        public int? AsignadoAId { get; set; }

        public DateTime? FechaAdquisicion { get; set; }
        public DateTime? FechaGarantia    { get; set; }

        // Catálogos
        public IList<ActivoTipo> TiposActivo { get; set; } = new List<ActivoTipo>();
        public IList<Sede>       Sedes       { get; set; } = new List<Sede>();
        public IList<Area>       Areas       { get; set; } = new List<Area>();
        public IList<Usuario>    Usuarios    { get; set; } = new List<Usuario>();

        public static readonly string[] Estados = ActivoIndexViewModel.Estados;

        public bool EsEdicion => Id > 0;
    }
}
