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
                s_currentDirectory = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory); ;

                if (args[0] == "-s")
                {
                    ListDirFile(true, false, "");
                    return;
                }
                else if (args[0] == "-c")
                {
                    try
                    {
                        int files = Directory.GetFiles(s_currentDirectory, "*.*", SearchOption.AllDirectories).Length;
                        var directories = Directory.GetDirectories(s_currentDirectory, "*", SearchOption.AllDirectories).Length;
                        Console.WriteLine($"Conting total directories/subdirectories and files on current location....\n");
                        Console.WriteLine($"Total directories/subdirectories: {directories}");
                        Console.WriteLine($"Total files (include subdirectories): {files}");
                        return;
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        FileSystem.ErrorWriteLine("You need administrator rights to run command in this place! Some directories/files cannot be accesd!");
                        return;
                    }
                }
                else if (args[0] == "-hl")
                {
                    try
                    {
                        string higlightedText = args[1];
                        ListDirFile(false, true, higlightedText);
                    }
                    catch
                    {
                        FileSystem.ErrorWriteLine("Check command. You must provide a text to highlight!");
                    }
                    return;
                }

                ListDirFile(false, false, "");
            }
            catch
            {
                ListDirFile(false, false, "");
            }
        }


        private static void ListDirFile(bool sizeCheck, bool highlight, string highLightText)
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
                    if (highlight && highLightText.Length > 0)
                    {
                        if (directoryInfo.Name.ToLower().Contains(highLightText.ToLower()))
                        {
                            FileSystem.ColorConsoleTextLine(ConsoleColor.Red, directoryInfo.Name);
                        }
                        else
                        {
                            FileSystem.ColorConsoleTextLine(ConsoleColor.DarkCyan, directoryInfo.Name);
                        }
                    }
                    else
                    {
                        FileSystem.ColorConsoleTextLine(ConsoleColor.DarkCyan, directoryInfo.Name);

                    }
                }
                foreach (var file in files)
                {
                    fileInfo = new FileInfo(file);
                    if (sizeCheck)
                    {
                        if (highlight && highLightText.Length > 0)
                        {
                            if (fileInfo.Name.ToLower().Contains(highLightText.ToLower()))
                            {
                                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, fileInfo.Name.PadRight(50, ' ') + $"Size: {FileSystem.GetFileSize(file, false)}");
                            }
                            else
                            {
                                Console.WriteLine(fileInfo.Name.PadRight(50, ' ') + $"Size: {FileSystem.GetFileSize(file, false)}");
                            }
                        }
                        else
                        {
                            Console.WriteLine(fileInfo.Name.PadRight(50, ' ') + $"Size: {FileSystem.GetFileSize(file, false)}");

                        }
                    }
                    else
                    {
                        if (highlight && highLightText.Length > 0)
                        {

                            if (fileInfo.Name.ToLower().Contains(highLightText.ToLower()))
                            {
                                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, fileInfo.Name);
                            }
                            else
                            {
                                Console.WriteLine(fileInfo.Name);
                            }
                        }
                        else
                        {
                            Console.WriteLine(fileInfo.Name);
                        }
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
