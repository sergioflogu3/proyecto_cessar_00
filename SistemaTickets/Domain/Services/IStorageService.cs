namespace SistemaTickets.Domain.Services
{
    public interface IStorageService
    {
        // Sube un archivo y devuelve la ruta del blob (ej: "tickets/5/informe.pdf")
        Task<string> UploadAsync(Stream content, string fileName, string contentType, int ticketId);

        // Descarga el blob y devuelve (stream, contentType)
        Task<(Stream Content, string ContentType)> DownloadAsync(string blobPath);
    }
}
