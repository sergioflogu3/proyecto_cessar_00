using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Domain.Repositories
{
    public interface ICampoRepository
    {
        // ── Visitas ──────────────────────────────────────────────────────────
        Task<VisitaTecnica?> GetVisitaByIdAsync(int id);
        Task<(IList<VisitaTecnica> Items, int Total)> GetVisitasPagedAsync(
            int page, int pageSize, string? search, string? estado, int? tecnicoId);
        Task<int>  SaveVisitaAsync(VisitaTecnica visita);
        Task UpdateVisitaAsync(VisitaTecnica visita);

        // ── Repuestos ─────────────────────────────────────────────────────────
        Task<Repuesto?> GetRepuestoByIdAsync(int id);
        Task<(IList<Repuesto> Items, int Total)> GetRepuestosPagedAsync(
            int page, int pageSize, string? search, string? categoria);
        Task<int>  SaveRepuestoAsync(Repuesto repuesto);
        Task UpdateRepuestoAsync(Repuesto repuesto);

        // ── Stock ─────────────────────────────────────────────────────────────
        Task<StockRepuesto?> GetStockAsync(int repuestoId, int sedeId);
        Task<IList<StockRepuesto>> GetStockByRepuestoAsync(int repuestoId);
        Task<IList<StockRepuesto>> GetStockBajoMinimoAsync();
        Task SaveStockAsync(StockRepuesto stock);
        Task UpdateStockAsync(StockRepuesto stock);

        // ── Consumos ──────────────────────────────────────────────────────────
        Task SaveConsumoAsync(ConsumoRepuesto consumo);
        Task<IList<ConsumoRepuesto>> GetConsumosByTicketAsync(int ticketId);
        Task<IList<ConsumoRepuesto>> GetConsumosByVisitaAsync(int visitaId);

        T GetRef<T>(int id) where T : class;
    }
}
