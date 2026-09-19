using System.ComponentModel.DataAnnotations;

namespace SistemaTickets.Models.Campo
{
    public class RepuestoFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [StringLength(50)]
        public string? Codigo { get; set; }

        [Required(ErrorMessage = "La categoría es obligatoria")]
        public string Categoria { get; set; } = string.Empty;

        [Required(ErrorMessage = "La unidad de medida es obligatoria")]
        public string UnidadMedida { get; set; } = "Unidad";

        [Range(0, 9999999, ErrorMessage = "Precio inválido")]
        public decimal PrecioUnitario { get; set; }

        public bool EsEdicion => Id > 0;
    }
}
