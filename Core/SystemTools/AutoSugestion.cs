using System;
using System.IO;
using System.Windows.Forms;

namespace Core.SystemTools
{
    public class AutoSugestion
    {

        /// <summary>
        /// Directories sugestion by first letters.
        /// </summary>
        /// <param name="startChar">Start letters.</param>
        /// <param name="currentDirectory">Current directory path.</param>
        public static void DirCompletion(string startChar, string currentDirectory)
        {
            var directories = Directory.GetDirectories(currentDirectory);
            int tabs = 5;
            Console.WriteLine();
            foreach (var dir in directories)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(dir);
                if (dirInfo.Name.ToLower().StartsWith(startChar.ToLower()))
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
            SendKeys.Send("{ENTER}");
        }

        /// <summary>
        /// Files sugestion by first letters.
        /// </summary>
        /// <param name="startChar">Start letters.</param>
        /// <param name="currentDirectory">Current directory path.</param>
        public static void FileCompletion(string startChar, string currentDirectory)
        {
            var files = Directory.GetFiles(currentDirectory);
            int tabs = 5;
            Console.WriteLine();
            foreach (var file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.Name.ToLower().StartsWith(startChar.ToLower()))
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
            SendKeys.Send("{ENTER}");
        }

        
        private static int PadLenghtCheck(int fileDirNameLenght)
        {
            return (fileDirNameLenght > 30) ? fileDirNameLenght + 5 : 30;
        }
    }
}
