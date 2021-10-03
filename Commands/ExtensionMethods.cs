using System;
using System.Collections.Generic;
using System.Linq;

namespace Commands
{
    internal static class ExtensionMethods
    {
        internal static bool ContainsText(this string source, string searchText)
        {
            return !string.IsNullOrWhiteSpace(source) &&
                   source.ToLowerInvariant().ContainsText(searchText.ToLowerInvariant());
        }

        internal static bool ContainsParameter(this IEnumerable<string> parameters, string parameter)
        {
            return parameters.Any(p => p.Equals(parameter, StringComparison.InvariantCulture));
        }

        internal static string ParameterAfter(this IEnumerable<string> parameters, string parameter)
        {
            var parms = parameters.ToList();

            int index = parms.FindIndex(s => s.Equals(parameter, StringComparison.InvariantCulture));

            // Return an empty string if the parameter does not exist,
            // or if there is not another value after the searched parameter.
            if (index == -1 || index + 1 <= parms.Count)
            {
                return "";
            }

            return parms[index + 1];
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