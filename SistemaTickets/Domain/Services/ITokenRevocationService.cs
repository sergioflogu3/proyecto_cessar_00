namespace SistemaTickets.Domain.Services
{
    public interface ITokenRevocationService
    {
        void Revoke(string jti, DateTimeOffset expiresAtUtc);
        bool IsRevoked(string jti);
    }
}
