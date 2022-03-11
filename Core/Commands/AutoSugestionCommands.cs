﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Commands
{
    public class AutoSugestionCommands
    {

        /// <summary>
        /// Output sugestion for a file or directory name in current directory.
        /// </summary>
        /// <param name="consoleInput">Input from the console.</param>
        /// <param name="command">Command you use.</param>
        /// <param name="currentDirectory">Current directory.</param>
        /// <param name="isFile">If sugestion is for file.</param>
        public static void FileDirSugestion(string consoleInput, string command, string currentDirectory, bool isFile)
        {
            int commandLenght = command.Length + 1;

            if (consoleInput.StartsWith(command) && consoleInput.Length > command.Length)
            {
                if (isFile)
                {
                    GlobalVariables.autoSugestion = true;
                    consoleInput = consoleInput.Substring(commandLenght, consoleInput.Length - commandLenght);
                    SystemTools.AutoSugestion.FileCompletion(consoleInput, currentDirectory);
                    return;
                }
                GlobalVariables.autoSugestion = true;
                consoleInput = consoleInput.Substring(commandLenght, consoleInput.Length - commandLenght);
                SystemTools.AutoSugestion.DirCompletion(consoleInput, currentDirectory);
            }
        }

    }
}
