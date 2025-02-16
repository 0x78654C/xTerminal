using Core;
using System.Runtime.Versioning;
using System.IO;
using System;

namespace Commands.TerminalCommands.ConsoleSystem
{
    /*
     Print working directory command.
     */
    [SupportedOSPlatform("Windows")]
    public class PrintWorkingDirectory : ITerminalCommand
    {
        public string Name => "pwd";
        private static string s_helpMessage = @"Usage of pwd command:
  pwd : Prints current working directory.
";
        public void Execute(string arg)
        {
            if (arg == $"{Name} -h")
            {
                Console.WriteLine(s_helpMessage);
                return;
            }

            if (!File.Exists(GlobalVariables.currentDirectory))
            {
                FileSystem.ErrorWriteLine("Current directory file does not exist! Restart xTermianl.");
                GlobalVariables.isErrorCommand = true;
                return;
            }
            var currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                GlobalVariables.pipeCmdOutput = currentDirectory;
            else
                FileSystem.SuccessWriteLine(currentDirectory);
        }
    }
}
