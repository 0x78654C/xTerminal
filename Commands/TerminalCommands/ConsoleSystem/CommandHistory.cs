﻿using Core;
using Core.Commands;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    /*
     Outputs the stored commands in history.db file under the user profile.
     */
    [SupportedOSPlatform("Windows")]
    public class CommandHistory : ITerminalCommand
    {
        public string Name => "ch";
        private static string s_historyFile = GlobalVariables.historyFile;
        private static int s_countCommands = 0;
        private static string s_helpMessage = @"Usage of ch command:
    For display the last X commands that was used: ch x(numbers of commands to be displayed) 
    -h   : Displays this message.
    -sz  : Set the limit of commands that can be stored in history. Default set is 2000.
         Example: ch -sz 1000
    -rz  : Read the limit of commands that can be stored in history.
";

        public void Execute(string args)
        {
            try
            {
                string cmd = "";
                if (args.Contains(" "))
                    cmd = args.Split(' ').Skip(1).FirstOrDefault();

                //Display help message.
                if (cmd.StartsWith("-h"))
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                // Read history file command size from registry.
                if (cmd.StartsWith("-rz"))
                {
                    var regReadHistory = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regHistoryLimitSize);
                    FileSystem.WarningWriteLine($"Current history command limit size: {regReadHistory}");
                    return;
                }

                // Set history files commands store size.
                if (cmd.StartsWith("-sz"))
                {
                    int size = Int32.Parse(args.Split(' ').Skip(2).FirstOrDefault().Trim());
                    if (size > 0)
                    {
                        RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regHistoryLimitSize, size.ToString());
                        FileSystem.WarningWriteLine($"History command limit size set to: {size}");
                    }
                    else
                        FileSystem.ErrorWriteLine("You need to se size for history command limit store!");
                    return;
                }

                // Output commands.
                if (Int32.TryParse(cmd, out var position))
                {
                    OutputHistoryCommands(s_historyFile, position);
                    return;
                }
                OutputHistoryCommands(s_historyFile, 20);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }

        /// <summary>
        /// Output the commands from history.
        /// </summary>
        /// <param name="historyFileName">Path to history command file.</param>
        /// <param name="linesNumber">Number of commands to be displayed.</param>
        private static void OutputHistoryCommands(string historyFileName, int linesNumber)
        {
            if (!HistoryCommands.FileHasContent(historyFileName))
            {
                return;
            }

            //Disable limitation on for future tests.

            //if (linesNumber > 100)
            //{
            //    FileSystem.ErrorWriteLine("Only up to 100 commands can be displayed!");
            //    return;
            //}

            if (linesNumber < 0)
            {
                FileSystem.ErrorWriteLine("Check command again. You cannot use negative numbers!");
                return;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(historyFileName);
            }
            catch
            {
                FileSystem.ErrorWriteLine("Reading history file failed!");
                return;
            }

            var filteredLines = lines
                .Skip(lines.Length - linesNumber)
                .Where(line => !string.IsNullOrEmpty(line))
                .Take(linesNumber);


            // Eclude line numbers that are not needed to be displayed.
            bool isLineEmpty = lines.Any(l => string.IsNullOrEmpty(l));
            var linesCount = lines.Count();

            if (linesNumber >= linesCount)
                s_countCommands = 0;
            else
                s_countCommands = linesCount - linesNumber;

            foreach (string line in filteredLines)
            {
                s_countCommands++;
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                    GlobalVariables.pipeCmdOutput += $"{line}\n";
                else
                {
                    FileSystem.ColorConsoleText(ConsoleColor.White, $" {s_countCommands} -> ");
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Magenta, line);
                }
            }
        }
    }
}
