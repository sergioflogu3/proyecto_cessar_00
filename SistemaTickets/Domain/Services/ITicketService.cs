using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Domain.Services
{
    public interface ITicketService
    {
        Task<Ticket?> GetByIdAsync(int id);
        Task<(IList<Ticket> Items, int Total)> GetPagedAsync(
            int page, int pageSize,
            string? search,
            int? estadoId,
            int? prioridadId,
            int? categoriaId,
            int? creadoPorId,
            int? asignadoAId);

        Task<int> CreateAsync(Ticket ticket, int creadoPorId);
        Task UpdateAsync(Ticket ticket, int modificadoPorId);
        Task AssignAsync(int ticketId, int asignadoAId, int modificadoPorId);
        Task AsignarPrioridadAsync(int ticketId, int prioridadId, int modificadoPorId);
        Task ChangeStatusAsync(int ticketId, int nuevoEstadoId, int modificadoPorId);
        Task CloseAsync(int ticketId, int cerradoPorId);
        Task AddCommentAsync(TicketComentario comentario);
        Task AddAdjuntoAsync(TicketAdjunto adjunto, int subidoPorId);
        Task<TicketAdjunto?> GetAdjuntoByIdAsync(int id);

        Task<IList<Ticket>> GetTicketsNuevosAsync();
        Task<IList<Ticket>> GetActivosParaSeguimientoAsync();
        Task TomarTicketAsync(int ticketId, int userId);

        Task<IList<TicketEstado>>    GetEstadosAsync();
        Task<IList<TicketPrioridad>> GetPrioridadesAsync();
        Task<IList<TicketCategoria>> GetCategoriasAsync();

        // Admin — configuración de catálogos
        Task<IList<TicketCategoria>> GetAllCategoriasAdminAsync();
        Task<IList<TicketPrioridad>> GetAllPrioridadesAdminAsync();
        Task<IList<TicketEstado>>    GetAllEstadosAdminAsync();

        Task CrearCategoriaAsync(string nombre, string? descripcion);
        Task EditarCategoriaAsync(int id, string nombre, string? descripcion);
        Task ToggleCategoriaAsync(int id);

        Task CrearPrioridadAsync(string nombre, string color, int orden, string? descripcion);
        Task EditarPrioridadAsync(int id, string nombre, string color, int orden, string? descripcion);
        Task TogglePrioridadAsync(int id);

        Task EditarEstadoAsync(int id, string color, int orden);
    }
}
