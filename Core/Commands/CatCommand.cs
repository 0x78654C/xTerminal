using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

namespace Core.Commands
{
    [SupportedOSPlatform("Windows")]
    public static class CatCommand
    {

        private static int s_linesCount = 0;
        private static int s_linesCountName = 0;

        /// <summary>
        /// Output to console specific lines from a file containing a specific string.
        /// </summary>
        /// <param name="searchString"> Search parameter. </param>
        /// <param name="currentDir">Current directory. </param>
        /// <param name="input"> File name to search in. </param>
        /// <param name="savedFile"> File name where to store the result data. </param>
        /// <returns>string</returns>
        public static string FileOutput(string input, string currentDir, string searchString = null, string savedFile = null, SearchType searchType = SearchType.contains)
        {
            var output = new StringBuilder();
            input = FileSystem.SanitizePath(input, currentDir);
            int lineCount = 0;

            if (!File.Exists(input))
            {
                if (!GlobalVariables.isPipeVar)
                    Console.WriteLine("File " + input + " dose not exist!");
                GlobalVariables.isPipeVar = false;
                return output.ToString();
            }

            using (var streamReader = new StreamReader(input))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (GlobalVariables.eventCancelKey)
                        break;

                    lineCount++;
                    if (string.IsNullOrWhiteSpace(searchString))
                    {
                        output.AppendLine(line);
                        continue;
                    }

                    switch(searchType)
                    {
                        case SearchType.startsWith:
                            if (line.StartsWith(searchString, StringComparison.OrdinalIgnoreCase))
                                output.AppendLine($"Line {lineCount} : {line}");
                            break;
                        case SearchType.endsWith:
                            if (line.EndsWith(searchString, StringComparison.OrdinalIgnoreCase))
                                output.AppendLine($"Line {lineCount} : {line}");
                            break;
                        case SearchType.equals:
                            if (line.Equals(searchString, StringComparison.OrdinalIgnoreCase))
                                output.AppendLine($"Line {lineCount} : {line}");
                            break;
                        default:
                            if (line.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                                output.AppendLine($"Line {lineCount} : {line}");
                            break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(savedFile))
            {
                savedFile = FileSystem.SanitizePath(savedFile, currentDir);
                return FileSystem.SaveFileOutput(savedFile, currentDir, output.ToString());
            }

            return output.ToString();
        }

        /// <summary>
        /// Output to console specific lines from a text data containing a specific string.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="searchString"></param>
        /// <param name="currentDir"></param>
        /// <param name="savedFile"></param>
        /// <returns></returns>
        public static string StringSearchOutput(string input, string currentDir, string searchString, string savedFile = null, SearchType searchType = SearchType.contains)
        {
            var output = new StringBuilder();
            int lineCount = 0;

            if (string.IsNullOrEmpty(input))
            {
                FileSystem.ErrorWriteLine("There is no data provided to search in it!");
                return output.ToString();
            }

            using (var streamReader = new StringReader(input))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (GlobalVariables.eventCancelKey)
                        break;

                    lineCount++;
                    if (string.IsNullOrWhiteSpace(searchString))
                    {
                        output.AppendLine(line);
                        continue;
                    }
                    switch (searchType)
                    {
                        case SearchType.startsWith:
                            if (line.StartsWith(searchString, StringComparison.OrdinalIgnoreCase))
                                output.AppendLine(line);
                            break;
                        case SearchType.endsWith:
                            if (line.EndsWith(searchString, StringComparison.OrdinalIgnoreCase))
                                output.AppendLine(line);
                            break;
                        case SearchType.equals:
                            if (line.Equals(searchString, StringComparison.OrdinalIgnoreCase))
                                output.AppendLine(line);
                            break;
                        default:
                            if (line.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                                output.AppendLine(line);
                            break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(savedFile))
            {
                savedFile = FileSystem.SanitizePath(savedFile, currentDir);
                return FileSystem.SaveFileOutput(savedFile, currentDir, output.ToString());
            }

            return output.ToString();
        }

        /// <summary>
        /// Search type for file output.
        /// </summary>
        public enum SearchType
        {
            startsWith,
            contains,
            equals,
            endsWith
        }


        /// <summary>
        /// Output to console lines that contains a specific string.
        /// </summary>
        /// <param name="searchString"> Search parameter. </param>
        /// <param name="currentDir">Current directory. </param>
        /// <param name="paths"> File names to search in. </param>
        /// <param name="savedFile"> File name where to store the result data. </param>
        /// <param name="searchAll"> Search in all files from current directory. </param>
        /// <returns>string</returns>
        public static string MultiFileOutput(string searchString, string currentDir, IEnumerable<string> paths, string savedFile, bool searchAll, string fileName = null)
        {
            try
            {
                StringBuilder output = new StringBuilder();
                if (searchAll)
                {
                    string[] files = Directory.GetFiles(currentDir);

                    if (!string.IsNullOrEmpty(fileName))
                    {
                        foreach (var file in files)
                        {
                            var fileInfo = new FileInfo(file);

                            if (!string.IsNullOrEmpty(fileName) && fileInfo.Name.Contains(fileName))
                            {
                                if (!File.Exists(file))
                                {
                                    FileSystem.ErrorWriteLine("File " + file + " dose not exist!");
                                    continue;
                                }
                                output.AppendLine(LineOutput(file, currentDir, searchString));
                            }
                        }
                    }
                    else
                    {
                        foreach (var file in files)
                        {
                            if (!File.Exists(file))
                            {
                                FileSystem.ErrorWriteLine("File " + file + " dose not exist!");
                                continue;
                            }
                            string o = LineOutput(file, currentDir, searchString);
                            output.AppendLine(o);
                        }
                    }
                    var directoryInfo = new DirectoryInfo(currentDir).GetDirectories();
                    foreach (var dir in directoryInfo)
                    {
                        string p = "";
                        string oD = "";

                        if (!GlobalVariables.eventCancelKey)
                            oD = MultiFileOutput(searchString, dir.FullName, p.Split(' '), savedFile, true, fileName);

                        if (!string.IsNullOrWhiteSpace(oD))
                        {
                            output.AppendLine(oD);
                        }
                    }
                }
                else
                {
                    foreach (var file in paths)
                    {
                        var nFile = FileSystem.SanitizePath(file, currentDir);
                        if (!File.Exists(nFile))
                        {
                            FileSystem.ErrorWriteLine("File " + nFile + " dose not exist!");
                            continue;
                        }
                        output.AppendLine(LineOutput(nFile, currentDir, searchString));
                    }
                }
                var outData = Regex.Replace(output.ToString(), @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);

                if (!string.IsNullOrEmpty(savedFile) && searchAll == false)
                {
                    return FileSystem.SaveFileOutput(savedFile, currentDir, outData.ToString());
                }

                return outData.ToString();
            }
            catch (UnauthorizedAccessException) { return ""; }
        }

        private static string LineOutput(string file, string currentDir, string searchString)
        {
            string oFile = FileOutput(file, currentDir, searchString);
            if (oFile.StartsWith("Line"))
            {
                return $"---------------- {file} ----------------"
                     + Environment.NewLine
                     + FileOutput(file, currentDir, searchString);
            }
            return null;
        }

        private static void TotalLinesCounter(string currentDir, string fileName, bool fileCount)
        {
            var files = Directory.GetFiles(currentDir);

            foreach (var file in files)
            {
                if (fileCount)
                {
                    if (file.Contains(fileName))
                    {
                        var lines = File.ReadLines(file);
                        foreach (var line in lines)
                        {
                            if (!string.IsNullOrEmpty(line))
                            {
                                s_linesCountName++;
                            }
                        }
                    }
                }
                else
                {
                    var lines = File.ReadLines(file);
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            s_linesCount++;
                        }
                    }
                }
            }

            var directories = new DirectoryInfo(currentDir).GetDirectories();
            foreach (var dir in directories)
            {
                if (!GlobalVariables.eventCancelKey)
                    TotalLinesCounter(dir.FullName, fileName, fileCount);
            }
        }

