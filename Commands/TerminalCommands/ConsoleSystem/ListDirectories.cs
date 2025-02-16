using Core;
using Core.SystemTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

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
        private static int s_countDirLen = 0;
        private static int s_sm = 0;
        private Stopwatch s_stopWatch;
        private TimeSpan s_timeSpan;
        private static List<string> s_listFiles = new List<string>();
        private static List<string> s_listDirs = new List<string>();
        private static List<string> s_listDuplicateFiles = new List<string>();
        private static List<string> s_listSearched = new List<string>();
        private static string s_virus;
        private static string s_tree;
        private static List<string> s_listParams = new List<string>() { "-h", "-d", "-s", "-c", "-cf", "-cd", "-hl", "-o", "-ct", "-la" };
        private static string s_Header = "";
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
          Example5: ls -d -length (sets the length of bytes from where will be the MD5 hash extracted. If is set to 0 or less than will scan the entire file.)   
    -s  : Displays size of files in current directory and subdirectories.
    -c  : Counts files and directories and subdirectories from current directory.
    -cf : Counts files from current directory and subdirectories with name containing a specific text.
          Example: ls -cf <search_text>
    -cd : Counts directories from current directory and subdirectories with name containing a specific text.
          Example: ls -cd <search_text>
    -ct : Displays creation date time of files and folders from current directory.
    -la : Displays last access date time of files and folders from current directory.
    -hl : Highlights specific files/directories with by a specific text. Ex.: ls -hl <higlighted_text>
    -o  : Saves the output to a file. Ex.: ls -o <file_to_save>
    -t  : Display tree structure of directories. Use with param -o for store the output in a file: Ex.: ls -t -o <file_name>

Commands can be canceled with CTRL+X key combination.

