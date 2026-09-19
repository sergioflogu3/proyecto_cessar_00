using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SistemaTickets.Domain.Services;

namespace SistemaTickets.Infrastructure.Services
{
    public class AzureBlobStorageService : IStorageService
    {
        private readonly BlobContainerClient _container;

        public AzureBlobStorageService(IConfiguration config)
        {
            var connectionString = config["AzureBlobStorage:ConnectionString"]!;
            var containerName    = config["AzureBlobStorage:ContainerName"] ?? "sistema";

            // Especificar versión de API para compatibilidad con Azurite local
            var options = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2023_11_03);
            _container = new BlobContainerClient(connectionString, containerName, options);
            _container.CreateIfNotExists();
        }

        public async Task<string> UploadAsync(
            Stream content, string fileName, string contentType, int ticketId)
        {
            // Sanitizar nombre: solo el filename, sin rutas
            var safeName = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(safeName))
                safeName = Guid.NewGuid().ToString();

            var blobPath = $"tickets/{ticketId}/{safeName}";
            var blob     = _container.GetBlobClient(blobPath);

            await blob.UploadAsync(content, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            });

            return blobPath;
        }

        public async Task<(Stream Content, string ContentType)> DownloadAsync(string blobPath)
        {
            var blob     = _container.GetBlobClient(blobPath);
            var response = await blob.DownloadStreamingAsync();

            return (
                response.Value.Content,
                response.Value.Details.ContentType ?? "application/octet-stream"
            );
        }
    }
}
