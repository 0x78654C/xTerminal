using System;
using System.IO;
using System.Security.Cryptography;

namespace Core.Encryption
{
    public class HashAlgo
    {
        /// <summary>
        /// Get MD5 Hash.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string GetMD5Hash(string file)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(file))
                {
                    var hash = md5.ComputeHash(stream);
                    var sourceMD5 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    return sourceMD5;
                }
            }
        }

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

        /// <summary>
        /// Get SHA512 hash
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string GetSHA512(string file)
        {
            using (var sha512 = SHA512.Create())
            {
                using (var stream = File.OpenRead(file))
                {
                    var hash = sha512.ComputeHash(stream);
                    var sourceSHA512 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    return sourceSHA512;
                }
            }
        }
    }
}
