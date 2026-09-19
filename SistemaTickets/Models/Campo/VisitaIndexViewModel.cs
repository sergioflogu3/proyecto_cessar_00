using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Models.Campo
{
    public class VisitaIndexViewModel
    {
        public IList<VisitaTecnica> Visitas { get; set; } = new List<VisitaTecnica>();
        public int  TotalRegistros { get; set; }
        public int  PaginaActual   { get; set; }
        public int  TotalPaginas   { get; set; }
        public string? Search      { get; set; }
        public string? FiltroEstado { get; set; }

        public const int PageSize = 10;
        public int DesdeRegistro => TotalRegistros == 0 ? 0 : (PaginaActual - 1) * PageSize + 1;
        public int HastaRegistro => Math.Min(PaginaActual * PageSize, TotalRegistros);
    }
}
