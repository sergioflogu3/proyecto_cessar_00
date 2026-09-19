using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Domain.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// Envía una copia del ticket al correo del solicitante.
        /// El campo Email del usuario viene encriptado; el servicio lo desencripta internamente.
        /// No lanza excepciones: si el SMTP no está configurado o falla, se omite silenciosamente.
        /// </summary>
        Task SendTicketCopyAsync(
            int      ticketId,
            string   titulo,
            string   descripcion,
            DateTime fechaCreacion,
            Usuario  solicitante);
    }
}
