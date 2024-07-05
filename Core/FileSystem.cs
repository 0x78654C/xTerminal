using Microsoft.VisualBasic;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Core
{
    [SupportedOSPlatform("windows")]
    public class FileSystem
    {
        private static readonly string[] s_sizes = { "B", "KB", "MB", "GB", "TB" };  // Array with types of store data
        private static readonly Regex s_regexNumber = new Regex("[^0-9.-]+"); //regex that matches disallowed text

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
            try
            {
                FileInfo[] fileInfos = directoryInfo.GetFiles();
                foreach (var fileInfo in fileInfos)
                {
                    size += fileInfo.Length;
                }

                DirectoryInfo[] directoryInfos = directoryInfo.GetDirectories();
                foreach (var dirInfo in directoryInfos)
                {
                    size += DirectorySize(dirInfo);
                }
            }
            catch { }
            return size;
        }

        /// <summary>
        /// Change color of a specific line in console.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="text"></param>
        public static void ColorConsoleTextLine(ConsoleColor color, string text)
        {
            ConsoleColor currentForeground = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = currentForeground;
        }

        /// <summary>
        /// Change color of a specific text in console.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="text"></param>
        public static void ColorConsoleText(ConsoleColor color, object data)
        {
            ConsoleColor currentForeground = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(data);
            Console.ForegroundColor = currentForeground;
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
        public static void ErrorWriteLine(object data)
        {
            ConsoleColor currentForeground = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Error: {data}");
            Console.ForegroundColor = currentForeground;
        }


        /// <summary>
        /// Write warning message in yellow.
        /// </summary>
        /// <param name="data"></param>
        public static void SuccessWriteLine(object data)
        {
            ConsoleColor currentForeground = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Error.WriteLine(data);
            Console.ForegroundColor = currentForeground;
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
                SystemTools.ProcessStart.ProcessExecute("explorer", dirPath, false, false,false);
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
        public static string SanitizePath(string path, string currentDir) => path.Contains(":") && path.Contains(@"\") ? path : $@"{currentDir}{path}";
    

        /// <summary>
        /// Check text if contains numbers only.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool IsNumberAllowed(string text)=> !s_regexNumber.IsMatch(text);

        /// <summary>
        /// Get file creation date time.
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public static string GetCreationDateFileInfo(FileInfo fileInfo) => $"{GetAttributes(fileInfo.FullName)}         " + fileInfo.Name.PadRight(40, ' ') + $"{fileInfo.CreationTime.ToLocalTime()}" + $"Size:  {FileSystem.GetFileSize(fileInfo.DirectoryName + "\\" + fileInfo.Name, false)}";

        /// <summary>
        /// Get file last access time.
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public static string GetLastAccessDateFileInfo(FileInfo fileInfo) => $"{GetAttributes(fileInfo.FullName)}         " + fileInfo.Name.PadRight(40, ' ') + $"{fileInfo.LastAccessTime.ToLocalTime()}" + $"Size:  {FileSystem.GetFileSize(fileInfo.DirectoryName + "\\" + fileInfo.Name, false)}";

        /// <summary>
        /// Get file last write time.
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public static string GetLastWriteDateFileInfo(FileInfo fileInfo) => $"{GetAttributes(fileInfo.FullName)}         " + fileInfo.Name.PadRight(40, ' ') + $"{fileInfo.LastWriteTime.ToLocalTime()}" + $"Size:  {FileSystem.GetFileSize(fileInfo.DirectoryName + "\\" + fileInfo.Name, false)}";

        /// <summary>
        /// Get directory creation date time.
        /// </summary>
        /// <param name="directoryInfo"></param>
        /// <returns></returns>
        public static string GetCreationDateDirInfo(DirectoryInfo directoryInfo) => $"{GetAttributes(directoryInfo.FullName)}         " + directoryInfo.Name.PadRight(40, ' ') + $"{directoryInfo.CreationTime.ToLocalTime()}";

        /// <summary>
        /// Get directory last access time.
        /// </summary>
        /// <param name="directoryInfo"></param>
        /// <returns></returns>
        public static string GetLastAccessDateDirInfo(DirectoryInfo directoryInfo) => $"{GetAttributes(directoryInfo.FullName)}         " + directoryInfo.Name.PadRight(40, ' ') + $"{directoryInfo.LastAccessTime.ToLocalTime()}";

        /// <summary>
        /// Get directorly last write time.
        /// </summary>
        /// <param name="directoryInfo"></param>
        /// <returns></returns>
        public static string GetLastWriteDateDirInfo(DirectoryInfo directoryInfo) => $"{GetAttributes(directoryInfo.FullName)}         " + directoryInfo.Name.PadRight(40, ' ') + $"{directoryInfo.LastWriteTime.ToLocalTime()}";

        /// <summary>
        /// Get MD5 and size of a specific file.
        /// - Method used in fcopy and fmove command  -
        /// </summary>
        /// <param name="sourceMD5"></param>
        /// <param name="sizeSource"></param>
        /// <param name="destinationMD5"></param>
        /// <param name="sizeDestination"></param>
        /// <param name="filePath"></param>
        /// <param name="source"></param>
        public static void GetMD5File(ref string sourceMD5, ref double sizeSource, ref string destinationMD5, ref double sizeDestination, string filePath, bool source)
        {
            if (source)
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        var hash = md5.ComputeHash(stream);
                        sourceMD5 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        FileSystem.ColorConsoleText(ConsoleColor.Blue, "Source File: ");
                        Console.Write(filePath);
                        FileSystem.ColorConsoleText(ConsoleColor.Blue, " MD5: ");
                        Console.Write(sourceMD5);
                        FileSystem.ColorConsoleText(ConsoleColor.Blue, " Size: ");
                        Console.WriteLine(FileSystem.GetFileSize(filePath, false));
                        sizeSource += Double.Parse(FileSystem.GetFileSize(filePath, true));
                    }
                }
            }
            else
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        var hash = md5.ComputeHash(stream);
                        destinationMD5 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        FileSystem.ColorConsoleText(ConsoleColor.Blue, "Destination File: ");
                        Console.Write(filePath);
                        FileSystem.ColorConsoleText(ConsoleColor.Blue, " MD5: ");
                        Console.Write(destinationMD5);
                        FileSystem.ColorConsoleText(ConsoleColor.Blue, " Size: ");
                        Console.WriteLine(FileSystem.GetFileSize(filePath, false));
                        sizeDestination += Double.Parse(FileSystem.GetFileSize(filePath, true));
                    }
                }
            }
        }
        
        /// <summary>
        /// Convert byte[] to uLong.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static ulong BytesToUInt64(byte[] bytes)
        {
            if (bytes == null)
                return 0;
            if (bytes.Length > 8)
                return 0;

            ulong result = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                result |= (ulong)bytes[i] << (i * 8);
            }
            return result;
        }

        /// <summary>
        /// Create UIint32 array Big-Endian by chunks.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string UInt32BigEndianConversionSlice(byte[] bytes, int slice)
        {
            string outData = string.Empty;
            int count = 0;
            byte[] val = new byte[slice];
            int c=0;
            int ite = 0;
            foreach (var b in bytes)
            {
                count++;
                if (count == slice)
                {
                    var arr = ArrayFromRange(bytes, c, slice);
                    Array.Reverse(arr);
                    ite++;
                    if (ite == 2)
                    {
                        outData += BinaryPrimitives.ReadUInt32BigEndian(arr).ToString();
                        outData += ", ";
                        ite = 0;
                    }
                    else
                        outData += BinaryPrimitives.ReadUInt32BigEndian(arr).ToString()+"/";
                    Array.Clear(val, 0, val.Length);
                    c = c+count;
                    count = 0;
                }
            }
            return outData;
        }

        /// <summary>
        /// Byte[] to usort conversion by chunks.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="slice"></param>
        /// <returns></returns>
        public static string UShortConversionSlice(byte[] bytes, int slice)
        {
            string outData = string.Empty;
            int count = 0;
            byte[] val = new byte[slice];
            int c = 0;
            foreach (var b in bytes)
            {
                count++;
                if (count == slice)
                {
                    var arr = ArrayFromRange(bytes, c, slice);
                    outData += BitConverter.ToUInt16(arr).ToString() + ", ";
                    Array.Clear(val, 0, val.Length);
                    c = c + count;
                    count = 0;
                }
            }
            return outData;
        }

        /// <summary>
        /// Create new array from original array by len and start index.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="originalArray"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private static T[] ArrayFromRange<T>(T[] originalArray, int startIndex, int length)
        {
            int actualLength = Math.Min(length, originalArray.Length - startIndex);
            T[] copy = new T[actualLength];
            Array.Copy(originalArray, startIndex, copy, 0, actualLength);
            return copy;
        }

        /// <summary>
        /// Parse the attributes and create output pattern.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetAttributes(string path)
        {
            var attributes = File.GetAttributes(path);
            var splitAttr = attributes.ToString().Split(',');
            var listParsed = new List<string>();
            foreach(var attr in splitAttr)
            {
                var parse = attr.Trim().Substring(0,1).ToLower();
                listParsed.Add(parse);
            }
            var calc = 9 - splitAttr.Length;
            for(int i =0; i <= calc; i++)
                listParsed.Add("-");
            var finalOutput = string.Join("",listParsed);
            return finalOutput;
        }
    }
}
