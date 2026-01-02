using System.Security.Cryptography;
using System.Text;

namespace Praxis.Services
{
    /// <summary>
    /// Service for encrypting and decrypting sensitive data (OAuth tokens, refresh tokens) using Windows DPAPI.
    /// Tokens are encrypted with the current Windows user's key, so only that user can decrypt them.
    /// </summary>
    public class TokenEncryptionService
    {
        private static readonly Lazy<TokenEncryptionService> _instance = 
            new(() => new TokenEncryptionService(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static TokenEncryptionService Instance => _instance.Value;

        private TokenEncryptionService()
        {
        }

        /// <summary>
        /// Encrypts a string using Windows DPAPI (Data Protection API).
        /// </summary>
        /// <param name="plainText">The text to encrypt.</param>
        /// <returns>Base64-encoded encrypted string, or null if encryption fails.</returns>
        public string? Encrypt(string? plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return null;

            try
            {
                // Convert string to bytes
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

                // Encrypt using DPAPI (CurrentUser scope - only this user can decrypt)
                byte[] encryptedBytes = ProtectedData.Protect(
                    plainBytes,
                    null, // Optional entropy (additional security)
                    DataProtectionScope.CurrentUser);

                // Convert to base64 for storage
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error encrypting token: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Decrypts a string that was encrypted using Windows DPAPI.
        /// </summary>
        /// <param name="encryptedText">The base64-encoded encrypted text.</param>
        /// <returns>The decrypted string, or null if decryption fails.</returns>
        public string? Decrypt(string? encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return null;

            try
            {
                // Convert from base64
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

                // Decrypt using DPAPI
                byte[] plainBytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    null, // Optional entropy (must match encryption)
                    DataProtectionScope.CurrentUser);

                // Convert back to string
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (CryptographicException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error decrypting token - may be encrypted for different user or corrupted: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error decrypting token: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks if a string appears to be encrypted (base64 format check).
        /// </summary>
        public bool IsEncrypted(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            try
            {
                // Try to decode as base64
                Convert.FromBase64String(text);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
