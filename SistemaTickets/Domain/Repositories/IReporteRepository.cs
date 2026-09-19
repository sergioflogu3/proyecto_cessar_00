using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Domain.Repositories
{
    public interface IReporteRepository
    {
        Task<IList<Ticket>>           GetTicketsAsync(DateTime desde, DateTime hasta, int? estadoId, int? prioridadId, int? categoriaId, int? clienteId, int? tecnicoId);
        Task<IList<TicketHistorial>>  GetHistorialAsync(DateTime desde, DateTime hasta, int? ticketId);
        Task<IList<TicketComentario>> GetComentariosAsync(DateTime desde, DateTime hasta);
        Task<IList<TicketAdjunto>>    GetAdjuntosAsync(DateTime desde, DateTime hasta);
        Task<IList<Usuario>>          GetTodosUsuariosAsync();
        Task<IList<Usuario>>          GetTecnicosAsync();
        Task<IList<Usuario>>          GetClientesAsync();
    }
}
