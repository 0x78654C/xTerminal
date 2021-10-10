﻿using Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Commands.TerminalCommands.ConsoleSystem
{
    /*
     Outputs the stored commands in history.db file under the user profile.
     Max numbers of output is 100. 
     */
    public class CommandHistory : ITerminalCommand
    {
        public string Name => "hcmd";
        private static readonly string s_accountName = Environment.UserName;
        private static string s_historyFilePath = $"C:\\Users\\{s_accountName}\\AppData\\Local\\xTerminal";
        private static string s_historyFile = s_historyFilePath + "\\History.db";

        public void Execute(string args)
        {
            try
            {
                string cmd = args.Split(' ').Skip(1).FirstOrDefault();

                if (Int32.TryParse(cmd, out var position))
                {
                    OutputHistoryCommands(s_historyFile, position);
                }
                else
                {
                    OutputHistoryCommands(s_historyFile, 1);
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }

        // We check if history file has any data in it.
        private static bool CheckHistoryFileLength(string historyFileName)
        {
            if (!File.Exists(historyFileName))
            {
                Console.WriteLine("History file not exists!");
                return false;
            }
            using (StringReader stringReader = new StringReader(historyFileName))
            {
                string historFileData = stringReader.ReadToEnd();
                if (historFileData.Length > 0)
                {
                    return true;
                }
                Console.WriteLine("No commands in list!");
                return false;
            }
        }

        /// <summary>
        /// Output the commands from history.
        /// </summary>
        /// <param name="historyFileName">Path to history command file.</param>
        /// <param name="linesNumber">Number of commnands to be displayed.</param>
        private static void OutputHistoryCommands(string historyFileName, int linesNumber)
        {
            if (CheckHistoryFileLength(historyFileName) == false)
            {
                return;
            }

            if (linesNumber > 100)
            {
                FileSystem.ErrorWriteLine("Only 100 commands can be displayed!");
                return;
            }

            if (linesNumber < 0)
            {
                FileSystem.ErrorWriteLine("Check command again. You cannot use negative numbers!");
                return;
            }

            int index = 0;
            int linesCount = File.ReadAllLines(historyFileName).Count();
            var lines = File.ReadLines(historyFileName).Skip(linesCount-linesNumber);

            foreach (var line in lines)
            {
                if (index <= linesNumber)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        FileSystem.ColorConsoleText(ConsoleColor.White, "--> ");
                        FileSystem.ColorConsoleTextLine(ConsoleColor.Magenta, line);
                    }
                    index++;
                }
            }
        }
    }
}
