using Core;
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

        // We check if history file has any data in it.
        private static bool FileHasContent(string historyFileName)
        {
            FileInfo fileInfo = new FileInfo(historyFileName);

            if (!fileInfo.Exists)
            {
                Console.WriteLine("History file does not exist!");
                return false;
            }

            if (fileInfo.Length < 0)
            {
                Console.WriteLine("No commands in list!");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Output the commands from history.
        /// </summary>
        /// <param name="historyFileName">Path to history command file.</param>
        /// <param name="linesNumber">Number of commands to be displayed.</param>
        private static void OutputHistoryCommands(string historyFileName, int linesNumber)
        {
            if (!FileHasContent(historyFileName))
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
            foreach (string line in filteredLines)
            {
                FileSystem.ColorConsoleText(ConsoleColor.White, "--> ");
                FileSystem.ColorConsoleTextLine(ConsoleColor.Magenta, line);
            }
        }
    }
}
