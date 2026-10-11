using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace Core.Commands
{
    [SupportedOSPlatform("Windows")]
    public class AutoSuggestionCommands
    {
        /// <summary>
        /// Complete a path after the command and any earlier arguments.
        /// </summary>
        /// <param name="consoleInput">Input from the console.</param>
        /// <param name="command">Command you use.</param>
        /// <param name="currentDirectory">Current directory.</param>
        /// <param name="typeSuggestions">The kinds of paths to suggest.</param>
        /// <param name="addedCompletion">The complete input line when exactly one path matches.</param>
        public static void FileDirSuggestion(string consoleInput, string command, string currentDirectory, GlobalVariables.TypeSuggestions typeSuggestions, ref string addedCompletion)
        {
            var suggestions = GetPathSuggestions(consoleInput, command, currentDirectory, typeSuggestions, out int pathStart);
            if (suggestions.Length == 0)
                return;

            string prefix = consoleInput.Substring(0, pathStart);
            if (prefix == command)
                prefix += " ";

            if (suggestions.Length == 1)
            {
                addedCompletion = prefix + suggestions[0];
                return;
            }

            GlobalVariables.commandOut = consoleInput;
            GlobalVariables.autoSuggestion = true;
            SystemTools.AutoSuggestion.DisplayFileDirSuggestions(suggestions);
            SendKeys.SendWait(EscapeSendKeys(consoleInput));
        }

        public static string[] GetPathSuggestions(string consoleInput, string command, string currentDirectory,
            GlobalVariables.TypeSuggestions typeSuggestions, out int pathStart)
        {
            pathStart = 0;
            if (string.IsNullOrEmpty(consoleInput) ||
                !(consoleInput == command || consoleInput.StartsWith(command + " ", StringComparison.Ordinal)))
                return Array.Empty<string>();

            var argumentStarts = new List<int>();
            bool quoted = false;
            bool inArgument = false;
            for (int i = command.Length; i < consoleInput.Length; i++)
            {
                char character = consoleInput[i];
                if (!quoted && char.IsWhiteSpace(character))
                    inArgument = false;
                else if (!inArgument)
                {
                    argumentStarts.Add(i);
                    inArgument = true;
                }

                if (character == '"')
                    quoted = !quoted;
            }

            // A trailing space starts a new path, including immediately after a parameter.
            if (!inArgument)
            {
                argumentStarts.Clear();
                argumentStarts.Add(consoleInput.Length);
            }

            // Try longer tails first to retain completion for unquoted paths containing spaces.
            foreach (int start in argumentStarts)
            {
                string argument = consoleInput.Substring(start);
                if (argument.StartsWith("-", StringComparison.Ordinal))
                    continue;

                bool isQuoted = argument.StartsWith('"');
                string path = isQuoted ? argument.Substring(1).TrimEnd('"') : argument;
                var suggestions = SystemTools.AutoSuggestion.GetFileDirSuggestions(path, currentDirectory, typeSuggestions);
                if (suggestions.Length == 0)
                    continue;

                pathStart = start;
                return isQuoted ? suggestions.Select(suggestion => $"\"{suggestion}\"").ToArray() : suggestions;
            }

            return Array.Empty<string>();
        }

        private static string EscapeSendKeys(string input) => string.Concat(input.Select(character =>
            "+^%~(){}[]".Contains(character) ? "{" + character + "}" : character.ToString()));

        /// <summary>
        /// Get the command parameters that match the current argument prefix.
        /// </summary>
        /// <param name="consoleInput">Input from the console.</param>
        /// <param name="lastParam">Argument prefix to complete.</param>
        public static string[] CommandSuggestion(string consoleInput, string lastParam)
        {
            if (string.IsNullOrWhiteSpace(consoleInput) || string.IsNullOrWhiteSpace(lastParam))
                return Array.Empty<string>();

            var splitConsoleInput = consoleInput.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (splitConsoleInput.Length < 2 ||
                !GlobalVariables.masterParamCommands.TryGetValue(splitConsoleInput[0], out var parameters))
                return Array.Empty<string>();

            // The first entry is the command itself, not an argument.
            return parameters.Skip(1)
                .Where(parameter => parameter.StartsWith(lastParam, StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
    }
}
