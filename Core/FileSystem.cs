﻿using System;
using System.Collections;
using System.IO;

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
        public static bool CheckPermission(string path, CheckType checkType)
        {
            switch (checkType)
            {
                case CheckType.Directory:
                    var dirInfo = new DirectoryInfo(path).GetAccessControl();
                    if (dirInfo.AreAccessRulesProtected)
                    {
                        Console.WriteLine($"Access to the path: {path} is denied!");
                        return false;
                    }
                    return true;
               
                case CheckType.File:
                    var fileInfo = new FileInfo(path).GetAccessControl();
                    if (fileInfo.AreAccessRulesProtected)
                    {
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

    }
}
