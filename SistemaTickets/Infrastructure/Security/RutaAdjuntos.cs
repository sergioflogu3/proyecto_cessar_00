using System.Text.RegularExpressions;

namespace SistemaTickets.Infrastructure.Security
{
    // Spec 004: reglas de rutas para guardar adjuntos en una carpeta del servidor. Son funciones
    // puras (no leen ni escriben en disco) para poder comprobarlas de forma aislada; el acceso
    // real a archivos vive en LocalFileStorageService.
    public static class RutaAdjuntos
    {
        private static readonly Regex ExtensionValida =
            new(@"^\.[A-Za-z0-9]{1,10}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Windows no distingue mayúsculas en rutas; Linux sí. Comparar sin distinguirlas en
        // Linux dejaría pasar "../ADJUNTOS/x" como si estuviera dentro de "/tmp/adjuntos".
        private static StringComparison ComparacionRutas =>
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        // RF-5, RF-6, RF-8: ruta absoluta de la carpeta de adjuntos. Una ruta relativa se
        // interpreta respecto de la carpeta de la aplicación (en MonsterASP, "../private/uploads"
        // queda fuera de /wwwroot). Nunca puede ser la carpeta pública ni estar dentro de ella:
        // UseStaticFiles serviría los adjuntos sin pasar por el control de permisos.
        public static string ResolverCarpeta(string? carpeta, string contentRoot, string? webRoot)
        {
            if (string.IsNullOrWhiteSpace(carpeta))
            {
                throw new InvalidOperationException(
                    "Adjuntos:Carpeta no está configurada y Adjuntos:Destino es \"Carpeta\". Defínela " +
                    "mediante variable de entorno (Adjuntos__Carpeta), por ejemplo \"../private/uploads\".");
            }

            var ruta = Path.TrimEndingDirectorySeparator(Path.GetFullPath(
                Path.IsPathRooted(carpeta) ? carpeta : Path.Combine(contentRoot, carpeta)));

            if (!string.IsNullOrWhiteSpace(webRoot) && EsIgualODentro(ruta, Path.GetFullPath(webRoot)))
            {
                throw new InvalidOperationException(
                    $"Adjuntos:Carpeta (\"{ruta}\") está dentro de la carpeta pública de la aplicación " +
                    $"(\"{webRoot}\"), desde donde se serviría sin control de permisos. Usa una carpeta " +
                    "fuera de ella, por ejemplo \"../private/uploads\".");
            }

            return ruta;
        }

        // RF-13: ruta absoluta de un adjunto registrado, o null si no es válida o saldría de la
        // carpeta de adjuntos (separadores sueltos, "..", rutas absolutas, unidades de Windows).
        public static string? ResolverDentroDeRaiz(string raiz, string? rutaRegistrada)
        {
            if (string.IsNullOrWhiteSpace(rutaRegistrada) ||
                rutaRegistrada.Contains('\0') ||
                rutaRegistrada.Contains(':') ||
                rutaRegistrada[0] is '/' or '\\' ||
                Path.IsPathRooted(rutaRegistrada))
            {
                return null;
            }

            var relativa = rutaRegistrada
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            var raizCompleta = Path.GetFullPath(raiz);
            var ruta = Path.GetFullPath(Path.Combine(raizCompleta, relativa));

            return ruta.StartsWith(ConSeparadorFinal(raizCompleta), ComparacionRutas) ? ruta : null;
        }

        // RF-10, RF-13: extensión del nombre subido, en minúsculas, solo si es alfanumérica y
        // corta; si no, "". El resto del nombre original nunca llega al disco.
        public static string ExtensionSegura(string? nombreArchivo)
        {
            if (string.IsNullOrWhiteSpace(nombreArchivo)) return string.Empty;

            var normalizado = nombreArchivo.Replace('\\', '/');
            var ultimoSegmento = normalizado[(normalizado.LastIndexOf('/') + 1)..];
            var extension = Path.GetExtension(ultimoSegmento);

            return ExtensionValida.IsMatch(extension) ? extension.ToLowerInvariant() : string.Empty;
        }

        private static bool EsIgualODentro(string ruta, string raiz)
        {
            var raizSinSeparador = Path.TrimEndingDirectorySeparator(raiz);
            return string.Equals(ruta, raizSinSeparador, ComparacionRutas) ||
                   ruta.StartsWith(ConSeparadorFinal(raizSinSeparador), ComparacionRutas);
        }

        private static string ConSeparadorFinal(string ruta) =>
            Path.EndsInDirectorySeparator(ruta) ? ruta : ruta + Path.DirectorySeparatorChar;
    }
}
