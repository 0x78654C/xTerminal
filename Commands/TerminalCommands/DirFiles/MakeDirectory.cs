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
        private string s_helpMessage = @"Usage of mkdir command:
    mkdir dir_name                        : Create one directory.
    mkdir dir_name1:dir_name2:dir_name3   : Create multiple directories.
    mkdir new:new2{snew1,snew3{dnew1,dnew3}}:new3{rnew1{tne1,tne2},rnew2} : Create directories with nested subdirectories.

Root directories are splitted with ':'
Sub directoriers must be between '{' '}' and splited by ','
";

        public void Execute(string arg)
        {
            try
            {
                GlobalVariables.isErrorCommand = false;
                int argLength = arg.Length - 6;

                string input = arg.Substring(6, argLength);
                if (input == "-h")
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
                GlobalVariables.isErrorCommand = true;
            }
        }
    }
}
