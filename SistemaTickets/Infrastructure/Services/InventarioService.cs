using SistemaTickets.Domain.Entities;
using SistemaTickets.Domain.Repositories;
using SistemaTickets.Domain.Services;

namespace SistemaTickets.Infrastructure.Services
{
    public class InventarioService : IInventarioService
    {
        private readonly IInventarioRepository _repo;

        public InventarioService(IInventarioRepository repo)
        {
            _repo = repo;
        }

        // ── Activos ──────────────────────────────────────────────────────────

        public Task<Activo?> GetActivoByIdAsync(int id)
            => _repo.GetActivoByIdAsync(id);

        public Task<(IList<Activo> Items, int Total)> GetActivosPagedAsync(
            int page, int pageSize, string? search, string? tipo, string? estado)
            => _repo.GetActivosPagedAsync(page, pageSize, search, tipo, estado);

        public async Task<int> CreateActivoAsync(Activo activo, int creadoPorId)
        {
            activo.CreadoEn  = DateTime.UtcNow;
            activo.Estado    = "Disponible";
            activo.EsActivo  = true;
            var id = await _repo.SaveActivoAsync(activo);

            await _repo.SaveMovimientoAsync(new InventarioMovimiento
            {
                Activo          = _repo.GetRef<Activo>(id),
                TipoMovimiento  = "Registro",
                Usuario         = _repo.GetRef<Usuario>(creadoPorId),
                FechaMovimiento = DateTime.UtcNow,
                Descripcion     = $"Activo registrado: {activo.Nombre}"
            });
            return id;
        }

        public async Task UpdateActivoAsync(Activo activo, int modificadoPorId)
        {
            var existing = await _repo.GetActivoByIdAsync(activo.Id)
                ?? throw new InvalidOperationException("Activo no encontrado");

            var estadoAnterior = existing.Estado;

            existing.Nombre           = activo.Nombre;
            existing.Descripcion      = activo.Descripcion;
            existing.Tipo             = activo.Tipo;
            existing.NumeroSerie      = activo.NumeroSerie;
            existing.Marca            = activo.Marca;
            existing.Modelo           = activo.Modelo;
            existing.Estado           = activo.Estado;
            existing.Sede             = activo.Sede;
            existing.Area             = activo.Area;
            existing.AsignadoA        = activo.AsignadoA;
            existing.FechaAdquisicion = activo.FechaAdquisicion;
            existing.FechaGarantia    = activo.FechaGarantia;

            await _repo.UpdateActivoAsync(existing);

            if (estadoAnterior != activo.Estado)
            {
                await _repo.SaveMovimientoAsync(new InventarioMovimiento
                {
                    Activo          = _repo.GetRef<Activo>(existing.Id),
                    TipoMovimiento  = "Cambio de estado",
                    Usuario         = _repo.GetRef<Usuario>(modificadoPorId),
                    FechaMovimiento = DateTime.UtcNow,
                    ValorAnterior   = estadoAnterior,
                    ValorNuevo      = activo.Estado
                });
            }
        }

        public async Task BajaActivoAsync(int id, int usuarioId)
        {
            var activo = await _repo.GetActivoByIdAsync(id)
                ?? throw new InvalidOperationException("Activo no encontrado");

            activo.Estado = "Baja";
            await _repo.UpdateActivoAsync(activo);

            await _repo.SaveMovimientoAsync(new InventarioMovimiento
            {
                Activo          = _repo.GetRef<Activo>(id),
                TipoMovimiento  = "Baja",
                Usuario         = _repo.GetRef<Usuario>(usuarioId),
                FechaMovimiento = DateTime.UtcNow,
                Descripcion     = "Activo dado de baja"
            });
        }

        public async Task AsignarActivoAsync(int activoId, int usuarioId, int asignadoPorId)
        {
            var activo = await _repo.GetActivoByIdAsync(activoId)
                ?? throw new InvalidOperationException("Activo no encontrado");

            var anteriorEstado = activo.Estado;
            activo.AsignadoA = _repo.GetRef<Usuario>(usuarioId);
            activo.Estado    = "Asignado";
            await _repo.UpdateActivoAsync(activo);

            await _repo.SaveMovimientoAsync(new InventarioMovimiento
            {
                Activo          = _repo.GetRef<Activo>(activoId),
                TipoMovimiento  = "Asignación",
                Usuario         = _repo.GetRef<Usuario>(asignadoPorId),
                FechaMovimiento = DateTime.UtcNow,
                ValorAnterior   = anteriorEstado,
                ValorNuevo      = $"Asignado a Id:{usuarioId}"
            });
        }

