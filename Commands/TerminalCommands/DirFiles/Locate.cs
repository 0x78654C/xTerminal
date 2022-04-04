using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Core;

namespace Commands.TerminalCommands.DirFiles
{
    /*Locate command for search files and directories*/

    class Locate : ITerminalCommand
    {
        public string Name => "locate";
        private string _currentLocation;
        private static string s_helpMessage = @"Usage of locate command:

    Example 1: locate <text> (Displays searched files/directories from current directory and subdirectories that includes a specific text.)
    Example 2: locate <text> -o <save_to_file> (Stores in to a file the searched files/directories from current directory and subdirectories that includes a specific text.)
  
Command be canceled with CTRL+X key combination.
";

        public void Execute(string arg)
        {
            try
            {
                _currentLocation = File.ReadAllText(GlobalVariables.currentDirectory);
                if (arg==Name)
                {
                    Console.WriteLine("You need to provide a text for search!");
                    return;
                }

                arg = arg.Replace($"{Name} ", string.Empty);

                if(arg.StartsWith("-h") && arg.Length == 2)
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                if(arg.Contains(" -o "))
                {
                    string outputFile = arg.SplitByText(" -o ", 1);
                    outputFile = FileSystem.SanitizePath(outputFile, _currentLocation);
                    File.WriteAllText(outputFile, "");
                    string param = arg.SplitByText(" -o ", 0);
                    Console.WriteLine($"Searching for: {param}" + Environment.NewLine);
                    GlobalVariables.eventKeyFlagX = true;
                    SearchFile(_currentLocation, param, outputFile, true);
                    GlobalVariables.eventCancelKey = false;
                    Console.WriteLine($"Data saved in {outputFile}");
                    return;
                }
                Console.WriteLine($"Searching for: {arg}" + Environment.NewLine);
                GlobalVariables.eventKeyFlagX = true;
                SearchFile(_currentLocation, arg,"",false);
                GlobalVariables.eventCancelKey = false;
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }


        /// <summary>
        /// Search file/directory in current directory and subdirectories by a specific text. 
        /// </summary>
        /// <param name="currentDirectory"></param>
        /// <param name="fileName"></param>
        /// <param name="outputFile"></param>
        /// <param name="saveToFile"></param>
        private void SearchFile(string currentDirectory, string fileName, string outputFile,bool saveToFile)
        {
     
            try
            {
                var dirsList = new List<string>();
                var filesList = new List<string>();
                Directory.GetDirectories(currentDirectory).ToList().ForEach(d => dirsList.Add(d));
                Directory.GetFiles(currentDirectory).ToList().ForEach(f => filesList.Add(f));
                foreach (var file in filesList)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    if (fileInfo.Name.ToLower().Contains(fileName.ToLower()) && FileSystem.CheckPermission(fileInfo.FullName, false, FileSystem.CheckType.Directory))
                    {
                        if (saveToFile)
                        {
                            File.AppendAllText(outputFile, "File: " + file + Environment.NewLine);
                        }
                        else
                        {
                            Console.WriteLine("File: " + file);
                        }
                    }
                }

                foreach (var dir in dirsList)
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(dir);
                    if (directoryInfo.Name.ToLower().Contains(fileName.ToLower()) && FileSystem.CheckPermission(directoryInfo.FullName, false, FileSystem.CheckType.Directory))
                    {
                        if (saveToFile)
                        {
                            File.AppendAllText(outputFile, "DIR: " + dir + Environment.NewLine);
                        }
                        else
                        {
                            FileSystem.ColorConsoleTextLine(ConsoleColor.Green, "DIR: " + dir);
                        }
                    }
                    if (!GlobalVariables.eventCancelKey)
                        SearchFile(dir, fileName, outputFile, saveToFile);
                }
            }
            catch (UnauthorizedAccessException){}
        }
    }
}
