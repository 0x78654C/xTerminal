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
  -n   : Displays first N lines from a file.
           Example: cat -n 10 <path_of_file_name>
  -l   : Displays data between two lines range.
           Example: cat -l 10-20 <path_of_file_name>
  -s   : Output lines containing a provided text from a file.
           Example: cat -s <search_text> -f <file_search_in>
  -so  : Saves the lines containing a provided text from a file.
           Example: cat -so <search_text> -f <file_search_in> -o <file_to_save>
  -sa  : Output lines containing a provided text from all files in current directory and subdirectories.
           Example1: cat -sa <search_text>
           Example2: cat -sa <search_text> -f <part_of_file_name> 
  -sao : Saves the lines containing a provided text from all files in current directory and subdirectories.
           Example1: cat -sao <search_text> -o <file_to_save>
           Example2: cat -sao <search_text> -f <part_of_file_name> -o <file_to_save>
  -sm  : Output lines containing a provided text from multiple fies in current directory.
           Example: cat -sm <search_text> -f <file_search_in1;file_search_in2;file_search_in_n> 
  -smo : Saves the lines containing a provided text from multiple files in current directory.
           Example: cat -smo <search_text> -f <file_search_in1;file_search_in2;file_search_in_n> -o <file_to_save>
  -lc  : Counts all the lines(without empty lines) in all files on current directory and subdirectories.
  -lfc : Counts all the lines(without empty lines) that contains a specific text in file name in current directory and subdirectories.
           Example: cat -lfc <file_name_text>
  -con : Concatenate text files to a single file.
           Example: cat -con file1;file2;file3 -o fileOut
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
                    case "-con":
                        string files = arg.MiddleString("-con", "-o");
                        string outputFile = arg.SplitByText("-o ", 1);
                        outputFile = FileSystem.SanitizePath(outputFile, s_currentDirectory);
                        File.WriteAllText(outputFile, "");
                        Core.Commands.CatCommand.ConcatenateFiles(files, outputFile, s_currentDirectory);
                        string outFileData = File.ReadAllText(outputFile);
                        if (outFileData.Length > 0)
                            Console.WriteLine($"Data was saved to: {outputFile}");
                        else
                            Console.WriteLine("No file ware concatenated!");

                        break;
                    case "-n":
                        string lineCounter = arg.Split(' ')[1];
                        if (!FileSystem.IsNumberAllowed(lineCounter))
                        {
                            FileSystem.ErrorWriteLine("Parameter invalid. You need to provide how many lines you want to display!");
                            return;
                        }
                        int lines = Int32.Parse(lineCounter);
                        string filePath = FileSystem.SanitizePath(arg.SplitByText(lineCounter + " ", 1), s_currentDirectory);
                        Core.Commands.CatCommand.OuputFirtsLines(filePath, lines);
                        break;
                    case "-l":
                        string linesRange = arg.Split(' ')[1];
                        if (!linesRange.Contains("-"))
                        {
                            FileSystem.ErrorWriteLine("Parameter invalid. You need to provide the range of lines for data display! Example: 10-20");
                            return;
                        }
                        string pathFile = FileSystem.SanitizePath(arg.SplitByText(linesRange + " ", 1), s_currentDirectory);
                        Core.Commands.CatCommand.OutputLinesRange(pathFile, linesRange);
                        break;
                    case "-s":
                        fileName = arg.SplitByText("-f ", 1);
                        searchString = arg.MiddleString("-s", "-f");
                        Console.WriteLine(Core.Commands.CatCommand.FileOutput(fileName, s_currentDirectory, searchString, ""));
                        break;
                    case "-sa":
                        if (arg.Contains(" -f "))
                            searchString = arg.MiddleString("-sa", "-f");
                        else
                            searchString = arg.SplitByText("-sa ", 1);
                        fileName = "";
                        if (arg.Contains(" -f "))
                            fileSearchIn = arg.SplitByText("-f ", 1);
                        else
                            fileSearchIn = "";

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
                        fileName = "";
                        searchString = arg.MiddleString("-sao", "-f");
                        saveToFile = FileSystem.SanitizePath(arg.SplitByText(" -o ", 1), s_currentDirectory);
                        string startMessage = "";
                        fileSearchIn = arg.MiddleString("-f", "-o");
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
                            fileName = arg.MiddleString("-f", "-o");
                            searchString = arg.MiddleString("-so", "-f");
                            saveToFile = FileSystem.SanitizePath(arg.SplitByText(" -o ", 1), s_currentDirectory);
                            Console.WriteLine(Core.Commands.CatCommand.FileOutput(fileName, s_currentDirectory, searchString, saveToFile));
                            break;
                        }
                    case "-sm":
                        fileName = input[2];
                        fileName = arg.SplitByText("-f ", 1);
                        searchString = arg.MiddleString("-sm", "-f");
                        Console.WriteLine(Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(';'), "", false));
                        break;
                    case "-smo":
                        {
                            fileName = arg.MiddleString("-f", "-o");
                            searchString = arg.MiddleString("-smo", "-f");
                            saveToFile = FileSystem.SanitizePath(arg.SplitByText(" -o ", 1), s_currentDirectory);
                            Console.WriteLine(Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(';'), saveToFile, false));
                            break;
                        }
                    case "-lfc":
                        {
                            try
                            {
                                fileName = arg.SplitByText("-lfc ", 1);
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
