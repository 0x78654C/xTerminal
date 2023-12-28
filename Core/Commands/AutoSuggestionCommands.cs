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
        public static void FileDirSuggestion(string consoleInput, string command, string currentDirectory, bool isFile)
        {
            try
            {
                if (consoleInput.StartsWith(".-"))
                    consoleInput = consoleInput.Replace(".-", "./");

                int commandLenght = command.Length + 1;
                if (consoleInput == command)
                {
                    GlobalVariables.autoSuggestion = true;
                    GlobalVariables.commandOut = command;
                    Console.WriteLine("\r\n For auto suggestion use: command<SPACE KEY>start characters of files/directories");
                    SendKeys.Send("{ENTER}");
                    SendKeys.Send(consoleInput);
                }
                if ((consoleInput.StartsWith(command) && consoleInput.Length > command.Length))
                {
                    if (isFile)
                    {
                        GlobalVariables.autoSuggestion = true;
                        consoleInput = consoleInput.Substring(commandLenght, consoleInput.Length - commandLenght);
                        SystemTools.AutoSuggestion.FileCompletion(consoleInput, currentDirectory);
                        consoleInput = consoleInput.Replace(".-", "./");
                        GlobalVariables.commandOut = command + " " + consoleInput;
                        SendKeys.Send(command + " " + consoleInput);
                        return;
                    }
                    GlobalVariables.autoSuggestion = true;
                    consoleInput = consoleInput.Substring(commandLenght, consoleInput.Length - commandLenght);
                    SystemTools.AutoSuggestion.DirCompletion(consoleInput, currentDirectory);
                    GlobalVariables.commandOut = command + " " + consoleInput;
                    SendKeys.Send(command + " " + consoleInput);
                }
            }
            catch { }
        }
    }
}
