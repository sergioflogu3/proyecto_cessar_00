using SistemaTickets.Domain.Entities;
using SistemaTickets.Models.Reportes;

namespace SistemaTickets.Domain.Services
{
    public interface IReporteService
    {
        // Catálogos para dropdowns
        Task<IList<TicketEstado>>    GetEstadosAsync();
        Task<IList<TicketPrioridad>> GetPrioridadesAsync();
        Task<IList<TicketCategoria>> GetCategoriasAsync();
        Task<IList<Usuario>>         GetTecnicosAsync();
        Task<IList<Usuario>>         GetClientesAsync();

        // Generadores Excel — retornan byte[]
        Task<byte[]> GenerarReporteGeneralAsync(ReporteFiltro filtro);
        Task<byte[]> GenerarReportePorEstadoAsync(ReporteFiltro filtro);
        Task<byte[]> GenerarReportePorPrioridadAsync(ReporteFiltro filtro);
        Task<byte[]> GenerarReporteSinResolverAsync(ReporteFiltro filtro);
        Task<byte[]> GenerarReporteProductividadAsync(ReporteFiltro filtro);
        Task<byte[]> GenerarReporteHistorialAsync(ReporteFiltro filtro);
        Task<byte[]> GenerarReporteComentariosAsync(ReporteFiltro filtro);
        Task<byte[]> GenerarReporteAdjuntosAsync(ReporteFiltro filtro);
        Task<byte[]> GenerarReporteUsuariosAsync();

        // Generadores PDF — retornan byte[]
        Task<byte[]> GenerarPdfAsync(string tipo, ReporteFiltro filtro);
    }
}
