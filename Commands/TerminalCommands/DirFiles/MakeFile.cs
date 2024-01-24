using Core;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.DirFiles
{
    [SupportedOSPlatform("Windows")]
    public class MakeFile : ITerminalCommand
    {
        public string Name => "mkfile";
        public void Execute(string arg)
        {
            string currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory); ;
            string file;
            try
            {
                int argLenght = arg.Length - 7;
                file = FileSystem.SanitizePath(arg.Substring(7, argLenght), currentDirectory);
                File.Create(file);
                FileSystem.SuccessWriteLine($"File {file} was created!");
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }
    }
}

