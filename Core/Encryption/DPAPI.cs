using System;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace Core.Encryption
{
    [SupportedOSPlatform("windows")]
    public class DPAPI
    {
        /// <summary>
        /// Ctor for DPAPI
        /// </summary>
        public DPAPI() { }

        /// <summary>
        /// Encrypts the data using DPAPI
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Encrypt(string data)
        {
            byte[] encryptedData = ProtectedData.Protect(Encoding.UTF8.GetBytes(data), null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }

        /// <summary>
        /// Decrypts the data using DPAPI
        /// </summary>
        /// <param name="encryptedData"></param>
        /// <returns></returns>
        public static string Decrypt(string encryptedData)
        {
            byte[] decryptedData = ProtectedData.Unprotect(Convert.FromBase64String(encryptedData), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedData);
        }
    }
}
