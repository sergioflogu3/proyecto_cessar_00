using SistemaTickets.Models.Dashboard;

namespace SistemaTickets.Domain.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardAsync(
            int?   tecnicoId,
            bool   esAdminOSupervisor,
            string? nombreTecnico = null);
    }
}
