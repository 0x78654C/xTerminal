
using System;
using System.IO;

namespace Core
{
    public static class Extensions
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

        public static string SplitByText(this string input, string parameter, int index)
        {
            // return Regex.Split(input, parameter)[index];
            try
            {
                string[] output = input.Split(new string[] { parameter }, StringSplitOptions.None);
                return output[index];
            }
            catch
            {
                return "";
            }
        }

        internal static string MiddleString(this string input, string firstParam, string secondParam)
        {
            string firstParamSplit = input.SplitByText(firstParam + " ", 1);
            return firstParamSplit.SplitByText(" " + secondParam, 0);
        }

        internal static string MiddleStringNoSpace(this string input, string firstParam, string secondParam)
        {
            string firstParamSplit = input.SplitByText(firstParam, 1);
            return firstParamSplit.SplitByText(secondParam, 0);
        }
    }
}
