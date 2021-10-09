using Core;
using System;
using System.IO;

namespace Commands.TerminalCommands.DirFiles
{
    public class MakeFile : ITerminalCommand
    {
        public string Name => "mkfile";
        public void Execute(string arg)
        {
            string currentDirectory = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory); ;
            string file;
            try
            {
                file = FileSystem.SanitizePath(arg.Split(' ')[1], currentDirectory);
                File.Create(file);
                Console.WriteLine($"File {file} was created!");
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }
    }
}

