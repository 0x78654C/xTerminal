using Core;
using System;
using System.IO;

namespace ListDirectories
{
    /*List files and directories*/
    class Program
    {
        private static string s_currentDirectory = string.Empty;
        static void Main(string[] args)
        {
            try
            {
                s_currentDirectory = File.ReadAllText(GlobalVariables.currentLocation);

                if (args[0] == "-s")
                {
                    ListDirFile(true);
                    return;
                }
                else if (args[0] == "-c")
                {
                    int files = Directory.GetFiles(s_currentDirectory,"*.*", SearchOption.AllDirectories).Length;
                    var directories = Directory.GetDirectories(s_currentDirectory,"*",SearchOption.AllDirectories).Length;
                    Console.WriteLine($"Conting total directories/subdirectories and files on current location....\n");
                    Console.WriteLine($"Total directories/subdirectories: {directories}");
                    Console.WriteLine($"Total files (include subdirectories): {files}");
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

            if (Directory.Exists(s_currentDirectory))
            {
                var files = Directory.GetFiles(s_currentDirectory);
                var directories = Directory.GetDirectories(s_currentDirectory);
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
                    string currentDirectorySize = FileSystem.GetDirSize(new DirectoryInfo(s_currentDirectory));
                    Console.WriteLine("---------------------------------------------\n");
                    Console.WriteLine($"Current directory size: {currentDirectorySize}\n");
                }
                Console.WriteLine("---------------------------------------------\n");
                Console.WriteLine($"Total directories: {directories.Length}");
                Console.WriteLine($"Total files: {files.Length}");
            }
            else
            {
                FileSystem.ErrorWriteLine($"Directory '{s_currentDirectory}' dose not exist!");
            }
        }
    }
}
