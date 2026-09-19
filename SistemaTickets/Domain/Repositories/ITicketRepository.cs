using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Domain.Repositories
{
    public interface ITicketRepository
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

        Task<int> SaveAsync(Ticket ticket);
        Task UpdateAsync(Ticket ticket);
        Task DeleteAsync(int id);

        // Catálogos
        Task<IList<TicketEstado>>    GetEstadosAsync();
        Task<IList<TicketPrioridad>> GetPrioridadesAsync();
        Task<IList<TicketCategoria>> GetCategoriasAsync();
        Task<TicketEstado?>    GetEstadoByNombreAsync(string nombre);

        // Comentarios / adjuntos / historial
        Task SaveComentarioAsync(TicketComentario comentario);
        Task SaveAdjuntoAsync(TicketAdjunto adjunto);
        Task<TicketAdjunto?> GetAdjuntoByIdAsync(int id);
        Task SaveHistorialAsync(TicketHistorial historial);

        Task<IList<Ticket>> GetTicketsNuevosAsync();
        Task<IList<Ticket>> GetActivosParaSeguimientoAsync();

        Task<Usuario?> GetUsuarioAsync(int id);

        // Proxy sin hit a BD (para referencias FK)
        T GetRef<T>(int id) where T : class;

        // Admin — catálogos (incluye inactivos)
        Task<IList<TicketCategoria>> GetAllCategoriasAdminAsync();
        Task<IList<TicketPrioridad>> GetAllPrioridadesAdminAsync();
        Task<IList<TicketEstado>>    GetAllEstadosAdminAsync();

        Task<TicketCategoria?> GetCategoriaByIdAsync(int id);
        Task<TicketPrioridad?> GetPrioridadByIdAsync(int id);
        Task<TicketEstado?>    GetEstadoByIdAsync(int id);

        Task SaveCategoriaAsync(TicketCategoria c);
        Task UpdateCategoriaAsync(TicketCategoria c);
        Task SavePrioridadAsync(TicketPrioridad p);
        Task UpdatePrioridadAsync(TicketPrioridad p);
        Task UpdateEstadoAsync(TicketEstado e);
    }
}
