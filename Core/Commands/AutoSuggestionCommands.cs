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
        public static void FileDirSuggestion(string consoleInput, string command, string currentDirectory, bool isFile, ref string addedCompletion)
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
                    if (isFile)
                    {
                        consoleInput = consoleInput.Substring(commandLenght, consoleInput.Length - commandLenght);
                        SystemTools.AutoSuggestion.FileCompletion(consoleInput, currentDirectory, ref addedCompletion);
                        GlobalVariables.commandOut = command + " " + consoleInput;
                        if (string.IsNullOrEmpty(addedCompletion))
                        {
                            GlobalVariables.autoSuggestion = true;
                            SendKeys.SendWait(command + " " + consoleInput);
                        }
                        return;
                    }
                    consoleInput = consoleInput.Substring(commandLenght, consoleInput.Length - commandLenght);
                    SystemTools.AutoSuggestion.DirCompletion(consoleInput, currentDirectory, ref addedCompletion);
                    GlobalVariables.commandOut = command + " " + consoleInput;
                    if (string.IsNullOrEmpty(addedCompletion))
                    {
                        GlobalVariables.autoSuggestion = true;
                        SendKeys.SendWait(command + " " + consoleInput);
                    }
                }
            }
            catch { }
        }
    }
}