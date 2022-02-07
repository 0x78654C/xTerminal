using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Core.Commands
{
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
        public static string FileOutput(string input, string currentDir, string searchString = null, string savedFile = null)
        {
            var output = new StringBuilder();
            input = FileSystem.SanitizePath(input, currentDir);
            int lineCount = 0;

            if (!File.Exists(input))
            {
                Console.WriteLine("File " + input + " dose not exist!");
                return output.ToString();
            }

            using (var streamReader = new StreamReader(input))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    lineCount++;
                    if (string.IsNullOrWhiteSpace(searchString))
                    {
                        output.AppendLine(line);
                        continue;
                    }

                    if (line.ToLower().Contains(searchString.ToLower()))
                    {
                        output.AppendLine($"Line {lineCount} : {line}");
                    }
                }

                streamReader.Close();
            }

            if (!string.IsNullOrEmpty(savedFile))
            {
                return FileSystem.SaveFileOutput(savedFile, currentDir, output.ToString());
            }

            return output.ToString();
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
                    string oD = MultiFileOutput(searchString, dir.FullName, p.Split(' '), savedFile, true, fileName);
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


            if (!string.IsNullOrEmpty(savedFile) && searchAll == false)
            {
                return FileSystem.SaveFileOutput(savedFile, currentDir, output.ToString());
            }

            return output.ToString();
        }

        private static string LineOutput(string file, string currentDir, string searchString)
        {
            string oFile = FileOutput(file, currentDir, searchString);
            if (oFile.StartsWith("Line"))
            {
               return $"---------------- {file} ----------------"
                    +Environment.NewLine
                    +FileOutput(file, currentDir, searchString);
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
    }
}