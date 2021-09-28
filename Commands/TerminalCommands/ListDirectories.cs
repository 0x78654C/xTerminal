using System;
using System.IO;
using System.Linq;
using Core;

namespace Commands.TerminalCommands
{
    public class ListDirectories : ITerminalCommand
    {
        private static string s_currentDirectory = string.Empty;

        public string Name => "ls";

        public void Execute(string args)
        {
            try
            {
                string[] arg = args.Split(' ');
                // This will be an empty string if there is no highlight text parameter passed
                string highlightSearchText = arg.ParameterAfter("-hl");

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

                // Display directory and file information
                DisplayCurrentDirectoryFiles(arg.ContainsParameter("-s"), highlightSearchText);

                if (arg.ContainsParameter("-c"))
                {
                    DisplaySubDirectoryAndFileCounts();
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

        private static void DisplaySubDirectoryAndFileCounts()
        {
            var files = Directory.GetFiles(s_currentDirectory, "*.*", SearchOption.AllDirectories);
            var directories = Directory.GetDirectories(s_currentDirectory, "*", SearchOption.AllDirectories);

            Console.WriteLine($"Counting total directories/subdirectories and files on current location....\n");
            Console.WriteLine($"Total directories/subdirectories: {directories}");
            Console.WriteLine($"Total files (include subdirectories): {files}");
        }

        private static void DisplayCurrentDirectoryFiles(bool displaySizes, string highlightSearchText)
        {
            if (!Directory.Exists(s_currentDirectory))
            {
                FileSystem.ErrorWriteLine($"Directory '{s_currentDirectory}' does not exist!");
                return;
            }

            DisplaySubDirectories(highlightSearchText);

            DisplayFiles(highlightSearchText, displaySizes);

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

        private static void DisplaySubDirectories(string highlightSearchText)
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
                    FileSystem.ColorConsoleTextLine(ConsoleColor.DarkCyan, directoryInfo.Name);
                }
            }
        }

        private static void DisplayFiles(string highlightSearchText, bool displaySizes)
        {
            // This LINQ statement converts a list of string file names to FileInfo objects
            var files = Directory.GetFiles(s_currentDirectory).Select(f => new FileInfo(f));

            foreach (var file in files)
            {
                string formattedText = GetFormattedFileInfoText(file, displaySizes);

                DisplayFileInfoText(formattedText, highlightSearchText);
            }
        }

        private static string GetFormattedFileInfoText(FileInfo fileInfo, bool displaySizes)
        {
            return displaySizes 
                ? fileInfo.Name.PadRight(50, ' ') + $"Size: {fileInfo.Length}" 
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