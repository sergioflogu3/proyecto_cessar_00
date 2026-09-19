using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Models.Users
{
    public class UsuarioIndexViewModel
    {
        public IList<Usuario> Usuarios  { get; set; } = new List<Usuario>();
        public int  TotalRegistros      { get; set; }
        public int  PaginaActual        { get; set; } = 1;
        public int  TotalPaginas        { get; set; }
        public string? Search           { get; set; }
        public bool?   FiltroActivo     { get; set; }
        public const int PageSize       = 10;

        public int DesdeRegistro => TotalRegistros == 0 ? 0 : (PaginaActual - 1) * PageSize + 1;
        public int HastaRegistro => Math.Min(PaginaActual * PageSize, TotalRegistros);
    }
}
