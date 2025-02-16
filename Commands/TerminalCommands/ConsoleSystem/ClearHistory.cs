using Core;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    /*
     Clears the commands from history file. Stored in History.db under current user profile.
     */
    [SupportedOSPlatform("Windows")]
    public class ClearHistory : ITerminalCommand
    {
        private static readonly string s_historyFile = GlobalVariables.historyFile;
        public string Name => "chistory";

        public void Execute(string arg)
        {
            if (File.Exists(s_historyFile))
            {
                try
                {
                    File.WriteAllText(s_historyFile, string.Empty);
                    Console.WriteLine("Command history log cleared!");
                }
                catch
                {
                    FileSystem.ErrorWriteLine("Clearing command history log failed!");
                    GlobalVariables.isErrorCommand = true;
                }
                return;
            }
            Console.WriteLine($"File '{s_historyFile}' does not exist!");
        }
    }
}
