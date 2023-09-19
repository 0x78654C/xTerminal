﻿using Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Commands.TerminalCommands.ConsoleSystem
{

    public class Dupe
    {
        public string FileName { get; set; }
        public string Md5 { get; set; }
    }

    /* ls command class*/
    [SupportedOSPlatform("Windows")]
    public class ListDirectories : ITerminalCommand
    {
        private static string s_currentDirectory = string.Empty;
        private static int s_countFiles = 0;
        private static int s_countFilesText = 0;
        private static int s_countDirectories = 0;
        private static int s_countDirectoriesText = 0;
        private Stopwatch s_stopWatch;
        private TimeSpan s_timeSpan;
        private static List<string> s_listFiles = new List<string>();
        private static List<string> s_listDirs = new List<string>();
        private static List<string> s_listDuplicateFiles = new List<string>();
        private static List<string> s_listSearched = new List<string>();
        private static string s_virus;
        private static List<string> s_listParams = new List<string>() { "-h", "-d", "-s", "-c", "-cf", "-cd", "-hl", "-o", "-se", "-ct" };
        private readonly Func<IGrouping<string, FileInfo>, IEnumerable<Dupe>[]> DupesEnumerable = items => items.Select(t => new Dupe { FileName = t.FullName, Md5 = GetMD5CheckSum(t.FullName) })
   .GroupBy(t => t.Md5)
   .Where(t => t.Count() > 1)
   .Select(t => t.Select(r => r))
   .Select(t => t)
   .ToArray();

        private static string s_helpMessage = @"Usage of ls command:
    -h  : Displays this message.
    -d  : Displays duplicate files in a directory and subdirectories.
          Example1: ls -d <directory_path>
          Example2: ls -d -e <directory_path> (scans for duplicate files with same extension)
          Example3: ls -d <directory_path> -o <file_to_save>
          Example4: ls -d -e <directory_path> -o <file_to_save>  (scans for duplicate files with same extension)
    -s  : Displays size of files in current directory and subdirectories.
    -se : Recursively lists files and directories containing a specific text.
          Example1: ls -se <search_text>
          Example2: ls -se <search_text> -o <file_to_save>
    -c  : Counts files and directories and subdirectories from current directory.
    -cf : Counts files from current directory and subdirectories with name containing a specific text.
          Example: ls -cf <search_text>
    -cd : Counts directories from current directory and subdirectories with name containing a specific text.
          Example: ls -cd <search_text>
    -ct : Displays creation date time of files and folders from current directory.
    -hl : Highlights specific files/directories with by a specific text. Ex.: ls -hl <higlighted_text>
    -o  : Saves the output to a file. Ex.: ls -o <file_to_save>

Commands can be canceled with CTRL+X key combination.
";
        public string Name => "ls";
        public void Execute(string args)
        {
            try
            {
                // Set directory, to be used in other functions
                s_currentDirectory =
                                File.ReadAllText(GlobalVariables.currentDirectory);

                string[] arg = args.Split(' ');
                GlobalVariables.eventCancelKey = false;

                // This will be an empty string if there is no highlight text parameter passed
                string highlightSearchText = arg.ParameterAfter("-hl");
                bool found = true;
                foreach (var param in s_listParams)
                {
                    bool paramt = arg.ContainsParameter(param);
                    if (!paramt)
                    {
                        if (!string.IsNullOrEmpty(arg.ParameterAfter("ls")))
                        {
                            if (found)
                            {
                                int arglength = args.Length - 3;
                                string path = args.Substring(3, arglength);
                                if (path.Length == 2 && path.EndsWith(":"))
                                {
                                    string pathDir = FileSystem.SanitizePath(args.Substring(3, arglength) + "\\", s_currentDirectory);
                                    s_currentDirectory = pathDir;
                                }
                                else
                                {
                                    string pathDir = FileSystem.SanitizePath(args.Substring(3, arglength), s_currentDirectory);
                                    s_currentDirectory = pathDir;
                                }
                                found = false;
                            }
                        }
                    }
                    else
                    {
                        s_currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);
                    }
                }

                // List file/folder creation time.
                if (arg.ContainsParameter("-ct"))
                {
                    GlobalVariables.eventKeyFlagX = true;
                    // Display directory and file information
                    DisplayCurrentDirectoryFiles(arg.ContainsParameter("-s"), highlightSearchText, false, true);
                    if (GlobalVariables.eventCancelKey)
                        FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                    return;
                }

                // List files/folders containing a specific text in name.
                if (arg.ContainsParameter("-se"))
                {
                    string searchedText = "";
                    if (arg.ContainsParameter("-o"))
                    {
                        searchedText = args.SplitByText("-se ", 1);
                        searchedText = searchedText.SplitByText(" -o", 0);
                        GlobalVariables.eventKeyFlagX = true;
                        DisplaySubDirectoryAndFileCounts(s_currentDirectory, searchedText, searchedText, true);
                        string saveData = args.SplitByText("-o ", 1);
                        string content = $"Searching for: {searchedText}\n";
                        content += string.Join("\n", s_listSearched);
                        content += $"\n\n    Search results: {s_listSearched.Count()} matches\n";
                        Console.WriteLine(FileSystem.SaveFileOutput(FileSystem.SanitizePath(saveData, s_currentDirectory), s_currentDirectory, content));
                        if (GlobalVariables.eventCancelKey)
                            FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                        s_listSearched.Clear();
                        return;
                    }
                    searchedText = args.SplitByText("-se ", 1);
                    GlobalVariables.eventKeyFlagX = true;
                    DisplaySubDirectoryAndFileCounts(s_currentDirectory, searchedText, searchedText, true);
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                    {
                        GlobalVariables.pipeCmdOutput += $"Searching for: {searchedText}\n{string.Join("\n", s_listSearched)}\n    Search results: {s_listSearched.Count()} matches\n";
                        if (GlobalVariables.eventCancelKey)
                            FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                        s_listSearched.Clear();
                    }
                    else
                    {
                        Console.WriteLine($"Searching for: {searchedText}\n");
                        Console.WriteLine(string.Join("\n", s_listSearched));
                        Console.WriteLine($"\n    Search results: {s_listSearched.Count()} matches\n");
                        if (GlobalVariables.eventCancelKey)
                            FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                        s_listSearched.Clear();
                    }
                    return;
                }

                // Grab the duplicate files.
                if (arg.ContainsParameter("-d"))
                {
                    string dirSearchIn = args.SplitByText(" -o", 0);
                    bool extensions = false;

                    if (arg.ContainsParameter("-o"))
                    {
                        if (arg.ContainsParameter("-e"))
                            extensions = true;

                        if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                            dirSearchIn = GlobalVariables.pipeCmdOutput.Trim();
                        else
                            dirSearchIn = arg.ContainsParameter("-e") ? dirSearchIn.Replace("ls -d -e ", "") : dirSearchIn.Replace("ls -d ", "");
                        string fileToSave = args.SplitByText("-o ", 1);
                        GlobalVariables.eventKeyFlagX = true;
                        GetDuplicateFiles(FileSystem.SanitizePath(dirSearchIn, s_currentDirectory), extensions, fileToSave);
                        if (GlobalVariables.eventCancelKey)
                            FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                        return;
                    }

                    if (arg.ContainsParameter("-e"))
                        extensions = true;
                    string nullDir = string.Empty;
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                        nullDir = GlobalVariables.pipeCmdOutput;
                    else
                        nullDir = arg.ContainsParameter("-e") ? args.Replace("ls -d -e", "") : args.Replace("ls -d", "");
                    if (!string.IsNullOrEmpty(nullDir))
                    {
                        if (arg.ContainsParameter("-e"))
                            extensions = true;
                        string searchDir = string.Empty;
                        if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                            searchDir = GlobalVariables.pipeCmdOutput.Trim();
                        else
                            searchDir = arg.ContainsParameter("-e") ? args.SplitByText("-e ", 1) : args.SplitByText("-d ", 1);
                        GlobalVariables.eventKeyFlagX = true;
                        GetDuplicateFiles(FileSystem.SanitizePath(searchDir, s_currentDirectory), extensions);
                        if (GlobalVariables.eventCancelKey)
                            FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                        return;
                    }
                    GlobalVariables.eventKeyFlagX = true;
                    GetDuplicateFiles(s_currentDirectory, extensions);
                    if (GlobalVariables.eventCancelKey)
                        FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                    return;
                }

                // Display help message.
                if (args == $"{Name} -h")
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

                if (arg.ContainsParameter("-c"))
                {
                    Console.WriteLine($"\nCounting total directories/subdirectories and files on current location...\n");
                    GlobalVariables.eventKeyFlagX = true;
                    DisplaySubDirectoryAndFileCounts(s_currentDirectory, string.Empty, string.Empty, false);
                    Console.WriteLine($"Total directories/subdirectories: {s_countDirectories}");
                    Console.WriteLine($"Total files (include subdirectories): {s_countFiles}");
                    if (GlobalVariables.eventCancelKey)
                        FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                    ClearCounters();
                    return;
                }

                if (arg.ContainsParameter("-cf"))
                {
                    if (!string.IsNullOrEmpty(arg.ParameterAfter("-cf")))
                    {
                        GlobalVariables.eventKeyFlagX = true;
                        DisplaySubDirectoryAndFileCounts(s_currentDirectory, arg.ParameterAfter("-cf"), "", false);
                        Console.WriteLine($"Total files count that contains '{arg.ParameterAfter("-cf")}' (including subdirectories): {s_countFilesText}\n");
                        if (GlobalVariables.eventCancelKey)
                            FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                        ClearCounters();
                        return;
                    }
                }

                if (arg.ContainsParameter("-cd"))
                {
                    if (!string.IsNullOrEmpty(arg.ParameterAfter("-cd")))
                    {
                        GlobalVariables.eventKeyFlagX = true;
                        DisplaySubDirectoryAndFileCounts(s_currentDirectory, "", arg.ParameterAfter("-cd").Trim(), false);
                        Console.WriteLine($"Total directories/subdirectories count that name contains '{arg.ParameterAfter("-cd")}': {s_countDirectoriesText}\n");
                        if (GlobalVariables.eventCancelKey)
                            FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                        ClearCounters();
                    }
                    return;
                }

                // Save ls output to a file
                if (arg.ContainsParameter("-o"))
                {
                    SaveLSOutput(FileSystem.SanitizePath(args.SplitByText(" -o ", 1), s_currentDirectory));
                    return;
                }
                else
                {
                    GlobalVariables.eventKeyFlagX = true;
                    // Display directory and file information
                    DisplayCurrentDirectoryFiles(arg.ContainsParameter("-s"), highlightSearchText, false, false);
                    if (GlobalVariables.eventCancelKey)
                        FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Command stopped!");
                }

            }
            catch (IndexOutOfRangeException)
            {
                FileSystem.ErrorWriteLine("The command parameters were invalid!");
            }
            catch (UnauthorizedAccessException)
            {
                FileSystem.ErrorWriteLine(
                    "You need administrator rights to run full command in this place! Some directories/files cannot be accessed!");
            }
            catch (Exception e)
            {
                if (e.Message.Contains("virus"))
                {
                    FileSystem.ErrorWriteLine(e.Message);
                    FileSystem.ErrorWriteLine($"Potential virused fle or unwanted file: {s_virus}");
                }
                FileSystem.ErrorWriteLine(e.Message);
            }
        }


        /// <summary>
        /// Get duplicates files based on MD5 checksuma and file size.
        /// A big thanks for @mkbmain for help.
        /// </summary>
        /// <param name="dirToScan">Directory path where to scan for duplicates.</param>
        /// <param name="checkExtension">Check duplicates by files extesnsion.</param>
        /// <param name="saveToFile">File where to save the output.</param>
        private void GetDuplicateFiles(string dirToScan, bool checkExtension, string saveToFile = null)
        {
            GlobalVariables.pipeCmdOutput = string.Empty;
            s_timeSpan = new TimeSpan();
            s_stopWatch = new Stopwatch();
            s_stopWatch.Start();
            string results = checkExtension ? $"List of duplicated files(extension check) in {dirToScan}: " + Environment.NewLine + Environment.NewLine : $"List of duplicated files in {dirToScan}:" + Environment.NewLine + Environment.NewLine;

            var allDupesBySize = Directory.GetFiles(dirToScan, "*", SearchOption.AllDirectories)
              .Select(f => new FileInfo(f))
              .GroupBy(t => t.Length.ToString())
              .Where(t => t.Count() > 1)
              .ToArray();

            var dupesList = new List<Dupe[]>();
            foreach (var item in allDupesBySize)
            {
                if (!checkExtension)
                {
                    dupesList.AddRange(DupesEnumerable(item).Select(t => t.ToArray()));
                    continue;
                }

                foreach (var group in item.GroupBy(t => t.Extension).Where(t => t.Count() > 1).Select(t => t))
                {
                    if (GlobalVariables.eventCancelKey)
                        return;
                    dupesList.AddRange(DupesEnumerable(group).Select(t => t.ToArray()));
                }
            }

            if (!string.IsNullOrEmpty(saveToFile))
            {
                s_stopWatch.Stop();
                s_timeSpan = s_stopWatch.Elapsed;
                results += string.Join($"{Environment.NewLine}{"".PadRight(20, '-')}{Environment.NewLine}", dupesList.Select(t => string.Join(Environment.NewLine, t.Select(e => e.FileName))));
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 && GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                {
                    GlobalVariables.pipeCmdOutput += $"{FileSystem.SaveFileOutput(saveToFile, s_currentDirectory, results)}\nSearch time: {s_timeSpan.Hours} hours {s_timeSpan.Minutes} mininutes {s_timeSpan.Seconds} seconds {s_timeSpan.Milliseconds} milliseconds";
                }
                else
                {
                    Console.WriteLine(FileSystem.SaveFileOutput(saveToFile, s_currentDirectory, results));
                    Console.WriteLine($"Search time: {s_timeSpan.Hours} hours {s_timeSpan.Minutes} mininutes {s_timeSpan.Seconds} seconds {s_timeSpan.Milliseconds} milliseconds");
                    return;
                }
            }
            s_stopWatch.Stop();
            s_timeSpan = s_stopWatch.Elapsed;
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 && GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
            {
                GlobalVariables.pipeCmdOutput += $"{results}\n{string.Join($"{Environment.NewLine}{"".PadRight(20, '-')}{Environment.NewLine}", dupesList.Select(t => string.Join(Environment.NewLine, t.Select(e => e.FileName))))}" +
                    $"\nSearch time: {s_timeSpan.Hours} hours {s_timeSpan.Minutes} mininutes {s_timeSpan.Seconds} seconds {s_timeSpan.Milliseconds} milliseconds\n";
            }
            else
            {
                Console.WriteLine(results);
                Console.WriteLine(string.Join($"{Environment.NewLine}{"".PadRight(20, '-')}{Environment.NewLine}", dupesList.Select(t => string.Join(Environment.NewLine, t.Select(e => e.FileName)))));
                Console.WriteLine($"\nSearch time: {s_timeSpan.Hours} hours {s_timeSpan.Minutes} mininutes {s_timeSpan.Seconds} seconds {s_timeSpan.Milliseconds} milliseconds");
            }
        }

        /// <summary>
        /// Get the MD5 checksum of a file.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static string GetMD5CheckSum(string file)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(file))
                {
                    s_virus = file; // The file where Windows Defender detects a potential malware.
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }


        // Clear the counters.
        private void ClearCounters()
        {
            s_countDirectories = 0;
            s_countFiles = 0;
            s_countDirectoriesText = 0;
            s_countFilesText = 0;
        }

        /// <summary>
        /// Save to file the ls ouput.
        /// </summary>
        /// <param name="path">Path to file</param>
        private static void SaveLSOutput(string path)
        {
            DisplayCurrentDirectoryFiles(false, "", true, false);
            string dirList = "-----Directories-----" + Environment.NewLine;
            dirList += string.Join(Environment.NewLine, s_listDirs);
            string fileList = Environment.NewLine + "-------Files-------" + Environment.NewLine;
            fileList += string.Join(Environment.NewLine, s_listFiles);
            string finalList = dirList + fileList;
            Console.WriteLine(FileSystem.SaveFileOutput(path, s_currentDirectory, finalList));
            s_listDirs.Clear();
            s_listFiles.Clear();
        }

        /// <summary>
        /// Display recursively the files or part of files/directory name and directories count.
        /// </summary>
        /// <param name="currentDirectory">Current directory location.</param>
        /// <param name="fileName">File name.</param>
        /// <param name="dirName">Directory name.</param>
        private static void DisplaySubDirectoryAndFileCounts(string currentDirectory, string fileName, string dirName, bool search)
        {
            var files = Directory.GetFiles(currentDirectory);
            var directories = Directory.GetDirectories(currentDirectory);

            foreach (var file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileName != string.Empty && fileInfo.Name.ToLower().Contains(fileName.ToLower()))
                {
                    s_countFilesText++;
                    if (search)
                        s_listSearched.Add($"File: {file}");
                }
                else
                {
                    s_countFiles++;
                }
            }

            foreach (var dir in directories)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(dir);
                if (dirName != string.Empty && directoryInfo.Name.ToLower().Contains(dirName.ToLower()))
                {
                    s_countDirectoriesText++;
                    if (search)
                    {
                        s_listSearched.Add("---------------------------------------------------------------");
                        s_listSearched.Add($"Dir: {dir}");
                    }
                }
                else
                {
                    s_countDirectories++;
                }
                if (!GlobalVariables.eventCancelKey)
                    DisplaySubDirectoryAndFileCounts(dir, fileName, dirName, search);
            }
        }

        /// <summary>
        /// Display files and directory list in the current directory
        /// </summary>
        /// <param name="displaySizes">Display size of files.</param>
        /// <param name="highlightSearchText">Thext to be highlighted in files or directories names.</param>
        /// <param name="saveToFile">Save output to a file.</param>
        private static void DisplayCurrentDirectoryFiles(bool displaySizes, string highlightSearchText, bool saveToFile, bool creationTime)
        {
            if (!Directory.Exists(s_currentDirectory))
            {
                FileSystem.ErrorWriteLine($"Directory '{s_currentDirectory}' does not exist!");
                return;
            }

            if (saveToFile)
            {
                DisplaySubDirectories(highlightSearchText, saveToFile, creationTime);
                DisplayFiles(highlightSearchText, displaySizes, saveToFile, creationTime);
            }
            else
            {
                DisplaySubDirectories(highlightSearchText, saveToFile, creationTime);
                DisplayFiles(highlightSearchText, displaySizes, saveToFile, creationTime);
            }

            if (displaySizes)
            {
                string currentDirectorySize =
                    FileSystem.GetDirSize(new DirectoryInfo(s_currentDirectory));
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                    GlobalVariables.pipeCmdOutput += $"----------------------------------------------\nCurrent directory size: {currentDirectorySize}\n";
                else
                {
                    Console.WriteLine("----------------------------------------------\n");
                    Console.WriteLine($"Current directory size: {currentDirectorySize}\n");
                }
            }
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                GlobalVariables.pipeCmdOutput += $"-----------Current Directory Count------------\nTotal directories: {Directory.GetDirectories(s_currentDirectory).Length}\nTotal files: {Directory.GetFiles(s_currentDirectory).Length}\n";
            else
            {
                Console.WriteLine("-----------Current Directory Count------------\n");
                Console.WriteLine($"Total directories: {Directory.GetDirectories(s_currentDirectory).Length}");
                Console.WriteLine($"Total files: {Directory.GetFiles(s_currentDirectory).Length}");
            }
        }


        /// <summary>
        /// Display directories.
        /// </summary>
        /// <param name="highlightSearchText">Thext to be highlighted in files or directories names.</param>
        /// <param name="saveToFile">Save output to a file.</param>
        private static void DisplaySubDirectories(string highlightSearchText, bool saveToFile, bool creationTime)
        {
            try
            {
                foreach (var dir in Directory.GetDirectories(s_currentDirectory))
                {
                    if (GlobalVariables.eventCancelKey)
                        return;

                    var directoryInfo = new DirectoryInfo(dir);
                    if (!GlobalVariables.excludeDirectories.Contains(directoryInfo.Name))
                    {
                        if (highlightSearchText.IsNotNullEmptyOrWhitespace() &&
                        directoryInfo.Name.ContainsText(highlightSearchText))
                        {
                            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                                GlobalVariables.pipeCmdOutput += $"{directoryInfo.Name}\n";
                            else
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
                                if (creationTime)
                                {
                                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                                        GlobalVariables.pipeCmdOutput += $"{FileSystem.GetCreationDateDirInfo(directoryInfo)}\n";
                                    else
                                        FileSystem.ColorConsoleTextLine(ConsoleColor.DarkCyan, FileSystem.GetCreationDateDirInfo(directoryInfo));
                                }
                                else
                                {
                                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                                        GlobalVariables.pipeCmdOutput += $"{directoryInfo.Name}\n";
                                    else
                                        FileSystem.ColorConsoleTextLine(ConsoleColor.DarkCyan, directoryInfo.Name);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private static void DisplayFiles(string highlightSearchText, bool displaySizes, bool saveToFile, bool creationTime)
        {
            try
            {
                // This LINQ statement converts a list of string file names to FileInfo objects
                var files = Directory.GetFiles(s_currentDirectory).Select(f => new FileInfo(f));

                foreach (var file in files)
                {
                    if (GlobalVariables.eventCancelKey)
                        return;
                    if (displaySizes)
                    {
                        if (!GlobalVariables.excludeFiles.Contains(file.Name))
                        {
                            string formattedText = GetFormattedFileInfoText(file, displaySizes);
                            if (saveToFile)
                            {
                                s_listFiles.Add(file.Name);
                            }
                            else
                            {
                                if (creationTime)
                                {
                                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                                        GlobalVariables.pipeCmdOutput += $"{FileSystem.GetCreationDateFileInfo(file)}\n";
                                    else
                                        Console.WriteLine(FileSystem.GetCreationDateFileInfo(file));
                                }
                                else
                                    DisplayFileInfoText(formattedText, highlightSearchText);
                            }
                        }
                    }
                    else
                    {
                        if (!GlobalVariables.excludeFiles.Contains(file.Name))
                        {
                            string formattedText = GetFormattedFileInfoText(file, displaySizes);
                            if (saveToFile)
                            {
                                s_listFiles.Add(file.Name);
                            }
                            else
                            {
                                if (creationTime)
                                {
                                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                                        GlobalVariables.pipeCmdOutput += $"{FileSystem.GetCreationDateFileInfo(file)}\n";
                                    else
                                        Console.WriteLine(FileSystem.GetCreationDateFileInfo(file));
                                }
                                else
                                    DisplayFileInfoText(formattedText, highlightSearchText);
                            }
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
        }

        /// <summary>
        /// Format the the space limit for files sizes.
        /// </summary>
        /// <param name="fileInfo">File info.</param>
        /// <param name="displaySizes">Display size</param>
        /// <returns></returns>
        private static string GetFormattedFileInfoText(FileInfo fileInfo, bool displaySizes)
        {
            return displaySizes
                ? fileInfo.Name.PadRight(50, ' ') + $"Size:  {FileSystem.GetFileSize(fileInfo.DirectoryName + "\\" + fileInfo.Name, false)}"
                : fileInfo.Name;
        }


        /// <summary>
        /// Highlight the speficied text in files names.
        /// </summary>
        /// <param name="text">Text to be higlighted.</param>
        /// <param name="highlightSearchText">Highlight</param>
        private static void DisplayFileInfoText(string text, string highlightSearchText)
        {
            if (highlightSearchText.IsNotNullEmptyOrWhitespace() &&
                text.ContainsText(highlightSearchText))
            {
                if(GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                    GlobalVariables.pipeCmdOutput+=$"{text}\n";
                else
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, text);
            }
            else
            {
                if (text.EndsWith(".exe") || text.EndsWith(".msi"))
                {
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                        GlobalVariables.pipeCmdOutput += $"{text}\n";
                    else
                        FileSystem.ColorConsoleTextLine(ConsoleColor.Magenta, text);
                }
                else
                {
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                        GlobalVariables.pipeCmdOutput += $"{text}\n";
                    else
                        Console.WriteLine(text);
                }
            }
        }
    }
}