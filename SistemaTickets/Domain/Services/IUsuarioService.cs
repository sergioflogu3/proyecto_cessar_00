using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Domain.Services
{
    public interface IUsuarioService
    {
        Task<Usuario?> GetByIdAsync(int id);
        Task<Usuario?> GetByEmailAsync(string email);
        Task<Usuario?> GetByUsernameAsync(string username);
        Task<Usuario?> GetByTelefonoAsync(string telefono);
        Task<Usuario?> GetByLoginAsync(string login);
        Task<bool>     ValidateCredentialsAsync(string login, string password);

        Task<(IList<Usuario> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search, bool? activo);
        Task CreateAsync(Usuario usuario, string plainPassword);
        Task UpdateAsync(Usuario usuario, string? newPlainPassword);
        Task DeactivateAsync(int id);
        Task<bool> CambiarPasswordAsync(int userId, string passwordActual, string nuevoPassword);
    }
}