        /// <summary>
        /// Outputs the lines count from all files in a directory and subdirectories.
        /// </summary>
        /// <param name="currentDir">Directory localtion.</param>
        /// <returns></returns>
        public static int LineCounts(string currentDir)
        {
            TotalLinesCounter(currentDir, "", false);
            return s_linesCount;
        }

        /// <summary>
        /// Ouputs the lines count all files that contains a specific text, in a directory and subdirectories.
        /// </summary>
        /// <param name="currentDir">Directory location.</param>
        /// <param name="fileName">Custom text included in filename.</param>
        /// <returns></returns>
        public static int LineCountsName(string currentDir, string fileName = null)
        {
            TotalLinesCounter(currentDir, fileName, true);
            return s_linesCountName;
        }

        /// <summary>
        /// Clears all the lines count.
        /// </summary>
        public static void ClearCounter()
        {
            s_linesCount = 0;
            s_linesCountName = 0;
        }

        /// <summary>
        /// Check if file is emtpy.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static bool CheckFileContent(string fileName)
        {
            if (!File.Exists(fileName))
            {
                FileSystem.ErrorWriteLine($"File does not exist: {fileName}");
                return false;
            }
            using (StringReader stringReader = new StringReader(fileName))
            {
                string historFileData = stringReader.ReadToEnd();
                if (historFileData.Length > 0)
                {
                    return true;
                }
                FileSystem.ErrorWriteLine("{fileName} is empty!");
                return false;
            }
        }

