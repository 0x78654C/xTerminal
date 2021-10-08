using Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Commands.TerminalCommands.ConsoleSystem
{
    /* ls command class*/
    public class ListDirectories : ITerminalCommand
    {
        private static string s_currentDirectory = string.Empty;

        private static int s_countFiles = 0;
        private static int s_countDirectories = 0;
        private static List<string> s_listFiles = new List<string>();
        private static List<string> s_listDirs = new List<string>();
        private static string s_helpMessage = @"
    -h  : Displays this message.
    -s  : Displays size of files in current directory.
    -c  : Counts files and directories (subdirs too) in current directory.
    -hl : Highlights specific files/directories with by a specific text. Ex.: ls -hl higlighted_text
    -o  : Saves the output to a file. Ex.: ls -o file_to_save
";
        public string Name => "ls";

        public void Execute(string args)
        {
            try
            {
                string[] arg = args.Split(' ');

                // This will be an empty string if there is no highlight text parameter passed
                string highlightSearchText = arg.ParameterAfter("-hl");


                // Display help message
                if (arg.ContainsParameter("-h"))
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                // If the user passed "-hl" parameter without a search text parameter, report an error.
                if (arg.ContainsParameter("-hl") &&
                    string.IsNullOrWhiteSpace(highlightSearchText))
                {
                    FileSystem.ErrorWriteLine("Check command. You must provide a text to highlight!");
                    return;
                }

                // Set directory, to be used in other functions
                s_currentDirectory =
                    RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory);

                // Save ls output to a file
                if (arg.ContainsParameter("-o"))
                {
                    SaveLSOutput(arg.ParameterAfter("-o"));
                }
                else
                {
                    // Display directory and file information
                    DisplayCurrentDirectoryFiles(arg.ContainsParameter("-s"), highlightSearchText, false);
                }

                if (arg.ContainsParameter("-c"))
                {
                    Console.WriteLine($"\nCounting total directories/subdirectories and files on current location....\n");
                    DisplaySubDirectoryAndFileCounts(s_currentDirectory);
                    Console.WriteLine($"Total directories/subdirectories: {s_countDirectories}");
                    Console.WriteLine($"Total files (include subdirectories): {s_countFiles}");
                    s_countDirectories = 0;
                    s_countFiles = 0;
                }
            }
            catch (IndexOutOfRangeException)
            {
                FileSystem.ErrorWriteLine("The command parameters were not valid");
            }
            catch (UnauthorizedAccessException)
            {
                FileSystem.ErrorWriteLine(
                    "You need administrator rights to run full command in this place! Some directories/files cannot be accessed!");
            }
            catch
            {
                FileSystem.ErrorWriteLine("Unable to perform the 'ls' command");
            }
        }

        private static void SaveLSOutput(string path)
        {
            DisplayCurrentDirectoryFiles(false, "", true);
            string dirList = "Directories:\n";
            dirList += string.Join("\n", s_listDirs);
            string fileList = "\n\nFiles:\n";
            fileList += string.Join("\n", s_listFiles);
            string finalList = dirList + fileList;
            Console.WriteLine(FileSystem.SaveFileOutput(path, s_currentDirectory, finalList));
        }

        private static void DisplaySubDirectoryAndFileCounts(string currentDirectory)
        {
            var files = Directory.GetFiles(currentDirectory);
            var directories = Directory.GetDirectories(currentDirectory);

            foreach (var file in files)
            {
                s_countFiles++;
            }

            foreach (var dir in directories)
            {
                DisplaySubDirectoryAndFileCounts(dir);
                s_countDirectories++;
            }
        }

        private static void DisplayCurrentDirectoryFiles(bool displaySizes, string highlightSearchText, bool saveToFile)
        {
            if (!Directory.Exists(s_currentDirectory))
            {
                FileSystem.ErrorWriteLine($"Directory '{s_currentDirectory}' does not exist!");
                return;
            }

            if (saveToFile)
            {
                DisplaySubDirectories(highlightSearchText, saveToFile);
                DisplayFiles(highlightSearchText, displaySizes, saveToFile);
            }
            else
            {
                DisplaySubDirectories(highlightSearchText, saveToFile);
                DisplayFiles(highlightSearchText, displaySizes, saveToFile);
            }

            if (displaySizes)
            {
                string currentDirectorySize =
                    FileSystem.GetDirSize(new DirectoryInfo(s_currentDirectory));

                Console.WriteLine("---------------------------------------------\n");
                Console.WriteLine($"Current directory size: {currentDirectorySize}\n");
            }

            Console.WriteLine("---------------------------------------------\n");
            Console.WriteLine($"Total directories: {Directory.GetDirectories(s_currentDirectory).Length}");
            Console.WriteLine($"Total files: {Directory.GetFiles(s_currentDirectory).Length}");
        }

        private static void DisplaySubDirectories(string highlightSearchText, bool saveToFile)
        {
            foreach (var dir in Directory.GetDirectories(s_currentDirectory))
            {
                var directoryInfo = new DirectoryInfo(dir);

                if (highlightSearchText.IsNotNullEmptyOrWhitespace() &&
                    directoryInfo.Name.ContainsText(highlightSearchText))
                {
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Red, directoryInfo.Name);
                }
                else
                {
                    if (saveToFile)
                    {
                        s_listDirs.Add(directoryInfo.Name);
                    }
                    else
                    {
                        FileSystem.ColorConsoleTextLine(ConsoleColor.DarkCyan, directoryInfo.Name);
                    }
                }
            }
        }

        private static void DisplayFiles(string highlightSearchText, bool displaySizes, bool saveToFile)
        {
            // This LINQ statement converts a list of string file names to FileInfo objects
            var files = Directory.GetFiles(s_currentDirectory).Select(f => new FileInfo(f));

            foreach (var file in files)
            {
                string formattedText = GetFormattedFileInfoText(file, displaySizes);
                if (saveToFile)
                {
                    s_listFiles.Add(file.Name);
                }
                else
                {
                    DisplayFileInfoText(formattedText, highlightSearchText);
                }
            }
        }

        private static string GetFormattedFileInfoText(FileInfo fileInfo, bool displaySizes)
        {
            return displaySizes
                ? fileInfo.Name.PadRight(50, ' ') + $"Size:  {FileSystem.GetFileSize(fileInfo.DirectoryName + "\\" + fileInfo.Name, false)}"
                : fileInfo.Name;
        }

        private static void DisplayFileInfoText(string text, string highlightSearchText)
        {
            if (highlightSearchText.IsNotNullEmptyOrWhitespace() &&
                text.ContainsText(highlightSearchText))
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, text);
            }
            else
            {
                Console.WriteLine(text);
            }
        }
    }
}