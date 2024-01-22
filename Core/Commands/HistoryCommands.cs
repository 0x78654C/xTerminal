using System;
using System.IO;
using System.Runtime.Versioning;

namespace Core.Commands
{
    [SupportedOSPlatform("Windows")]

    public class HistoryCommands
    {

        /// <summary>
        ///  Get command at a speciofic position.
        /// </summary>
        /// <param name="historyFileName"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string GetHistoryCommand(string historyFileName, int number)
        {
            string command = "";
            if (!FileHasContent(historyFileName))
            {
                return "";
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(historyFileName);
            }
            catch
            {
                FileSystem.ErrorWriteLine("Reading history file failed!");
                return "";
            }
            int countLines = 0;
            foreach (string line in lines)
            {
                countLines++;
                if (countLines == number)
                {
                    FileSystem.ColorConsoleText(ConsoleColor.White, $" {countLines} -> ");
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Magenta, line);
                    command = line;
                }
            }
            return command;
        }

        /// <summary>
        ///  We check if history file has any data in it.
        /// </summary>
        /// <param name="historyFileName"></param>
        /// <returns></returns>
        public static bool FileHasContent(string historyFileName)
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
    }
}
