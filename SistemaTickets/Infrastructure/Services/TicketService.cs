using SistemaTickets.Domain.Entities;
using SistemaTickets.Domain.Repositories;
using SistemaTickets.Domain.Services;

namespace SistemaTickets.Infrastructure.Services
{
    public class TicketService : ITicketService
    {
        private readonly ITicketRepository _repo;

        public TicketService(ITicketRepository repo)
        {
            _repo = repo;
        }

        public Task<Ticket?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public Task<IList<Ticket>> GetTicketsNuevosAsync()
            => _repo.GetTicketsNuevosAsync();

        public Task<IList<Ticket>> GetActivosParaSeguimientoAsync()
            => _repo.GetActivosParaSeguimientoAsync();

        public async Task TomarTicketAsync(int ticketId, int userId)
        {
            var ticket = await _repo.GetByIdAsync(ticketId)
                ?? throw new InvalidOperationException("Ticket no encontrado");

            var estadoEnProgreso = await _repo.GetEstadoByNombreAsync("En Progreso")
                ?? throw new InvalidOperationException("Estado 'En Progreso' no encontrado");

            var anteriorNombre = ticket.AsignadoA is null
                ? "(sin asignar)"
                : $"{ticket.AsignadoA.Nombre} {ticket.AsignadoA.Apellido}";

            var anteriorEstado = ticket.Estado.Nombre;

            var nuevoAgente = await _repo.GetUsuarioAsync(userId);
            var nuevoNombre = nuevoAgente != null
                ? $"{nuevoAgente.Nombre} {nuevoAgente.Apellido}"
                : $"Id:{userId}";

            ticket.AsignadoA          = _repo.GetRef<Usuario>(userId);
            ticket.Estado             = estadoEnProgreso;
            ticket.FechaActualizacion = DateTime.Now;

            await _repo.UpdateAsync(ticket);

            await _repo.SaveHistorialAsync(new TicketHistorial
            {
                Ticket        = _repo.GetRef<Ticket>(ticketId),
                Usuario       = _repo.GetRef<Usuario>(userId),
                Accion        = "Tomar ticket",
                ValorAnterior = $"{anteriorNombre} / {anteriorEstado}",
                ValorNuevo    = $"{nuevoNombre} / En Progreso",
                FechaAccion   = DateTime.Now
            });
        }

        public Task<(IList<Ticket> Items, int Total)> GetPagedAsync(
            int page, int pageSize,
            string? search,
            int? estadoId,
            int? prioridadId,
            int? categoriaId,
            int? creadoPorId,
            int? asignadoAId)
            => _repo.GetPagedAsync(page, pageSize, search, estadoId, prioridadId, categoriaId, creadoPorId, asignadoAId);

        public async Task<int> CreateAsync(Ticket ticket, int creadoPorId)
        {
            var estadoAbierto = await _repo.GetEstadoByNombreAsync("Abierto")
                ?? throw new InvalidOperationException("Estado 'Abierto' no encontrado");

            ticket.Estado            = estadoAbierto;
            ticket.CreadoPor         = _repo.GetRef<Usuario>(creadoPorId);
            ticket.FechaCreacion     = DateTime.UtcNow;
            ticket.FechaActualizacion = DateTime.UtcNow;

            var id = await _repo.SaveAsync(ticket);

            await _repo.SaveHistorialAsync(new TicketHistorial
            {
                Ticket      = _repo.GetRef<Ticket>(id),
                Usuario     = _repo.GetRef<Usuario>(creadoPorId),
                Accion      = "Creación",
                ValorAnterior = null,
                ValorNuevo  = ticket.Titulo,
                FechaAccion = DateTime.UtcNow
            });

            return id;
        }

        public async Task UpdateAsync(Ticket ticket, int modificadoPorId)
        {
            var existing = await _repo.GetByIdAsync(ticket.Id)
                ?? throw new InvalidOperationException("Ticket no encontrado");

            var anteriorTitulo = existing.Titulo;

            existing.Titulo            = ticket.Titulo;
            existing.Descripcion       = ticket.Descripcion;
            if (ticket.Prioridad != null) existing.Prioridad = ticket.Prioridad;
            existing.Categoria         = ticket.Categoria;
            existing.AsignadoA         = ticket.AsignadoA;
            existing.FechaVencimiento  = ticket.FechaVencimiento;
            existing.FechaActualizacion = DateTime.UtcNow;

            await _repo.UpdateAsync(existing);

            if (anteriorTitulo != ticket.Titulo)
            {
                await _repo.SaveHistorialAsync(new TicketHistorial
                {
                    Ticket      = _repo.GetRef<Ticket>(existing.Id),
                    Usuario     = _repo.GetRef<Usuario>(modificadoPorId),
                    Accion      = "Edición",
                    ValorAnterior = anteriorTitulo,
                    ValorNuevo  = ticket.Titulo,
                    FechaAccion = DateTime.UtcNow
                });
            }
        }

