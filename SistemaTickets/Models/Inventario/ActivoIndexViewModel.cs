using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Models.Inventario
{
    public class ActivoIndexViewModel
    {
        public IList<Activo> Activos       { get; set; } = new List<Activo>();
        public int  TotalRegistros         { get; set; }
        public int  PaginaActual           { get; set; }
        public int  TotalPaginas           { get; set; }
        public string? Search              { get; set; }
        public string? FiltroTipo          { get; set; }
        public string? FiltroEstado        { get; set; }
        public IList<ActivoTipo> TiposActivo { get; set; } = new List<ActivoTipo>();

        public const int PageSize = 10;
        public int DesdeRegistro => TotalRegistros == 0 ? 0 : (PaginaActual - 1) * PageSize + 1;
        public int HastaRegistro => Math.Min(PaginaActual * PageSize, TotalRegistros);

        public static readonly string[] Estados = { "Disponible", "Asignado", "En Mantenimiento", "Baja" };
    }
}
