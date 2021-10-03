using System;
using System.IO;

namespace Commands.TerminalCommands.ConsoleSystem
{
    public class ClearHistory : ITerminalCommand
    {
        private static readonly string s_accountName = Environment.UserName;
        private static string s_historyFilePath = $"C:\\Users\\{s_accountName}\\AppData\\Local\\xTerminal";
        private static string s_historyFile = s_historyFilePath + "\\History.db";
        public string Name => "chistory";
        public void Execute(string arg)
        {
            if (File.Exists(s_historyFile))
            {
                File.WriteAllText(s_historyFile, string.Empty);
                Console.WriteLine("Command history log cleared!");
            }
            else
            {
                Console.WriteLine("File '" + s_historyFile + "' dose not exist!");
            }
        }
    }
}
