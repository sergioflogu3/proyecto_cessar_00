namespace SistemaTickets.Domain.Services
{
    public interface ILoginAttemptService
    {
        bool IsLockedOut(string loginKey, out TimeSpan retryAfter);
        void RegisterFailure(string loginKey);
        void RegisterSuccess(string loginKey);
    }
}
