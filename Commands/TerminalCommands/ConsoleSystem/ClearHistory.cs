using System;
using System.IO;
using Core;

namespace Commands.TerminalCommands.ConsoleSystem
{
    /*
     Clears the commands from history file. Stored in History.db under current user profile.
     */
    public class ClearHistory : ITerminalCommand
    {
        private static string s_historyFile = GlobalVariables.historyFile;
        public string Name => "chistory";
        public void Execute(string arg)
        {
            if (File.Exists(s_historyFile))
            {
                File.WriteAllText(s_historyFile, Environment.NewLine);
                Console.WriteLine("Command history log cleared!");
                return;
            }
            Console.WriteLine("File '" + s_historyFile + "' dose not exist!");
        }
    }
}
