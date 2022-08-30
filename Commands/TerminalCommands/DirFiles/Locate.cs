﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Core;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.DirFiles
{
    /*Locate command for search files and directories*/
    [SupportedOSPlatform("windows")]
    class Locate : ITerminalCommand
    {
        public string Name => "locate";
        private string _currentLocation;
        private static string s_helpMessage = @"Usage of locate command:

    Example 1: locate <text> (Displays searched files/directories from the current directory and subdirectories that includes a specific text.)
    Example 2: locate <text> -o <save_to_file> (Stores in to a file the searched files/directories from current directory and subdirectories that includes a specific text.)
  
Command can be canceled with CTRL+X key combination.
";

        public void Execute(string arg)
        {
            try
            {
                GlobalVariables.eventCancelKey = false;
                _currentLocation = File.ReadAllText(GlobalVariables.currentDirectory);
                if (arg == Name)
                {
                    Console.WriteLine("You need to provide a text for search!");
                    return;
                }

                arg = arg.Replace($"{Name} ", string.Empty);

                if (arg.StartsWith("-h") && arg.Length == 2)
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                if (arg.Contains(" -o "))
                {
                    string outputFile = arg.SplitByText(" -o ", 1);
                    outputFile = FileSystem.SanitizePath(outputFile, _currentLocation);
                    File.WriteAllText(outputFile, "");
                    string param = arg.SplitByText(" -o ", 0);
                    Console.WriteLine($"Searching for: {param}" + Environment.NewLine);
                    GlobalVariables.eventKeyFlagX = true;
                    SearchFile(_currentLocation, param, outputFile, true, ActionFind.Contains);
                    if (GlobalVariables.eventCancelKey)
                        FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                    GlobalVariables.eventCancelKey = false;
                    Console.WriteLine($"Data saved in {outputFile}");
                    return;
                }
                Console.WriteLine($"Searching for: {arg}" + Environment.NewLine);
                GlobalVariables.eventKeyFlagX = true;
                SearchFile(_currentLocation, arg, "", false, ActionFind.Contains);
                if (GlobalVariables.eventCancelKey)
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
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
        private void SearchFile(string currentDirectory, string fileName, string outputFile, bool saveToFile, ActionFind actionFind)
        {

            try
            {
                var dirsList = new List<string>();
                var filesList = new List<string>();
                bool action = false;
                Directory.GetDirectories(currentDirectory).ToList().ForEach(d => dirsList.Add(d));
                Directory.GetFiles(currentDirectory).ToList().ForEach(f => filesList.Add(f));
                foreach (var file in filesList)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    action = ShowFilesDir(fileInfo.Name, fileName, actionFind);
                    if (action)
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
                    action = ShowFilesDir(directoryInfo.Name, fileName, actionFind);
                    if (action)
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
                        SearchFile(dir, fileName, outputFile, saveToFile, actionFind);
                }
            }
            catch { }
        }


        /// <summary>
        /// Check for file/dir by category
        /// </summary>
        /// <param name="fileDirName"></param>
        /// <param name="fileName"></param>
        /// <param name="actionFind"></param>
        /// <returns></returns>
        private bool ShowFilesDir(string fileDirName, string fileName, ActionFind actionFind)
        {
            bool outStat;
            switch (actionFind)
            {
                case ActionFind.Contains:
                    outStat = fileDirName.ToLower().Contains(fileName.ToLower());
                    break;
                case ActionFind.StartsWith:
                    outStat = fileDirName.ToLower().StartsWith(fileName.ToLower());
                    break;
                case ActionFind.EndsWith:
                    outStat = fileDirName.ToLower().EndsWith(fileName.ToLower());
                    break;
                case ActionFind.Equal:
                    outStat = fileDirName.ToLower().Equals(fileName.ToLower());
                    break;
                default:
                    outStat = false;
                    break;
            }
            return outStat;
        }

        /// <summary>
        /// Locate catergories.
        /// </summary>
        enum ActionFind
        {
            Contains,
            StartsWith,
            EndsWith,
            Equal
        }
    }
}
