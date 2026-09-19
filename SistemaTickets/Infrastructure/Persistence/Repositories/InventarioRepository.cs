using NHibernate.Linq;
using SistemaTickets.Domain.Entities;
using SistemaTickets.Domain.Repositories;

namespace SistemaTickets.Infrastructure.Persistence.Repositories
{
    public class InventarioRepository : IInventarioRepository
    {
        private readonly NHibernate.ISession _session;

        public InventarioRepository(NHibernate.ISession session)
        {
            _session = session;
        }

        // ── Activos ──────────────────────────────────────────────────────────

        public async Task<Activo?> GetActivoByIdAsync(int id)
            => await _session.GetAsync<Activo>(id);

        public async Task<(IList<Activo> Items, int Total)> GetActivosPagedAsync(
            int page, int pageSize, string? search, string? tipo, string? estado)
        {
            var q = _session.Query<Activo>().Where(a => a.EsActivo);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(a => a.Nombre.Contains(search)
                              || (a.NumeroSerie != null && a.NumeroSerie.Contains(search))
                              || (a.Marca != null && a.Marca.Contains(search)));

            if (!string.IsNullOrWhiteSpace(tipo))
                q = q.Where(a => a.Tipo == tipo);

            if (!string.IsNullOrWhiteSpace(estado))
                q = q.Where(a => a.Estado == estado);

            var total = await q.CountAsync();
            var items = await q.OrderBy(a => a.Nombre)
                               .Skip((page - 1) * pageSize).Take(pageSize)
                               .ToListAsync();
            return (items, total);
        }

        public async Task<int> SaveActivoAsync(Activo activo)
        {
            using var tx = _session.BeginTransaction();
            var id = (int)await _session.SaveAsync(activo);
            await tx.CommitAsync();
            return id;
        }

        public async Task UpdateActivoAsync(Activo activo)
        {
            using var tx = _session.BeginTransaction();
            await _session.MergeAsync(activo);
            await tx.CommitAsync();
        }

        // ── Herramientas ─────────────────────────────────────────────────────

        public async Task<Herramienta?> GetHerramientaByIdAsync(int id)
            => await _session.GetAsync<Herramienta>(id);

        public async Task<(IList<Herramienta> Items, int Total)> GetHerramientasPagedAsync(
            int page, int pageSize, string? search, string? tipo, string? estado)
        {
            var q = _session.Query<Herramienta>().Where(h => h.Activa);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(h => h.Nombre.Contains(search)
                              || (h.Codigo != null && h.Codigo.Contains(search)));

            if (!string.IsNullOrWhiteSpace(tipo))
                q = q.Where(h => h.Tipo == tipo);

            if (!string.IsNullOrWhiteSpace(estado))
                q = q.Where(h => h.Estado == estado);

            var total = await q.CountAsync();
            var items = await q.OrderBy(h => h.Nombre)
                               .Skip((page - 1) * pageSize).Take(pageSize)
                               .ToListAsync();
            return (items, total);
        }

        public async Task<int> SaveHerramientaAsync(Herramienta herramienta)
        {
            using var tx = _session.BeginTransaction();
            var id = (int)await _session.SaveAsync(herramienta);
            await tx.CommitAsync();
            return id;
        }

        public async Task UpdateHerramientaAsync(Herramienta herramienta)
        {
            using var tx = _session.BeginTransaction();
            await _session.MergeAsync(herramienta);
            await tx.CommitAsync();
        }

        // ── Asignaciones ─────────────────────────────────────────────────────

        public async Task<AsignacionHerramienta?> GetAsignacionActivaAsync(int herramientaId)
            => await _session.Query<AsignacionHerramienta>()
                .FirstOrDefaultAsync(a => a.Herramienta.Id == herramientaId && a.Activa);

        public async Task<IList<AsignacionHerramienta>> GetAsignacionesByTecnicoAsync(int tecnicoId)
            => await _session.Query<AsignacionHerramienta>()
                .Where(a => a.Tecnico.Id == tecnicoId && a.Activa)
                .OrderBy(a => a.FechaAsignacion)
                .ToListAsync();

