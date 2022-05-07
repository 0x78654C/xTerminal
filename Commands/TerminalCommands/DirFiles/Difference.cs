using System;
using System.IO;
using System.Linq;
using Core;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace Commands.TerminalCommands.DirFiles
{
    class Difference : ITerminalCommand
    {
        /*Diff command class for compare two files line by line.*/
        public string Name => "diff";
        private string _currentLocation = string.Empty;
        private string _helpMessage = @"Usage of diff command:
 diff first_file_name;second_file_name                               : Display the difference from second file in comperison to first file.
 diff first_file_name;second_file_name -verbose                      : Display the entire second file with the difference in comperison to first file.
 diff first_file_name;second_file_name -f save_to_file_name          : Saves to file the difference from second file in comperison to first file.
 diff first_file_name;second_file_name -f save_to_file_name -verbose : Saves to file the entire second file with the marked difference in comperison to first file.
";
        public void Execute(string args)
        {
            _currentLocation = File.ReadAllText(GlobalVariables.currentDirectory);

            if (args.Length == 4)
            {
                Console.WriteLine($"Use -h param for {Name} command usage!");
                return;
            }

            // Display help message.
            if (args == "diff -h")
            {
                Console.WriteLine(_helpMessage);
                return;
            }

            DiffFiles(args, _currentLocation);
        }


        /// <summary>
        /// Check difference between two files line by line with DiffPlex
        /// https://www.nuget.org/packages/DiffPlex
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="currentDirectory"></param>
        private void DiffFiles(string arg, string currentDirectory)
        {
            try
            {
                arg = arg.Replace("diff ", string.Empty);
                string files = arg.SplitByText(" -f ", 0);
                var filesSplit = files.Split(';');
                int countSplit = filesSplit.Count();
                if (countSplit == 2)
                {
                    string firstFile = FileSystem.SanitizePath(filesSplit[0], currentDirectory);
                    string secondFile;
                    if (arg.Contains(" -verbose") && !arg.Contains(" -f "))
                        secondFile = FileSystem.SanitizePath(filesSplit[1].Replace(" -verbose",string.Empty), currentDirectory);
                    else
                        secondFile = FileSystem.SanitizePath(filesSplit[1], currentDirectory);

                    var linesFirst = File.ReadAllText(firstFile);
                    var linesSecond = File.ReadAllText(secondFile);
                    var diff = InlineDiffBuilder.Diff(linesFirst, linesSecond);
                    var savedColor = Console.ForegroundColor;
                    if (!arg.Contains(" -f "))
                    {
                        if (!arg.Contains(" -verbose"))
                        {
                            if (!diff.HasDifferences)
                            {
                                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "There is no difference between files!");
                                return;
                            }
                            foreach (var line in diff.Lines)
                            {
                                switch (line.Type)
                                {
                                    case ChangeType.Inserted:
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine($"Ins Line:{line.Position} {line.Text}");
                                        Console.ForegroundColor = savedColor;
                                        break;
                                    case ChangeType.Deleted:
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"Del Line:{line.Position} {line.Text}");
                                        Console.ForegroundColor = savedColor;
                                        break;
                                }
                            }
                            return;
                        }
                        var diffVerbose = InlineDiffBuilder.Diff(linesFirst, linesSecond);
                        foreach (var line in diffVerbose.Lines)
                        {
                            switch (line.Type)
                            {
                                case ChangeType.Inserted:
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.Write("+ ");
                                    break;
                                case ChangeType.Deleted:
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Write("- ");
                                    break;
                                default:
                                    Console.ForegroundColor = ConsoleColor.Gray; // compromise for dark or light background
                                    Console.Write("  ");
                                    break;
                            }
                            Console.WriteLine(line.Text);
                        }
                        Console.ForegroundColor = savedColor;
                        return;
                    }
                    if (!arg.Contains(" -verbose"))
                    {
                        string outFile = FileSystem.SanitizePath(arg.SplitByText(" -f ", 1), currentDirectory);
                        string outData = string.Empty;
                        foreach (var line in diff.Lines)
                        {
                            switch (line.Type)
                            {
                                case ChangeType.Inserted:
                                    outData += $"Ins Line:{line.Position} {line.Text}\n";
                                    break;
                                case ChangeType.Deleted:
                                    outData += $"Del Line:{line.Position} {line.Text}\n";
                                    break;
                            }
                        }
                        if (outData.Length > 0)
                            FileSystem.ColorConsoleTextLine(ConsoleColor.Green, FileSystem.SaveFileOutput(outFile, currentDirectory, outData));
                        else
                            FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "There is no difference between files!");
                    }
                    else
                    {
                        string outFile = FileSystem.SanitizePath(arg.MiddleString("-f", "-verbose"), currentDirectory);
                        string outData = string.Empty;
                        foreach (var line in diff.Lines)
                        {
                            switch (line.Type)
                            {
                                case ChangeType.Inserted:
                                    outData += $"Ins Line:{line.Position} {line.Text}\n";
                                    break;
                                case ChangeType.Deleted:
                                    outData += $"Del Line:{line.Position} {line.Text}\n";
                                    break;
                                default:
                                    outData += $"{line.Text}\n";
                                    break;
                            }
                        }
                        if (outData.Length > 0)
                            FileSystem.ColorConsoleTextLine(ConsoleColor.Green, FileSystem.SaveFileOutput(outFile, currentDirectory, outData));
                        else
                            FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "There is no difference between files!");
                    }
                    return;
                }
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Only 2 files can be compared!");
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }
    }
}
