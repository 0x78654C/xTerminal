using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Versioning;
using System.Windows.Forms;
using static Core.GlobalVariables;

namespace Core.SystemTools
{
    [SupportedOSPlatform("Windows")]
    public class AutoSuggestion
    {
        /// <summary>
        /// Convert keydata to normal value.
        /// </summary>
        /// <param name="keycode"></param>
        /// <param name="startWith"></param>
        /// <param name="len"></param>
        /// <param name="replace"></param>
        /// <param name="act"></param>
        /// <returns></returns>
        public static string KeyConvertor(string keycode, string startWith, int len, string replace, Action act = null) { if (keycode.StartsWith(startWith) && keycode.Length == len) { act?.Invoke(); return keycode.Replace(startWith, replace); } return string.Empty; }


        /// <summary>
        /// Directories sugestion by first letters.
        /// </summary>
        /// <param name="startChar">Start letters.</param>
        /// <param name="currentDirectory">Current directory path.</param>
        public static void FileDirCompletion(string startChar, string currentDirectory , GlobalVariables.TypeSuggestions typeSuggestions, ref string addedCompletion)
        {
            switch(typeSuggestions)
            {
                case GlobalVariables.TypeSuggestions.File:
                    FileCompletion(startChar, currentDirectory, ref addedCompletion);
                    break;
                case GlobalVariables.TypeSuggestions.Directory:
                    DirCompletion(startChar, currentDirectory, ref addedCompletion);
                    break;
                case GlobalVariables.TypeSuggestions.All:
                    AllCompletion(startChar, currentDirectory, ref addedCompletion);
                    break;

            }
        }

        private static void AllCompletion(string startChar, string currentDirectory, ref string addedCompletion)
        {
            int tabs = 5;
            addedCompletion = "";
            var directories = Directory.GetDirectories(currentDirectory);
            var dirStart = directories.Where(d => new DirectoryInfo(d).Name.StartsWith(startChar, StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (dirStart.Count == 1)
            {
                addedCompletion = new DirectoryInfo(dirStart[0]).Name;
                return;
            }

            var files = Directory.GetFiles(currentDirectory);
            var fileStart = files.Where(d => new DirectoryInfo(d).Name.StartsWith(startChar, StringComparison.InvariantCultureIgnoreCase)).ToList();
            if (fileStart.Count == 1)
            {
                addedCompletion = new DirectoryInfo(fileStart[0]).Name;
                return;
            }

            Console.WriteLine();
            foreach (var dir in directories)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(dir);
                if (dirInfo.Name.StartsWith(startChar, StringComparison.InvariantCultureIgnoreCase))
                {
                    tabs--;
                    if (tabs != 0)
                    {
                        FileSystem.ColorConsoleText(ConsoleColor.Green, $"{dirInfo.Name}\\".PadRight(PadLenghtCheck(dirInfo.Name.Length), ' '));
                    }
                    else
                    {
                        FileSystem.ColorConsoleTextLine(ConsoleColor.Green, $"{dirInfo.Name}\\".PadRight(PadLenghtCheck(dirInfo.Name.Length), ' '));
                        tabs = 5;
                    }
                }
            }
            foreach (var file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.Name.StartsWith(startChar, StringComparison.InvariantCultureIgnoreCase))
                {
                    tabs--;
                    if (tabs != 0)
                    {
                        Console.Write($"{fileInfo.Name}".PadRight(PadLenghtCheck(fileInfo.Name.Length), ' '));
                    }
                    else
                    {
                        Console.WriteLine($"{fileInfo.Name}".PadRight(PadLenghtCheck(fileInfo.Name.Length), ' '));
                        tabs = 5;
                    }
                }
            }
            SendKeys.SendWait("{ENTER}");
        }


        private static void DirCompletion(string startChar, string currentDirectory, ref string addedCompletion)
        {
            if (startChar.Contains("\\"))
            {
                var splitStart = startChar.Split('\\').ToList();
                var endChars = splitStart[splitStart.Count - 1];
                splitStart.RemoveAt(splitStart.Count - 1);
                var continuePath  = string.Join("\\", splitStart);
                currentDirectory = currentDirectory+continuePath+"\\";
            }
            
            var directories = Directory.GetDirectories(currentDirectory);
            int tabs = 5;
            addedCompletion = "";
            
            var dirStart = directories.Where(d => new DirectoryInfo(d).Name.StartsWith(startChar, StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (dirStart.Count == 1)
            {
                addedCompletion = new DirectoryInfo(dirStart[0]).Name;
                return;
            }

            Console.WriteLine();
            foreach (var dir in directories)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(dir);
                if (dirInfo.Name.StartsWith(startChar, StringComparison.InvariantCultureIgnoreCase))
                {
                    tabs--;
                    if (tabs != 0)
                    {
                        FileSystem.ColorConsoleText(ConsoleColor.Green, $"{dirInfo.Name}\\".PadRight(PadLenghtCheck(dirInfo.Name.Length), ' '));
                    }
                    else
                    {
                        FileSystem.ColorConsoleTextLine(ConsoleColor.Green, $"{dirInfo.Name}\\".PadRight(PadLenghtCheck(dirInfo.Name.Length), ' '));
                        tabs = 5;
                    }
                }
            }
            SendKeys.SendWait("{ENTER}");
        }

        /// <summary>
        /// Files sugestion by first letters.
        /// </summary>
        /// <param name="startChar">Start letters.</param>
        /// <param name="currentDirectory">Current directory path.</param>
        private static void FileCompletion(string startChar, string currentDirectory, ref string addedCompletion)
        {
            var files = Directory.GetFiles(currentDirectory);
            int tabs = 5;
            var fileStart = files.Where(d => new DirectoryInfo(d).Name.StartsWith(startChar, StringComparison.InvariantCultureIgnoreCase)).ToList();
            addedCompletion = "";
            if (fileStart.Count == 1)
            {
                addedCompletion = new DirectoryInfo(fileStart[0]).Name;
                return;
            }
            Console.WriteLine();
            foreach (var file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.Name.StartsWith(startChar, StringComparison.InvariantCultureIgnoreCase))
                {
                    tabs--;
                    if (tabs != 0)
                    {
                        Console.Write($"{fileInfo.Name}".PadRight(PadLenghtCheck(fileInfo.Name.Length), ' '));
                    }
                    else
                    {
                        Console.WriteLine($"{fileInfo.Name}".PadRight(PadLenghtCheck(fileInfo.Name.Length), ' '));
                        tabs = 5;
                    }
                }
            }
            SendKeys.SendWait("{ENTER}");
        }

        /// <summary>
        /// Pad lenggt check for print info.
        /// </summary>
        /// <param name="fileDirNameLenght"></param>
        /// <returns></returns>
        private static int PadLenghtCheck(int fileDirNameLenght) =>
            (fileDirNameLenght > 30) ? fileDirNameLenght + 5 : 30;
    }
}