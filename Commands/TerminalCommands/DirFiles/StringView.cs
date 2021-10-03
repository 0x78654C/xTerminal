﻿using Core;
using System;

namespace Commands.TerminalCommands.DirFiles
{
    public class StringView : ITerminalCommand
    {
        public string Name => "cat";

        private static string s_currentDirectory;

        private static string s_helpMessage = @"
    -h   : Displays this message.
    -s   : Output lines containing a provided text from a file.
    -so  : Saves the lines containing a provided text from a file.
    -sa  : Output lines containing a provided text from all files in current directory and subdirectories.
    -sao : Saves the lines containing a provided text from all files in current directory and subdirectories.
    -sm  : Output lines containing a provided text from multiple fies in current directory.
    -smo : Saves the lines containing a provided text from multiple files in current directory.
";


        public void Execute(string arg)
        {
            try
            {
                s_currentDirectory = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory);
                arg = arg.Replace("cat ", "");
                string[] input = arg.Split(' ');
                string searchString;
                string fileName;
                string saveToFile;
                if (input.Length == 1)
                {
                    if (input[0].Contains("-h"))
                    {
                        Console.WriteLine(s_helpMessage);
                        return;
                    }
                    Console.WriteLine(Core.Commands.CatCommand.FileOutput(input[0], s_currentDirectory));
                    return;
                }
;
                switch (input[0])
                {
                    case "-s":
                        fileName = input[2];
                        fileName = fileName.Replace(";", " ");
                        searchString = input[1];
                        Console.WriteLine(Core.Commands.CatCommand.FileOutput(fileName, s_currentDirectory, searchString, ""));
                        break;
                    case "-sa":
                        searchString = input[1];
                        fileName = "";
                        Console.WriteLine(Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(' '), "", true)); ;
                        break;
                    case "-sao":
                        searchString = input[1];
                        fileName = "";
                        saveToFile = input[2];
                        string output = Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(' '), saveToFile, true);
                        Console.WriteLine(FileSystem.SaveFileOutput(saveToFile, s_currentDirectory, output));
                        break;
                    case "-so":
                        {
                            fileName = input[2];
                            fileName = fileName.Replace(";", " ");
                            searchString = input[1];
                            saveToFile = input[3];
                            Console.WriteLine(Core.Commands.CatCommand.FileOutput(fileName, s_currentDirectory, searchString, saveToFile));
                            break;
                        }
                    case "-sm":
                        fileName = input[2];
                        fileName = fileName.Replace(";", " ");
                        searchString = input[1];
                        Console.WriteLine(Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(' '), "", false));
                        break;
                    case "-smo":
                        {
                            fileName = input[2];
                            fileName = fileName.Replace(";", " ");
                            searchString = input[1];
                            saveToFile = input[3];
                            Console.WriteLine(Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(' '), saveToFile, false));
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }
    }
}
