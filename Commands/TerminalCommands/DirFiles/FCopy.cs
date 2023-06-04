﻿using Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;

namespace Commands.TerminalCommands.DirFiles
{
    [SupportedOSPlatform("windows")]
    public class FCopy : ITerminalCommand
    {
        public string Name => "fcopy";
        private string _sourceMd5 = string.Empty;
        private string _destinationMd5 = string.Empty;
        private double _sizeSource = 0;
        private double _sizeDestination = 0;
        private int _count = 0;
        private static string s_helpMessage = @"Usage of fcopy command:
    fcopy <source_file> -o <destination_file>. Can be used with the following parameters:
    fcopy -h : Displays this message
    fcopy -ca <destination_directory> : Copies all files from the current directory to a specific directory
    fcopy -ca : Copies source files in the same directory";


        /// <summary>
        /// Sanitize arguments
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private string GetParam(string arg)
        {
            if (arg.Contains(" - ca"))
                arg = arg.Replace("fcopy -ca ", string.Empty);
            else
                arg = arg.Replace("fcopy ", string.Empty);
            return arg;
        }
        public void Execute(string arg)
        {

            // Run old version  of fcopy
            // OldFCopy(arg);
            //TODO: implement command cancel with ctrl+x
            try
            {
                FCopyRun(arg);
            }
            catch (UnauthorizedAccessException u)
            {
                FileSystem.ErrorWriteLine(u.Message);
            }
            catch (Exception x)
            {
                if (x.Message.Contains("is being used by another process"))
                {
                    FileSystem.ErrorWriteLine(x.Message);
                }
                else
                {
                    FileSystem.ErrorWriteLine(x.Message);
                    FileSystem.ErrorWriteLine("\nCommand should look like this: fcopy source_file -o target_file");
                }
            }
        }


        private void FCopyRun(string param)
        {
            if (param == $"{Name} -h")
            {
                Console.WriteLine(s_helpMessage);
                return;
            }

            if (param.Length == 5)
            {
                Console.WriteLine($"Use -h param for {Name} command usage!");
                return;
            }

            param = GetParam(param);
            var currentLocation = File.ReadAllText(GlobalVariables.currentDirectory);

            SimpleCopy(param, currentLocation);

        }

