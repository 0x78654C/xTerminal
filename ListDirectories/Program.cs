using Core;
using System;
using System.IO;

namespace ListDirectories
{
    /*List files and directories*/
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args[0] == "-s")
                {
                    ListDirFile(true);
                    return;
                }
                ListDirFile(false);
            }
            catch
            {
                ListDirFile(false);
            }
        }

        private static void ListDirFile(bool sizeCheck)
        {
            string cDir = File.ReadAllText(GlobalVariables.currentLocation);
            if (Directory.Exists(cDir))
            {
                var files = Directory.GetFiles(cDir);
                var directories = Directory.GetDirectories(cDir);
                DirectoryInfo directoryInfo;
                FileInfo fileInfo;
                foreach (var dir in directories)
                {
                    directoryInfo = new DirectoryInfo(dir);
                    FileSystem.ColorConsoleTextLine(ConsoleColor.DarkCyan, directoryInfo.Name);
                }
                foreach (var file in files)
                {
                    fileInfo = new FileInfo(file);
                    if (sizeCheck)
                    {
                        Console.WriteLine(fileInfo.Name.PadRight(50, ' ') + $"Size: {FileSystem.GetFileSize(file, false)}");
                    }
                    else
                    {
                        Console.WriteLine(fileInfo.Name);
                    }
                }
                if (sizeCheck)
                {
                    string currentDirectorySize = FileSystem.GetDirSize(new DirectoryInfo(cDir));
                    Console.WriteLine("---------------------------------------------\n");
                    Console.WriteLine($"Current directory size: {currentDirectorySize}\n");
                }
            }
            else
            {
                FileSystem.ErrorWriteLine($"Directory '{cDir}' dose not exist!");
            }
        }
    }
}