Attributes legend:
d - Directory
a - Archive
r - ReadOnly
h - Hidden
s - System
l - ReparsePoint
c - Compressed
e - Encrypted
";
        public string Name => "ls";
        public void Execute(string args)
        {
            try
            {
                // Set directory, to be used in other functions
                s_currentDirectory =
                                File.ReadAllText(GlobalVariables.currentDirectory);
                s_countDirLen = s_currentDirectory.Split('\\').Count() - 1;
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
                        FileSystem.SuccessWriteLine("Command stopped!");
                    return;
                }

                // List file/folder last access time.
                if (arg.ContainsParameter("-la"))
                {
                    GlobalVariables.eventKeyFlagX = true;
                    // Display directory and file information
                    DisplayCurrentDirectoryFiles(arg.ContainsParameter("-s"), highlightSearchText, false, false, true);
                    if (GlobalVariables.eventCancelKey)
                        FileSystem.SuccessWriteLine("Command stopped!");
                    return;
                }

                // Grab the duplicate files.
                if (arg.ContainsParameter("-d"))
                {

                    if (arg.ContainsParameter("-length"))
                    {
                        var length = args.SplitByText("-length", 1).Trim();
                        GlobalVariables.fileHexLength = Int32.Parse(length);
                        FileSystem.SuccessWriteLine($"Duplicate file scan bytes length is set to: {length}");
                        return;
                    }

                    string dirSearchIn = string.Empty;
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0)
                        dirSearchIn = GlobalVariables.pipeCmdOutput.Trim();
                    else
                        dirSearchIn = args.SplitByText(" -o", 0);
                    bool extensions = false;


                    if (arg.ContainsParameter("-o"))
                    {
                        if (arg.ContainsParameter("-e"))
                            extensions = true;

                        if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0)
                            dirSearchIn = GlobalVariables.pipeCmdOutput.Trim();
                        else
                            dirSearchIn = arg.ContainsParameter("-e") ? dirSearchIn.Replace("ls -d -e", "") : dirSearchIn.Replace("ls -d", "");
                        string fileToSave = args.SplitByText("-o ", 1);
                        GlobalVariables.eventKeyFlagX = true;
                        GetDuplicateFiles(FileSystem.SanitizePath(dirSearchIn.Trim(), s_currentDirectory), extensions, fileToSave);
                        if (GlobalVariables.eventCancelKey)
                            FileSystem.SuccessWriteLine("Command stopped!");
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
                        if (string.IsNullOrEmpty(searchDir))
                            searchDir = s_currentDirectory;
                        GlobalVariables.eventKeyFlagX = true;
                        GetDuplicateFiles(FileSystem.SanitizePath(searchDir, s_currentDirectory), extensions);
                        if (GlobalVariables.eventCancelKey)
                            FileSystem.SuccessWriteLine("Command stopped!");
                        return;
                    }
                    GlobalVariables.eventKeyFlagX = true;
                    GetDuplicateFiles(s_currentDirectory, extensions);
                    if (GlobalVariables.eventCancelKey)
                        FileSystem.SuccessWriteLine("Command stopped!");
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
                    GlobalVariables.isErrorCommand = true;
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
                        FileSystem.SuccessWriteLine("Command stopped!");
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
                            FileSystem.SuccessWriteLine("Command stopped!");
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
                            FileSystem.SuccessWriteLine("Command stopped!");
                        ClearCounters();
                    }
                    return;
                }

                if (arg.ContainsParameter("-t"))
                {
                    GlobalVariables.eventKeyFlagX = true;
                    var currDir = File.ReadAllText(GlobalVariables.currentDirectory);
                    DisplayTreeDirStructure(currDir);
                    if (arg.ContainsParameter("-o"))
                    {
                        var fileName = args.SplitByText("-o", 1).Trim();
                        FileSystem.SuccessWriteLine(FileSystem.SaveFileOutput(fileName, currDir, s_tree));
                        s_tree = "";
                    }
                    else
                        FileSystem.SuccessWriteLine(s_tree);
                    s_tree = "";
                    if (GlobalVariables.eventCancelKey)
                        FileSystem.SuccessWriteLine("Command stopped!");
                    ClearCounters();
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
                    if (GlobalVariables.isPipeCommand && !string.IsNullOrEmpty(GlobalVariables.pipeCmdOutput))
                        s_currentDirectory = FileSystem.SanitizePath(GlobalVariables.pipeCmdOutput.Trim(), s_currentDirectory);
                    GlobalVariables.eventKeyFlagX = true;

                    // Display directory and file information
                    DisplayCurrentDirectoryFiles(arg.ContainsParameter("-s"), highlightSearchText, false, false);
                    if (GlobalVariables.eventCancelKey)
                        FileSystem.SuccessWriteLine("Command stopped!");
                }

            }
            catch (IndexOutOfRangeException)
            {
                FileSystem.ErrorWriteLine("The command parameters were invalid!");
                GlobalVariables.isErrorCommand = true;
            }
            catch (UnauthorizedAccessException)
            {
                FileSystem.ErrorWriteLine(
                    "You need administrator rights to run full command in this place! Some directories/files cannot be accessed!");
                GlobalVariables.isErrorCommand = true;
            }
            catch (Exception e)
            {
                if (e.Message.Contains("virus"))
                {
                    FileSystem.ErrorWriteLine(e.Message);
                    FileSystem.ErrorWriteLine($"Potential virused fle or unwanted file: {s_virus}");
                    GlobalVariables.isErrorCommand = true;
                }
                else
                {
                    FileSystem.ErrorWriteLine(e.Message);
                    GlobalVariables.isErrorCommand = true;
                }
            }
        }

        /// <summary>
        /// Display structure dirs.
        /// </summary>
        /// <param name="currDir"></param>
        private void DisplayTreeDirStructure(string currDir, string indent = "", bool isLast = true)
        {
            try
            {
                var directories = Directory.GetDirectories(currDir);
                var dirInfo = new DirectoryInfo(currDir);
                s_tree += indent + (isLast ? "└─ " : "├─ ") + dirInfo.Name + "\n";
                indent += isLast ? "   " : "│  ";
                for (int i = 0; i < directories.Length; i++)
                {
                    var directory = directories[i];
                    bool isLastDirectory = (i == directories.Length - 1);
                    DisplayTreeDirStructure(directory, indent, isLastDirectory);
                }
            }
            catch
            {
                // Ignore if no access or any exceptions.
            }
        }

        private string SeparatorIncrement(int count)
        {
            var sep = "";
            for (int i = 0; i < count; i++)
                sep += "  ";
            return sep;
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
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, FileSystem.SaveFileOutput(saveToFile, s_currentDirectory, results));
                Console.WriteLine($"Search time: {s_timeSpan.Hours} hours {s_timeSpan.Minutes} mininutes {s_timeSpan.Seconds} seconds {s_timeSpan.Milliseconds} milliseconds");
                return;
            }
            s_stopWatch.Stop();
            s_timeSpan = s_stopWatch.Elapsed;
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 && GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
            {
                GlobalVariables.pipeCmdOutput = string.Empty;
                GlobalVariables.pipeCmdOutput += $"{results}\n{string.Join($"{Environment.NewLine}{"".PadRight(20, '-')}{Environment.NewLine}", dupesList.Select(t => string.Join(Environment.NewLine, t.Select(e => e.FileName))))}" +
                    $"\nSearch time: {s_timeSpan.Hours} hours {s_timeSpan.Minutes} mininutes {s_timeSpan.Seconds} seconds {s_timeSpan.Milliseconds} milliseconds\n";
            }
            else if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == GlobalVariables.pipeCmdCountTemp)
            {
                GlobalVariables.pipeCmdOutput = string.Empty;
                GlobalVariables.pipeCmdOutput += $"{results}\n{string.Join($"{Environment.NewLine}{"".PadRight(20, '-')}{Environment.NewLine}", dupesList.Select(t => string.Join(Environment.NewLine, t.Select(e => e.FileName))))}" +
                     $"\nSearch time: {s_timeSpan.Hours} hours {s_timeSpan.Minutes} mininutes {s_timeSpan.Seconds} seconds {s_timeSpan.Milliseconds} milliseconds\n";
            }
            else if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
            {
                GlobalVariables.pipeCmdOutput = string.Empty;
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
            s_virus = file; // The file where Windows Defender detects a potential malware.
            if (GlobalVariables.fileHexLength > 0)
            {
                var hexDump = new HexDmpLine(file, GlobalVariables.fileHexLength);
                var hex = hexDump.GetHex();
                var computeHexHash = ComputeHash(hex);
                return computeHexHash;
            }
            var computeHash = ComputeHash(file, true);
            return computeHash;
        }

        /// <summary>
        /// Get MD5 from a file or string.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="isFile"></param>
        /// <returns></returns>
        private static string ComputeHash(string data, bool isFile = false)
        {
            var result = "";
            using (var md5 = MD5.Create())
            {
                s_virus = data; // The file where Windows Defender detects a potential malware.
                if (isFile)
                {
                    using (var stream = File.OpenRead(data))
                    {
                        var hashFile = md5.ComputeHash(stream);
                        result = BitConverter.ToString(hashFile).Replace("-", "").ToLowerInvariant();
                    }
                    return result;
                }
                var bytes = Encoding.ASCII.GetBytes(data);
                var hash = md5.ComputeHash(bytes);
                result = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            return result;
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
            SetHeader(TypeHeader.LastWrite, true);
            string dirList = "-----Directories-----" + Environment.NewLine;
            dirList += string.Join(Environment.NewLine, s_listDirs);
            string fileList = Environment.NewLine + "-------Files-------" + Environment.NewLine;
            fileList += string.Join(Environment.NewLine, s_listFiles);
            string finalList = s_Header + dirList + fileList;
            FileSystem.SuccessWriteLine(FileSystem.SaveFileOutput(path, s_currentDirectory, finalList));
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
        private static void DisplayCurrentDirectoryFiles(bool displaySizes, string highlightSearchText, bool saveToFile,
            bool isCreationTime = false, bool isLastAccessTime = false)
        {
            if (GlobalVariables.isPipeCommand)
                GlobalVariables.pipeCmdOutput = string.Empty;

            if (!Directory.Exists(s_currentDirectory))
            {
                FileSystem.ErrorWriteLine($"Directory '{s_currentDirectory}' does not exist!");
                GlobalVariables.isErrorCommand = true;
                return;
            }

            if (displaySizes)
            {
                string currentDirectorySize =
                    FileSystem.GetDirSize(new DirectoryInfo(s_currentDirectory));
                FileSystem.SuccessWriteLine($"Current directory size: {currentDirectorySize}\n");
                return;
            }

            if (saveToFile)
            {
                DisplaySubDirectories(highlightSearchText, saveToFile, isCreationTime, isLastAccessTime);
                DisplayFiles(highlightSearchText, displaySizes, saveToFile, isCreationTime, isLastAccessTime);
            }
            else
            {
                var isPipe = GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0;
                if (isCreationTime)
                    SetHeader(TypeHeader.CreationTime);
                else if (isLastAccessTime)
                    SetHeader(TypeHeader.LastAccess);
                else if (!isPipe)
                    SetHeader(TypeHeader.LastWrite);

                DisplaySubDirectories(highlightSearchText, saveToFile, isCreationTime, isLastAccessTime);
                DisplayFiles(highlightSearchText, displaySizes, saveToFile, isCreationTime, isLastAccessTime);
            }


            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                GlobalVariables.pipeCmdOutput += $"\n-----------Current Directory Count------------\nTotal directories: {Directory.GetDirectories(s_currentDirectory).Length}\nTotal files: {Directory.GetFiles(s_currentDirectory).Length}\n";
            else
            {
                Console.WriteLine("\n-----------Current Directory Count------------\n");
                Console.WriteLine($"Total directories: {Directory.GetDirectories(s_currentDirectory).Length}");
                Console.WriteLine($"Total files: {Directory.GetFiles(s_currentDirectory).Length}");
            }
        }


        /// <summary>
        ///  Display directories.
        /// </summary>
        /// <param name="highlightSearchText"></param>
        /// <param name="saveToFile"></param>
        /// <param name="isCreationTime"></param>
        /// <param name="isLastAccessTime"></param>
        /// <param name="isLastWriteTime"></param>
        private static void DisplaySubDirectories(string highlightSearchText, bool saveToFile,
            bool isCreationTime = false, bool isLastAccessTime = false)
        {
            try
            {
                foreach (var dir in Directory.GetDirectories(s_currentDirectory))
                {
                    if (GlobalVariables.eventCancelKey)
                        return;
                    var attributes = FileSystem.GetAttributes(dir);

                    var directoryInfo = new DirectoryInfo(dir);
                    if (!GlobalVariables.excludeDirectories.Contains(directoryInfo.Name))
                    {
                        if (highlightSearchText.IsNotNullEmptyOrWhitespace() &&
                        directoryInfo.Name.ContainsText(highlightSearchText))
                        {
                            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                                GlobalVariables.pipeCmdOutput += $"{directoryInfo.Name}\n";
                            else
                                FileSystem.ColorConsoleTextLine(ConsoleColor.Red, $"{attributes}".PadRight(20, ' ') + $"{FileSystem.GetFileDirOwner(directoryInfo.FullName)}".PadRight(20, ' ') + $"{directoryInfo.LastWriteTime.ToLocalTime()}".PadRight(50, ' ') + $"{directoryInfo.Name}");
                        }
                        else
                        {
                            if (saveToFile)
                            {
                                s_listDirs.Add($"{attributes}".PadRight(20, ' ') + $"{FileSystem.GetFileDirOwner(directoryInfo.FullName)}".PadRight(20, ' ') + $"{directoryInfo.LastWriteTime.ToLocalTime()}".PadRight(50, ' ') + $"{directoryInfo.Name}");
                            }
                            else
                            {
                                if (isCreationTime)
                                {
                                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                                        GlobalVariables.pipeCmdOutput += $"{FileSystem.GetCreationDateDirInfo(directoryInfo)}\n";
                                    else
                                        FileSystem.ColorConsoleTextLine(ConsoleColor.DarkCyan, FileSystem.GetCreationDateDirInfo(directoryInfo));
                                }
                                else if (isLastAccessTime)
                                {
                                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                                        GlobalVariables.pipeCmdOutput += $"{FileSystem.GetLastAccessDateDirInfo(directoryInfo)}\n";
                                    else
                                        FileSystem.ColorConsoleTextLine(ConsoleColor.DarkCyan, FileSystem.GetLastAccessDateDirInfo(directoryInfo));
                                }
                                else
                                {
                                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                                        GlobalVariables.pipeCmdOutput += $"{directoryInfo.Name}\n";
                                    else
                                        FileSystem.ColorConsoleTextLine(ConsoleColor.DarkCyan, $"{attributes}".PadRight(20, ' ') + $"{FileSystem.GetFileDirOwner(directoryInfo.FullName)}".PadRight(20, ' ') + $"{directoryInfo.LastWriteTime.ToLocalTime()}".PadRight(50, ' ') + $"{directoryInfo.Name}");
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Displaies files with propierties.
        /// </summary>
        /// <param name="highlightSearchText"></param>
        /// <param name="displaySizes"></param>
        /// <param name="saveToFile"></param>
        /// <param name="isCreationTime"></param>
        /// <param name="isLastAccessTime"></param>
        /// <param name="isLastWriteTime"></param>
        private static void DisplayFiles(string highlightSearchText, bool displaySizes, bool saveToFile,
            bool isCreationTime = false, bool isLastAccessTime = false)
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
                            string formattedText = GetFormattedFileInfoText(file);
                            if (saveToFile)
                            {
                                s_listFiles.Add(formattedText);
                            }
                            else
                            {
                                if (isCreationTime)
                                {
                                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                                        GlobalVariables.pipeCmdOutput += $"{FileSystem.GetCreationDateFileInfo(file)}\n";
                                    else
                                        Console.WriteLine(FileSystem.GetCreationDateFileInfo(file));
                                }
                                else if (isLastAccessTime)
                                {
                                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                                        GlobalVariables.pipeCmdOutput += $"{FileSystem.GetLastAccessDateFileInfo(file)}\n";
                                    else
                                        Console.WriteLine(FileSystem.GetLastAccessDateFileInfo(file));
                                }
                                else
                                {
                                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                                        GlobalVariables.pipeCmdOutput += $"{file.Name}\n";
                                    else
                                        DisplayFileInfoText(formattedText, highlightSearchText);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!GlobalVariables.excludeFiles.Contains(file.Name))
                        {
                            string formattedText = GetFormattedFileInfoText(file);
                            if (saveToFile)
                            {
                                s_listFiles.Add(formattedText);
                            }
                            else
                            {
                                if (isCreationTime)
                                {
                                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                                        GlobalVariables.pipeCmdOutput += $"{FileSystem.GetCreationDateFileInfo(file)}\n";
                                    else
                                        Console.WriteLine(FileSystem.GetCreationDateFileInfo(file));
                                }
                                else if (isLastAccessTime)
                                {
                                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                                        GlobalVariables.pipeCmdOutput += $"{FileSystem.GetLastAccessDateFileInfo(file)}\n";
                                    else
                                        Console.WriteLine(FileSystem.GetLastAccessDateFileInfo(file));
                                }
                                else
                                {
                                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                                        GlobalVariables.pipeCmdOutput += $"{file.Name}\n";
                                    else
                                        DisplayFileInfoText(formattedText, highlightSearchText);
                                }
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
        /// <param name="fileInfo">File info.</param>.PadRight(30, ' ')
        /// <param name="displaySizes">Display size</param>
        /// <returns></returns>
        private static string GetFormattedFileInfoText(FileInfo fileInfo)
        {
            var fileAttribute = FileSystem.GetAttributes(fileInfo.FullName);
            return $"{fileAttribute}".PadRight(20, ' ') + $"{FileSystem.GetFileDirOwner(fileInfo.FullName)}".PadRight(20, ' ') + $"{fileInfo.LastWriteTime.ToLocalTime()}".PadRight(30, ' ') + $"{FileSystem.GetFileSize(fileInfo.DirectoryName + "\\" + fileInfo.Name, false)}".PadRight(20, ' ') + fileInfo.Name;
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
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                    GlobalVariables.pipeCmdOutput += $"{text}\n";
                else
                    FileSystem.SuccessWriteLine(text);
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

        /// <summary>
        /// Create display header at the beggining of ls command.
        /// </summary>
        /// <param name="typeHeader"></param>
        private static void SetHeader(TypeHeader typeHeader, bool isSaveToFile = false)
        {
            var typeHead = "";
            var sperataor = "";
            var spacesSize = "";
            var spacesFile = "";
            switch (typeHeader)
            {
                case TypeHeader.LastWrite:
                    typeHead = "Last Write";
                    sperataor = "----------";
                    spacesSize = "                    ";
                    spacesFile = "                ";
                    break;
                case TypeHeader.LastAccess:
                    typeHead = "Last Access";
                    sperataor = "-----------";
                    spacesSize = "                   ";
                    spacesFile = "                ";
                    break;
                case TypeHeader.CreationTime:
                    typeHead = "Creation Time";
                    sperataor = "-------------";
                    spacesSize = "                 ";
                    spacesFile = "                ";
                    break;

            }
            var header = $@"
Attributes          Owner               {typeHead}{spacesSize}Size{spacesFile}Directory/File Name
----------          -----               {sperataor}{spacesSize}----{spacesFile}-------------------               
";
            if (isSaveToFile)
            {
                s_Header = header + "\n";
            }
            else
                Console.WriteLine(header);
        }

        enum TypeHeader
        {
            LastWrite,
            LastAccess,
            CreationTime
        }
    }
}