using System;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace Core.Commands
{
    [SupportedOSPlatform("Windows")]
    public class AutoSuggestionCommands
    {

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
                {
                    GlobalVariables.autoSuggestion = true;
                    GlobalVariables.commandOut = command;
                    Console.WriteLine("\r\n For auto suggestion use: command<SPACE KEY>start characters of files/directories");
                    SendKeys.SendWait("{ENTER}");
                    SendKeys.SendWait(consoleInput);
                }
                if ((consoleInput.StartsWith(command) && consoleInput.Length > command.Length))
                {
                    consoleInput = consoleInput.Substring(commandLenght, consoleInput.Length - commandLenght);
                    SystemTools.AutoSuggestion.FileDirCompletion(consoleInput, currentDirectory, typeSuggestions, ref addedCompletion);
                    if (multiParam.Length > 0)
                        GlobalVariables.commandOut = $"{command} {multiParam} {consoleInput}";
                    else
                        GlobalVariables.commandOut = $"{command} {consoleInput}";

                    if (string.IsNullOrEmpty(addedCompletion))
                    {
                        GlobalVariables.autoSuggestion = true;
                        if (multiParam.Length > 0)
                            SendKeys.SendWait($"{command} {multiParam} {consoleInput}");
                        else
                            SendKeys.SendWait($"{command} {consoleInput}");
                    }
                }
            }
            catch { }
        }
    }
}