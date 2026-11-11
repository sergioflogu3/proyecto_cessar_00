using System.Globalization;
using SistemaTickets.Domain.Services;
using SistemaTickets.Infrastructure.Security;

namespace SistemaTickets.Infrastructure.Services
{
    // Spec 004: adjuntos de tickets en una carpeta del servidor, para hostings sin almacenamiento
    // en la nube (p. ej. MonsterASP con "../private/uploads"). La carpeta llega ya resuelta y
    // validada desde Program.cs (RF-5 a RF-8); este servicio solo lee y escribe dentro de ella.
    public class LocalFileStorageService : IStorageService
    {
        private const int TamanioBuffer = 81920;

        private readonly string _raiz;

        public LocalFileStorageService(string raiz)
        {
            _raiz = Path.GetFullPath(raiz);
        }

        // contentType no se guarda: al descargar se deduce de la extensión, que ya se validó
        // contra el contenido antes de llegar aquí (plan 004, D2).
        public async Task<string> UploadAsync(
            Stream content, string fileName, string contentType, int ticketId)
        {
            var carpetaTicket = Path.Combine(
                _raiz, "tickets", ticketId.ToString(CultureInfo.InvariantCulture));
            Directory.CreateDirectory(carpetaTicket);

            // RF-10, RF-13: nombre interno aleatorio; el original solo se guarda en la BD
            // (TicketAdjunto.NombreArchivo) y es el que recibe el usuario al descargar.
            var nombre       = Guid.NewGuid().ToString("N") + RutaAdjuntos.ExtensionSegura(fileName);
            var rutaFinal    = Path.Combine(carpetaTicket, nombre);
            var rutaTemporal = rutaFinal + ".tmp";

            // RF-14: se escribe en un temporal y se mueve al final, para que un fallo a mitad de
            // la copia (disco lleno, cliente que corta) no deje un adjunto incompleto.
            try
            {
                await using (var destino = new FileStream(
                    rutaTemporal, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                    TamanioBuffer, useAsync: true))
                {
                    await content.CopyToAsync(destino);
                }

                File.Move(rutaTemporal, rutaFinal);
            }
            catch
            {
                // Si el borrado también falla, se conserva el error original de la subida.
                try { File.Delete(rutaTemporal); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                throw;
            }

            return $"tickets/{ticketId.ToString(CultureInfo.InvariantCulture)}/{nombre}";
        }

        // RF-12, RF-13: FileNotFoundException (sin rutas en el mensaje) si la ruta registrada no
        // es válida, sale de la carpeta o el archivo ya no existe; TicketsController la convierte
        // en 404. AzureBlobStorageService nunca la lanza, así que el destino nube no cambia.
        public Task<(Stream Content, string ContentType)> DownloadAsync(string blobPath)
        {
            var ruta = RutaAdjuntos.ResolverDentroDeRaiz(_raiz, blobPath);
            if (ruta is null || !File.Exists(ruta))
                throw new FileNotFoundException("El contenido del adjunto no existe en la carpeta de adjuntos.");

            Stream stream;
            try
            {
                stream = new FileStream(
                    ruta, FileMode.Open, FileAccess.Read, FileShare.Read,
                    TamanioBuffer, FileOptions.Asynchronous | FileOptions.SequentialScan);
            }
            catch (DirectoryNotFoundException)
            {
                // Borrado entre la comprobación y la apertura: mismo caso que si no existiera.
                throw new FileNotFoundException("El contenido del adjunto no existe en la carpeta de adjuntos.");
            }

            var tipo = AttachmentValidator.ContentTypePorExtension(Path.GetExtension(ruta))
                       ?? "application/octet-stream";

            return Task.FromResult((stream, tipo));
        }
    }
}
