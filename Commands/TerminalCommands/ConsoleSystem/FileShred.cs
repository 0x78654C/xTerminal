﻿using Core;
using Core.SystemTools;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class FileShred : ITerminalCommand
    {
        public string Name => "shred";
        private Shred _shred;
        private static string s_helpMessage = @"Usage of shred command:
  shred <file_path> :   Will shred the file with the default of 3 passes.
  shred <file_path> -i <number_of_passes> :   Will shred the file with the specified number!
";

        public void Execute(string args)
        {
            try
            {
                if (args.Length == 5)
                {
                    Console.WriteLine($"Use -h param for {Name} command usage!");
                    return;
                }
                if (args == $"{Name} -h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                args = args.Replace("shred ", String.Empty);
                var currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);

                if (args.Contains(" -i "))
                {
                    int passes = Int32.Parse(args.SplitByText("-i ", 1).Trim());
                    string filePath = args.SplitByText(" -i", 0).Trim();
                    string fileSanitizeI = FileSystem.SanitizePath(filePath, currentDirectory);
                    _shred = new Shred(fileSanitizeI, passes);
                    _shred.ShredFile();
                    return;
                }
                string fileSanitize = FileSystem.SanitizePath(args, currentDirectory);
                _shred = new Shred(fileSanitize, 0);
                _shred.ShredFile();
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine($"{ex.Message}\nUse -h param for {{Name}} command usage!");
            }
        }
    }
}
