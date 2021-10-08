using Core;
using System;

namespace Commands.TerminalCommands.DirFiles
{
    public class StringView : ITerminalCommand
    {
        public string Name => "cat";

        private static string s_currentDirectory;
        private static string s_output = string.Empty;
        private static string s_helpMessage = @"
  -h   : Displays this message.
  -s   : Output lines containing a provided text from a file.
           Example: cat -s <search_text> <file_search_in>
  -so  : Saves the lines containing a provided text from a file.
           Example: cat -s <search_text> <file_search_in> <file_to_save>
  -sa  : Output lines containing a provided text from all files in current directory and subdirectories.
           Example1: cat -sa <search_text>
           Example2: cat -sa <search_text> <part_of_file_name> 
  -sao : Saves the lines containing a provided text from all files in current directory and subdirectories.
           Example1: cat -sao <search_text> <file_to_save>
           Example2: cat -sao <search_text> <part_of_file_name> <file_to_save>
  -sm  : Output lines containing a provided text from multiple fies in current directory.
           Example: cat -sm <search_text> <file_search_in1;file_search_in2;file_search_in_n> 
  -smo : Saves the lines containing a provided text from multiple files in current directory.
           Example: cat -smo <search_text> <file_search_in1;file_search_in2;file_search_in_n> <file_to_save>
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
                string fileSearchIn = string.Empty;
                string saveToFile;

                try
                {
                    fileSearchIn = input[2];
                }
                catch { }

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
                        if (!string.IsNullOrEmpty(fileSearchIn))
                        {
                            Console.WriteLine($"---Searching in files containing '{fileSearchIn}' in name---\n");
                            s_output = Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(' '), "", true, fileSearchIn);
                        }
                        else
                        {
                            s_output = Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(' '), "", true);
                        }
                        s_output = string.IsNullOrWhiteSpace(s_output) ? "No files with names that contains that text!" : s_output;
                        Console.WriteLine(s_output);
                        break;
                    case "-sao":
                        searchString = input[1];
                        fileName = "";
                        saveToFile = input[3];
                        string startMessage="";
                        if (!string.IsNullOrEmpty(fileSearchIn))
                        {
                            startMessage=$"---Searching in files containing '{fileSearchIn}' in name---\n\n";
                            s_output = Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(' '), saveToFile, true, fileSearchIn);
                        }
                        else
                        {
                            s_output = Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(' '), saveToFile, true);
                        }

                        if (string.IsNullOrWhiteSpace(s_output))
                        {
                            Console.WriteLine("No files with names that contains that text!");
                        }
                        else
                        {
                            Console.WriteLine(FileSystem.SaveFileOutput(saveToFile, s_currentDirectory, startMessage+s_output));
                        }
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