        public async Task AsignarPrioridadAsync(int ticketId, int prioridadId, int modificadoPorId)
        {
            var ticket = await _repo.GetByIdAsync(ticketId)
                ?? throw new InvalidOperationException("Ticket no encontrado");

            var anteriorPrioridad = ticket.Prioridad?.Nombre ?? "(sin prioridad)";

            var prioridades    = await _repo.GetPrioridadesAsync();
            var nuevaPrioridad = prioridades.FirstOrDefault(p => p.Id == prioridadId);

            ticket.Prioridad          = _repo.GetRef<TicketPrioridad>(prioridadId);
            ticket.FechaActualizacion = DateTime.Now;

            await _repo.UpdateAsync(ticket);

            await _repo.SaveHistorialAsync(new TicketHistorial
            {
                Ticket        = _repo.GetRef<Ticket>(ticketId),
                Usuario       = _repo.GetRef<Usuario>(modificadoPorId),
                Accion        = "Prioridad asignada",
                ValorAnterior = anteriorPrioridad,
                ValorNuevo    = nuevaPrioridad?.Nombre ?? $"Id:{prioridadId}",
                FechaAccion   = DateTime.Now
            });
        }

        public async Task AssignAsync(int ticketId, int asignadoAId, int modificadoPorId)
        {
            var ticket = await _repo.GetByIdAsync(ticketId)
                ?? throw new InvalidOperationException("Ticket no encontrado");

            var anteriorNombre = ticket.AsignadoA is null
                ? "(sin asignar)"
                : $"{ticket.AsignadoA.Nombre} {ticket.AsignadoA.Apellido}";

            var usuarioAsignado = await _repo.GetUsuarioAsync(asignadoAId);
            var nuevoNombreAsig = usuarioAsignado != null
                ? $"{usuarioAsignado.Nombre} {usuarioAsignado.Apellido}"
                : $"Id:{asignadoAId}";

            ticket.AsignadoA          = _repo.GetRef<Usuario>(asignadoAId);
            ticket.FechaActualizacion = DateTime.Now;

            await _repo.UpdateAsync(ticket);

            await _repo.SaveHistorialAsync(new TicketHistorial
            {
                Ticket      = _repo.GetRef<Ticket>(ticketId),
                Usuario     = _repo.GetRef<Usuario>(modificadoPorId),
                Accion      = "Asignación",
                ValorAnterior = anteriorNombre,
                ValorNuevo  = nuevoNombreAsig,
                FechaAccion = DateTime.Now
            });
        }

        private static readonly HashSet<string> _estadosTerminales =
            new(StringComparer.OrdinalIgnoreCase) { "Cerrado", "Resuelto", "Cancelado" };

        public async Task ChangeStatusAsync(int ticketId, int nuevoEstadoId, int modificadoPorId)
        {
            var ticket = await _repo.GetByIdAsync(ticketId)
                ?? throw new InvalidOperationException("Ticket no encontrado");

            // Cargar el nuevo estado para saber su nombre
            var estados     = await _repo.GetEstadosAsync();
            var nuevoEstado = estados.FirstOrDefault(e => e.Id == nuevoEstadoId)
                ?? throw new InvalidOperationException("Estado no encontrado");

            var anteriorEstado        = ticket.Estado.Nombre;
            ticket.Estado             = _repo.GetRef<TicketEstado>(nuevoEstadoId);
            ticket.FechaActualizacion = DateTime.Now;

            if (_estadosTerminales.Contains(nuevoEstado.Nombre))
                ticket.FechaCierre = DateTime.Now;

            await _repo.UpdateAsync(ticket);

            await _repo.SaveHistorialAsync(new TicketHistorial
            {
                Ticket      = _repo.GetRef<Ticket>(ticketId),
                Usuario     = _repo.GetRef<Usuario>(modificadoPorId),
                Accion      = "Cambio de estado",
                ValorAnterior = anteriorEstado,
                ValorNuevo  = nuevoEstado.Nombre,
                FechaAccion = DateTime.Now
            });
        }

        public async Task CloseAsync(int ticketId, int cerradoPorId)
        {
            var ticket = await _repo.GetByIdAsync(ticketId)
                ?? throw new InvalidOperationException("Ticket no encontrado");

            var estadoCerrado = await _repo.GetEstadoByNombreAsync("Cerrado")
                ?? throw new InvalidOperationException("Estado 'Cerrado' no encontrado");

            var anteriorEstado = ticket.Estado.Nombre;
            ticket.Estado             = estadoCerrado;
            ticket.FechaCierre        = DateTime.Now;
            ticket.FechaActualizacion = DateTime.Now;

            await _repo.UpdateAsync(ticket);

            await _repo.SaveHistorialAsync(new TicketHistorial
            {
                Ticket      = _repo.GetRef<Ticket>(ticketId),
                Usuario     = _repo.GetRef<Usuario>(cerradoPorId),
                Accion      = "Cierre",
                ValorAnterior = anteriorEstado,
                ValorNuevo  = "Cerrado",
                FechaAccion = DateTime.UtcNow
            });
        }

