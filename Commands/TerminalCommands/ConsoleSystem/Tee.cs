using Core;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("windows")]
    internal class Tee : ITerminalCommand
    {
        public string Name => "tee";
        private static string s_helpMessage = @"Usage of tee command parameters:
    tee <file_name>     : Writes previous command output to a file.
    tee -a <file_name>  : Appends previous command output to an existing file.

Tee is used with pipe commands and it saves the output from previous command in the pipe line to a file.
    Example: ls | cat -t 10 | tee data.txt | cat -s exe
";
        public void Execute(string arg)
        {
            try
            {
                var commandPipeData = GlobalVariables.pipeCmdOutput;
                var param = arg.Split('|')[0].Substring(4);
                var currentPath = File.ReadAllText(GlobalVariables.currentDirectory);

                if (arg == Name)
                {
                    FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                    return;
                }

                // Display help message.
                if (param.Trim() == "-h" && !GlobalVariables.isPipeCommand)
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                if (param.StartsWith("-a"))
                {
                    var removeParam = param.Substring(2).Trim();
                    var fileName = FileSystem.SanitizePath(removeParam, currentPath);
                    File.AppendAllText(fileName, commandPipeData);
                }
                else
                {
                    var removeParam = param.Trim();
                    var fileName = FileSystem.SanitizePath(removeParam, currentPath);
                    File.WriteAllText(fileName, commandPipeData);
                }
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine($"{ex.Message}. Use -h for more information!");
            }
        }
    }
}