        /// <summary>
        /// Check file srouce and destination MD5 hash.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="source"></param>
        private void GetMD5File(string filePath, bool source)
        {
            if (source)
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        var hash = md5.ComputeHash(stream);
                        _sourceMd5 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        Console.WriteLine("Source File: " + filePath + " | MD5: " + _sourceMd5 + " | Size: " + FileSystem.GetFileSize(filePath, false));
                        _sizeSource += Double.Parse(FileSystem.GetFileSize(filePath, true));
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
                        _destinationMd5 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        Console.WriteLine("Destination File: " + filePath + " | MD5: " + _destinationMd5 + " | Size: " + FileSystem.GetFileSize(filePath, false));
                        _sizeDestination += Double.Parse(FileSystem.GetFileSize(filePath, true));
                    }
                }
            }
        }

        /// <summary>
        /// Check if source file has same MD5 has as destination file.
        /// </summary>
        private bool IsSameMD5 => _sourceMd5 == _destinationMd5;


        /// <summary>
        /// Simple file copy.
        /// </summary>
        /// <param name="param"></param>
        /// <param name="currentLocation"></param>
        private void SimpleCopy(string param, string currentLocation)
        {
            var sourceFile = param.SplitByText(" -o ", 0).Trim();
            var destinationFile = param.SplitByText(" -o ", 1).Trim();

            sourceFile = FileSystem.SanitizePath(sourceFile, currentLocation);
            destinationFile = FileSystem.SanitizePath(destinationFile, currentLocation);

            if (!File.Exists(sourceFile))
            {
                Console.WriteLine($"Source file '{sourceFile}' does not exist!" + Environment.NewLine);
                return;
            }

            GetMD5File(sourceFile, true);

            if (!File.Exists(destinationFile))
            {
                File.Copy(sourceFile, destinationFile);
            }
            else
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Destination file '{destinationFile}' already exist!\nDo you want to copy with new file name? Yes[Y] or No[N]");
                var consoleInput = Console.ReadLine();
                if (consoleInput.Trim().ToLower() == "y")
                {
                    destinationFile = FileRename(destinationFile);
                    File.Copy(sourceFile, destinationFile);
                }
                else
                    return;
            }

            GetMD5File(destinationFile, false);

            if (IsSameMD5)
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Green, "MD5 match! File was copied OK!" + Environment.NewLine);
            }
            else
            {
                if (File.Exists(destinationFile))
                    File.Delete(destinationFile);
                FileSystem.ColorConsoleTextLine(ConsoleColor.Red, "MD5 does not match! File was not copied." + Environment.NewLine);
            }
        }

        /// <summary>
        /// Create new file name for destination file if exists.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string FileRename(string fileName)
        {
            var fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);
            var fileExt = new FileInfo(fileName).Extension;
            var filePath = Path.GetDirectoryName(fileName);
            var newFileName = string.Empty;
            while (true)
            {
                _count++;
                newFileName = filePath + @"\" + fileNameNoExt + $"_{_count}_" + fileExt;
                if (!File.Exists(newFileName))
                    break;
            }
            _count = 0;
            return newFileName;
        }
        /*WIP
        private void SetFinalCopyMessage()
        {
            string ErrorCopy = string.Join("\n\r", FilesErrorCopy);
            var files = Directory.GetFiles(dlocation);
            var countFilesS = files.Count();
            var dfiles = Directory.GetFiles(NewPath);
            var countFilesD = dfiles.Count();

            double sizeSourceRound = Math.Round(sizeSourceFiles, 2);
            double sizeDestinationRound = Math.Round(sizeDestinationFiles, 2);

            if (!string.IsNullOrWhiteSpace(ErrorCopy))
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Red, "List of files not copied/moved. MD5 missmatch:\n\r" + ErrorCopy + Environment.NewLine);
                Console.WriteLine("Total Files Source Directory: " + countFilesS.ToString() + " | Total Size: " + sizeSourceRound + " MB");
                Console.WriteLine("Total Files Destination Directory: " + countFilesD.ToString() + " | Total Size: " + sizeDestinationRound + " MB \n\r");
            }
            else
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Cyan, "\n\r----- All files are copied -----\n\r");
                Console.WriteLine("Total Files Source Directory: " + countFilesS.ToString() + " | Total Size: " + sizeSourceRound + " MB");
                Console.WriteLine("Total Files Destination Directory: " + countFilesD.ToString() + " | Total Size: " + sizeDestinationRound + " MB \n\r");
            }
        }
        */

        private void OldFCopy(string arg)
        {

            Console.WriteLine(" ");

            string dlocation = File.ReadAllText(GlobalVariables.currentDirectory);
            string crcSource = null;
            string crcDestination = null;
            string Source = null;
            string Destination = null;
            string NewPath = null;
            List<string> FilesErrorCopy = new List<string>();
            int countFilesS = 0;
            int countFilesD = 0;
            string[] files;
            string[] dfiles;
            string cmdType = null;
            string codeBase = Assembly.GetExecutingAssembly().GetName().Name;
            double sizeSourceFiles = 0;
            double sizeDestinationFiles = 0;
            string param = GetParam(arg);
            if (arg.Length == 5)
            {
                Console.WriteLine($"Use -h param for {Name} command usage!");
                return;
            }
            Console.WriteLine("\n\r");
            try
            {
                files = Directory.GetFiles(dlocation);

                Source = param.SplitByText(" -o ", 0);
                cmdType = Source;
                if (Source.Contains(@":\"))
                {
                    NewPath = param;
                }
                else { NewPath = dlocation; }

                if (arg == $"{Name} -h")
                {
                    Console.WriteLine(@"Usage of fcopy command:
    fcopy <source_file> -o <destination_file>. Can be used with the following parameters:
    fcopy -h : Displays this message
    fcopy -ca <destination_directory> : Copies all files from the current directory to a specific directory
    fcopy -ca : Copies source files in the same directory
");
                    return;
                }

                //copy all with and without args
                if (arg.Contains(" -ca "))
                {
                    dfiles = Directory.GetFiles(NewPath);
                    string dFilesList = string.Join("", dfiles);

                    if (NewPath != dlocation && dFilesList.Length > 1)
                    {
                        foreach (var file in dfiles)
                        {
                            if (!file.Contains(") - ") && !file.Contains(codeBase))
                            {

                                int FileCount = 0;
                                int woIndex = 0;
                                int wIndex = 0;
                                int countF = Regex.Matches(file, @"\\").Count;
                                // We get the file name
                                string delilmiterSplitF = file.Split('\\')[countF];

                                // We check if file is already indexed or not
                                if (delilmiterSplitF.StartsWith("(") && delilmiterSplitF.Contains(") - "))
                                {
                                    string fileNameSplit2 = delilmiterSplitF.Split(')')[1];
                                    fileNameSplit2 = fileNameSplit2.Split('-')[1].Trim();
                                    woIndex += Regex.Matches(string.Join(";", dfiles), fileNameSplit2).Count;
                                }
                                else
                                {
                                    wIndex = Regex.Matches(string.Join(";", dfiles), delilmiterSplitF).Count;
                                }
                                FileCount = woIndex + wIndex;
                                FileCount--;

                                Source = dlocation + "\\" + delilmiterSplitF;
                                using (var md5 = MD5.Create())
                                {
                                    if (File.Exists(Source))
                                    {
                                        using (var stream = File.OpenRead(Source))
                                        {
                                            var hash = md5.ComputeHash(stream);
                                            crcSource = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                                            Console.WriteLine("Source File: " + Source + " | MD5: " + crcSource + " | Size: " + FileSystem.GetFileSize(Source, false));
                                            sizeSourceFiles += Double.Parse(FileSystem.GetFileSize(Source, true));
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Source file '{Source}' does not exist!" + Environment.NewLine);
                                    }
                                }

                                Destination = Source;

                                if (!Destination.Contains(":") || !Destination.Contains(@"\"))
                                {
                                    Destination = NewPath + Destination;
                                }
                                else
                                {
                                    Destination = NewPath + delilmiterSplitF;
                                }


                                // Copy module
                                if (File.Exists(Source))
                                {
                                    if (!File.Exists(Destination))
                                    {
                                        File.Copy(Source, Destination);
                                    }
                                    else
                                    {
                                        int index = 0;
                                        int count = Regex.Matches(Destination, @"\\").Count;
                                        string delilmiterSplit = Destination.Split('\\')[count];

                                        if (delilmiterSplit.Contains(") - "))
                                        {
                                            string fileNameSplit = delilmiterSplit.Split(')')[1];
                                            if (fileNameSplit.StartsWith(" - "))
                                            {
                                                string fileNameSplit2 = delilmiterSplit.Split(')')[0];
                                                fileNameSplit2 = fileNameSplit2.Replace("(", "");
                                                fileNameSplit = fileNameSplit.Split('-')[1].Trim();
                                                Destination = NewPath + "\\(" + FileCount + ") - " + fileNameSplit;
                                                if (!File.Exists(Destination))
                                                {
                                                    File.Copy(Source, Destination);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Destination = NewPath + "\\(" + index + ") - " + delilmiterSplit;
                                            if (File.Exists(Destination))
                                            {
                                                index = 0;
                                                count = Regex.Matches(Destination, @"\\").Count;
                                                delilmiterSplit = Destination.Split('\\')[count];
                                                if (delilmiterSplit.Contains(") - "))
                                                {
                                                    string fileNameSplit = delilmiterSplit.Split(')')[1];
                                                    if (fileNameSplit.StartsWith(" - "))
                                                    {
                                                        string fileNameSplit2 = delilmiterSplit.Split(')')[0];
                                                        fileNameSplit2 = fileNameSplit2.Replace("(", "");
                                                        fileNameSplit = fileNameSplit.Split('-')[1].Trim();
                                                        Destination = NewPath + "\\(" + FileCount + ") - " + fileNameSplit;
                                                        if (!File.Exists(Destination))
                                                        {
                                                            File.Copy(Source, Destination);
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {

                                                File.Copy(Source, Destination);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Source file '{Source}' does not exist!" + Environment.NewLine);
                                }
                                //-------------------------

                                // Grabing destination file crc
                                using (var md5 = MD5.Create())
                                {
                                    using (var stream = File.OpenRead(Destination))
                                    {
                                        var hash = md5.ComputeHash(stream);
                                        crcDestination = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                                        Console.WriteLine("Destination File: " + Destination + " | MD5: " + crcDestination + " | Size: " + FileSystem.GetFileSize(Destination, false));
                                        sizeDestinationFiles += Double.Parse(FileSystem.GetFileSize(Destination, true));
                                    }
                                }
                                //--------------------------------


                                if (crcSource == crcDestination)
                                {
                                    FileSystem.ColorConsoleTextLine(ConsoleColor.Green, "MD5 match! File was copied OK!" + Environment.NewLine);
                                }
                                else
                                {
                                    File.Delete(Destination);
                                    FileSystem.ColorConsoleTextLine(ConsoleColor.Red, "MD5 does not match! File was not copied." + Environment.NewLine);
                                    FilesErrorCopy.Add(Source);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var file in files)
                        {

                            if (!file.Contains(") - "))
                            {
                                int FileCount = 0;
                                int woIndex = 0;
                                int wIndex = 0;
                                int countF = Regex.Matches(file, @"\\").Count;
                                // We get the file name
                                string delilmiterSplitF = file.Split('\\')[countF];

                                // We check if file is already indexed or not
                                if (delilmiterSplitF.StartsWith("(") && delilmiterSplitF.Contains(") - "))
                                {
                                    string fileNameSplit2 = delilmiterSplitF.Split(')')[1];
                                    fileNameSplit2 = fileNameSplit2.Split('-')[1].Trim();
                                    woIndex += Regex.Matches(string.Join(";", files), fileNameSplit2).Count;
                                }
                                else
                                {
                                    wIndex = Regex.Matches(string.Join(";", files), delilmiterSplitF).Count;
                                }
                                FileCount = woIndex + wIndex;
                                FileCount--;
                                Source = file;

                                // Check if source is current path
                                if (!Source.Contains(":") || !Source.Contains(@"\"))
                                {
                                    Source = dlocation + Source;
                                }

                                using (var md5 = MD5.Create())
                                {
                                    if (File.Exists(Source))
                                    {
                                        using (var stream = File.OpenRead(Source))
                                        {
                                            var hash = md5.ComputeHash(stream);
                                            crcSource = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                                            Console.WriteLine("Source File: " + Source + " | MD5: " + crcSource + " | Size: " + FileSystem.GetFileSize(Source, false));
                                            sizeSourceFiles += Double.Parse(FileSystem.GetFileSize(Source, true));
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Source file '{Source}' does not exist!" + Environment.NewLine);
                                    }
                                }

                                Destination = Source;

                                if (!Destination.Contains(":") || !Destination.Contains(@"\"))
                                {
                                    Destination = NewPath + "\\" + Destination;
                                }
                                else
                                {
                                    Destination = NewPath + "\\" + delilmiterSplitF;
                                }


                                // Copy module
                                if (File.Exists(Source))
                                {
                                    if (!File.Exists(Destination))
                                    {

                                        File.Copy(Source, Destination);
                                    }
                                    else
                                    {
                                        int index = 0;
                                        int count = Regex.Matches(Destination, @"\\").Count;
                                        string delilmiterSplit = Destination.Split('\\')[count];

                                        if (delilmiterSplit.Contains(") - "))
                                        {
                                            string fileNameSplit = delilmiterSplit.Split(')')[1];
                                            if (fileNameSplit.StartsWith(" - "))
                                            {
                                                string fileNameSplit2 = delilmiterSplit.Split(')')[0];
                                                fileNameSplit2 = fileNameSplit2.Replace("(", "");
                                                fileNameSplit = fileNameSplit.Split('-')[1].Trim();
                                                Destination = NewPath + "\\(" + FileCount + ") - " + fileNameSplit;

                                                if (!File.Exists(Destination))
                                                {
                                                    File.Copy(Source, Destination);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Destination = NewPath + "\\(" + index + ") - " + delilmiterSplit;
                                            if (File.Exists(Destination))
                                            {
                                                index = 0;
                                                count = Regex.Matches(Destination, @"\\").Count;
                                                delilmiterSplit = Destination.Split('\\')[count];
                                                if (delilmiterSplit.Contains(") - "))
                                                {
                                                    string fileNameSplit = delilmiterSplit.Split(')')[1];
                                                    if (fileNameSplit.StartsWith(" - "))
                                                    {
                                                        string fileNameSplit2 = delilmiterSplit.Split(')')[0];
                                                        fileNameSplit2 = fileNameSplit2.Replace("(", "");
                                                        fileNameSplit = fileNameSplit.Split('-')[1].Trim();
                                                        Destination = NewPath + "\\(" + FileCount + ") - " + fileNameSplit;

                                                        if (!File.Exists(Destination))
                                                        {
                                                            File.Copy(Source, Destination);
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {

                                                File.Copy(Source, Destination);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Source file '{Source}' does not exist!" + Environment.NewLine);
                                }
                                //-------------------------

                                // Grabing destination file crc
                                using (var md5 = MD5.Create())
                                {
                                    using (var stream = File.OpenRead(Destination))
                                    {
                                        var hash = md5.ComputeHash(stream);
                                        crcDestination = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                                        Console.WriteLine("Destination File: " + Destination + " | MD5: " + crcDestination + " | Size: " + FileSystem.GetFileSize(Destination, false));
                                        sizeDestinationFiles += Double.Parse(FileSystem.GetFileSize(Destination, true));
                                    }
                                }
                                //--------------------------------


                                if (crcSource == crcDestination)
                                {
                                    FileSystem.ColorConsoleTextLine(ConsoleColor.Green, "MD5 match! File was copied OK!" + Environment.NewLine);
                                }
                                else
                                {
                                    File.Delete(Destination);
                                    FileSystem.ColorConsoleTextLine(ConsoleColor.Red, "MD5 does not match! File was not copied." + Environment.NewLine);
                                    FilesErrorCopy.Add(Source);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (!Source.Contains(":") || !Source.Contains(@"\"))
                    {
                        Source = dlocation + Source;
                    }
                    using (var md5 = MD5.Create())
                    {
                        if (File.Exists(Source))
                        {
                            using (var stream = File.OpenRead(Source))
                            {
                                var hash = md5.ComputeHash(stream);
                                crcSource = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                                Console.WriteLine("Source File: " + Source + " | MD5: " + crcSource + " | Size: " + FileSystem.GetFileSize(Source, false));
                                sizeSourceFiles += Double.Parse(FileSystem.GetFileSize(Source, true));
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Source file '{Source}' does not exist!" + Environment.NewLine);
                        }
                    }

                    Destination = param.SplitByText(" -o ", 1);

                    if (!Destination.Contains(":") || !Destination.Contains(@"\"))
                    {
                        Destination = dlocation + Destination;
                    }

                    // Copy module
                    if (File.Exists(Source))
                    {
                        if (!File.Exists(Destination))
                        {
                            File.Copy(Source, Destination);
                        }
                        else
                        {
                            Console.WriteLine($"Destination file '{Destination}' already exists!" + Environment.NewLine);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Source file '{Source}' does not exist!" + Environment.NewLine);
                    }
                    //-------------------------
                    //Grabing destination file crc
                    using (var md5 = MD5.Create())
                    {
                        using (var stream = File.OpenRead(Destination))
                        {
                            var hash = md5.ComputeHash(stream);
                            crcDestination = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                            Console.WriteLine("Destination File: " + Destination + " | MD5: " + crcDestination + " | Size: " + FileSystem.GetFileSize(Destination, false));
                        }
                    }

                    if (crcSource == crcDestination)
                    {
                        FileSystem.ColorConsoleTextLine(ConsoleColor.Green, "MD5 match! File was copied OK!" + Environment.NewLine);
                    }
                    else
                    {
                        File.Delete(Destination);
                        FileSystem.ColorConsoleTextLine(ConsoleColor.Red, "MD5 does not match! File was not copied." + Environment.NewLine);
                        FilesErrorCopy.Add(Source);
                    }
                }
            }
            catch (UnauthorizedAccessException u)
            {
                FileSystem.ErrorWriteLine(u.Message);
            }
            catch (Exception x)
            {
                if (x.Message.Contains("is being used by another process"))
                {
                    FileSystem.ErrorWriteLine("File '" + Destination + "' is being used by another process. Terminated!");
                }
                else
                {
                    FileSystem.ErrorWriteLine(x.Message);
                    FileSystem.ErrorWriteLine("\nCommand should look like this: fcopy source_file -o target_file");
                }

            }
            if (cmdType.StartsWith("-ca"))
            {
                string ErrorCopy = string.Join("\n\r", FilesErrorCopy);
                files = Directory.GetFiles(dlocation);
                countFilesS = files.Count();
                dfiles = Directory.GetFiles(NewPath);
                countFilesD = dfiles.Count();

                double sizeSourceRound = Math.Round(sizeSourceFiles, 2);
                double sizeDestinationRound = Math.Round(sizeDestinationFiles, 2);

                if (!string.IsNullOrWhiteSpace(ErrorCopy))
                {
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Red, "List of files not copied/moved. MD5 missmatch:\n\r" + ErrorCopy + Environment.NewLine);
                    Console.WriteLine("Total Files Source Directory: " + countFilesS.ToString() + " | Total Size: " + sizeSourceRound + " MB");
                    Console.WriteLine("Total Files Destination Directory: " + countFilesD.ToString() + " | Total Size: " + sizeDestinationRound + " MB \n\r");
                }
                else
                {
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Cyan, "\n\r----- All files are copied -----\n\r");
                    Console.WriteLine("Total Files Source Directory: " + countFilesS.ToString() + " | Total Size: " + sizeSourceRound + " MB");
                    Console.WriteLine("Total Files Destination Directory: " + countFilesD.ToString() + " | Total Size: " + sizeDestinationRound + " MB \n\r");
                }
            }
        }
    }
}
