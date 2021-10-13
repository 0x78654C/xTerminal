﻿using Core;

namespace Commands.TerminalCommands.ConsoleSystem
{
    public class OpenDirectory : ITerminalCommand
    {
        /*
         * Opens current directory or other directory path provided.
         */
        public string Name => "odir";

        public void Execute(string arg)
        {
            string currentDirectory = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory);
            string args = arg.Split(' ').ParameterAfter("odir");

            if (!string.IsNullOrEmpty(args))
            {
                int lengthPath = arg.Length-5;
                string dirlocation = arg.Substring(5, lengthPath);
                FileSystem.OpenCurrentDiretory(dirlocation,currentDirectory);
                return;
            }
            FileSystem.OpenCurrentDiretory(currentDirectory,currentDirectory);
        }
    }
}
