using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Models.Campo
{
    public class RepuestoIndexViewModel
    {
        public IList<Repuesto>      Repuestos    { get; set; } = new List<Repuesto>();
        public IList<StockRepuesto> StockAlerta  { get; set; } = new List<StockRepuesto>();
        public int  TotalRegistros  { get; set; }
        public int  PaginaActual    { get; set; }
        public int  TotalPaginas    { get; set; }
        public string? Search       { get; set; }
        public string? FiltroCategoria { get; set; }

        public const int PageSize = 10;
        public int DesdeRegistro => TotalRegistros == 0 ? 0 : (PaginaActual - 1) * PageSize + 1;
        public int HastaRegistro => Math.Min(PaginaActual * PageSize, TotalRegistros);
    }
}
