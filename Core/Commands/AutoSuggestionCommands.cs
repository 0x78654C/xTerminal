using System;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Core.Commands
{
    [SupportedOSPlatform("Windows")]
    public class AutoSuggestionCommands
    {
        private static List<string> s_listParamsSuggested = new();
        /// <summary>
        /// Output sugestion for a file or directory name in current directory.
        /// </summary>
        /// <param name="consoleInput">Input from the console.</param>
        /// <param name="command">Command you use.</param>
        /// <param name="currentDirectory">Current directory.</param>
        /// <param name="isFile">If sugestion is for file.</param>
        public static void FileDirSuggestion(string consoleInput, string multiParam, string command, string currentDirectory, GlobalVariables.TypeSuggestions typeSuggestions, ref string addedCompletion)
        {
            try
            {
                int commandLenght = command.Length + 1;
                if (consoleInput == command)
                    consoleInput = command + " ";

                if (consoleInput.Split(' ')[0] == command && consoleInput.Length > command.Length)
                {
                    consoleInput = consoleInput.Substring(commandLenght, consoleInput.Length - commandLenght);
                    SystemTools.AutoSuggestion.FileDirCompletion(consoleInput, currentDirectory, typeSuggestions, ref addedCompletion);
                    if (multiParam.Length > 0)
                        GlobalVariables.commandOut = $"{command} {multiParam.Trim()} {consoleInput}";
                    else
                        GlobalVariables.commandOut = $"{command} {consoleInput}";

                    if (string.IsNullOrEmpty(addedCompletion))
                    {
                        GlobalVariables.autoSuggestion = true;
                        if (multiParam.Length > 0)
                            SendKeys.SendWait($"{command} {multiParam.Trim()} {consoleInput}");
                        else
                            SendKeys.SendWait($"{command} {consoleInput}");
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Autocomplete command parameters.
        /// </summary>
        /// <param name="consoleInput"></param>
        public static void CommandSuggestion(string consoleInput,string lastParam, out bool isComplete)
        {
            isComplete = false;
            if (string.IsNullOrEmpty(consoleInput))
                return;

            var splitConsoleInput = consoleInput.Split(" ");
     
            var command = splitConsoleInput[0];
            if (lastParam.StartsWith("-"))
            {
                var listParams = GlobalVariables.masterParamCommands;
                foreach (var paramList in listParams)
                {
                    if (paramList.Key == command)
                    {
                        var isAlreadySuggested = s_listParamsSuggested.Any(p => p == lastParam);
                        var suggest = paramList.Value.Where(p => p.StartsWith(lastParam) && !isAlreadySuggested).First();
                        isComplete = true;
                        s_listParamsSuggested.Add(suggest);
                        Console.SetCursorPosition(0, consoleInput.Length - lastParam.Length);
                        SendKeys.SendWait($"{command} {suggest}");
                        if (s_listParamsSuggested.Count == paramList.Value.Count)
                            s_listParamsSuggested.Clear();
                        break; 
                    }
                }
            }
        }
    }
}