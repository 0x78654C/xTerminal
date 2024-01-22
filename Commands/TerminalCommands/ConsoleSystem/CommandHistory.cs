using Core;
using Core.Commands;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    /*
     Outputs the stored commands in history.db file under the user profile.
     Max numbers of output is 100. 
     */
    [SupportedOSPlatform("Windows")]
    public class CommandHistory : ITerminalCommand
    {
        public string Name => "ch";
        private static string s_historyFile = GlobalVariables.historyFile;
        private static int countCommands = 100;

        public void Execute(string args)
        {
            try
            {
                string cmd = args.Split(' ').Skip(1).FirstOrDefault();

                if (Int32.TryParse(cmd, out var position))
                {
                    OutputHistoryCommands(s_historyFile, position);
                    return;
                }
                OutputHistoryCommands(s_historyFile, 10);
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

            if (linesNumber > 100)
            {
                FileSystem.ErrorWriteLine("Only up to 100 commands can be displayed!");
                return;
            }

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
            countCommands = countCommands - linesNumber;

            foreach (string line in filteredLines)
            {
                countCommands++;
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                    GlobalVariables.pipeCmdOutput += $"{line}\n";
                else
                {
                    FileSystem.ColorConsoleText(ConsoleColor.White, $" {countCommands} -> ");
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Magenta, line);
                }
            }
        }
    }
}
