using System.Collections.Concurrent;
using SistemaTickets.Domain.Services;

namespace SistemaTickets.Infrastructure.Services
{
    // C3: lista de revocación en memoria, consultada en OnTokenValidated (Program.cs).
    // Limitación conocida: se pierde al reiniciar el proceso y no se comparte entre instancias;
    // si se despliega con múltiples instancias/reinicios frecuentes, migrar a una tabla persistida.
    public class InMemoryTokenRevocationService : ITokenRevocationService
    {
        private readonly ConcurrentDictionary<string, DateTimeOffset> _revoked = new();

        public void Revoke(string jti, DateTimeOffset expiresAtUtc)
        {
            _revoked[jti] = expiresAtUtc;
            PurgeExpired();
        }

        public bool IsRevoked(string jti)
        {
            if (!_revoked.TryGetValue(jti, out var expiresAtUtc))
                return false;

            if (expiresAtUtc <= DateTimeOffset.UtcNow)
            {
                _revoked.TryRemove(jti, out _);
                return false;
            }

            return true;
        }

        private void PurgeExpired()
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var (jti, expiresAtUtc) in _revoked)
            {
                if (expiresAtUtc <= now)
                    _revoked.TryRemove(jti, out _);
            }
        }
    }
}
