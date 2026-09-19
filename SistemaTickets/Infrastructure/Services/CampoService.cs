using SistemaTickets.Domain.Entities;
using SistemaTickets.Domain.Repositories;
using SistemaTickets.Domain.Services;

namespace SistemaTickets.Infrastructure.Services
{
    public class CampoService : ICampoService
    {
        private readonly ICampoRepository _repo;

        public CampoService(ICampoRepository repo)
        {
            _repo = repo;
        }

        // ── Visitas ──────────────────────────────────────────────────────────

        public Task<VisitaTecnica?> GetVisitaByIdAsync(int id)
            => _repo.GetVisitaByIdAsync(id);

        public Task<(IList<VisitaTecnica> Items, int Total)> GetVisitasPagedAsync(
            int page, int pageSize, string? search, string? estado, int? tecnicoId)
            => _repo.GetVisitasPagedAsync(page, pageSize, search, estado, tecnicoId);

        public async Task<int> CreateVisitaAsync(VisitaTecnica visita, int creadoPorId)
        {
            visita.FechaCreacion = DateTime.UtcNow;
            visita.Estado        = "Programada";
            visita.Tecnico       = _repo.GetRef<Usuario>(creadoPorId);
            return await _repo.SaveVisitaAsync(visita);
        }

        public async Task UpdateVisitaAsync(VisitaTecnica visita)
        {
            var existing = await _repo.GetVisitaByIdAsync(visita.Id)
                ?? throw new InvalidOperationException("Visita no encontrada");

            existing.FechaVisita   = visita.FechaVisita;
            existing.Direccion     = visita.Direccion;
            existing.Descripcion   = visita.Descripcion;
            existing.Observaciones = visita.Observaciones;
            existing.Ticket        = visita.Ticket;

            await _repo.UpdateVisitaAsync(existing);
        }

        public async Task CambiarEstadoVisitaAsync(int visitaId, string nuevoEstado)
        {
            var visita = await _repo.GetVisitaByIdAsync(visitaId)
                ?? throw new InvalidOperationException("Visita no encontrada");

            visita.Estado = nuevoEstado;
            await _repo.UpdateVisitaAsync(visita);
        }

        // ── Repuestos ─────────────────────────────────────────────────────────

        public Task<Repuesto?> GetRepuestoByIdAsync(int id)
            => _repo.GetRepuestoByIdAsync(id);

        public Task<(IList<Repuesto> Items, int Total)> GetRepuestosPagedAsync(
            int page, int pageSize, string? search, string? categoria)
            => _repo.GetRepuestosPagedAsync(page, pageSize, search, categoria);

        public async Task<int> CreateRepuestoAsync(Repuesto repuesto)
        {
            repuesto.CreadoEn = DateTime.UtcNow;
            repuesto.Activo   = true;
            return await _repo.SaveRepuestoAsync(repuesto);
        }

        public async Task UpdateRepuestoAsync(Repuesto repuesto)
        {
            var existing = await _repo.GetRepuestoByIdAsync(repuesto.Id)
                ?? throw new InvalidOperationException("Repuesto no encontrado");

            existing.Nombre         = repuesto.Nombre;
            existing.Descripcion    = repuesto.Descripcion;
            existing.Codigo         = repuesto.Codigo;
            existing.Categoria      = repuesto.Categoria;
            existing.UnidadMedida   = repuesto.UnidadMedida;
            existing.PrecioUnitario = repuesto.PrecioUnitario;

            await _repo.UpdateRepuestoAsync(existing);
        }

        // ── Stock ─────────────────────────────────────────────────────────────

        public Task<IList<StockRepuesto>> GetStockByRepuestoAsync(int repuestoId)
            => _repo.GetStockByRepuestoAsync(repuestoId);

        public Task<IList<StockRepuesto>> GetStockBajoMinimoAsync()
            => _repo.GetStockBajoMinimoAsync();

        public async Task AjustarStockAsync(int repuestoId, int sedeId, int cantidad, bool esEntrada)
        {
            var stock = await _repo.GetStockAsync(repuestoId, sedeId);

            if (stock is null)
            {
                stock = new StockRepuesto
                {
                    Repuesto           = _repo.GetRef<Repuesto>(repuestoId),
                    Sede               = _repo.GetRef<Sede>(sedeId),
                    CantidadActual     = esEntrada ? cantidad : 0,
                    CantidadMinima     = 5,
                    FechaActualizacion = DateTime.UtcNow
                };
                await _repo.SaveStockAsync(stock);
            }
            else
            {
                stock.CantidadActual     = esEntrada
                    ? stock.CantidadActual + cantidad
                    : Math.Max(0, stock.CantidadActual - cantidad);
                stock.FechaActualizacion = DateTime.UtcNow;
                await _repo.UpdateStockAsync(stock);
            }
        }

        // ── Consumos ──────────────────────────────────────────────────────────

        public async Task RegistrarConsumoAsync(ConsumoRepuesto consumo, int? sedeId)
        {
            consumo.FechaConsumo = DateTime.UtcNow;
            await _repo.SaveConsumoAsync(consumo);

            // Descontar stock si se especificó sede
            if (sedeId.HasValue)
                await AjustarStockAsync(consumo.Repuesto.Id, sedeId.Value, consumo.Cantidad, esEntrada: false);
        }

        public Task<IList<ConsumoRepuesto>> GetConsumosByTicketAsync(int ticketId)
            => _repo.GetConsumosByTicketAsync(ticketId);

        public Task<IList<ConsumoRepuesto>> GetConsumosByVisitaAsync(int visitaId)
            => _repo.GetConsumosByVisitaAsync(visitaId);
    }
}
