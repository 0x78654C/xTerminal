﻿using System;
using System.IO;
using System.Text;

namespace Core
{
    public class FileSystem
    {
        private static readonly string[] s_sizes = { "B", "KB", "MB", "GB", "TB" };  // Array with types of store data


        /// <summary>
        /// Get the size of a file.
        /// </summary>
        /// <param name="fileName"> Specify the file path.</param>
        /// <param name="fixedSize">Type of check</param>
        /// <returns>string</returns>
        public static string GetFileSize(string fileName, bool fixedSize)
        {
            double len = new FileInfo(fileName).Length;
            if (fixedSize)
            {
                string sLen = String.Format("{0:0.##}", len);
                double fLen = Convert.ToDouble(sLen);
                for (int i = 0; i < 2; i++)
                {
                    fLen /= 1024;
                }
                return fLen.ToString();
            }
            else
            {
                int order = 0;
                while (len >= 1024 && order < s_sizes.Length - 1)
                {
                    order++;
                    len /= 1024;
                }
                return String.Format("{0:0.##} {1}", len, s_sizes[order]);
            }
        }

        /// <summary>
        /// Convert bytes as string to size information,
        /// </summary>
        /// <param name="fileSize">Bytes in string format</param>
        /// <param name="fixedSize">Type of check.</param>
        /// <returns></returns>
        public static string GetSize(string fileSize, bool fixedSize)
        {
            double len = Convert.ToDouble(fileSize);
            if (fixedSize)
            {
                string sLen = String.Format("{0:0.##}", len);
                double fLen = Convert.ToDouble(sLen);
                for (int i = 0; i < 2; i++)
                {
                    fLen /= 1024;
                }
                return fLen.ToString();
            }
            else
            {
                int order = 0;
                while (len >= 1024 && order < s_sizes.Length - 1)
                {
                    order++;
                    len /= 1024;
                }
                return String.Format("{0:0.##} {1}", len, s_sizes[order]);
            }
        }

        /// <summary>
        /// Return size of a directory.
        /// </summary>
        /// <param name="info">Directory info of a specificic path directory.</param>
        /// <returns>string</returns>
        public static string GetDirSize(DirectoryInfo info)
        {
            int order = 0;
            double length = DirectorySize(info);
            while (length >= 1024 && order < s_sizes.Length - 1)
            {
                order++;
                length /= 1024;
            }
            return String.Format("{0:0.##} {1}", length, s_sizes[order]);
        }

        /// <summary>
        /// Grab size in bytes from every file in a directory and sub directory.
        /// </summary>
        /// <param name="directoryInfo"></param>
        /// <returns></returns>
        private static double DirectorySize(DirectoryInfo directoryInfo)
        {
            double size = 0;

            FileInfo[] fileInfos = directoryInfo.GetFiles();
            foreach (var fileInfo in fileInfos)
            {
                size += fileInfo.Length;
            }

            DirectoryInfo[] directoryInfos = directoryInfo.GetDirectories();
            foreach (var dirInfo in directoryInfos)
            {
                if (CheckPermission(dirInfo.FullName,true,CheckType.Directory) == true)
                {
                    size += DirectorySize(dirInfo);
                }
            }
            return size;
        }

        /// <summary>
        /// Change color of a specific line in console.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="text"></param>
        public static void ColorConsoleTextLine(ConsoleColor color, string text)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Change color of a specific text in console.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="text"></param>
        public static void ColorConsoleText(ConsoleColor color, string text)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Check write permission to a directory or file.
        /// </summary>
        /// <param name="path">Path to the directory or fie you want to check.</param>
        /// <param name="checkType">File or Directory</param>
        /// <returns>bool</returns>
        public static bool CheckPermission(string path, bool displayMessage, CheckType checkType)
        {
            switch (checkType)
            {
                case CheckType.Directory:
                    try
                    {
                        var dirInfo = new DirectoryInfo(path).GetAccessControl();
                        if (dirInfo.AreAccessRulesProtected)
                        {
                            if (displayMessage)
                                Console.WriteLine($"Access to the path: {path} is denied!");
                            return false;
                        }
                        return true;
                    }
                    catch
                    {
                        return false;
                    }

                case CheckType.File:
                    var fileInfo = new FileInfo(path).GetAccessControl();
                    if (fileInfo.AreAccessRulesProtected)
                    {
                        if (displayMessage)
                            Console.WriteLine($"Access to the file: {path} is denied!");
                        return false;
                    }
                    return true;

                default:
                    return false;
            }
        }

        // Check permission types
        public enum CheckType
        {
            Directory,
            File
        }

        /// <summary>
        /// Write error output in color Red.
        /// </summary>
        /// <param name="text"></param>
        public static void ErrorWriteLine(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {text}");
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Opens a directory in Windows Explorer.
        /// </summary>
        /// <param name="dirPath"></param>
        public static void OpenCurrentDiretory(string dirPath, string currentDirectory)
        {
            dirPath = SanitizePath(dirPath, currentDirectory);
            if (Directory.Exists(dirPath))
            {
                SystemTools.ProcessStart.ProcessExecute("explorer", dirPath, false, false);
                return;
            }
            Console.WriteLine($"Directory '{dirPath}' does not exist!");
        }

        /// <summary>
        /// Save file to file with sanitize path.
        /// </summary>
        /// <param name="path"> Filename with path where to save.</param>
        /// <param name="currentDir">Terminal current directory.</param>
        /// <param name="contents">Data to be saved.</param>
        /// <param name="unicode">Unicode format for hex dump file./param>
        /// <returns>string</returns>
        public static string SaveFileOutput(string path, string currentDir, string contents, bool unicode = false)
        {
            path = SanitizePath(path, currentDir);
            if (!unicode)
            {
                File.WriteAllText(path, contents);
            }
            else
            {
                File.WriteAllText(path, contents, Encoding.Unicode);
            }
            return $"Data saved in {path}";
        }

        /// <summary>
        /// Sanitize path if includes current directory from terminal. 
        /// </summary>
        /// <param name="path">File path.</param>
        /// <param name="currentDir">Terminal current direcotory.</param>
        /// <returns>string</returns>
        public static string SanitizePath(string path, string currentDir)
        {
            return path.Contains(":") && path.Contains(@"\") ? path : $@"{currentDir}\{path}";
        }
    }
}
