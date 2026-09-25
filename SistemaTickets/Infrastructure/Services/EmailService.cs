using System.Net;
using System.Net.Mail;
using System.Text;
using SistemaTickets.Domain.Entities;
using SistemaTickets.Domain.Services;
using SistemaTickets.Infrastructure.Security;

namespace SistemaTickets.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendTicketCopyAsync(
            int      ticketId,
            string   titulo,
            string   descripcion,
            DateTime fechaCreacion,
            Usuario  solicitante)
        {
            var smtpHost = _config["Email:SmtpHost"];
            if (string.IsNullOrWhiteSpace(smtpHost))
            {
                _logger.LogDebug("Email no enviado: SMTP no configurado (Email:SmtpHost vacío).");
                return;
            }

            try
            {
                var encKey   = _config["Encryption:Key"] ?? string.Empty;
                var toEmail  = EncryptionHelper.Decrypt(solicitante.Email, encKey);

                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    _logger.LogWarning("Email no enviado para ticket #{TicketId}: correo vacío.", ticketId);
                    return;
                }

                var smtpPort    = int.TryParse(_config["Email:SmtpPort"], out var p) ? p : 587;
                var enableSsl   = !string.Equals(_config["Email:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase);
                var username    = _config["Email:Username"]    ?? string.Empty;
                var password    = _config["Email:Password"]    ?? string.Empty;
                var fromAddress = _config["Email:FromAddress"] ?? username;
                var fromName    = _config["Email:FromName"]    ?? "SistemaTickets";

                var subject = $"[Ticket #{ticketId}] {titulo}";
                var body    = BuildHtmlBody(ticketId, titulo, descripcion, fechaCreacion, solicitante.NombreCompleto);

                using var smtp = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl        = enableSsl,
                    Credentials      = new NetworkCredential(username, password),
                    DeliveryMethod   = SmtpDeliveryMethod.Network,
                    Timeout          = 10_000
                };

                using var msg = new MailMessage
                {
                    From            = new MailAddress(fromAddress, fromName, Encoding.UTF8),
                    Subject         = subject,
                    Body            = body,
                    IsBodyHtml      = true,
                    BodyEncoding    = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };
                msg.To.Add(new MailAddress(toEmail, solicitante.NombreCompleto, Encoding.UTF8));

                await smtp.SendMailAsync(msg);
                // B4: no loguear el email en texto plano — es un dato desencriptado justo para
                // este envío (se guarda cifrado en la BD); el UsuarioId alcanza para correlacionar
                // en logs sin volver a exponer el correo.
                _logger.LogInformation(
                    "Copia de ticket #{TicketId} enviada al solicitante (UsuarioId {UsuarioId}).",
                    ticketId, solicitante.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar copia del ticket #{TicketId}.", ticketId);
                // No propagar: el envío de correo no debe fallar la operación principal.
            }
        }

        // ── HTML ─────────────────────────────────────────────────────────────

        private static string BuildHtmlBody(
            int ticketId, string titulo, string descripcion,
            DateTime fecha, string nombreSolicitante)
        {
            var tituloHtml      = Encode(titulo);
            var descripcionHtml = Encode(descripcion).Replace("\n", "<br/>");
            var nombreHtml      = Encode(nombreSolicitante);
            var fechaStr        = fecha.ToString("dd/MM/yyyy HH:mm");
            var year            = DateTime.Now.Year;

            return $@"<!DOCTYPE html>
<html lang=""es"">
<head><meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width""></head>
<body style=""margin:0;padding:0;background:#f4f6f8;font-family:Arial,Helvetica,sans-serif;"">
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f4f6f8;padding:32px 0;"">
  <tr><td align=""center"">
  <table width=""600"" cellpadding=""0"" cellspacing=""0""
         style=""background:#ffffff;border-radius:10px;overflow:hidden;box-shadow:0 2px 12px rgba(0,0,0,.08);"">

    <!-- Header -->
    <tr>
      <td style=""background:#198754;padding:22px 32px;"">
        <span style=""color:#ffffff;font-size:18px;font-weight:bold;"">TT Soluciones &mdash; SistemaTickets</span>
      </td>
    </tr>

    <!-- Hero -->
    <tr>
      <td style=""padding:28px 32px 20px;"">
        <p style=""margin:0 0 4px;font-size:13px;color:#6c757d;text-transform:uppercase;letter-spacing:.5px;"">Nueva solicitud registrada</p>
        <h1 style=""margin:0;font-size:22px;color:#212529;"">Ticket <span style=""color:#198754;"">#{ticketId}</span></h1>
        <p style=""margin:6px 0 0;font-size:14px;color:#495057;"">{tituloHtml}</p>
      </td>
    </tr>

    <!-- Info table -->
    <tr>
      <td style=""padding:0 32px 24px;"">
        <table width=""100%"" cellpadding=""0"" cellspacing=""0""
               style=""border:1px solid #dee2e6;border-radius:6px;overflow:hidden;font-size:14px;"">
          <tr style=""background:#f8f9fa;"">
            <td colspan=""2"" style=""padding:10px 16px;font-weight:bold;color:#495057;border-bottom:1px solid #dee2e6;"">
              Resumen del ticket
            </td>
          </tr>
          <tr>
            <td style=""padding:10px 16px;color:#6c757d;width:130px;border-bottom:1px solid #f0f0f0;"">Número</td>
            <td style=""padding:10px 16px;font-weight:bold;color:#198754;border-bottom:1px solid #f0f0f0;"">#{ticketId}</td>
          </tr>
          <tr style=""background:#fafafa;"">
            <td style=""padding:10px 16px;color:#6c757d;border-bottom:1px solid #f0f0f0;"">Estado</td>
            <td style=""padding:10px 16px;color:#212529;border-bottom:1px solid #f0f0f0;"">
              <span style=""background:#d1e7dd;color:#0f5132;padding:2px 8px;border-radius:4px;font-size:12px;"">Abierto</span>
            </td>
          </tr>
          <tr>
            <td style=""padding:10px 16px;color:#6c757d;border-bottom:1px solid #f0f0f0;"">Solicitante</td>
            <td style=""padding:10px 16px;color:#212529;border-bottom:1px solid #f0f0f0;"">{nombreHtml}</td>
          </tr>
          <tr style=""background:#fafafa;"">
            <td style=""padding:10px 16px;color:#6c757d;"">Fecha</td>
            <td style=""padding:10px 16px;color:#212529;"">{fechaStr}</td>
          </tr>
        </table>
      </td>
    </tr>

    <!-- Description -->
    <tr>
      <td style=""padding:0 32px 28px;"">
        <p style=""margin:0 0 8px;font-size:12px;color:#6c757d;text-transform:uppercase;letter-spacing:.5px;font-weight:bold;"">Descripción</p>
        <div style=""background:#f8f9fa;border-left:3px solid #198754;padding:14px 16px;
                     border-radius:0 4px 4px 0;font-size:14px;color:#212529;line-height:1.65;"">
          {descripcionHtml}
        </div>
      </td>
    </tr>

    <!-- Note -->
    <tr>
      <td style=""padding:0 32px 28px;"">
        <div style=""background:#fff3cd;border:1px solid #ffc107;border-radius:6px;padding:12px 16px;font-size:13px;color:#664d03;"">
          <strong>Importante:</strong> Nuestro equipo de soporte revisará tu solicitud a la brevedad posible.
          Recibirás actualizaciones cuando se asigne o resuelva el ticket.
        </div>
      </td>
    </tr>

    <!-- Footer -->
    <tr>
      <td style=""background:#f8f9fa;border-top:1px solid #dee2e6;padding:16px 32px;text-align:center;"">
        <p style=""margin:0;font-size:12px;color:#adb5bd;"">
          Este es un correo automático, por favor no respondas a este mensaje.
        </p>
        <p style=""margin:4px 0 0;font-size:12px;color:#adb5bd;"">&copy; {year} TT Soluciones &mdash; SistemaTickets</p>
      </td>
    </tr>

  </table>
  </td></tr>
</table>
</body>
</html>";
        }

        private static string Encode(string text)
            => System.Net.WebUtility.HtmlEncode(text ?? string.Empty);
    }
}
