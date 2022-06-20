using System;
using System.IO;
using System.Security.AccessControl;
using System.Text;

namespace Core
{
    public static class FileSystem
    {
        private static readonly string[] s_sizes = { "B", "KB", "MB", "GB", "TB" };  // Array with types of store data

        /// <summary>
        /// Gets the size of a file in megabytes.
        /// </summary>
        /// <param name="fileName">Path to the file.</param>
        /// <returns>Size of a file in megabytes.</returns>
        public static double GetFileSize(string fileName)
        {
            return new FileInfo(fileName).Length / (double)(1024 * 1024);
        }

        /// <summary>
        /// Gets the size of a file, formatted as a string with a size unit postfix.
        /// </summary>
        /// <param name="fileName">Path to the file.</param>
        /// <returns>File size formatted with a size unit.</returns>
        public static string GetFileSizeString(string fileName)
        {
            double length = new FileInfo(fileName).Length;
            return GetFileSizeString(length);
        }

        /// <summary>
        /// Gets the size of a file, formatted as a string with a size unit postfix.
        /// </summary>
        /// <param name="fileSize">Size of the file in bytes.</param>
        /// <returns>File size formatted with a size unit.</returns>
        public static string GetFileSizeString(double fileSize)
        {
            int order = Math.Min(s_sizes.Length - 1, (int)Math.Log(fileSize, 1024));
            fileSize /= Math.Pow(1024, order);
            return $"{fileSize:0.##} {s_sizes[order]}";
        }

        /// <summary>
        /// Gets size of a directory.
        /// </summary>
        /// <param name="info">Directory info of a specificic path directory.</param>
        /// <returns>string</returns>
        public static string GetDirectorySizeString(DirectoryInfo info)
        {
            return GetFileSizeString(GetDirectorySize(info));
        }

        /// <summary>
        /// Grabs size in bytes from every file in a directory and sub directory.
        /// </summary>
        /// <param name="directoryInfo"></param>
        /// <returns></returns>
        private static double GetDirectorySize(DirectoryInfo directoryInfo)
        {
            double size = 0;

            foreach (var fileInfo in directoryInfo.EnumerateFiles())
            {
                size += fileInfo.Length;
            }

            foreach (var dirInfo in directoryInfo.EnumerateDirectories())
            {
                if (HasFilePermissions(dirInfo.FullName, displayMessage: true))
                {
                    size += GetDirectorySize(dirInfo);
                }
            }

            return size;
        }

        /// <summary>
        /// Prints out a line of text to the console, using specified color.
        /// </summary>
        /// <param name="color">Color to be used.</param>
        /// <param name="text">Text to be printed.</param>
        public static void ColorConsoleTextLine(ConsoleColor color, string text)
        {
            ConsoleColor currentForeground = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = currentForeground;
        }

        /// <summary>
        /// Prints out text to the console, using specified color.
        /// </summary>
        /// <param name="color">Color to be used.</param>
        /// <param name="text">Text to be printed.</param>
        public static void ColorConsoleText(ConsoleColor color, string text)
        {
            ConsoleColor currentForeground = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = currentForeground;
        }

        /// <summary>
        /// Checks write permissions to a directory or a file.
        /// </summary>
        /// <param name="path">Path to the directory/file you want to check permissions for.</param>
        /// <param name="displayMessage">Whether a message should be displayed when permissions are insufficient.</param>
        /// <returns>True if permissions are sufficient, otherwise false.</returns>
        public static bool HasFilePermissions(string path, bool displayMessage)
        {
            FileAttributes fileAttributes = File.GetAttributes(path);
            FileSystemSecurity securityInfo = ((fileAttributes & FileAttributes.Directory) == 0) ?
                (FileSystemSecurity)
                File.GetAccessControl(path) :
                Directory.GetAccessControl(path);
            try
            {
                if (securityInfo.AreAccessRulesProtected)
                {
                    if (displayMessage)
                        Console.WriteLine($"Access to the path: {path} is denied!");
                    return false;
                }
                return true;
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// Write error output in color Red.
        /// </summary>
        /// <param name="text"></param>
        public static void ErrorWriteLine(string text)
        {
            ConsoleColor currentForeground = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Error: {text}");
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
        /// <returns>Message about the result of this operation.</returns>
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
            return path.Contains(":") && path.Contains(@"\") ? path : $@"{currentDir}{path}";
        }

        /// <summary>
        /// Gets creation time for a file/directory, formatted as a string.
        /// </summary>
        /// <param name="fileSystemInfo">Information about the file/directory.</param>
        /// <returns>Creation time of a file/directory, formatted as a string.</returns>
        public static string GetCreationDateString(FileSystemInfo fileSystemInfo)
        {
            return $"{fileSystemInfo.Name,-40}{fileSystemInfo.CreationTime.ToLocalTime()}";
        }
    }
}
