using NHibernate;
using NHibernate.Linq;
using SistemaTickets.Domain.Entities;
using SistemaTickets.Domain.Repositories;

namespace SistemaTickets.Infrastructure.Persistence.Repositories
{
    public class TicketRepository : ITicketRepository
    {
        private readonly NHibernate.ISession _session;

        public TicketRepository(NHibernate.ISession session)
        {
            _session = session;
        }

        public async Task<Ticket?> GetByIdAsync(int id)
            => await _session.GetAsync<Ticket>(id);

        public async Task<(IList<Ticket> Items, int Total)> GetPagedAsync(
            int page, int pageSize,
            string? search,
            int? estadoId,
            int? prioridadId,
            int? categoriaId,
            int? creadoPorId,
            int? asignadoAId)
        {
            var query = _session.Query<Ticket>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t =>
                    t.Titulo.Contains(search) ||
                    t.Descripcion.Contains(search));

            if (estadoId.HasValue)
                query = query.Where(t => t.Estado.Id == estadoId.Value);

            if (prioridadId.HasValue)
                query = query.Where(t => t.Prioridad.Id == prioridadId.Value);

            if (categoriaId.HasValue)
                query = query.Where(t => t.Categoria != null && t.Categoria.Id == categoriaId.Value);

            if (creadoPorId.HasValue)
                query = query.Where(t => t.CreadoPor.Id == creadoPorId.Value);

            if (asignadoAId.HasValue)
                query = query.Where(t => t.AsignadoA != null && t.AsignadoA.Id == asignadoAId.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(t => t.FechaCreacion)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<IList<Ticket>> GetActivosParaSeguimientoAsync()
        {
            // Trae todos los tickets sin FechaCierre con las relaciones necesarias
            IQueryable<Ticket> q = _session.Query<Ticket>()
                .Where(t => t.FechaCierre == null);

            // NHibernate: fetch many-to-one en pasos separados para evitar cartesianos
            var tickets = await q
                .Fetch(t => t.Estado)
                .Fetch(t => t.Prioridad)
                .OrderBy(t => t.FechaCreacion)
                .ToListAsync();

            // Cargar AsignadoA por separado (puede ser null)
            foreach (var t in tickets)
            {
                if (t.AsignadoA != null)
                    await NHibernateUtil.InitializeAsync(t.AsignadoA);
            }

            return tickets;
        }

        public async Task<IList<Ticket>> GetTicketsNuevosAsync()
        {
            IQueryable<Ticket> q = _session.Query<Ticket>()
                .Where(t => t.AsignadoA == null && t.FechaCierre == null);

            return await q
                .Fetch(t => t.Estado)
                .Fetch(t => t.Prioridad)
                .Fetch(t => t.CreadoPor)
                .OrderBy(t => t.FechaCreacion)
                .ToListAsync();
        }

        public async Task<int> SaveAsync(Ticket ticket)
        {
            using var tx = _session.BeginTransaction();
            var id = (int)await _session.SaveAsync(ticket);
            await tx.CommitAsync();
            return id;
        }

        public async Task UpdateAsync(Ticket ticket)
        {
            using var tx = _session.BeginTransaction();
            await _session.MergeAsync(ticket);
            await tx.CommitAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var ticket = await _session.GetAsync<Ticket>(id)
                ?? throw new InvalidOperationException("Ticket no encontrado");
            using var tx = _session.BeginTransaction();
            await _session.DeleteAsync(ticket);
            await tx.CommitAsync();
        }

        public async Task<IList<TicketEstado>> GetEstadosAsync()
            => await _session.Query<TicketEstado>()
                .Where(e => e.Activo)
                .OrderBy(e => e.Orden)
                .ToListAsync();

        public async Task<IList<TicketPrioridad>> GetPrioridadesAsync()
            => await _session.Query<TicketPrioridad>()
                .Where(p => p.Activo)
                .OrderBy(p => p.Orden)
                .ToListAsync();

        public async Task<IList<TicketCategoria>> GetCategoriasAsync()
            => await _session.Query<TicketCategoria>()
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

        public async Task<TicketEstado?> GetEstadoByNombreAsync(string nombre)
            => await _session.Query<TicketEstado>()
                .FirstOrDefaultAsync(e => e.Nombre == nombre);

        public async Task SaveComentarioAsync(TicketComentario comentario)
        {
            using var tx = _session.BeginTransaction();
            await _session.SaveAsync(comentario);
            await tx.CommitAsync();
        }

        public async Task SaveAdjuntoAsync(TicketAdjunto adjunto)
        {
            using var tx = _session.BeginTransaction();
            await _session.SaveAsync(adjunto);
            await tx.CommitAsync();
        }

        public async Task<TicketAdjunto?> GetAdjuntoByIdAsync(int id)
            => await _session.Query<TicketAdjunto>()
                .Fetch(a => a.Ticket)
                .Where(a => a.Id == id)
                .FirstOrDefaultAsync();

        public async Task SaveHistorialAsync(TicketHistorial historial)
        {
            using var tx = _session.BeginTransaction();
            await _session.SaveAsync(historial);
            await tx.CommitAsync();
        }

        public async Task<Usuario?> GetUsuarioAsync(int id)
            => await _session.GetAsync<Usuario>(id);

        public T GetRef<T>(int id) where T : class
            => _session.Load<T>(id);

        // Admin — catálogos
        public async Task<IList<TicketCategoria>> GetAllCategoriasAdminAsync()
            => await _session.Query<TicketCategoria>()
                .OrderBy(c => c.Nombre)
                .ToListAsync();

        public async Task<IList<TicketPrioridad>> GetAllPrioridadesAdminAsync()
            => await _session.Query<TicketPrioridad>()
                .OrderBy(p => p.Orden).ThenBy(p => p.Nombre)
                .ToListAsync();

        public async Task<IList<TicketEstado>> GetAllEstadosAdminAsync()
            => await _session.Query<TicketEstado>()
                .OrderBy(e => e.Orden).ThenBy(e => e.Nombre)
                .ToListAsync();

        public async Task<TicketCategoria?> GetCategoriaByIdAsync(int id)
            => await _session.GetAsync<TicketCategoria>(id);

        public async Task<TicketPrioridad?> GetPrioridadByIdAsync(int id)
            => await _session.GetAsync<TicketPrioridad>(id);

        public async Task<TicketEstado?> GetEstadoByIdAsync(int id)
            => await _session.GetAsync<TicketEstado>(id);

        public async Task SaveCategoriaAsync(TicketCategoria c)
        {
            using var tx = _session.BeginTransaction();
            await _session.SaveAsync(c);
            await tx.CommitAsync();
        }

        public async Task UpdateCategoriaAsync(TicketCategoria c)
        {
            using var tx = _session.BeginTransaction();
            await _session.MergeAsync(c);
            await tx.CommitAsync();
        }

        public async Task SavePrioridadAsync(TicketPrioridad p)
        {
            using var tx = _session.BeginTransaction();
            await _session.SaveAsync(p);
            await tx.CommitAsync();
        }

        public async Task UpdatePrioridadAsync(TicketPrioridad p)
        {
            using var tx = _session.BeginTransaction();
            await _session.MergeAsync(p);
            await tx.CommitAsync();
        }

        public async Task UpdateEstadoAsync(TicketEstado e)
        {
            using var tx = _session.BeginTransaction();
            await _session.MergeAsync(e);
            await tx.CommitAsync();
        }
    }
}
