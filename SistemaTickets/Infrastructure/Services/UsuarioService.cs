using SistemaTickets.Domain.Entities;
using SistemaTickets.Domain.Enums;
using SistemaTickets.Domain.Repositories;
using SistemaTickets.Domain.Services;
using SistemaTickets.Infrastructure.Security;

namespace SistemaTickets.Infrastructure.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly string _encryptionKey;

        public UsuarioService(IUsuarioRepository usuarioRepository, IConfiguration configuration)
        {
            _usuarioRepository = usuarioRepository;
            _encryptionKey = configuration["Encryption:Key"]!;
        }

        // ── Lecturas individuales ────────────────────────────────────────────

        public async Task<Usuario?> GetByIdAsync(int id)
        {
            var user = await _usuarioRepository.GetByIdAsync(id);
            return user is null ? null : ToDecrypted(user);
        }

        public async Task<Usuario?> GetByEmailAsync(string email)
        {
            var encrypted = EncryptionHelper.Encrypt(email, _encryptionKey);
            var user = await _usuarioRepository.GetByEmailAsync(encrypted);
            return user is null ? null : ToDecrypted(user);
        }

        public async Task<Usuario?> GetByUsernameAsync(string username)
        {
            var user = await _usuarioRepository.GetByUsernameAsync(username);
            return user is null ? null : ToDecrypted(user);
        }

        public async Task<Usuario?> GetByTelefonoAsync(string telefono)
        {
            var encrypted = EncryptionHelper.Encrypt(telefono, _encryptionKey);
            var user = await _usuarioRepository.GetByTelefonoAsync(encrypted);
            return user is null ? null : ToDecrypted(user);
        }

        public async Task<Usuario?> GetByLoginAsync(string login)
        {
            if (string.IsNullOrWhiteSpace(login)) return null;

            if (login.Contains('@')) return await GetByEmailAsync(login);

            var byUsername = await GetByUsernameAsync(login);
            if (byUsername is not null) return byUsername;

            return await GetByTelefonoAsync(login);
        }

        public async Task<bool> ValidateCredentialsAsync(string login, string password)
        {
            var user = await GetRawByLoginAsync(login);
            if (user is null || !user.Activo) return false;
            return EncryptionHelper.VerifyPassword(password, user.PasswordHaseado);
        }

        // ── Listado paginado ─────────────────────────────────────────────────

        public async Task<(IList<Usuario> Items, int Total)> GetPagedAsync(
            int page, int pageSize, string? search, bool? activo)
        {
            var (items, total) = await _usuarioRepository.GetPagedAsync(page, pageSize, search, activo);
            return (items.Select(ToDecrypted).ToList(), total);
        }

        // ── Escrituras ───────────────────────────────────────────────────────

        public async Task CreateAsync(Usuario usuario, string plainPassword)
        {
            usuario.PasswordHaseado = EncryptionHelper.HashPassword(plainPassword);
            usuario.Email           = EncryptionHelper.Encrypt(usuario.Email, _encryptionKey);
            if (!string.IsNullOrEmpty(usuario.NumeroTelefono))
                usuario.NumeroTelefono = EncryptionHelper.Encrypt(usuario.NumeroTelefono, _encryptionKey);
            usuario.CreadoEn = DateTime.UtcNow;
            usuario.Activo   = true;
            await _usuarioRepository.SaveAsync(usuario);
        }

        public async Task UpdateAsync(Usuario usuario, string? newPlainPassword)
        {
            var existing = await _usuarioRepository.GetByIdAsync(usuario.Id)
                ?? throw new InvalidOperationException("Usuario no encontrado");

            var updated = new Usuario
            {
                Id             = existing.Id,
                Username       = usuario.Username,
                PasswordHaseado = string.IsNullOrWhiteSpace(newPlainPassword)
                    ? existing.PasswordHaseado
                    : EncryptionHelper.HashPassword(newPlainPassword),
                Email          = EncryptionHelper.Encrypt(usuario.Email, _encryptionKey),
                NumeroTelefono = string.IsNullOrEmpty(usuario.NumeroTelefono)
                    ? null
                    : EncryptionHelper.Encrypt(usuario.NumeroTelefono, _encryptionKey),
                Nombre   = usuario.Nombre,
                Apellido = usuario.Apellido,
                Rol      = usuario.Rol,
                Activo   = existing.Activo,
                CreadoEn = existing.CreadoEn
            };

            await _usuarioRepository.UpdateAsync(updated);
        }

        public async Task DeactivateAsync(int id)
        {
            var existing = await _usuarioRepository.GetByIdAsync(id)
                ?? throw new InvalidOperationException("Usuario no encontrado");

            await _usuarioRepository.UpdateAsync(new Usuario
            {
                Id             = existing.Id,
                Username       = existing.Username,
                PasswordHaseado = existing.PasswordHaseado,
                Email          = existing.Email,
                NumeroTelefono = existing.NumeroTelefono,
                Nombre         = existing.Nombre,
                Apellido       = existing.Apellido,
                Rol            = existing.Rol,
                Activo         = false,
                CreadoEn       = existing.CreadoEn
            });
        }

        public async Task<bool> CambiarPasswordAsync(int userId, string passwordActual, string nuevoPassword)
        {
            var user = await _usuarioRepository.GetByIdAsync(userId);
            if (user is null || !user.Activo) return false;

            if (!EncryptionHelper.VerifyPassword(passwordActual, user.PasswordHaseado))
                return false;

            await _usuarioRepository.UpdateAsync(new Usuario
            {
                Id              = user.Id,
                Username        = user.Username,
                PasswordHaseado = EncryptionHelper.HashPassword(nuevoPassword),
                Email           = user.Email,
                NumeroTelefono  = user.NumeroTelefono,
                Nombre          = user.Nombre,
                Apellido        = user.Apellido,
                Rol             = user.Rol,
                Activo          = user.Activo,
                CreadoEn        = user.CreadoEn
            });
            return true;
        }

        // ── Helpers privados ─────────────────────────────────────────────────

        private async Task<Usuario?> GetRawByLoginAsync(string login)
        {
            if (string.IsNullOrWhiteSpace(login)) return null;

            if (login.Contains('@'))
                return await _usuarioRepository.GetByEmailAsync(
                    EncryptionHelper.Encrypt(login, _encryptionKey));

            var byUsername = await _usuarioRepository.GetByUsernameAsync(login);
            if (byUsername is not null) return byUsername;

            return await _usuarioRepository.GetByTelefonoAsync(
                EncryptionHelper.Encrypt(login, _encryptionKey));
        }

        private Usuario ToDecrypted(Usuario source) => new()
        {
            Id             = source.Id,
            Username       = source.Username,
            PasswordHaseado = source.PasswordHaseado,
            Email          = EncryptionHelper.Decrypt(source.Email, _encryptionKey),
            NumeroTelefono = string.IsNullOrEmpty(source.NumeroTelefono)
                ? source.NumeroTelefono
                : EncryptionHelper.Decrypt(source.NumeroTelefono, _encryptionKey),
            Nombre   = source.Nombre,
            Apellido = source.Apellido,
            Activo   = source.Activo,
            CreadoEn = source.CreadoEn,
            Rol      = source.Rol
        };
    }
}
