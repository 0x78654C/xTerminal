using Core;
using System;
using System.IO;

namespace Commands.TerminalCommands.DirFiles
{
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

                //reading current location(for test no, after i make dynamic)
                string dlocation = File.ReadAllText(GlobalVariables.currentDirectory); ;
                string cLocation = Directory.GetCurrentDirectory();

                //we grab the file names for source and destination
                string FileName = FileSystem.SanitizePath(arg.SplitByText(" -o ", 0), dlocation);
                string NewName = FileSystem.SanitizePath(arg.SplitByText(" -o ", 1), dlocation);

                //we check if file exists
                if (File.Exists(FileName))
                {
                    File.Move(FileName, NewName);
                    Console.WriteLine($"File renamed from {FileName} to {NewName}");
                    return;
                }
                FileSystem.ErrorWriteLine("File " + FileName + " dose not exist!");
            }
            catch
            {
                FileSystem.ErrorWriteLine("You must type the file name!");
            }
        }
    }
}
