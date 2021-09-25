using Core;
using System;
using System.Linq;

namespace StringView
{
    /*Cat function */
    class Program
    {
        private static string[] MultiFileTypes = { "-s", "-so" };
        private static string[] SingleFileypes = { "-sm", "-smo" };
        private static readonly string s_currentDirectory = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory);

        private static string s_helpMessage = @"
    -s   : Output lines containing a provided text from a file.
    -so  : Saves the lines containing a provided text from a file.
    -sm  : Output lines containing a provided text from multiple fies.
    -smo : Saves the lines containing a provided text from mutiple files in current path location.
";

        static void Main(string[] args)
        {
            try
            {
                if (args == null || args.Length < 3)
                {
                    throw new Exception("Unexpected number of arguments " + s_helpMessage);
                }


                var input = args[0];
                var fileName = args[2];
                var searchString = args[1];
                if (SingleFileypes.Contains(input))
                {
                    Console.WriteLine(Core.Commands.CatCommand.FileOutput(fileName, s_currentDirectory,searchString , input == "-s" ? "" : args[3]));
                }
                else if (MultiFileTypes.Contains(input))
                {
                    Console.WriteLine(Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(' '), input == "-sm" ? "" : args[3]));
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
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }
    }
}