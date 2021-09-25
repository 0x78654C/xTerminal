using Core;
using System;

namespace StringView
{
    /*Cat function */
    class Program
    {
        private static readonly string s_currentDirectory = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory);

        private static string s_helpMessage = @"
    -h   : Displays this message.
    -s   : Output lines containing a provided text from a file.
    -so  : Saves the lines containing a provided text from a file.
    -sm  : Output lines containing a provided text from multiple fies.
    -smo : Saves the lines containing a provided text from multiple files in current path location.
";

        static void Main(string[] args)
        {
            try
            {
                string input = args[0];
                if (args.Length == 1)
                {
                    Console.WriteLine(Core.Commands.CatCommand.FileOutput(input, s_currentDirectory));
                }

                string fileName = args[2];
                string searchString = args[1];
                switch (input)
                {
                    case "-s":
                        Console.WriteLine(Core.Commands.CatCommand.FileOutput(fileName, s_currentDirectory, searchString, ""));
                        break;
                    case "-so":
                        {
                            string saveToFile = args[3];
                            Console.WriteLine(Core.Commands.CatCommand.FileOutput(fileName, s_currentDirectory, searchString, saveToFile));
                            break;
                        }
                    case "-sm":
                        Console.WriteLine(Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(' '), ""));
                        break;
                    case "-smo":
                        {
                            string saveToFile = args[3];
                            Console.WriteLine(Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(' '), saveToFile));
                            break;
                        }
                    case "-h":
                        Console.WriteLine(s_helpMessage);
                        break;
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }
    }
}