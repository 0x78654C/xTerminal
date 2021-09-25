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
                if (input == "-s")
                {
                    string fileName = args[2];
                    string searchString = args[1];
                    Console.WriteLine(Core.Commands.CatCommand.FileOutput(searchString, s_currentDirectory,fileName,""));
                }else if (input =="-so")
                {
                    string fileName = args[2];
                    string searchString = args[1];
                    string saveToFile = args[3];
                    Console.WriteLine(Core.Commands.CatCommand.FileOutput(searchString, s_currentDirectory, fileName, saveToFile));
                }
                else if (input == "-sm")
                {
                    string fileName = args[2];
                    string searchString = args[1];
                    Console.WriteLine(Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName, ""));
                }
                else if (input == "-smo")
                {
                    string fileName = args[2];
                    string searchString = args[1];
                    string saveToFile = args[3];
                    Console.WriteLine(Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName, saveToFile));
                }
                else if (input == "-h")
                {
                    Console.WriteLine(s_helpMessage);
                }
                else
                {
                    Console.WriteLine(Core.Commands.CatCommand.FileOutput(input, s_currentDirectory));
                }
            }
            catch(Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }

    }
}
