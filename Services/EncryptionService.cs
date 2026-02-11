using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Jot.Models;

namespace Jot.Services
{
    /// <summary>
    /// Servicio para cifrar y descifrar documentos
    /// </summary>
    public class EncryptionService
    {
        private const int KeySize = 256;
        private const int BlockSize = 128;
        private const int Iterations = 10000;

        /// <summary>
        /// Cifra el contenido de un documento con una contraseña
        /// </summary>
        public async Task<bool> EncryptDocumentAsync(Document document, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(password))
                {
                    return false;
                }

                // Generar hash de la contraseña para verificación
                document.PasswordHash = HashPassword(password);

                // Cifrar el contenido
                document.EncryptedContent = EncryptString(document.Content, password);
                
                // Marcar como cifrado y limpiar contenido en texto plano
                document.IsEncrypted = true;
                document.Content = "[ENCRYPTED - Enter password to view]";

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error encrypting document: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Descifra el contenido de un documento con una contraseña
        /// </summary>
        public async Task<bool> DecryptDocumentAsync(Document document, string password)
        {
            try
            {
                if (!document.IsEncrypted || string.IsNullOrEmpty(password))
                {
                    return false;
                }

                // Verificar contraseña
                if (!VerifyPassword(password, document.PasswordHash))
                {
                    return false;
                }

                // Descifrar contenido
                document.Content = DecryptString(document.EncryptedContent, password);
                
                // Marcar como descifrado (temporalmente)
                document.IsEncrypted = false;

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error decrypting document: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Bloquea un documento (lo vuelve a cifrar)
        /// </summary>
        public async Task<bool> LockDocumentAsync(Document document, string password)
        {
            try
            {
                if (document.IsEncrypted)
                {
                    return true; // Ya está bloqueado
                }

                // Si tiene contenido cifrado guardado, solo marcarlo como cifrado
                if (!string.IsNullOrEmpty(document.EncryptedContent))
                {
                    document.Content = "[ENCRYPTED - Enter password to view]";
                    document.IsEncrypted = true;
                    return true;
                }

                // Si no, cifrarlo de nuevo
                return await EncryptDocumentAsync(document, password);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error locking document: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Verifica si una contraseña es correcta para un documento
        /// </summary>
        public bool VerifyPassword(string password, string passwordHash)
        {
            try
            {
                var hash = HashPassword(password);
                return hash == passwordHash;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Genera un hash SHA256 de la contraseña
        /// </summary>
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Cifra una cadena usando AES-256
        /// </summary>
        private string EncryptString(string plainText, string password)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return "";
            }

            byte[] salt = GenerateSalt();
            
            using (var aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;

                var key = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
                aes.Key = key.GetBytes(aes.KeySize / 8);
                aes.IV = key.GetBytes(aes.BlockSize / 8);

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var msEncrypt = new MemoryStream())
                {
                    // Escribir el salt al inicio
                    msEncrypt.Write(salt, 0, salt.Length);

                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        /// <summary>
        /// Descifra una cadena usando AES-256
        /// </summary>
        private string DecryptString(string cipherText, string password)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                return "";
            }

            byte[] buffer = Convert.FromBase64String(cipherText);
            byte[] salt = new byte[32];
            Array.Copy(buffer, 0, salt, 0, salt.Length);

            using (var aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;

                var key = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
                aes.Key = key.GetBytes(aes.KeySize / 8);
                aes.IV = key.GetBytes(aes.BlockSize / 8);

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var msDecrypt = new MemoryStream(buffer, salt.Length, buffer.Length - salt.Length))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Genera un salt aleatorio
        /// </summary>
        private byte[] GenerateSalt()
        {
            byte[] salt = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        /// <summary>
        /// Cambia la contraseña de un documento cifrado
        /// </summary>
        public async Task<bool> ChangePasswordAsync(Document document, string oldPassword, string newPassword)
        {
            try
            {
                // Verificar contraseña actual
                if (!VerifyPassword(oldPassword, document.PasswordHash))
                {
                    return false;
                }

                // Descifrar con contraseña actual
                var plainContent = DecryptString(document.EncryptedContent, oldPassword);

                // Cifrar con nueva contraseña
                document.PasswordHash = HashPassword(newPassword);
                document.EncryptedContent = EncryptString(plainContent, newPassword);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error changing password: {ex.Message}");
                return false;
            }
        }
    }
}
