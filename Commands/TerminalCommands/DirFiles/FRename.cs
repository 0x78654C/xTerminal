using Core;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.DirFiles
{
    [SupportedOSPlatform("Windows")]
    public class FRename : ITerminalCommand
    {
        /*
         File rename.
         */
        public string Name => "frename";

        public void Execute(string arg)
        {
            try
            {
                arg = arg.Replace("frename ", "");

                // Reading current location(for test no, after i make dynamic)
                string dlocation = File.ReadAllText(GlobalVariables.currentDirectory); ;

                // We grab the file names for source and destination
                string fileName = string.Empty;
                string newName = string.Empty;
                if (GlobalVariables.isPipeCommand)
                    fileName = FileSystem.SanitizePath(GlobalVariables.pipeCmdOutput.Trim(), dlocation);
                else
                    fileName = FileSystem.SanitizePath(arg.SplitByText(" -o ", 0), dlocation);
                if (GlobalVariables.isPipeCommand)
                    newName = FileSystem.SanitizePath(arg.SplitByText("-o ", 1), dlocation);
                else
                    newName = FileSystem.SanitizePath(arg.SplitByText(" -o ", 1), dlocation);

                // We check if file exists
                if (File.Exists(fileName))
                {
                    File.Move(fileName, newName);
                    Console.WriteLine($"File renamed from {fileName} to {newName}");
                    return;
                }
                FileSystem.ErrorWriteLine("File " + fileName + " does not exist!");
            }
            catch
            {
                FileSystem.ErrorWriteLine("You must type the file name!");
            }
        }
    }
}