        public async Task AddCommentAsync(TicketComentario comentario)
        {
            comentario.FechaCreacion = DateTime.UtcNow;
            await _repo.SaveComentarioAsync(comentario);

            var historial = new TicketHistorial
            {
                Ticket      = comentario.Ticket,
                Usuario     = comentario.Usuario,
                Accion      = comentario.EsInterno ? "Nota interna" : "Comentario",
                ValorAnterior = null,
                ValorNuevo  = comentario.Contenido.Length > 100
                    ? comentario.Contenido[..100] + "…"
                    : comentario.Contenido,
                FechaAccion = DateTime.UtcNow
            };
            await _repo.SaveHistorialAsync(historial);
        }

        public async Task AddAdjuntoAsync(TicketAdjunto adjunto, int subidoPorId)
        {
            adjunto.SubidoPor   = _repo.GetRef<Usuario>(subidoPorId);
            adjunto.FechaSubida = DateTime.UtcNow;
            await _repo.SaveAdjuntoAsync(adjunto);
        }

        public Task<TicketAdjunto?> GetAdjuntoByIdAsync(int id)
            => _repo.GetAdjuntoByIdAsync(id);

        public Task<IList<TicketEstado>>    GetEstadosAsync()    => _repo.GetEstadosAsync();
        public Task<IList<TicketPrioridad>> GetPrioridadesAsync() => _repo.GetPrioridadesAsync();
        public Task<IList<TicketCategoria>> GetCategoriasAsync()  => _repo.GetCategoriasAsync();

        // Admin — configuración de catálogos
        public Task<IList<TicketCategoria>> GetAllCategoriasAdminAsync() => _repo.GetAllCategoriasAdminAsync();
        public Task<IList<TicketPrioridad>> GetAllPrioridadesAdminAsync() => _repo.GetAllPrioridadesAdminAsync();
        public Task<IList<TicketEstado>>    GetAllEstadosAdminAsync()    => _repo.GetAllEstadosAdminAsync();

        public async Task CrearCategoriaAsync(string nombre, string? descripcion)
        {
            await _repo.SaveCategoriaAsync(new TicketCategoria
            {
                Nombre      = nombre,
                Descripcion = descripcion,
                Activo      = true
            });
        }

        public async Task EditarCategoriaAsync(int id, string nombre, string? descripcion)
        {
            var c = await _repo.GetCategoriaByIdAsync(id)
                ?? throw new InvalidOperationException("Categoría no encontrada");
            c.Nombre      = nombre;
            c.Descripcion = descripcion;
            await _repo.UpdateCategoriaAsync(c);
        }

        public async Task ToggleCategoriaAsync(int id)
        {
            var c = await _repo.GetCategoriaByIdAsync(id)
                ?? throw new InvalidOperationException("Categoría no encontrada");
            c.Activo = !c.Activo;
            await _repo.UpdateCategoriaAsync(c);
        }

        public async Task CrearPrioridadAsync(string nombre, string color, int orden, string? descripcion)
        {
            await _repo.SavePrioridadAsync(new TicketPrioridad
            {
                Nombre      = nombre,
                Color       = color,
                Orden       = orden,
                Descripcion = descripcion,
                Activo      = true
            });
        }

        public async Task EditarPrioridadAsync(int id, string nombre, string color, int orden, string? descripcion)
        {
            var p = await _repo.GetPrioridadByIdAsync(id)
                ?? throw new InvalidOperationException("Prioridad no encontrada");
            p.Nombre      = nombre;
            p.Color       = color;
            p.Orden       = orden;
            p.Descripcion = descripcion;
            await _repo.UpdatePrioridadAsync(p);
        }

        public async Task TogglePrioridadAsync(int id)
        {
            var p = await _repo.GetPrioridadByIdAsync(id)
                ?? throw new InvalidOperationException("Prioridad no encontrada");
            p.Activo = !p.Activo;
            await _repo.UpdatePrioridadAsync(p);
        }

        public async Task EditarEstadoAsync(int id, string color, int orden)
        {
            var e = await _repo.GetEstadoByIdAsync(id)
                ?? throw new InvalidOperationException("Estado no encontrado");
            e.Color = color;
            e.Orden = orden;
            await _repo.UpdateEstadoAsync(e);
        }
    }
}
