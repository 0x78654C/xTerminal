using System.Security.Cryptography;

namespace AutoUpdater.Utils
{
    public static class Encryption
    {
        /// <summary>
        /// Get SHA256 hash
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string GetSHA256(string file)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var stream = File.OpenRead(file))
                {
                    var hash = sha256.ComputeHash(stream);
                    var sourceSHA256 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    return sourceSHA256;
                }
            }
        }
    }
}
