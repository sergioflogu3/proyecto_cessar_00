using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Models.Inventario
{
    public class HerramientaIndexViewModel
    {
        public IList<Herramienta> Herramientas { get; set; } = new List<Herramienta>();
        public int  TotalRegistros             { get; set; }
        public int  PaginaActual               { get; set; }
        public int  TotalPaginas               { get; set; }
        public string? Search                  { get; set; }
        public string? FiltroTipo              { get; set; }
        public string? FiltroEstado            { get; set; }

        public const int PageSize = 10;
        public int DesdeRegistro => TotalRegistros == 0 ? 0 : (PaginaActual - 1) * PageSize + 1;
        public int HastaRegistro => Math.Min(PaginaActual * PageSize, TotalRegistros);

        public static readonly string[] Tipos   = { "Hardware", "Diagnóstico", "Red", "Eléctrico", "Otro" };
        public static readonly string[] Estados = { "Disponible", "Asignada", "En Reparación", "Baja" };
    }
}
