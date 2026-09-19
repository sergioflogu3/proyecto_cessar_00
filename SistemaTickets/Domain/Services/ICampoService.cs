using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Domain.Services
{
    public interface ICampoService
    {
        // ── Visitas ──────────────────────────────────────────────────────────
        Task<VisitaTecnica?> GetVisitaByIdAsync(int id);
        Task<(IList<VisitaTecnica> Items, int Total)> GetVisitasPagedAsync(
            int page, int pageSize, string? search, string? estado, int? tecnicoId);
        Task<int> CreateVisitaAsync(VisitaTecnica visita, int creadoPorId);
        Task UpdateVisitaAsync(VisitaTecnica visita);
        Task CambiarEstadoVisitaAsync(int visitaId, string nuevoEstado);

        // ── Repuestos ─────────────────────────────────────────────────────────
        Task<Repuesto?> GetRepuestoByIdAsync(int id);
        Task<(IList<Repuesto> Items, int Total)> GetRepuestosPagedAsync(
            int page, int pageSize, string? search, string? categoria);
        Task<int> CreateRepuestoAsync(Repuesto repuesto);
        Task UpdateRepuestoAsync(Repuesto repuesto);

        // ── Stock ─────────────────────────────────────────────────────────────
        Task<IList<StockRepuesto>> GetStockByRepuestoAsync(int repuestoId);
        Task<IList<StockRepuesto>> GetStockBajoMinimoAsync();
        Task AjustarStockAsync(int repuestoId, int sedeId, int cantidad, bool esEntrada);

        // ── Consumos ──────────────────────────────────────────────────────────
        Task RegistrarConsumoAsync(ConsumoRepuesto consumo, int? sedeId);
        Task<IList<ConsumoRepuesto>> GetConsumosByTicketAsync(int ticketId);
        Task<IList<ConsumoRepuesto>> GetConsumosByVisitaAsync(int visitaId);
    }
}
