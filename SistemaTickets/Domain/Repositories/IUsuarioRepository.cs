using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Domain.Repositories
{
    public interface IUsuarioRepository
    {
        Task<Usuario?> GetByIdAsync(int id);
        Task<Usuario?> GetByUsernameAsync(string username);
        Task<Usuario?> GetByEmailAsync(string email);
        Task<Usuario?> GetByTelefonoAsync(string telefono);
        Task<(IList<Usuario> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search, bool? activo);
        Task SaveAsync(Usuario usuario);
        Task UpdateAsync(Usuario usuario);
    }
}