        public async Task SaveAsignacionAsync(AsignacionHerramienta asignacion)
        {
            using var tx = _session.BeginTransaction();
            await _session.SaveAsync(asignacion);
            await tx.CommitAsync();
        }

        public async Task UpdateAsignacionAsync(AsignacionHerramienta asignacion)
        {
            using var tx = _session.BeginTransaction();
            await _session.MergeAsync(asignacion);
            await tx.CommitAsync();
        }

        // ── Movimientos ──────────────────────────────────────────────────────

        public async Task SaveMovimientoAsync(InventarioMovimiento movimiento)
        {
            using var tx = _session.BeginTransaction();
            await _session.SaveAsync(movimiento);
            await tx.CommitAsync();
        }

        // ── Catálogos (solo activos) ─────────────────────────────────────────

        public async Task<IList<Sede>> GetSedesAsync()
            => await _session.Query<Sede>().Where(s => s.Activa)
                .OrderBy(s => s.Nombre).ToListAsync();

        public async Task<IList<Area>> GetAreasAsync(int? sedeId = null)
        {
            var q = _session.Query<Area>().Where(a => a.Activa);
            if (sedeId.HasValue) q = q.Where(a => a.Sede.Id == sedeId.Value);
            return await q.OrderBy(a => a.Nombre).ToListAsync();
        }

        public async Task<IList<ActivoTipo>> GetActivoTiposAsync()
            => await _session.Query<ActivoTipo>().Where(t => t.Activo)
                .OrderBy(t => t.Nombre).ToListAsync();

        // ── Catálogos admin ──────────────────────────────────────────────────

        public async Task<IList<ActivoTipo>> GetAllActivoTiposAdminAsync()
            => await _session.Query<ActivoTipo>().OrderBy(t => t.Nombre).ToListAsync();

        public async Task<ActivoTipo?> GetActivoTipoByIdAsync(int id)
            => await _session.GetAsync<ActivoTipo>(id);

        public async Task SaveActivoTipoAsync(ActivoTipo tipo)
        {
            using var tx = _session.BeginTransaction();
            await _session.SaveAsync(tipo);
            await tx.CommitAsync();
        }

        public async Task UpdateActivoTipoAsync(ActivoTipo tipo)
        {
            using var tx = _session.BeginTransaction();
            await _session.MergeAsync(tipo);
            await tx.CommitAsync();
        }

        public async Task<IList<Sede>> GetAllSedesAdminAsync()
            => await _session.Query<Sede>().OrderBy(s => s.Nombre).ToListAsync();

        public async Task<Sede?> GetSedeByIdAsync(int id)
            => await _session.GetAsync<Sede>(id);

        public async Task SaveSedeAsync(Sede sede)
        {
            using var tx = _session.BeginTransaction();
            await _session.SaveAsync(sede);
            await tx.CommitAsync();
        }

        public async Task UpdateSedeAsync(Sede sede)
        {
            using var tx = _session.BeginTransaction();
            await _session.MergeAsync(sede);
            await tx.CommitAsync();
        }

        public async Task<IList<Area>> GetAllAreasAdminAsync()
            => await _session.Query<Area>()
                .Fetch(a => a.Sede)
                .OrderBy(a => a.Sede.Nombre).ThenBy(a => a.Nombre)
                .ToListAsync();

        public async Task<Area?> GetAreaByIdAsync(int id)
            => await _session.GetAsync<Area>(id);

        public async Task SaveAreaAsync(Area area)
        {
            using var tx = _session.BeginTransaction();
            await _session.SaveAsync(area);
            await tx.CommitAsync();
        }

        public async Task UpdateAreaAsync(Area area)
        {
            using var tx = _session.BeginTransaction();
            await _session.MergeAsync(area);
            await tx.CommitAsync();
        }

        public T GetRef<T>(int id) where T : class
            => _session.Load<T>(id);
    }
}
