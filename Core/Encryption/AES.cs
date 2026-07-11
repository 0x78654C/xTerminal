using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Encryption
{
    /// <summary>
    /// Authenticated password-based encryption for password-vault data.
    /// </summary>
    public static class AES
    {
        private const int CurrentVersion = 2;
        private const int SaltSize = 16;
        private const int NonceSize = 12;
        private const int TagSize = 16;
        private const int MemorySize = 64 * 1024;
        private const int Iterations = 3;
        private const int Parallelism = 2;
        private static readonly Encoding s_encoding = Encoding.UTF8;
        private static readonly byte[] s_associatedData = s_encoding.GetBytes("xTerminal-password-vault:v2");

        /// <summary>
        /// Encrypt plaintext with an Argon2id-derived key and AES-256-GCM.
        /// </summary>
        public static string Encrypt(string plainText, string password)
        {
            byte[] key = null;
            try
            {
                if (plainText == null)
                    throw new ArgumentNullException(nameof(plainText));
                if (string.IsNullOrEmpty(password))
                    throw new ArgumentException("A master password is required.", nameof(password));

                byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
                byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
                byte[] plaintextBytes = s_encoding.GetBytes(plainText);
                byte[] ciphertext = new byte[plaintextBytes.Length];
                byte[] tag = new byte[TagSize];
                key = Argon2.Argon2HashPassword(password, salt, MemorySize, Iterations, Parallelism);

                using (var aes = new AesGcm(key, TagSize))
                    aes.Encrypt(nonce, plaintextBytes, ciphertext, tag, s_associatedData);

                var payload = new VaultPayload
                {
                    Version = CurrentVersion,
                    Kdf = "argon2id",
                    MemorySize = MemorySize,
                    Iterations = Iterations,
                    Parallelism = Parallelism,
                    Salt = Convert.ToBase64String(salt),
                    Nonce = Convert.ToBase64String(nonce),
                    Value = Convert.ToBase64String(ciphertext),
                    Tag = Convert.ToBase64String(tag)
                };

                return Convert.ToBase64String(s_encoding.GetBytes(JsonSerializer.Serialize(payload)));
            }
            catch (Exception e)
            {
                return "Error encrypting: " + e.Message;
            }
            finally
            {
                if (key != null)
                    CryptographicOperations.ZeroMemory(key);
            }
        }

        /// <summary>
        /// Decrypt and authenticate password-vault data.
        /// </summary>
        public static string Decrypt(string encryptedText, string password)
        {
            byte[] key = null;
            try
            {
                if (string.IsNullOrEmpty(password))
                    throw new ArgumentException("A master password is required.", nameof(password));

                string json = s_encoding.GetString(Convert.FromBase64String(encryptedText));
                using (var document = JsonDocument.Parse(json))
                {
                    if (!document.RootElement.TryGetProperty("version", out var versionElement)
                        || versionElement.GetInt32() != CurrentVersion)
                        return DecryptLegacy(json, password);
                }

                var payload = JsonSerializer.Deserialize<VaultPayload>(json);
                ValidatePayload(payload);

                byte[] salt = Convert.FromBase64String(payload.Salt);
                byte[] nonce = Convert.FromBase64String(payload.Nonce);
                byte[] ciphertext = Convert.FromBase64String(payload.Value);
                byte[] tag = Convert.FromBase64String(payload.Tag);
                byte[] plaintext = new byte[ciphertext.Length];
                key = Argon2.Argon2HashPassword(
                    password,
                    salt,
                    payload.MemorySize,
                    payload.Iterations,
                    payload.Parallelism);

                using (var aes = new AesGcm(key, TagSize))
                    aes.Decrypt(nonce, ciphertext, tag, plaintext, s_associatedData);

                return s_encoding.GetString(plaintext);
            }
            catch (Exception e)
            {
                return "Error decrypting: " + e.Message;
            }
            finally
            {
                if (key != null)
                    CryptographicOperations.ZeroMemory(key);
            }
        }

        /// <summary>
        /// Return true when a vault uses the authenticated legacy format and should be rewritten as v2.
        /// </summary>
        public static bool NeedsMigration(string encryptedText)
        {
            try
            {
                string json = s_encoding.GetString(Convert.FromBase64String(encryptedText));
                using var document = JsonDocument.Parse(json);
                return !document.RootElement.TryGetProperty("version", out var version)
                    || version.ValueKind != JsonValueKind.Number
                    || version.GetInt32() != CurrentVersion;
            }
            catch
            {
                return false;
            }
        }

        private static void ValidatePayload(VaultPayload payload)
        {
            if (payload == null
                || payload.Version != CurrentVersion
                || !string.Equals(payload.Kdf, "argon2id", StringComparison.Ordinal)
                || payload.MemorySize < 8 * 1024 || payload.MemorySize > 256 * 1024
                || payload.Iterations < 1 || payload.Iterations > 10
                || payload.Parallelism < 1 || payload.Parallelism > 8)
                throw new CryptographicException("Invalid vault encryption parameters.");

            if (Convert.FromBase64String(payload.Salt).Length < SaltSize
                || Convert.FromBase64String(payload.Nonce).Length != NonceSize
                || Convert.FromBase64String(payload.Tag).Length != TagSize)
                throw new CryptographicException("Invalid vault encryption payload.");
        }

        // Read-only compatibility for vaults created before authenticated encryption was introduced.
        // Saving a vault always emits the v2 format above.
        private static string DecryptLegacy(string json, string password)
        {
            byte[] key = null;
            try
            {
                var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (payload == null || !payload.ContainsKey("iv") || !payload.ContainsKey("value") || !payload.ContainsKey("mac"))
                    throw new CryptographicException("Unsupported vault format.");
                if (password.Length < 12)
                    throw new CryptographicException("Invalid master password.");

                byte[] salt = s_encoding.GetBytes(password.Substring(2, 10));
                key = Argon2.Argon2HashPassword(password, salt, 4096, 40, 2);

                string authenticatedValue = payload["iv"] + payload["value"];
                byte[] expectedMac = Convert.FromHexString(payload["mac"]);
                byte[] actualMac;
                using (var hmac = new HMACSHA256(s_encoding.GetBytes(password)))
                    actualMac = hmac.ComputeHash(s_encoding.GetBytes(authenticatedValue));
                if (!CryptographicOperations.FixedTimeEquals(expectedMac, actualMac))
                    throw new CryptographicException("Vault authentication failed.");

                using (var aes = System.Security.Cryptography.Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.BlockSize = 128;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Mode = CipherMode.CBC;
                    aes.Key = key;
                    aes.IV = Convert.FromBase64String(payload["iv"]);
                    using (var decryptor = aes.CreateDecryptor())
                    {
                        byte[] ciphertext = Convert.FromBase64String(payload["value"]);
                        return s_encoding.GetString(decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length));
                    }
                }
            }
            finally
            {
                if (key != null)
                    CryptographicOperations.ZeroMemory(key);
            }
        }

        private sealed class VaultPayload
        {
            [JsonPropertyName("version")]
            public int Version { get; set; }

            [JsonPropertyName("kdf")]
            public string Kdf { get; set; }

            [JsonPropertyName("memorySize")]
            public int MemorySize { get; set; }

            [JsonPropertyName("iterations")]
            public int Iterations { get; set; }

            [JsonPropertyName("parallelism")]
            public int Parallelism { get; set; }

            [JsonPropertyName("salt")]
            public string Salt { get; set; }

            [JsonPropertyName("nonce")]
            public string Nonce { get; set; }

            [JsonPropertyName("value")]
            public string Value { get; set; }

            [JsonPropertyName("tag")]
            public string Tag { get; set; }
        }
    }
}
