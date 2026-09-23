using System.Text;

namespace SistemaTickets.Infrastructure.Security
{
    // A2: lista blanca de extensiones para adjuntos de tickets, verificada contra los
    // "magic bytes" reales del archivo (no basta con el nombre/extensión declarados).
    // El Content-Type que manda el navegador nunca se usa como fuente de verdad: se descarta
    // y se reemplaza por el MIME canónico de la extensión ya validada contra su contenido.
    public static class AttachmentValidator
    {
        private sealed record AllowedType(string ContentType, Func<byte[], bool> MatchesSignature);

        private static readonly Dictionary<string, AllowedType> AllowedTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [".pdf"]  = new("application/pdf", Signature(0x25, 0x50, 0x44, 0x46)),
                [".jpg"]  = new("image/jpeg", Signature(0xFF, 0xD8, 0xFF)),
                [".jpeg"] = new("image/jpeg", Signature(0xFF, 0xD8, 0xFF)),
                [".png"]  = new("image/png", Signature(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)),
                [".gif"]  = new("image/gif", Signature(0x47, 0x49, 0x46, 0x38)),
                [".webp"] = new("image/webp", IsWebP),
                [".doc"]  = new("application/msword", Signature(0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1)),
                [".xls"]  = new("application/vnd.ms-excel", Signature(0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1)),
                // .docx/.xlsx son contenedores ZIP (Office Open XML); comparten firma con .zip,
                // suficiente para rechazar un ejecutable/HTML renombrado con esta extensión.
                [".docx"] = new("application/vnd.openxmlformats-officedocument.wordprocessingml.document", Signature(0x50, 0x4B, 0x03, 0x04)),
                [".xlsx"] = new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Signature(0x50, 0x4B, 0x03, 0x04)),
                [".txt"]  = new("text/plain", IsLikelyText),
                [".csv"]  = new("text/csv", IsLikelyText),
            };

        // Valida extensión + magic bytes. Devuelve el content-type canónico si es válido,
        // o null si debe rechazarse. Deja la posición del stream como estaba al entrar.
        public static async Task<string?> ValidateAndResolveContentTypeAsync(string fileName, Stream content)
        {
            var ext = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(ext) || !AllowedTypes.TryGetValue(ext, out var allowed))
                return null;

            var header = new byte[16];
            var originalPosition = content.CanSeek ? content.Position : 0;

            var read = await content.ReadAsync(header.AsMemory(0, header.Length));
            if (content.CanSeek) content.Position = originalPosition;

            if (read <= 0) return null;

            var actualHeader = read == header.Length ? header : header[..read];
            return allowed.MatchesSignature(actualHeader) ? allowed.ContentType : null;
        }

        private static Func<byte[], bool> Signature(params byte[] expected) =>
            header => header.Length >= expected.Length && header.AsSpan(0, expected.Length).SequenceEqual(expected);

        private static bool IsWebP(byte[] header)
        {
            if (header.Length < 12) return false;
            return Encoding.ASCII.GetString(header, 0, 4) == "RIFF" &&
                   Encoding.ASCII.GetString(header, 8, 4) == "WEBP";
        }

        private static bool IsLikelyText(byte[] header)
        {
            if (header.Length == 0) return false;
            if (Array.IndexOf(header, (byte)0) >= 0) return false; // bytes nulos: no es texto plano
            if (header.Length >= 2 && header[0] == (byte)'M' && header[1] == (byte)'Z') return false; // EXE/DLL
            return true;
        }
    }
}
