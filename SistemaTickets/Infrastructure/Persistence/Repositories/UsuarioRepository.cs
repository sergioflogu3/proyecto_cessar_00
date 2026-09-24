using NHibernate;
using NHibernate.Linq;
using SistemaTickets.Domain.Entities;
using SistemaTickets.Domain.Repositories;

namespace SistemaTickets.Infrastructure.Persistence.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly NHibernate.ISession _session;

        public UsuarioRepository(NHibernate.ISession session)
        {
            _session = session;
        }

        public async Task<Usuario?> GetByIdAsync(int id)
            => await _session.GetAsync<Usuario>(id);

        public async Task<Usuario?> GetByUsernameAsync(string username)
            => await _session.Query<Usuario>()
                .FirstOrDefaultAsync(x => x.Username == username);

        public async Task<Usuario?> GetByEmailAsync(string email)
            => await _session.Query<Usuario>()
                .FirstOrDefaultAsync(x => x.Email == email);

        public async Task<Usuario?> GetByTelefonoAsync(string telefono)
            => await _session.Query<Usuario>()
                .FirstOrDefaultAsync(x => x.NumeroTelefono == telefono);

        public async Task<(IList<Usuario> Items, int Total)> GetPagedAsync(
            int page, int pageSize, string? search, bool? activo)
        {
            var query = _session.Query<Usuario>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u =>
                    u.Nombre.Contains(search) ||
                    u.Apellido.Contains(search) ||
                    u.Username.Contains(search));

            if (activo.HasValue)
                query = query.Where(u => u.Activo == activo.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(u => u.Apellido)
                .ThenBy(u => u.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task SaveAsync(Usuario usuario)
        {
            using var tx = _session.BeginTransaction();
            await _session.SaveAsync(usuario);
            await tx.CommitAsync();
        }

        public async Task UpdateAsync(Usuario usuario)
        {
            using var tx = _session.BeginTransaction();
            await _session.MergeAsync(usuario);
            await tx.CommitAsync();
        }

        public async Task RegistrarAuditoriaAsync(AuditoriaUsuario auditoria)
        {
            using var tx = _session.BeginTransaction();
            await _session.SaveAsync(auditoria);
            await tx.CommitAsync();
        }

        public T GetRef<T>(int id) where T : class
            => _session.Load<T>(id);
    }
}
