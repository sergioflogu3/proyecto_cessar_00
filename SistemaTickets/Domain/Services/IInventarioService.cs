using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Domain.Services
{
    public interface IInventarioService
    {
        // ── Activos ──────────────────────────────────────────────────────────
        Task<Activo?> GetActivoByIdAsync(int id);
        Task<(IList<Activo> Items, int Total)> GetActivosPagedAsync(
            int page, int pageSize, string? search, string? tipo, string? estado);
        Task<int> CreateActivoAsync(Activo activo, int creadoPorId);
        Task UpdateActivoAsync(Activo activo, int modificadoPorId);
        Task BajaActivoAsync(int id, int usuarioId);
        Task AsignarActivoAsync(int activoId, int usuarioId, int asignadoPorId);

        // ── Herramientas ─────────────────────────────────────────────────────
        Task<Herramienta?> GetHerramientaByIdAsync(int id);
        Task<(IList<Herramienta> Items, int Total)> GetHerramientasPagedAsync(
            int page, int pageSize, string? search, string? tipo, string? estado);
        Task<int> CreateHerramientaAsync(Herramienta herramienta, int creadoPorId);
        Task UpdateHerramientaAsync(Herramienta herramienta, int modificadoPorId);
        Task AsignarHerramientaAsync(int herramientaId, int tecnicoId, int asignadoPorId, string? observaciones);
        Task DevolverHerramientaAsync(int herramientaId, int usuarioId, string? observaciones);
        Task<IList<AsignacionHerramienta>> GetHerramientasByTecnicoAsync(int tecnicoId);

        // ── Catálogos (dropdowns) ────────────────────────────────────────────
        Task<IList<Sede>>      GetSedesAsync();
        Task<IList<Area>>      GetAreasAsync(int? sedeId = null);
        Task<IList<ActivoTipo>> GetActivoTiposAsync();

        // ── Admin: Tipos de Activo ───────────────────────────────────────────
        Task<IList<ActivoTipo>> GetAllActivoTiposAdminAsync();
        Task CrearActivoTipoAsync(string nombre);
        Task EditarActivoTipoAsync(int id, string nombre);
        Task ToggleActivoTipoAsync(int id);

        // ── Admin: Sedes ─────────────────────────────────────────────────────
        Task<IList<Sede>> GetAllSedesAdminAsync();
        Task CrearSedeAsync(string nombre, string ciudad, string? direccion);
        Task EditarSedeAsync(int id, string nombre, string ciudad, string? direccion);
        Task ToggleSedeAsync(int id);

        // ── Admin: Áreas ─────────────────────────────────────────────────────
        Task<IList<Area>> GetAllAreasAdminAsync();
        Task CrearAreaAsync(string nombre, int sedeId);
        Task EditarAreaAsync(int id, string nombre, int sedeId);
        Task ToggleAreaAsync(int id);
    }
}
