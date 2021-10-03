using Core;
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
    -sm  : Output lines containing a provided text from multiple fies.
    -smo : Saves the lines containing a provided text from multiple files in current path location.
";


        public void Execute(string arg)
        {
            try
            {
                s_currentDirectory = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory);
                arg = arg.Replace("cat ", "");
                string[] input = arg.Split(' ');
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

                string fileName = input[2];
                fileName = fileName.Replace(";", " ");
                string searchString = input[1];
                switch (input[0])
                {
                    case "-s":
                        Console.WriteLine(Core.Commands.CatCommand.FileOutput(fileName, s_currentDirectory, searchString, ""));
                        break;
                    case "-so":
                        {
                            string saveToFile = input[3];
                            Console.WriteLine(Core.Commands.CatCommand.FileOutput(fileName, s_currentDirectory, searchString, saveToFile));
                            break;
                        }
                    case "-sm":
                        Console.WriteLine(Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(' '), ""));
                        break;
                    case "-smo":
                        {
                            string saveToFile = input[3];
                            Console.WriteLine(Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(' '), saveToFile));
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.ToString());
            }
        }
    }
}
