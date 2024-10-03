using Core;
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
                string path = FileSystem.SanitizePath(input, currentDir);
                Directory.CreateDirectory(path);
                FileSystem.SuccessWriteLine($"Directory {input} is created!");
            }
            catch (Exception)
            {
                FileSystem.ErrorWriteLine("Something went wrong. Check path maybe!");
            }
        }
    }
}
