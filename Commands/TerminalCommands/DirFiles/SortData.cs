using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using Core;

namespace Commands.TerminalCommands.DirFiles
{
    /*Sort ascending/descending data in files. */
    [SupportedOSPlatform("Windows")]
    class SortData : ITerminalCommand
    {
        public string Name => "sort";
        private string _currentDirectory;
        private string _helpMessage = @"Usage of sort command:

    Example 1: sort -a filePath  (Sorts data in ascending order and displays it.)
    Example 2: sort -a filePath -o saveFilePath  (Sorts data in ascending order and saves it to a file.)
    Example 3: sort -d filePath  (Sorts data in descending order and displays it.)
    Example 4: sort -d filePath -o saveFilePath  (Sorts data in descending order and saves it to a file.)

Command running without saving to file can be canceled with CTRL+X key combination.
";

        public void Execute(string arg)
        {
            try
            {
                GlobalVariables.eventCancelKey = false;
                if (arg == $"{Name} -h")
                {
                    Console.WriteLine(_helpMessage);
                    return;
                }
               // GlobalVariables.pipeCmdOutput = string.Empty;
                arg = arg.Replace($"{Name} ", "");
                _currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);
                GlobalVariables.eventKeyFlagX = true;
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 && !arg.ContainsText("-t"))
                    arg = $"{arg} {GlobalVariables.pipeCmdOutput.Trim()}";

                // Ascend sort.
                AscendDataOutput(arg);

                // Descend sort.
                DescendDataOutput(arg);

                if (GlobalVariables.eventCancelKey)
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
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
                    var saveFilePath = arg.SplitByText(" -o ", 1).Split(' ')[0];
                    var startPath = arg.Substring(3, arg.Length - 3);
                    if (GlobalVariables.isPipeCommand)
                        filePath = GlobalVariables.pipeCmdOutput;
                    else
                        filePath = startPath.SplitByText(" -o ", 0);
                    filePath = FileSystem.SanitizePath(filePath, _currentDirectory).Trim();
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

                if (GlobalVariables.isPipeCommand)
                    filePath = GlobalVariables.pipeCmdOutput;
                else
                    filePath = arg.SplitByText("-a ", 1);
                filePath = FileSystem.SanitizePath(filePath, _currentDirectory).Trim();
                if (!File.Exists(filePath))
                {
                    FileSystem.ErrorWriteLine($"File {filePath} does not exist!");
                    return;
                }
                GlobalVariables.pipeCmdOutput = string.Empty;
                foreach (var lineAscend in SortFileAscending(filePath))
                {
                    if (GlobalVariables.eventCancelKey)
                        return;
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 && GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                        GlobalVariables.pipeCmdOutput += $"{lineAscend}\n";
                    else
                        Console.WriteLine(lineAscend);
                }
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
                    var saveFilePath = arg.SplitByText(" -o ", 1).Split(' ')[0];
                    var startPath = arg.Substring(3, arg.Length - 3);
                    if (GlobalVariables.isPipeCommand)
                        filePath = GlobalVariables.pipeCmdOutput;
                    else
                        filePath = startPath.SplitByText(" -o ", 0);
                    filePath = FileSystem.SanitizePath(filePath, _currentDirectory).Trim();
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

                if (GlobalVariables.isPipeCommand)
                    filePath = GlobalVariables.pipeCmdOutput;
                else
                    filePath = arg.SplitByText("-d ", 1);
                filePath = FileSystem.SanitizePath(filePath, _currentDirectory).Trim();
                if (!File.Exists(filePath))
                {
                    FileSystem.ErrorWriteLine($"File {filePath} does not exist!");
                    return;
                }
                GlobalVariables.pipeCmdOutput = string.Empty;
                foreach (var lineAscend in SortFileDescending(filePath))
                {
                    if (GlobalVariables.eventCancelKey)
                        return;
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 && GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                        GlobalVariables.pipeCmdOutput += $"{lineAscend}\n";
                    else
                        Console.WriteLine(lineAscend);
                }
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
