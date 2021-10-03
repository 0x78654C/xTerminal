using System;
using System.IO;
using Core;

namespace Commands.TerminalCommands.DirFiles
{
    public class MakeFile : ITerminalCommand
    {
        public string Name => "mkfile";
        public void Execute(string arg)
        {
            string CLocation = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory); ;
            string file;
            try
            {
                file = arg.Split(' ')[1];
                if (Directory.Exists(CLocation))
                {
                    File.Create(CLocation + @"\" + file);
                }
                else
                {
                    FileSystem.ErrorWriteLine("Directory dose not exist!");
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }
    }
}

