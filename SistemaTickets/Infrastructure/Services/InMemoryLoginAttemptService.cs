using System.Collections.Concurrent;
using SistemaTickets.Domain.Services;

namespace SistemaTickets.Infrastructure.Services
{
    // A1: bloqueo temporal por usuario tras N intentos fallidos consecutivos.
    // Se complementa con el rate limiting por IP configurado en Program.cs (política "login"),
    // que acota el volumen de solicitudes independientemente del usuario objetivo.
    // Limitación conocida: estado en memoria (se pierde al reiniciar el proceso, no se comparte
    // entre instancias) y sin límite de entradas distintas; migrar a una tabla persistida si se
    // despliega con múltiples instancias o si el volumen de intentos se vuelve un problema.
    public class InMemoryLoginAttemptService : ILoginAttemptService
    {
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private sealed record State(int FailedAttempts, DateTimeOffset LastFailureAt, DateTimeOffset? LockedUntil);

        private readonly ConcurrentDictionary<string, State> _attempts = new();

        public bool IsLockedOut(string loginKey, out TimeSpan retryAfter)
        {
            retryAfter = TimeSpan.Zero;
            var key = Normalize(loginKey);

            if (_attempts.TryGetValue(key, out var state) && state.LockedUntil is { } until)
            {
                var now = DateTimeOffset.UtcNow;
                if (until > now)
                {
                    retryAfter = until - now;
                    return true;
                }

                // El bloqueo ya expiró: se limpia para reiniciar el conteo de intentos.
                _attempts.TryRemove(key, out _);
            }

            return false;
        }

        public void RegisterFailure(string loginKey)
        {
            var key = Normalize(loginKey);
            var now = DateTimeOffset.UtcNow;

            _attempts.AddOrUpdate(key,
                _ => new State(1, now, null),
                (_, existing) =>
                {
                    var withinWindow = now - existing.LastFailureAt <= FailureWindow;
                    var failedAttempts = (withinWindow ? existing.FailedAttempts : 0) + 1;
                    var lockedUntil = failedAttempts >= MaxFailedAttempts
                        ? now.Add(LockoutDuration)
                        : (DateTimeOffset?)null;

                    return new State(failedAttempts, now, lockedUntil);
                });
        }

        public void RegisterSuccess(string loginKey) => _attempts.TryRemove(Normalize(loginKey), out _);

        private static string Normalize(string loginKey) => loginKey.Trim().ToLowerInvariant();
    }
}
