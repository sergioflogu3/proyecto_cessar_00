using System.Security.Cryptography;
using System.Text;

namespace SistemaTickets.Infrastructure.Security
{
    public static class EncryptionHelper
    {
        public static string HashPassword(string password) =>
            BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        public static bool VerifyPassword(string password, string hash) =>
            BCrypt.Net.BCrypt.Verify(password, hash);

        // Encriptación AES-256 determinista: mismo texto + clave = mismo resultado.
        // Necesario para poder buscar por email/teléfono encriptado en la BD.
        public static string Encrypt(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            var keyBytes = DeriveKeyBytes(key);
            var iv = keyBytes[..16];

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = iv;

            var encryptor = aes.CreateEncryptor();
            var inputBytes = Encoding.UTF8.GetBytes(plainText);
            var encrypted = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
            return Convert.ToBase64String(encrypted);
        }

        public static string Decrypt(string cipherText, string key)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try
            {
                var keyBytes = DeriveKeyBytes(key);
                var iv = keyBytes[..16];

                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.IV = iv;

                var decryptor = aes.CreateDecryptor();
                var inputBytes = Convert.FromBase64String(cipherText);
                var decrypted = decryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                // Dato almacenado sin encriptar (registros previos a la migración): devolver tal cual.
                return cipherText;
            }
        }

        // Normaliza cualquier clave de config a exactamente 32 bytes para AES-256.
        private static byte[] DeriveKeyBytes(string key)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        }
    }
}
