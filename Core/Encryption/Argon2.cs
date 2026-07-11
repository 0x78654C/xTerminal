using Konscious.Security.Cryptography;
using System;
using System.Text;

namespace Core.Encryption
{
    public static class Argon2
    {
        /// <summary>
        /// Derive key material from a password and a caller-provided random salt.
        /// </summary>
        public static byte[] Argon2HashPassword(
            string password,
            byte[] salt,
            int memorySize,
            int iterations,
            int degreeOfParallelism,
            int outputLength = 32)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));
            if (salt == null || salt.Length < 8)
                throw new ArgumentException("Argon2 salts must contain at least 8 bytes.", nameof(salt));
            if (memorySize < 8 * 1024 || memorySize > 1024 * 1024)
                throw new ArgumentOutOfRangeException(nameof(memorySize));
            if (iterations < 1 || iterations > 100)
                throw new ArgumentOutOfRangeException(nameof(iterations));
            if (degreeOfParallelism < 1 || degreeOfParallelism > 16)
                throw new ArgumentOutOfRangeException(nameof(degreeOfParallelism));

            using (var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = (byte[])salt.Clone(),
                DegreeOfParallelism = degreeOfParallelism,
                Iterations = iterations,
                MemorySize = memorySize
            })
            {
                return argon2.GetBytes(outputLength);
            }
        }
    }
}
