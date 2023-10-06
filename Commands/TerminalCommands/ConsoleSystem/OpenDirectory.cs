using Core;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class OpenDirectory : ITerminalCommand
    {
        /*
         * Opens current directory or other directory path provided.
         */
        public string Name => "odir";
        private static string s_helpMessage = @"Usage of odir command:

                    odir : Opens the current directory.
   odir <directory_path> : Opens the specified directory.
";

        public void Execute(string arg)
        {
            if (arg == $"{Name} -h")
            {
                Console.WriteLine(s_helpMessage);
                return;
            }

            string currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);
            string args = string.Empty;

            if (GlobalVariables.isPipeCommand)
            {
                args = GlobalVariables.pipeCmdOutput.Trim();
                GlobalVariables.pipeCmdOutput = string.Empty;
            }
            else
                args = arg.Replace("odir", string.Empty).Trim();

            if (!string.IsNullOrEmpty(args))
                FileSystem.OpenCurrentDiretory(args, currentDirectory);
            else
                FileSystem.OpenCurrentDiretory(currentDirectory, currentDirectory);
        }
    }
}
