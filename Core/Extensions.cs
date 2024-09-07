
using System.IO;

namespace Core
{
    internal static class Extensions
    {
        internal static int CountLines(this string input)
        {
            var reader = new StringReader(input);
            string line;
            var count = 0;
            while ((line = reader.ReadLine()) != null)
                count++;
            return count;
        }
    }
}
