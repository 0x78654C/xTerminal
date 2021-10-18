﻿using Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Commands.TerminalCommands.ConsoleSystem
{

    public class Dupe
    {
        public string FileName { get; set; }
        public string Md5 { get; set; }
    }

    /* ls command class*/
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
        private static string s_virus;
        private static List<string> s_listParams = new List<string>() { "-h", "-s", "-c", "-cf", "-cd", "-hl", "-o" };
        private readonly Func<IGrouping<string, FileInfo>, IEnumerable<Dupe>[]> DupesEnumerable = items => items.Select(t => new Dupe { FileName = t.FullName, Md5 = GetMD5CheckSum(t.FullName) })
   .GroupBy(t => t.Md5)
   .Where(t => t.Count() > 1)
   .Select(t => t.Select(r => r))
   .Select(t => t)
   .ToArray();

        private static string s_helpMessage = @"
    -h  : Displays this message.
    -d  : Display duplicate files in a directory and subdirectories.
          Example1: ls -d <directory_path>
          Example2: ls -d -e <directory_path> (scanns for dulpicate files with same extension)
          Example3: ls -d <directory_path> -o <file_to_save>
          Example4: ls -d -e <directory_path> -o <file_to_save>  (scanns for dulpicate files with same extension)
    -s  : Displays size of files in current directory and subdirectories.
    -c  : Counts files and directories and subdirectories from current directory.
    -cf : Counts files from current directory and subdirectories with name containing a specific text.
          Example: ls -cf <search_text>
    -cd : Counts directories from current directory and subdirectories with name containing a specific text.
          Example: ls -cd <search_text>
    -hl : Highlights specific files/directories with by a specific text. Ex.: ls -hl <higlighted_text>
    -o  : Saves the output to a file. Ex.: ls -o <file_to_save>
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

                // This will be an empty string if there is no highlight text parameter passed
                string highlightSearchText = arg.ParameterAfter("-hl");
                bool found = true;
                foreach (var param in s_listParams)
                {
                    if (arg.ParameterAfter("ls") != param && arg.ParameterAfter("ls").Contains(":\\"))
                    {
                        if (found)
                        {
                            int arglength = args.Length - 3;
                            s_currentDirectory = args.Substring(3, arglength);
                            found = false;
                        }
                    }
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

                        dirSearchIn = arg.ContainsParameter("-e") ? dirSearchIn.Replace("ls -d -e ", "") : dirSearchIn.Replace("ls -d ", "");
                        string fileToSave = args.SplitByText("-o ", 1);
                        GetDuplicateFiles(dirSearchIn, extensions, fileToSave);
                        return;
                    }

                    if (arg.ContainsParameter("-e"))
                        extensions = true;
                    string nullDir = arg.ContainsParameter("-e") ? args.Replace("ls -d -e", "") : args.Replace("ls -d", "");
                    if (!string.IsNullOrEmpty(nullDir))
                    {
                        if (arg.ContainsParameter("-e"))
                            extensions = true;
                        string searchDir = arg.ContainsParameter("-e") ? args.SplitByText("-e ", 1) : args.SplitByText("-d ", 1);
                        GetDuplicateFiles(FileSystem.SanitizePath(searchDir, s_currentDirectory), extensions);
                        return;
                    }
                    GetDuplicateFiles(s_currentDirectory, extensions);
                    return;
                }

                // Display help message.
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

                // Save ls output to a file
                if (arg.ContainsParameter("-o"))
                {
                    SaveLSOutput(FileSystem.SanitizePath(args.SplitByText(" -o ", 1), s_currentDirectory));
                }
                else
                {
                    // Display directory and file information
                    DisplayCurrentDirectoryFiles(arg.ContainsParameter("-s"), highlightSearchText, false);
                }

                if (arg.ContainsParameter("-c"))
                {
                    Console.WriteLine($"\nCounting total directories/subdirectories and files on current location....\n");
                    DisplaySubDirectoryAndFileCounts(s_currentDirectory, string.Empty, string.Empty);
                    Console.WriteLine($"Total directories/subdirectories: {s_countDirectories}");
                    Console.WriteLine($"Total files (include subdirectories): {s_countFiles}");
                    ClearCounters();
                }

                if (arg.ContainsParameter("-cf"))
                {
                    if (!string.IsNullOrEmpty(arg.ParameterAfter("-cf")))
                    {
                        Console.WriteLine("---------------------------------------------\n");
                        DisplaySubDirectoryAndFileCounts(s_currentDirectory, arg.ParameterAfter("-cf"), "");
                        Console.WriteLine($"Total files that contains '{arg.ParameterAfter("-cf")}' (included subdirectories): {s_countFilesText}\n");
                        ClearCounters();
                    }
                }

                if (arg.ContainsParameter("-cd"))
                {
                    if (!string.IsNullOrEmpty(arg.ParameterAfter("-cd")))
                    {
                        Console.WriteLine("---------------------------------------------\n");
                        DisplaySubDirectoryAndFileCounts(s_currentDirectory, "", arg.ParameterAfter("-cd"));
                        Console.WriteLine($"Total directories/subdirectories that name contains '{arg.ParameterAfter("-cd")}': {s_countDirectoriesText}\n");
                        ClearCounters();
                    }
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
            s_timeSpan = new TimeSpan();
            s_stopWatch = new Stopwatch();
            s_stopWatch.Start();
            string results = checkExtension ? $"List of duplicated files(extension check) in {dirToScan} \n" : $"List of duplicated files in {dirToScan} :\n";

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
                    dupesList.AddRange(DupesEnumerable(group).Select(t => t.ToArray()));
                }
            }

            if (!string.IsNullOrEmpty(saveToFile))
            {
                s_stopWatch.Stop();
                s_timeSpan = s_stopWatch.Elapsed;
                results += string.Join($"{Environment.NewLine}{"".PadRight(20, '-')}{Environment.NewLine}", dupesList.Select(t => string.Join(Environment.NewLine, t.Select(e => e.FileName))));
                Console.WriteLine(FileSystem.SaveFileOutput(saveToFile, s_currentDirectory, results));
                Console.WriteLine($"Search time: {s_timeSpan.Hours} hours {s_timeSpan.Minutes} mininutes {s_timeSpan.Seconds} seconds {s_timeSpan.Milliseconds} milliseconds");
                return;
            }
            s_stopWatch.Stop();
            s_timeSpan = s_stopWatch.Elapsed;
            Console.WriteLine(results);
            Console.WriteLine(string.Join($"{Environment.NewLine}{"".PadRight(20, '-')}{Environment.NewLine}", dupesList.Select(t => string.Join(Environment.NewLine, t.Select(e => e.FileName)))));
            Console.WriteLine($"\nSearch time: {s_timeSpan.Hours} hours {s_timeSpan.Minutes} mininutes {s_timeSpan.Seconds} seconds {s_timeSpan.Milliseconds} milliseconds");
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
            DisplayCurrentDirectoryFiles(false, "", true);
            string dirList = "Directories:\n";
            dirList += string.Join("\n", s_listDirs);
            string fileList = "\n\nFiles:\n";
            fileList += string.Join("\n", s_listFiles);
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
        private static void DisplaySubDirectoryAndFileCounts(string currentDirectory, string fileName, string dirName)
        {
            var files = Directory.GetFiles(currentDirectory);
            var directories = Directory.GetDirectories(currentDirectory);

            foreach (var file in files)
            {
                if (fileName != string.Empty && file.ToLower().Contains(fileName.ToLower()))
                {
                    s_countFilesText++;
                }
                else
                {
                    s_countFiles++;
                }
            }

            foreach (var dir in directories)
            {
                if (dirName != string.Empty && dir.ToLower().Contains(dirName.ToLower()))
                {
                    s_countDirectoriesText++;
                }
                else
                {
                    s_countDirectories++;
                }
                DisplaySubDirectoryAndFileCounts(dir, fileName, dirName);
            }
        }

        /// <summary>
        /// Display files and directory list in the current directory
        /// </summary>
        /// <param name="displaySizes">Display size of files.</param>
        /// <param name="highlightSearchText">Thext to be highlighted in files or directories names.</param>
        /// <param name="saveToFile">Save output to a file.</param>
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

            Console.WriteLine("-----------Current Directory Count------------\n");
            Console.WriteLine($"Total directories: {Directory.GetDirectories(s_currentDirectory).Length}");
            Console.WriteLine($"Total files: {Directory.GetFiles(s_currentDirectory).Length}");
        }


        /// <summary>
        /// Display directories.
        /// </summary>
        /// <param name="highlightSearchText">Thext to be highlighted in files or directories names.</param>
        /// <param name="saveToFile">Save output to a file.</param>
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
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, text);
            }
            else
            {
                Console.WriteLine(text);
            }
        }
    }
}