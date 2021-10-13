using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Commands
{
    internal static class ExtensionMethods
    {
        internal static bool ContainsText(this string source, string searchText)
        {
            return !string.IsNullOrWhiteSpace(source) &&
                   source.ToLowerInvariant().Contains(searchText.ToLowerInvariant());
        }

        internal static bool ContainsParameter(this IEnumerable<string> parameters, string parameter)
        {
            return parameters.Any(p => p.Equals(parameter, StringComparison.InvariantCulture));
        }

        internal static string ParameterAfter(this IEnumerable<string> parameters, string parameter)
        {
            var parms = parameters.ToList();
            string p = string.Join(" ", parms);
            int index = parms.FindIndex(s => s.Equals(parameter, StringComparison.InvariantCulture));
            
            // Return an empty string if the parameter does not exist,
            // or if there is not another value after the searched parameter.
            if (index == -1 || index + 1 == parms.Count)
            {
                return "";
            }

            return parms[index + 1];
        }

        internal static string SplitByText( this string input,string parameter,int index)
        {
            // return Regex.Split(input, parameter)[index];
            string[] output = input.Split(new string[] { parameter }, StringSplitOptions.None);
            return output[index];
        }

        internal static bool IsNotNullEmptyOrWhitespace(this string text)
        {
            if (text == null)
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(text);
        }
    }
}