using System;
using System.IO;
using System.Security.Cryptography;

namespace Core.Encryption
{
    public class MD5Hash
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
    }
}