        // ── Herramientas ─────────────────────────────────────────────────────

        public Task<Herramienta?> GetHerramientaByIdAsync(int id)
            => _repo.GetHerramientaByIdAsync(id);

        public Task<(IList<Herramienta> Items, int Total)> GetHerramientasPagedAsync(
            int page, int pageSize, string? search, string? tipo, string? estado)
            => _repo.GetHerramientasPagedAsync(page, pageSize, search, tipo, estado);

        public async Task<int> CreateHerramientaAsync(Herramienta herramienta, int creadoPorId)
        {
            herramienta.CreadoEn = DateTime.UtcNow;
            herramienta.Estado   = "Disponible";
            herramienta.Activa   = true;
            var id = await _repo.SaveHerramientaAsync(herramienta);

            await _repo.SaveMovimientoAsync(new InventarioMovimiento
            {
                Herramienta     = _repo.GetRef<Herramienta>(id),
                TipoMovimiento  = "Registro",
                Usuario         = _repo.GetRef<Usuario>(creadoPorId),
                FechaMovimiento = DateTime.UtcNow,
                Descripcion     = $"Herramienta registrada: {herramienta.Nombre}"
            });
            return id;
        }

        public async Task UpdateHerramientaAsync(Herramienta herramienta, int modificadoPorId)
        {
            var existing = await _repo.GetHerramientaByIdAsync(herramienta.Id)
                ?? throw new InvalidOperationException("Herramienta no encontrada");

            existing.Nombre      = herramienta.Nombre;
            existing.Descripcion = herramienta.Descripcion;
            existing.Codigo      = herramienta.Codigo;
            existing.Tipo        = herramienta.Tipo;

            await _repo.UpdateHerramientaAsync(existing);
        }

        public async Task AsignarHerramientaAsync(int herramientaId, int tecnicoId, int asignadoPorId, string? observaciones)
        {
            var herramienta = await _repo.GetHerramientaByIdAsync(herramientaId)
                ?? throw new InvalidOperationException("Herramienta no encontrada");

            if (herramienta.Estado != "Disponible")
                throw new InvalidOperationException("La herramienta no está disponible para asignación.");

            herramienta.Estado = "Asignada";
            await _repo.UpdateHerramientaAsync(herramienta);

            await _repo.SaveAsignacionAsync(new AsignacionHerramienta
            {
                Herramienta     = _repo.GetRef<Herramienta>(herramientaId),
                Tecnico         = _repo.GetRef<Usuario>(tecnicoId),
                AsignadoPor     = _repo.GetRef<Usuario>(asignadoPorId),
                FechaAsignacion = DateTime.UtcNow,
                Observaciones   = observaciones,
                Activa          = true
            });

            await _repo.SaveMovimientoAsync(new InventarioMovimiento
            {
                Herramienta     = _repo.GetRef<Herramienta>(herramientaId),
                TipoMovimiento  = "Asignación",
                Usuario         = _repo.GetRef<Usuario>(asignadoPorId),
                FechaMovimiento = DateTime.UtcNow,
                ValorAnterior   = "Disponible",
                ValorNuevo      = $"Asignada a Id:{tecnicoId}"
            });
        }

        public async Task DevolverHerramientaAsync(int herramientaId, int usuarioId, string? observaciones)
        {
            var herramienta = await _repo.GetHerramientaByIdAsync(herramientaId)
                ?? throw new InvalidOperationException("Herramienta no encontrada");

            var asignacion = await _repo.GetAsignacionActivaAsync(herramientaId)
                ?? throw new InvalidOperationException("No hay asignación activa para esta herramienta.");

            asignacion.FechaDevolucion = DateTime.UtcNow;
            asignacion.Activa          = false;
            if (!string.IsNullOrWhiteSpace(observaciones))
                asignacion.Observaciones = observaciones;

            await _repo.UpdateAsignacionAsync(asignacion);

            herramienta.Estado = "Disponible";
            await _repo.UpdateHerramientaAsync(herramienta);

            await _repo.SaveMovimientoAsync(new InventarioMovimiento
            {
                Herramienta     = _repo.GetRef<Herramienta>(herramientaId),
                TipoMovimiento  = "Devolución",
                Usuario         = _repo.GetRef<Usuario>(usuarioId),
                FechaMovimiento = DateTime.UtcNow,
                ValorAnterior   = "Asignada",
                ValorNuevo      = "Disponible"
            });
        }

