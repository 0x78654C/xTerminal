using Core;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.DirFiles
{
    [SupportedOSPlatform("Windows")]
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
  -n   : Displays first N lines from a file. Works as pipe command.
           Example: cat -n 10 <path_of_file_name>
  -l   : Displays data between two lines range. WWorks as pipe command.
           Example: cat -l 10-20 <path_of_file_name>
  -s   : Outputs lines containing a provided text from a file. Works as pipe command.
           Example: cat -s <search_text> -f <file_search_in>
  -so  : Saves the lines containing a provided text from a file. Works as pipe command.
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
  -con : Concatenates text files to a single file.
           Example: cat -con file1;file2;file3 -o fileOut

Commands can be canceled with CTRL+X key combination.
";


        public void Execute(string arg)
        {
            try
            {
                GlobalVariables.eventCancelKey = false;
                s_currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);
                arg = arg.Replace("cat ", "");
                string[] input = arg.Split(' ');
                string searchString;
                string fileName;
                string fileSearchIn = string.Empty;
                string saveToFile;

                if (arg.Length == 3 && !arg.Contains("-lc") && !GlobalVariables.isPipeCommand)
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
                        GlobalVariables.eventKeyFlagX = true;
                        int totalLinesCount = Core.Commands.CatCommand.LineCounts(s_currentDirectory);
                        Core.Commands.CatCommand.ClearCounter();
                        if (GlobalVariables.eventCancelKey)
                            FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                        GlobalVariables.eventCancelKey = false;
                        Console.WriteLine($"Total lines in all files(without empty lines): {totalLinesCount}");
                        return;
                    }

                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 && GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                    {
                        GlobalVariables.pipeCmdOutput = $"{Core.Commands.CatCommand.FileOutput(GlobalVariables.pipeCmdOutput.Trim(), s_currentDirectory)}";
                    }
                    else if (GlobalVariables.pipeCmdCount == GlobalVariables.pipeCmdCountTemp)
                    {
                        GlobalVariables.pipeCmdOutput = $"{Core.Commands.CatCommand.FileOutput(arg.Trim(), s_currentDirectory)}";
                        return;
                    }

                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0)
                        Console.WriteLine(Core.Commands.CatCommand.FileOutput(GlobalVariables.pipeCmdOutput.Trim(), s_currentDirectory));
                    
                    if(!GlobalVariables.isPipeCommand)
                        Console.WriteLine(Core.Commands.CatCommand.FileOutput(arg.Trim(), s_currentDirectory));

                    return;
                }

                switch (input[0])
                {
                    case "-con":
                        string files = arg.MiddleString("-con", "-o");
                        string outputFile = arg.SplitByText("-o ", 1);
                        outputFile = FileSystem.SanitizePath(outputFile, s_currentDirectory);
                        File.WriteAllText(outputFile, "");
                        GlobalVariables.eventKeyFlagX = true;
                        Core.Commands.CatCommand.ConcatenateFiles(files, outputFile, s_currentDirectory);
                        if (GlobalVariables.eventCancelKey)
                            FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                        GlobalVariables.eventCancelKey = false;
                        string outFileData = File.ReadAllText(outputFile);
                        if (outFileData.Length > 0)
                            Console.WriteLine($"Data was saved to: {outputFile}");
                        else
                            Console.WriteLine("No files were concatenated!");
                        break;
                    case "-n":
                        string lineCounter = arg.Split(' ')[1];
                        if (!FileSystem.IsNumberAllowed(lineCounter))
                        {
                            FileSystem.ErrorWriteLine("Invalid parameter. You need to provide how many lines you want to display!");
                            return;
                        }
                        int lines = Int32.Parse(lineCounter);
                        GlobalVariables.eventKeyFlagX = true;
                        if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                        {
                            var dataPipe = GlobalVariables.pipeCmdOutput;
                            Core.Commands.CatCommand.OutputFirstLinesFromString(dataPipe, lines);
                            if (GlobalVariables.eventCancelKey)
                                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                            GlobalVariables.eventCancelKey = false;
                        }
                        else
                        {
                            string filePath = FileSystem.SanitizePath(arg.SplitByText(lineCounter + " ", 1), s_currentDirectory);
                            Core.Commands.CatCommand.OutputFirtsLines(filePath, lines);
                            if (GlobalVariables.eventCancelKey)
                                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                            GlobalVariables.eventCancelKey = false;
                        }
                        break;
                    case "-l":
                        string linesRange = arg.Split(' ')[1];
                        if (!linesRange.Contains("-"))
                        {
                            FileSystem.ErrorWriteLine("Invalid parameter. You need to provide the range of lines for data display! Example: 10-20");
                            return;
                        }
                        if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                        {
                            GlobalVariables.eventKeyFlagX = true;
                            Core.Commands.CatCommand.OutputLinesRange(GlobalVariables.pipeCmdOutput, linesRange);
                            if (GlobalVariables.eventCancelKey)
                                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                            GlobalVariables.eventCancelKey = false;
                        }
                        else
                        {
                            string pathFile = FileSystem.SanitizePath(arg.SplitByText(linesRange + " ", 1), s_currentDirectory);
                            GlobalVariables.eventKeyFlagX = true;
                            Core.Commands.CatCommand.OutputLinesRange(pathFile, linesRange);
                            if (GlobalVariables.eventCancelKey)
                                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                            GlobalVariables.eventCancelKey = false;
                        }
                        break;
                    case "-s":

                        if (GlobalVariables.isPipeCommand)
                        {
                            searchString = arg.SplitByText("-s ", 1);
                            GlobalVariables.eventKeyFlagX = true;
                            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 && GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                                GlobalVariables.pipeCmdOutput = Core.Commands.CatCommand.StringSearchOutput(GlobalVariables.pipeCmdOutput, s_currentDirectory, searchString, "");
                            else
                                Console.WriteLine(Core.Commands.CatCommand.StringSearchOutput(GlobalVariables.pipeCmdOutput, s_currentDirectory, searchString, ""));

                            if (GlobalVariables.eventCancelKey)
                                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                            GlobalVariables.eventCancelKey = false;
                        }
                        else
                        {
                            fileName = arg.SplitByText("-f ", 1);
                            searchString = arg.MiddleString("-s", "-f");
                            GlobalVariables.eventKeyFlagX = true;
                            Console.WriteLine(Core.Commands.CatCommand.FileOutput(fileName, s_currentDirectory, searchString, ""));
                            if (GlobalVariables.eventCancelKey)
                                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                            GlobalVariables.eventCancelKey = false;
                        }
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
                            if (!GlobalVariables.isPipeCommand)
                                Console.WriteLine($"---Searching in files containing '{fileSearchIn}' in name---\n");
                            GlobalVariables.eventKeyFlagX = true;
                            s_output = Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(' '), "", true, fileSearchIn);
                        }
                        else
                        {
                            GlobalVariables.eventKeyFlagX = true;
                            s_output = Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(' '), "", true);
                        }

                        s_output = string.IsNullOrWhiteSpace(s_output) ? "No file names contain that text!" : s_output;

                        if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                            GlobalVariables.pipeCmdOutput = s_output;
                        else
                            Console.WriteLine(s_output);

                        if (GlobalVariables.eventCancelKey)
                            FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                        GlobalVariables.eventCancelKey = false;
                        break;
                    case "-sao":
                        fileName = "";
                        if (arg.Contains(" -f "))
                            searchString = arg.MiddleString("-sao", "-f");
                        else
                            searchString = arg.MiddleString("-sao", "-o");
                        saveToFile = FileSystem.SanitizePath(arg.SplitByText(" -o ", 1), s_currentDirectory);
                        string startMessage = "";
                        if (arg.Contains(" -f ") && arg.Contains(" -o "))
                            fileSearchIn = arg.MiddleString("-f", "-o");
                        else
                            fileSearchIn = "";
                        if (!string.IsNullOrEmpty(fileSearchIn))
                        {
                            GlobalVariables.eventKeyFlagX = true;
                            startMessage = $"---Searching in files containing '{fileSearchIn}' in name---\n\n";
                            s_output = Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(' '), saveToFile, true, fileSearchIn);
                            if (GlobalVariables.eventCancelKey)
                                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                            GlobalVariables.eventCancelKey = false;
                        }
                        else
                        {
                            GlobalVariables.eventKeyFlagX = true;
                            s_output = Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(' '), saveToFile, true);
                            if (GlobalVariables.eventCancelKey)
                                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                            GlobalVariables.eventCancelKey = false;
                        }

                        if (string.IsNullOrWhiteSpace(s_output))
                        {
                            Console.WriteLine("No file names contain that text!");
                        }
                        else
                        {
                            Console.WriteLine(FileSystem.SaveFileOutput(saveToFile, s_currentDirectory, startMessage + s_output));
                        }
                        break;
                    case "-so":
                        {
                            if (GlobalVariables.isPipeCommand)
                            {
                                searchString = arg.MiddleString("-so", "-o");
                                saveToFile = FileSystem.SanitizePath(arg.SplitByText(" -o ", 1), s_currentDirectory);
                                GlobalVariables.eventKeyFlagX = true;
                                Console.WriteLine(Core.Commands.CatCommand.StringSearchOutput(GlobalVariables.pipeCmdOutput, s_currentDirectory, searchString, saveToFile));
                                if (GlobalVariables.eventCancelKey)
                                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                                GlobalVariables.eventCancelKey = false;
                            }
                            else
                            {
                                fileName = arg.MiddleString("-f", "-o");
                                searchString = arg.MiddleString("-so", "-f");
                                saveToFile = FileSystem.SanitizePath(arg.SplitByText(" -o ", 1), s_currentDirectory);
                                GlobalVariables.eventKeyFlagX = true;
                                Console.WriteLine(Core.Commands.CatCommand.FileOutput(fileName, s_currentDirectory, searchString, saveToFile));
                                if (GlobalVariables.eventCancelKey)
                                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                                GlobalVariables.eventCancelKey = false;
                            }
                            break;
                        }
                    case "-sm":
                        fileName = input[2];
                        fileName = arg.SplitByText("-f ", 1);
                        searchString = arg.MiddleString("-sm", "-f");
                        GlobalVariables.eventKeyFlagX = true;
                        if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                            GlobalVariables.pipeCmdOutput += $"{Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(';'), "", false)}\n";
                        else
                            Console.WriteLine(Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(';'), "", false));
                        if (GlobalVariables.eventCancelKey)
                            FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                        GlobalVariables.eventCancelKey = false;
                        break;
                    case "-smo":
                        {
                            fileName = arg.MiddleString("-f", "-o");
                            searchString = arg.MiddleString("-smo", "-f");
                            saveToFile = FileSystem.SanitizePath(arg.SplitByText(" -o ", 1), s_currentDirectory);
                            GlobalVariables.eventKeyFlagX = true;
                            Console.WriteLine(Core.Commands.CatCommand.MultiFileOutput(searchString, s_currentDirectory, fileName.Split(';'), saveToFile, false));
                            if (GlobalVariables.eventCancelKey)
                                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                            GlobalVariables.eventCancelKey = false;
                            break;
                        }
                    case "-lfc":
                        {
                            try
                            {
                                fileName = arg.SplitByText("-lfc ", 1);
                                GlobalVariables.eventKeyFlagX = true;
                                int totalLinesCount = Core.Commands.CatCommand.LineCountsName(s_currentDirectory, fileName);
                                Core.Commands.CatCommand.ClearCounter();
                                if (GlobalVariables.eventCancelKey)
                                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                                GlobalVariables.eventCancelKey = false;
                                Console.WriteLine($"Total lines in files that name contains '{fileName}' (without empty lines): {totalLinesCount}");
                            }
                            catch (Exception e)
                            {
                                FileSystem.ErrorWriteLine(e.Message);
                            }
                        }
                        break;
                    default:
                        if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 && GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                        {
                            GlobalVariables.pipeCmdOutput = $"{Core.Commands.CatCommand.FileOutput(GlobalVariables.pipeCmdOutput.Trim(), s_currentDirectory)}";
                        }
                        else if (GlobalVariables.pipeCmdCount == GlobalVariables.pipeCmdCountTemp)
                        {
                            GlobalVariables.pipeCmdOutput = $"{Core.Commands.CatCommand.FileOutput(arg.Trim(), s_currentDirectory)}";
                        }

                        if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0)
                            Console.WriteLine(Core.Commands.CatCommand.FileOutput(GlobalVariables.pipeCmdOutput.Trim(), s_currentDirectory));

                        if (!GlobalVariables.isPipeCommand)
                            Console.WriteLine(Core.Commands.CatCommand.FileOutput(arg.Trim(), s_currentDirectory));

                        break;
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine($"{e.Message}. Check command!");
            }
        }
    }
}
