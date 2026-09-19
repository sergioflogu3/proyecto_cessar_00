using NHibernate;
using NHibernate.Linq;
using SistemaTickets.Domain.Entities;
using SistemaTickets.Domain.Enums;
using SistemaTickets.Domain.Repositories;
using System.Security.Cryptography.Xml;

namespace SistemaTickets.Infrastructure.Persistence.Repositories
{
    public class ReporteRepository : IReporteRepository
    {
        private readonly NHibernate.ISession _session;

        public ReporteRepository(NHibernate.ISession session)
        {
            _session = session;
        }
        public async Task<IList<Ticket>> GetTicketsAsync(
            DateTime desde, DateTime hasta,
            int? estadoId, int? prioridadId, int? categoriaId,
            int? clienteId, int? tecnicoId)
        {
            var q = _session.Query<Ticket>()
                .Where(t => t.FechaCreacion >= desde && t.FechaCreacion <= hasta);

            if (estadoId.HasValue)    q = q.Where(t => t.Estado.Id == estadoId.Value);
            if (prioridadId.HasValue) q = q.Where(t => t.Prioridad != null && t.Prioridad.Id == prioridadId.Value);
            if (categoriaId.HasValue) q = q.Where(t => t.Categoria != null && t.Categoria.Id == categoriaId.Value);
            if (clienteId.HasValue)   q = q.Where(t => t.CreadoPor.Id == clienteId.Value);
            if (tecnicoId.HasValue)   q = q.Where(t => t.AsignadoA != null && t.AsignadoA.Id == tecnicoId.Value);

            var tickets = await q
                .Fetch(t => t.Estado)
                .Fetch(t => t.CreadoPor)
                .OrderByDescending(t => t.FechaCreacion)
                .ToListAsync();

            foreach (var t in tickets)
            {
                if (t.Prioridad != null) await NHibernateUtil.InitializeAsync(t.Prioridad);
                if (t.Categoria != null) await NHibernateUtil.InitializeAsync(t.Categoria);
                if (t.AsignadoA != null) await NHibernateUtil.InitializeAsync(t.AsignadoA);
            }

            return tickets;
        }

        public async Task<IList<TicketHistorial>> GetHistorialAsync(
            DateTime desde, DateTime hasta, int? ticketId)
        {
            var q = _session.Query<TicketHistorial>()
                .Where(h => h.FechaAccion >= desde && h.FechaAccion <= hasta);

            if (ticketId.HasValue) q = q.Where(h => h.Ticket.Id == ticketId.Value);

            return await q
                .Fetch(h => h.Ticket)
                .Fetch(h => h.Usuario)
                .OrderBy(h => h.FechaAccion)
                .ToListAsync();
        }

        public async Task<IList<TicketComentario>> GetComentariosAsync(
            DateTime desde, DateTime hasta)
        {
            return await _session.Query<TicketComentario>()
                .Where(c => c.FechaCreacion >= desde && c.FechaCreacion <= hasta)
                .Fetch(c => c.Ticket)
                .Fetch(c => c.Usuario)
                .OrderBy(c => c.FechaCreacion)
                .ToListAsync();
        }

        public async Task<IList<TicketAdjunto>> GetAdjuntosAsync(
            DateTime desde, DateTime hasta)
        {
            return await _session.Query<TicketAdjunto>()
                .Where(a => a.FechaSubida >= desde && a.FechaSubida <= hasta)
                .Fetch(a => a.Ticket)
                .Fetch(a => a.SubidoPor)
                .OrderBy(a => a.FechaSubida)
                .ToListAsync();
        }

        public async Task<IList<Usuario>> GetTodosUsuariosAsync()
            => await _session.Query<Usuario>()
                .OrderBy(u => u.Nombre).ThenBy(u => u.Apellido)
                .ToListAsync();

        public async Task<IList<Usuario>> GetTecnicosAsync()
            => await _session.Query<Usuario>()
                .Where(u => u.Rol == RolUsuario.Soporte
                         || u.Rol == RolUsuario.Supervisor
                         || u.Rol == RolUsuario.Administrador)
                .OrderBy(u => u.Nombre).ThenBy(u => u.Apellido)
                .ToListAsync();

        public async Task<IList<Usuario>> GetClientesAsync()
            => await _session.Query<Usuario>()
                .Where(u => u.Rol == RolUsuario.Usuario && u.Activo)
                .OrderBy(u => u.Nombre).ThenBy(u => u.Apellido)
                .ToListAsync();
    }
}
