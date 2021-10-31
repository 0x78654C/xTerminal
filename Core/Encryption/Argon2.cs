using System.Linq;
using System.Text;
using Konscious.Security.Cryptography;


namespace Core.Encryption
{
    public static class Argon2
    {
        public static Argon2id s_argon2;

		/// <summary>
		/// Argon2 Password Hash
		/// </summary>
		/// <param name="password"></param>
		/// <returns></returns>
		public static byte[] Argon2HashPassword(string password)
		{
			s_argon2 = new Argon2id(Encoding.UTF8.GetBytes(password));
			s_argon2.Salt = Encoding.UTF8.GetBytes(password.Substring(2, 10));
			s_argon2.DegreeOfParallelism = 2;
			s_argon2.Iterations = 40;
			s_argon2.MemorySize = 4096;
			return s_argon2.GetBytes(32);
		}
	}
}
