using Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Commands.TerminalCommands.DirFiles
{
    [SupportedOSPlatform("Windows")]
    public class MD5Check : ITerminalCommand
    {
        /*
         * Checks MD5 hash of a file.
         */

        public string Name => "md5";
        private static string s_currentDirectory;
        private static string s_helpMessage = @"Usage of MD5 command:
 md5 <file_name> : Displays the MD5 CheckSUM of a file.
 md5 -d <dire_name> : Displays the MD5 CheckSUM list of all the files in a directory and subdirectories.
 md5 -d <dire_name> -o <save_to_file> : Saves the MD5 CheckSUM list of all the files in a directory and subdirectories.

Command md5 -d can be canceled with CTRL+X key combination.
";
        public void Execute(string arg)
        {
            try
            {
                GlobalVariables.isErrorCommand = false;
                GlobalVariables.eventCancelKey = false;
                s_currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);

                // Display help message.
                if (arg == $"{Name} -h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                // Empty command info display.
                if (arg == Name)
                {
                    FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                    return;
                }

                // Display files md5 from a specific directory and subdirectories and output to a file if necesary.
                if (arg.ContainsText(" -d "))
                {
                    if (arg.ContainsText(" -o "))
                    {
                        string dParam = arg.SplitByText(" -d ", 1);
                        string dirPath = dParam.SplitByText(" -o", 0);
                        dirPath = FileSystem.SanitizePath(dirPath, s_currentDirectory);
                        if (!Directory.Exists(dirPath))
                        {
                            FileSystem.ErrorWriteLine($"Directory {dirPath} does not exist!");
                            GlobalVariables.isErrorCommand = true;
                            return;
                        }
                        string fileSaved = FileSystem.SanitizePath(dParam.SplitByText(" -o ", 1), s_currentDirectory);
                        GlobalVariables.eventKeyFlagX = true;
                        FileSystem.SuccessWriteLine(FileSystem.SaveFileOutput(fileSaved, s_currentDirectory, MD5DirCheckFiles(dirPath)));
                        if (GlobalVariables.eventCancelKey)
                            FileSystem.SuccessWriteLine("Command stopped!");
                        return;
                    }
                    string dirName = FileSystem.SanitizePath(arg.SplitByText(" -d ", 1), s_currentDirectory);
                    if (!Directory.Exists(dirName))
                    {
                        FileSystem.ErrorWriteLine($"Directory {dirName} does not exist!");
                        GlobalVariables.isErrorCommand = true;
                        return;
                    }
                    if(!GlobalVariables.isPipeCommand)
                         Console.WriteLine($"MD5 CheckSUM list for files located in {dirName}:\n");
                    GlobalVariables.eventKeyFlagX = true;
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount >0)
                        GlobalVariables.pipeCmdOutput = MD5DirCheckFiles(dirName);
                    else
                        Console.WriteLine(MD5DirCheckFiles(dirName));
                    if (GlobalVariables.eventCancelKey)
                        FileSystem.SuccessWriteLine("Command stopped!");
                    return;
                }
                int argLenght = arg.Length - 4;
                string input = arg.Substring(4, argLenght);

                // Display md5 CheckSUM of a single file.
                Console.WriteLine(MD5CheckSum(input, true));
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.ToString());
                GlobalVariables.isErrorCommand = true;
            }
        }

        /// <summary>
        /// Check MD5 of all files in a directory and subdirectories.
        /// </summary>
        /// <param name="dirName">Path to directory.</param>
        /// <returns>string</returns>
        private string MD5DirCheckFiles(string dirName)
        {
            var dirs = Directory.GetDirectories(dirName);
            var files = Directory.GetFiles(dirName);
            List<string> md5List = new List<string>();
            string retValue;
            foreach (var file in files)
            {
                md5List.Add(MD5CheckSum(file, false));
            }
            foreach (var dir in dirs)
            {
                if (!GlobalVariables.eventCancelKey)
                    md5List.Add(MD5DirCheckFiles(dir));
            }
            retValue = string.Join("\n", md5List);
            return retValue;
        }

        /// <summary>
        /// Check MD5 of a file.
        /// </summary>
        /// <param name="arg">Path of file to be checked</param>
        /// <param name="fileCheck">Display if file not exist.</param>
        /// <returns>string</returns>
        private string MD5CheckSum(string arg, bool fileCheck)
        {
            string retValue = string.Empty;
            try
            {
                string cDir = File.ReadAllText(GlobalVariables.currentDirectory);
                if (arg.Contains(":") && arg.Contains(@"\"))
                {
                    if (File.Exists(arg))
                    {
                        using (var md5 = MD5.Create())
                        {
                            using (var stream = File.OpenRead(arg))
                            {
                                var hash = md5.ComputeHash(stream);
                                retValue = arg + "   MD5: " + BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                            }
                        }
                    }
                    else
                    {
                        if (fileCheck)
                            retValue = $"File {arg} does not exist!";
                    }
                }
                else
                {
                    if (File.Exists(cDir + @"\" + arg))
                    {
                        using (var md5 = MD5.Create())
                        {
                            using (var stream = File.OpenRead(cDir + @"\" + arg))
                            {
                                var hash = md5.ComputeHash(stream);
                                retValue = cDir + arg + "   MD5: " + BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                            }
                        }
                    }
                    else
                    {
                        if (fileCheck)
                            retValue += $"File {cDir}\\{arg} does not exist!";
                    }
                }
                return retValue;
            }
            catch (Exception e)
            {
                GlobalVariables.isErrorCommand = true;
                return $"{e.Message}. Have you typed the file name?";
            }
        }
    }
}
