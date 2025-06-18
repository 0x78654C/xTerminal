﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

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

        internal static string SplitByText(this string input, string parameter, int index)
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

        internal static string[] SplitByText(this string input, string parameter)
        {
            // return Regex.Split(input, parameter)[index];
            try
            {
                string[] output = input.Split(new string[] { parameter }, StringSplitOptions.None);
                return output;
            }
            catch
            {
                return null;
            }
        }
        internal static bool IsNotNullEmptyOrWhitespace(this string text)
        {
            if (text == null)
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(text);
        }

        internal static string MiddleString(this string input, string firstParam, string secondParam)
        {
            string firstParamSplit = input.SplitByText(firstParam + " ", 1);
            return firstParamSplit.SplitByText(" " + secondParam, 0);
        }

        /// <summary>
        /// Extension for read parameter value.
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        internal static string GetParamValue(this string arg, string param) => arg.SplitByText(param, 1).Trim().Split(' ')[0];

        /// <summary>
        /// Converts the secure string to string.
        /// </summary>
        /// <returns>The secure string to string.</returns>
        /// <param name="data">Data.</param>
        internal static string ConvertSecureStringToString(this SecureString data)
           => new System.Net.NetworkCredential(string.Empty, data).Password;

        /// <summary>
        /// Convert string to secure string.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static SecureString StringToSecureString(this string data)
        {
            var secureString = new SecureString();
            foreach (var c in data)
                secureString.AppendChar(c);
            return secureString;
        }
    }
}