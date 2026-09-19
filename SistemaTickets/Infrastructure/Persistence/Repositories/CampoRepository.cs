using NHibernate.Linq;
using SistemaTickets.Domain.Entities;
using SistemaTickets.Domain.Repositories;

namespace SistemaTickets.Infrastructure.Persistence.Repositories
{
    public class CampoRepository : ICampoRepository
    {
        private readonly NHibernate.ISession _session;

        public CampoRepository(NHibernate.ISession session)
        {
            _session = session;
        }

        // ── Visitas ──────────────────────────────────────────────────────────

        public async Task<VisitaTecnica?> GetVisitaByIdAsync(int id)
            => await _session.GetAsync<VisitaTecnica>(id);

        public async Task<(IList<VisitaTecnica> Items, int Total)> GetVisitasPagedAsync(
            int page, int pageSize, string? search, string? estado, int? tecnicoId)
        {
            var q = _session.Query<VisitaTecnica>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(v => v.Direccion.Contains(search)
                              || (v.Descripcion != null && v.Descripcion.Contains(search)));

            if (!string.IsNullOrWhiteSpace(estado))
                q = q.Where(v => v.Estado == estado);

            if (tecnicoId.HasValue)
                q = q.Where(v => v.Tecnico.Id == tecnicoId.Value);

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(v => v.FechaVisita)
                               .Skip((page - 1) * pageSize).Take(pageSize)
                               .ToListAsync();
            return (items, total);
        }

        public async Task<int> SaveVisitaAsync(VisitaTecnica visita)
        {
            using var tx = _session.BeginTransaction();
            var id = (int)await _session.SaveAsync(visita);
            await tx.CommitAsync();
            return id;
        }

        public async Task UpdateVisitaAsync(VisitaTecnica visita)
        {
            using var tx = _session.BeginTransaction();
            await _session.MergeAsync(visita);
            await tx.CommitAsync();
        }

        // ── Repuestos ─────────────────────────────────────────────────────────

        public async Task<Repuesto?> GetRepuestoByIdAsync(int id)
            => await _session.GetAsync<Repuesto>(id);

        public async Task<(IList<Repuesto> Items, int Total)> GetRepuestosPagedAsync(
            int page, int pageSize, string? search, string? categoria)
        {
            var q = _session.Query<Repuesto>().Where(r => r.Activo);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(r => r.Nombre.Contains(search)
                              || (r.Codigo != null && r.Codigo.Contains(search)));

            if (!string.IsNullOrWhiteSpace(categoria))
                q = q.Where(r => r.Categoria == categoria);

            var total = await q.CountAsync();
            var items = await q.OrderBy(r => r.Nombre)
                               .Skip((page - 1) * pageSize).Take(pageSize)
                               .ToListAsync();
            return (items, total);
        }

        public async Task<int> SaveRepuestoAsync(Repuesto repuesto)
        {
            using var tx = _session.BeginTransaction();
            var id = (int)await _session.SaveAsync(repuesto);
            await tx.CommitAsync();
            return id;
        }

        public async Task UpdateRepuestoAsync(Repuesto repuesto)
        {
            using var tx = _session.BeginTransaction();
            await _session.MergeAsync(repuesto);
            await tx.CommitAsync();
        }

        // ── Stock ─────────────────────────────────────────────────────────────

        public async Task<StockRepuesto?> GetStockAsync(int repuestoId, int sedeId)
            => await _session.Query<StockRepuesto>()
                .FirstOrDefaultAsync(s => s.Repuesto.Id == repuestoId && s.Sede.Id == sedeId);

        public async Task<IList<StockRepuesto>> GetStockByRepuestoAsync(int repuestoId)
            => await _session.Query<StockRepuesto>()
                .Where(s => s.Repuesto.Id == repuestoId)
                .OrderBy(s => s.Sede.Nombre)
                .ToListAsync();

        public async Task<IList<StockRepuesto>> GetStockBajoMinimoAsync()
            => await _session.Query<StockRepuesto>()
                .Where(s => s.CantidadActual < s.CantidadMinima)
                .OrderBy(s => s.Repuesto.Nombre)
                .ToListAsync();

        public async Task SaveStockAsync(StockRepuesto stock)
        {
            using var tx = _session.BeginTransaction();
            await _session.SaveAsync(stock);
            await tx.CommitAsync();
        }

        public async Task UpdateStockAsync(StockRepuesto stock)
        {
            using var tx = _session.BeginTransaction();
            await _session.MergeAsync(stock);
            await tx.CommitAsync();
        }

        // ── Consumos ──────────────────────────────────────────────────────────

        public async Task SaveConsumoAsync(ConsumoRepuesto consumo)
        {
            using var tx = _session.BeginTransaction();
            await _session.SaveAsync(consumo);
            await tx.CommitAsync();
        }

        public async Task<IList<ConsumoRepuesto>> GetConsumosByTicketAsync(int ticketId)
            => await _session.Query<ConsumoRepuesto>()
                .Where(c => c.Ticket != null && c.Ticket.Id == ticketId)
                .OrderByDescending(c => c.FechaConsumo)
                .ToListAsync();

        public async Task<IList<ConsumoRepuesto>> GetConsumosByVisitaAsync(int visitaId)
            => await _session.Query<ConsumoRepuesto>()
                .Where(c => c.Visita != null && c.Visita.Id == visitaId)
                .OrderByDescending(c => c.FechaConsumo)
                .ToListAsync();

        public T GetRef<T>(int id) where T : class
            => _session.Load<T>(id);
    }
}
