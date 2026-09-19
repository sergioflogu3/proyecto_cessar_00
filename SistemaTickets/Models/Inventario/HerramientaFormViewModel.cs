using System.ComponentModel.DataAnnotations;

namespace SistemaTickets.Models.Inventario
{
    public class HerramientaFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [StringLength(50)]
        public string? Codigo { get; set; }

        [Required(ErrorMessage = "El tipo es obligatorio")]
        public string Tipo { get; set; } = string.Empty;

        public static readonly string[] Tipos = HerramientaIndexViewModel.Tipos;

        public bool EsEdicion => Id > 0;
    }
}
