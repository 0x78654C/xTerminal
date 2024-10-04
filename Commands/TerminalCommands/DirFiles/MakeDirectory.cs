﻿using Core;
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
        private string s_helpMessage = @"Usage of mkdir command:
    mkdir <dir_name>
    mkdir <dir_name1;dir_name2;dir_name3>
";

        public void Execute(string arg)
        {
            try
            {
                int argLength = arg.Length - 6;

                string input = arg.Substring(6, argLength);
                if (input == $"{Name} -h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }
                string currentDir = File.ReadAllText(GlobalVariables.currentDirectory);  // Get the new location
                var makeDirectory = new DirectoryMake( currentDir);
                makeDirectory.Create(input);
            }
            catch (Exception)
            {
                FileSystem.ErrorWriteLine("Something went wrong. Check path maybe!");
            }
        }
    }
}