        /// <summary>
        /// Display first N line from a file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="linesCount"></param>
        public static void OutputFirtsLastLines(string fileName, int linesCount, bool isLast = false)
        {
            if (CheckFileContent(fileName))
            {
                try
                {
                    IEnumerable<string> lines = null;
                    var linesC = File.ReadLines(fileName).Count();
                    if (isLast)
                        lines = File.ReadLines(fileName).Skip(linesC - linesCount).Take(linesCount);
                    else
                        lines = File.ReadLines(fileName).Take(linesCount);
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            if (GlobalVariables.eventCancelKey)
                                break;
                            Console.WriteLine(line);
                        }
                    }
                }
                catch (Exception e)
                {
                    FileSystem.ErrorWriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// Output first N lines from string.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="linesCount"></param>
        public static void OutputFirstLinesFromString(string data, int linesCount)
        {
            if (string.IsNullOrEmpty(data))
                return;
            var reader = new StringReader(data);
            string line;
            int count = 0;
            GlobalVariables.pipeCmdOutput = string.Empty;
            while ((line = reader.ReadLine()) != null)
            {
                if (count == linesCount) break;
                if (GlobalVariables.pipeCmdCount > 0)
                    GlobalVariables.pipeCmdOutput += $"{line}\n";
                else
                    Console.WriteLine(line);
                count++;
            }
        }

        /// <summary>
        /// Display last N lines from string.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="linesCount"></param>
        public static void OutputLastLinesFromString(string data, int linesCount)
        {
            if (string.IsNullOrEmpty(data))
                return;
            var reader = new StringReader(data);
            string line;
            var count = 0;
            GlobalVariables.pipeCmdOutput = string.Empty;
            var lastLinesCount = data.CountLines() - linesCount;
            while ((line = reader.ReadLine()) != null)
            {
                if (lastLinesCount <= count)
                {
                    if (GlobalVariables.pipeCmdCount > 0)
                        GlobalVariables.pipeCmdOutput += $"{line}\n";
                    else
                        Console.WriteLine(line);
                }
                count++;
            }
        }

        /// <summary>
        /// Display content between two lines range.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="range"></param>
        public static void OutputLinesRange(string fileName, string range)
        {
            //if (!CheckFileContent(fileName))
            //    return;

            string firstLine = range.Split('-')[0];
            string secondLine = range.Split('-')[1];
            if (!FileSystem.IsNumberAllowed(firstLine) && !FileSystem.IsNumberAllowed(secondLine))
            {
                FileSystem.ErrorWriteLine("Parameter invalid: You need to provide the range of lines for data display! Example: 10-20");
                return;
            }
            int first = Int32.Parse(firstLine);
            int second = Int32.Parse(secondLine);
            GlobalVariables.pipeCmdOutput = string.Empty;
            if (GlobalVariables.isPipeCommand)
            {
                using (var streamReader = new StringReader(fileName))
                {
                    for (int i = 0; i < second; ++i)
                    {
                        var line = streamReader.ReadLine();

                        if (i < first - 1)
                            continue;
                        if (GlobalVariables.eventCancelKey)
                            break;
                        if (GlobalVariables.pipeCmdCount > 0)
                            GlobalVariables.pipeCmdOutput += $"{line}\n";
                        else
                            Console.WriteLine(line);
                    }
                }
            }
            else
            {
                using (var streamReader = new StreamReader(fileName))
                {
                    for (int i = 0; i < second && !streamReader.EndOfStream; ++i)
                    {
                        var line = streamReader.ReadLine();

                        if (i < first - 1)
                            continue;
                        if (GlobalVariables.eventCancelKey)
                            break;
                        Console.WriteLine(line);
                    }
                }
            }
        }

        /// <summary>
        /// Store readed data from a file to another file file line by line.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="outputFile"></param>
        private static void ReadStoreData(string file, string outputFile)
        {
            string line;
            using (StreamReader reader = new StreamReader(file))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    File.AppendAllText(outputFile, line + Environment.NewLine);
                }
            }
        }

        /// <summary>
        /// Concatenate files to one single file.
        /// </summary>
        /// <param name="argfiles"></param>
        /// <param name="outputFIle"></param>
        /// <param name="currentDirectory"></param>
        public static void ConcatenateFiles(string argfiles, string outputFIle, string currentDirectory)
        {
            string sFile;
            if (!argfiles.Contains(";"))
            {
                Console.WriteLine($"You need more than 1 file to concatenate and needs to be separated with ; character!");
                return;
            }

            string[] files = argfiles.Split(';');
            foreach (var file in files)
            {
                if (GlobalVariables.eventCancelKey)
                    return;
                sFile = FileSystem.SanitizePath(file, currentDirectory);
                if (!File.Exists(sFile))
                    Console.WriteLine($"File '{sFile}' does not exist!");
                else
                    ReadStoreData(sFile, outputFIle);
            }
        }
    }
}