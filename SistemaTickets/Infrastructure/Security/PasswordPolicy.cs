using System.Reflection;

namespace SistemaTickets.Infrastructure.Security
{
    // M2: política mínima de contraseñas — longitud mínima + lista de contraseñas comunes.
    // La lista viene de SecLists (danielmiessler/SecLists, MIT), archivo
    // Passwords/Common-Credentials/10k-most-common.txt, embebida en el ensamblado
    // (ver SistemaTickets.csproj) como SistemaTickets.CommonPasswords.txt.
    public static class PasswordPolicy
    {
        public const int MinLength = 12;

        private const string ResourceName = "SistemaTickets.CommonPasswords.txt";

        private static readonly Lazy<HashSet<string>> ContraseñasComunes = new(CargarContraseñasComunes);

        public static bool EsContraseñaComun(string password) =>
            !string.IsNullOrEmpty(password) && ContraseñasComunes.Value.Contains(password);

        // M2: exige al menos una minúscula, una mayúscula, un número y un carácter especial
        // (cualquier cosa que no sea letra ni dígito).
        public static bool TieneComplejidadSuficiente(string password) =>
            !string.IsNullOrEmpty(password) &&
            password.Any(char.IsLower) &&
            password.Any(char.IsUpper) &&
            password.Any(char.IsDigit) &&
            password.Any(c => !char.IsLetterOrDigit(c));

        private static HashSet<string> CargarContraseñasComunes()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"No se encontró el recurso embebido '{ResourceName}'.");
            using var reader = new StreamReader(stream);

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                var trimmed = line.Trim();
                if (trimmed.Length > 0) set.Add(trimmed);
            }

            return set;
        }
    }
}
