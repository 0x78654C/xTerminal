﻿using System;
using System.Collections.Generic;
using System.IO;
using Core;

namespace Commands.TerminalCommands.DirFiles
{
    /*Sort ascending/descending data in files. */
    class SortData : ITerminalCommand
    {
        public string Name => "sort";
        private string _currentDirectory;
        private string _helpMessage = @"Usage of sort command:

    Example 1: sort -a filePath  (Sort data ascending and displays it.)
    Example 2: sort -a filePath -o saveFilePath  (Sort data ascending and saves it to a file.)
    Example 3: sort -d filePath  (Sort data descending and displays it.)
    Example 4: sort -d filePath -o saveFilePath  (Sort data descending and saves it to a file.)
";

        public void Execute(string arg)
        {
            try
            {
                if(arg == $"{Name} -h")
                {
                    Console.WriteLine(_helpMessage);
                    return;
                }

                arg = arg.Replace($"{Name} ", "");
                _currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);

                // Ascend sort.
                AscendDataOutput(arg);

                // Descend sort.
                DescendDataOutput(arg);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }

        /// <summary>
        /// Ascending data file output and store.
        /// </summary>
        /// <param name="arg"></param>
        private void AscendDataOutput(string arg)
        {
            if (arg.StartsWith("-a"))
            {
                string filePath;
                if (arg.Contains(" -o "))
                {
                    var saveFilePath = arg.SplitByText(" -o ", 1);
                    var startPath = arg.Substring(3, arg.Length - 3);
                    filePath = startPath.SplitByText(" -o ", 0);
                    filePath = FileSystem.SanitizePath(filePath, _currentDirectory);
                    if (!File.Exists(filePath))
                    {
                        FileSystem.ErrorWriteLine($"File {filePath} does not exist!");
                        return;
                    }
                    saveFilePath = FileSystem.SanitizePath(saveFilePath, _currentDirectory);
                    var ascendingData = string.Join(Environment.NewLine, SortFileAscending(filePath));
                    File.WriteAllText(saveFilePath, ascendingData);
                    Console.WriteLine($"Sorted data saved in: {saveFilePath}");
                    return;
                }

                filePath = arg.SplitByText("-a ", 1);
                filePath = FileSystem.SanitizePath(filePath, _currentDirectory);
                if (!File.Exists(filePath))
                {
                    FileSystem.ErrorWriteLine($"File {filePath} does not exist!");
                    return;
                }
                foreach (var lineAscend in SortFileAscending(filePath))
                    Console.WriteLine(lineAscend);
            }
        }

        /// <summary>
        /// Descending data file output and store.
        /// </summary>
        /// <param name="arg"></param>
        private void DescendDataOutput(string arg)
        {
            if (arg.StartsWith("-d"))
            {
                string filePath;
                if (arg.Contains(" -o "))
                {
                    var saveFilePath = arg.SplitByText(" -o ", 1);
                    var startPath = arg.Substring(3, arg.Length - 3);
                    filePath = startPath.SplitByText(" -o ", 0);
                    filePath = FileSystem.SanitizePath(filePath, _currentDirectory);
                    if (!File.Exists(filePath))
                    {
                        FileSystem.ErrorWriteLine($"File {filePath} does not exist!");
                        return;
                    }
                    saveFilePath = FileSystem.SanitizePath(saveFilePath, _currentDirectory);
                    var ascendingData = string.Join(Environment.NewLine, SortFileDescending(filePath));
                    File.WriteAllText(saveFilePath, ascendingData);
                    Console.WriteLine($"Sorted data saved in: {saveFilePath}");
                    return;
                }

                filePath = arg.SplitByText("-d ", 1);
                filePath = FileSystem.SanitizePath(filePath, _currentDirectory);
                if (!File.Exists(filePath))
                {
                    FileSystem.ErrorWriteLine($"File {filePath} does not exist!");
                    return;
                }
                foreach (var lineAscend in SortFileDescending(filePath))
                    Console.WriteLine(lineAscend);
            }
        }

        /// <summary>
        /// Sort data desceding from file.
        /// </summary>
        /// <param name="file"></param>
        private List<string> SortFileDescending(string file)
        {
            List<string> sortList = new List<string>();
            var lines = File.ReadAllLines(file);
            foreach (var line in lines)
            {
                sortList.Add(line);
            }
            sortList.Sort((a, b) => b.CompareTo(a));
            return sortList;
        }

        /// <summary>
        /// Sort data ascending from file.
        /// </summary>
        /// <param name="file"></param>
        private List<string> SortFileAscending(string file)
        {
            List<string> sortList = new List<string>();
            var lines = File.ReadAllLines(file);
            foreach (var line in lines)
            {
                sortList.Add(line);
            }
            sortList.Sort((a, b) => a.CompareTo(b));
            return sortList;
        }
    }
}