        public Task<IList<AsignacionHerramienta>> GetHerramientasByTecnicoAsync(int tecnicoId)
            => _repo.GetAsignacionesByTecnicoAsync(tecnicoId);

        // ── Catálogos (dropdowns) ────────────────────────────────────────────

        public Task<IList<Sede>>      GetSedesAsync() => _repo.GetSedesAsync();
        public Task<IList<Area>>      GetAreasAsync(int? sedeId = null) => _repo.GetAreasAsync(sedeId);
        public Task<IList<ActivoTipo>> GetActivoTiposAsync() => _repo.GetActivoTiposAsync();

        // ── Admin: Tipos de Activo ───────────────────────────────────────────

        public Task<IList<ActivoTipo>> GetAllActivoTiposAdminAsync() => _repo.GetAllActivoTiposAdminAsync();

        public async Task CrearActivoTipoAsync(string nombre)
            => await _repo.SaveActivoTipoAsync(new ActivoTipo { Nombre = nombre, Activo = true });

        public async Task EditarActivoTipoAsync(int id, string nombre)
        {
            var t = await _repo.GetActivoTipoByIdAsync(id)
                ?? throw new InvalidOperationException("Tipo no encontrado");
            t.Nombre = nombre;
            await _repo.UpdateActivoTipoAsync(t);
        }

        public async Task ToggleActivoTipoAsync(int id)
        {
            var t = await _repo.GetActivoTipoByIdAsync(id)
                ?? throw new InvalidOperationException("Tipo no encontrado");
            t.Activo = !t.Activo;
            await _repo.UpdateActivoTipoAsync(t);
        }

        // ── Admin: Sedes ─────────────────────────────────────────────────────

        public Task<IList<Sede>> GetAllSedesAdminAsync() => _repo.GetAllSedesAdminAsync();

        public async Task CrearSedeAsync(string nombre, string ciudad, string? direccion)
            => await _repo.SaveSedeAsync(new Sede
            {
                Nombre    = nombre,
                Ciudad    = ciudad,
                Direccion = direccion,
                Activa    = true
            });

        public async Task EditarSedeAsync(int id, string nombre, string ciudad, string? direccion)
        {
            var s = await _repo.GetSedeByIdAsync(id)
                ?? throw new InvalidOperationException("Sede no encontrada");
            s.Nombre    = nombre;
            s.Ciudad    = ciudad;
            s.Direccion = direccion;
            await _repo.UpdateSedeAsync(s);
        }

        public async Task ToggleSedeAsync(int id)
        {
            var s = await _repo.GetSedeByIdAsync(id)
                ?? throw new InvalidOperationException("Sede no encontrada");
            s.Activa = !s.Activa;
            await _repo.UpdateSedeAsync(s);
        }

        // ── Admin: Áreas ─────────────────────────────────────────────────────

        public Task<IList<Area>> GetAllAreasAdminAsync() => _repo.GetAllAreasAdminAsync();

        public async Task CrearAreaAsync(string nombre, int sedeId)
            => await _repo.SaveAreaAsync(new Area
            {
                Nombre = nombre,
                Sede   = _repo.GetRef<Sede>(sedeId),
                Activa = true
            });

        public async Task EditarAreaAsync(int id, string nombre, int sedeId)
        {
            var a = await _repo.GetAreaByIdAsync(id)
                ?? throw new InvalidOperationException("Área no encontrada");
            a.Nombre = nombre;
            a.Sede   = _repo.GetRef<Sede>(sedeId);
            await _repo.UpdateAreaAsync(a);
        }

        public async Task ToggleAreaAsync(int id)
        {
            var a = await _repo.GetAreaByIdAsync(id)
                ?? throw new InvalidOperationException("Área no encontrada");
            a.Activa = !a.Activa;
            await _repo.UpdateAreaAsync(a);
        }
    }
}
