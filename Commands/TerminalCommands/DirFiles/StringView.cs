using Core;
using System;
using System.IO;

namespace Commands.TerminalCommands.DirFiles
{
    public class StringView : ITerminalCommand
    {
        /*
         Read data from files with certain paramters.
         */

        public string Name => "cat";

        private static string s_currentDirectory;
        private static string s_output = string.Empty;
        private static string s_helpMessage = @"Usage of cat command:
  -h   : Displays this message.
  -s   : Output lines containing a provided text from a file.
           Example: cat -s <search_text> <file_search_in>
  -so  : Saves the lines containing a provided text from a file.
           Example: cat -so <search_text> <file_search_in> -o <file_to_save>
  -sa  : Output lines containing a provided text from all files in current directory and subdirectories.
           Example1: cat -sa <search_text>
           Example2: cat -sa <search_text> <part_of_file_name> 
  -sao : Saves the lines containing a provided text from all files in current directory and subdirectories.
           Example1: cat -sao <search_text> -o <file_to_save>
           Example2: cat -sao <search_text> <part_of_file_name> -o <file_to_save>
  -sm  : Output lines containing a provided text from multiple fies in current directory.
           Example: cat -sm <search_text> <file_search_in1;file_search_in2;file_search_in_n> 
  -smo : Saves the lines containing a provided text from multiple files in current directory.
           Example: cat -smo <search_text> <file_search_in1;file_search_in2;file_search_in_n> -o <file_to_save>
  -lc  : Counts all the lines(without empty lines) in all files on current directory and subdirectories.
  -lfc : Counts all the lines(without empty lines) that contains a specific text in file name in current directory and subdirectories.
           Example: cat -lfc <file_name_text>
";


        public void Execute(string arg)
        {
            try
            {
                s_currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);
                arg = arg.Replace("cat ", "");
                string[] input = arg.Split(' ');
                string searchString;
                string fileName;
                string fileSearchIn = string.Empty;
                string saveToFile;

                if (arg.Length == 3 && !arg.Contains("-lc"))
                {
                    Console.WriteLine($"Use -h param for {Name} command usage!");
                    return;
                }

                try
                {
                    fileSearchIn = input[2];
                }
                catch { }

                if (input.Length == 1)
                {
                    if (arg == "-h")
                    {
                        Console.WriteLine(s_helpMessage);
                        return;
                    }
                    if (input[0].Contains("-lc"))
                    {
                        int totalLinesCount = Core.Commands.CatCommand.LineCounts(s_currentDirectory);
                        Core.Commands.CatCommand.ClearCounter();
                        Console.WriteLine($"Total lines in all files(without empty lines): {totalLinesCount}");
                        return;
                    }
                    Console.WriteLine(Core.Commands.CatCommand.FileOutput(arg, s_currentDirectory));
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
                        saveToFile = FileSystem.SanitizePath(arg.SplitByText(" -o ", 1), s_currentDirectory);
                        string startMessage = "";
                        fileSearchIn = arg.SplitByText(searchString, 1);
                        fileSearchIn = fileSearchIn.SplitByText(" -o", 0);
                        if (!string.IsNullOrEmpty(fileSearchIn))
                        {
                            startMessage = $"---Searching in files containing '{fileSearchIn}' in name---\n\n";
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
                            Console.WriteLine(FileSystem.SaveFileOutput(saveToFile, s_currentDirectory, startMessage + s_output));
                        }
                        break;
                    case "-so":
                        {
                            fileName = input[2];
                            fileName = fileName.Replace(";", " ");
                            searchString = input[1];
                            saveToFile = FileSystem.SanitizePath(arg.SplitByText(" -o ", 1), s_currentDirectory);
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
                            saveToFile = FileSystem.SanitizePath(arg.SplitByText(" -o ", 1), s_currentDirectory);
                            Console.WriteLine(Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(' '), saveToFile, false));
                            break;
                        }
                    case "-lfc":
                        {
                            try
                            {
                                fileName = input[1];
                                int totalLinesCount = Core.Commands.CatCommand.LineCountsName(s_currentDirectory, fileName);
                                Core.Commands.CatCommand.ClearCounter();
                                Console.WriteLine($"Total lines in files that name contains '{fileName}' (without empty lines): {totalLinesCount}");
                            }
                            catch (Exception e)
                            {
                                FileSystem.ErrorWriteLine(e.Message);
                            }
                        }
                        break;
                    default:
                        Console.WriteLine(Core.Commands.CatCommand.FileOutput(arg, s_currentDirectory));
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
