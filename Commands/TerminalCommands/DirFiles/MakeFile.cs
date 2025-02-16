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
        private string s_helpMessage = @"Usage of mkfile command:
    mkfile <file_name>                        : Create one file.
    mkfile <file_name1;file_name2;file_name3> : Create multiple files.
";
        public void Execute(string arg)
        {
            string currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory); ;
            try
            {
                int argLenght = arg.Length - 7;
                var param = arg.Substring(7, argLenght);
                if (param == "-h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                if (param.Contains(";"))
                {
                    var files = param.Split(';');
                    foreach (var fileIn in files)
                    {
                        var sanitizedPath = FileSystem.SanitizePath(fileIn, currentDirectory);
                        File.Create(sanitizedPath);
                        FileSystem.SuccessWriteLine($"File {fileIn} was created!");
                    }
                    return;
                }
                var file = FileSystem.SanitizePath(param, currentDirectory);
                File.Create(file);
                FileSystem.SuccessWriteLine($"File {file} was created!");
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }
    }
}

