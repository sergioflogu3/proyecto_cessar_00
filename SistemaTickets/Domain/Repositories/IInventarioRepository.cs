using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Domain.Repositories
{
    public interface IInventarioRepository
    {
        // ── Activos ──────────────────────────────────────────────────────────
        Task<Activo?> GetActivoByIdAsync(int id);
        Task<(IList<Activo> Items, int Total)> GetActivosPagedAsync(
            int page, int pageSize, string? search, string? tipo, string? estado);
        Task<int>  SaveActivoAsync(Activo activo);
        Task UpdateActivoAsync(Activo activo);

        // ── Herramientas ─────────────────────────────────────────────────────
        Task<Herramienta?> GetHerramientaByIdAsync(int id);
        Task<(IList<Herramienta> Items, int Total)> GetHerramientasPagedAsync(
            int page, int pageSize, string? search, string? tipo, string? estado);
        Task<int>  SaveHerramientaAsync(Herramienta herramienta);
        Task UpdateHerramientaAsync(Herramienta herramienta);

        // ── Asignaciones ─────────────────────────────────────────────────────
        Task<AsignacionHerramienta?> GetAsignacionActivaAsync(int herramientaId);
        Task<IList<AsignacionHerramienta>> GetAsignacionesByTecnicoAsync(int tecnicoId);
        Task SaveAsignacionAsync(AsignacionHerramienta asignacion);
        Task UpdateAsignacionAsync(AsignacionHerramienta asignacion);

        // ── Movimientos ──────────────────────────────────────────────────────
        Task SaveMovimientoAsync(InventarioMovimiento movimiento);

        // ── Catálogos (solo activos, para dropdowns) ─────────────────────────
        Task<IList<Sede>>      GetSedesAsync();
        Task<IList<Area>>      GetAreasAsync(int? sedeId = null);
        Task<IList<ActivoTipo>> GetActivoTiposAsync();

        // ── Catálogos admin (incluye inactivos) ──────────────────────────────
        Task<IList<ActivoTipo>> GetAllActivoTiposAdminAsync();
        Task<ActivoTipo?>       GetActivoTipoByIdAsync(int id);
        Task SaveActivoTipoAsync(ActivoTipo tipo);
        Task UpdateActivoTipoAsync(ActivoTipo tipo);

        Task<IList<Sede>> GetAllSedesAdminAsync();
        Task<Sede?>       GetSedeByIdAsync(int id);
        Task SaveSedeAsync(Sede sede);
        Task UpdateSedeAsync(Sede sede);

        Task<IList<Area>> GetAllAreasAdminAsync();
        Task<Area?>       GetAreaByIdAsync(int id);
        Task SaveAreaAsync(Area area);
        Task UpdateAreaAsync(Area area);

        T GetRef<T>(int id) where T : class;
    }
}
