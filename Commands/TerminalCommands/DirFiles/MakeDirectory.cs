using Core;
using Core.DirFiles;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.DirFiles
{
    [SupportedOSPlatform("Windows")]
    public class MakeDirectory : ITerminalCommand
    {
        public string Name => "mkdir";
        public void Execute(string arg)
        {
            try
            {
                int argLength = arg.Length - 6;

                string input = arg.Substring(6, argLength);
                string currentDir = File.ReadAllText(GlobalVariables.currentDirectory); ; // Get the new location
                var makeDirectory = new DirectoryMake(input, currentDir);
                makeDirectory.Create();
            }
            catch (Exception)
            {
                FileSystem.ErrorWriteLine("Something went wrong. Check path maybe!");
            }
        }
    }
}
