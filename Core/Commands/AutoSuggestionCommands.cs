using System.Windows.Forms;

namespace Core.Commands
{
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
            int commandLenght = command.Length + 1;

            if (consoleInput.StartsWith(command) && consoleInput.Length > command.Length)
            {
                if (isFile)
                {
                    GlobalVariables.autoSuggestion = true;
                    consoleInput = consoleInput.Substring(commandLenght, consoleInput.Length - commandLenght);
                    SystemTools.AutoSuggestion.FileCompletion(consoleInput, currentDirectory);
                    SendKeys.Send(command + " ");
                    return;
                }
                GlobalVariables.autoSuggestion = true;
                consoleInput = consoleInput.Substring(commandLenght, consoleInput.Length - commandLenght);
                SystemTools.AutoSuggestion.DirCompletion(consoleInput, currentDirectory);
                SendKeys.Send(command+" ");
            }
        }
    }
}
