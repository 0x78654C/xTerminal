﻿using Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
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
        private List<string> _errorCopy;
        private static string s_helpMessage = @"Usage of fcopy command:
    fcopy <source_file> -o <destination_file>. 
    
    Can be used with the following parameters:
    fcopy -h : Displays this message.
    fcopy -ca <source_directory> -o <destination_directory> : Copies all files from the source directory to a specific directory.
    fcopy -ca : Copies source files in the same directory.

    fcopy -ca command can be canceled with CTRL+X key combination.
";

        /// <summary>
        /// Sanitize arguments
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private string GetParam(string arg)
        {
            if (arg.Contains(" -ca"))
                arg = arg.Replace("fcopy -ca ", string.Empty);
            else
                arg = arg.Replace("fcopy ", string.Empty);
            return arg;
        }

        /// <summary>
        /// Main function run.
        /// </summary>
        /// <param name="arg"></param>
        public void Execute(string arg)
        {
            GlobalVariables.isErrorCommand = false;
            try
            {
                FCopyRun(arg);
            }
            catch (UnauthorizedAccessException u)
            {
                FileSystem.ErrorWriteLine(u.Message);
                GlobalVariables.isErrorCommand = true;
            }
            catch (Exception x)
            {
                if (x.Message.Contains("is being used by another process"))
                {
                    FileSystem.ErrorWriteLine(x.Message);
                    GlobalVariables.isErrorCommand = true;
                }
                else
                {
                    FileSystem.ErrorWriteLine($"{x.Message}\nUse -h param for {Name} command usage!");
                    GlobalVariables.isErrorCommand = true;
                }
            }
        }

        /// <summary>
        /// Main function of simple copy and multiple item copy run.
        /// </summary>
        /// <param name="param"></param>
        private void FCopyRun(string param)
        {
            string source = string.Empty;
            string destination = string.Empty;
            if (param == $"{Name} -h")
            {
                Console.WriteLine(s_helpMessage);
                return;
            }

            if (param == Name)
            {
                FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                return;
            }
            var currentLocation = File.ReadAllText(GlobalVariables.currentDirectory);

            if (param.Contains(" -ca"))
            {
                param = GetParam(param);
                if (param.Contains(" -o "))
                {
                    source = param.SplitByText(" -o ", 0).Trim();
                    destination = param.SplitByText(" -o ", 1).Trim();
                }
                else
                {
                    source = currentLocation;
                    destination = currentLocation;
                }
                MultipleCopy(source, destination, currentLocation);
                GlobalVariables.eventCancelKey = false;
                ClearVars();
                return;
            }
            param = GetParam(param);
            source = param.SplitByText(" -o ", 0).Trim();
            destination = param.SplitByText(" -o ", 1).Trim();
            SimpleCopy(source, destination, currentLocation);
            ClearVars();
        }

        /// <summary>
        /// Clear file sizes and counters
        /// </summary>
        private void ClearVars()
        {
            _sizeDestination = 0;
            _sizeSource = 0;
            _count = 0;
        }

        /// <summary>
        /// Simple file copy.
        /// </summary>
        /// <param name="param"></param>
        /// <param name="currentLocation"></param>
        private void SimpleCopy(string sourceFile, string destinationFile, string currentLocation)
        {

            sourceFile = FileSystem.SanitizePath(sourceFile, currentLocation);
            destinationFile = FileSystem.SanitizePath(destinationFile, currentLocation);

            if (!File.Exists(sourceFile))
            {
                FileSystem.ErrorWriteLine($"Source file '{sourceFile}' does not exist!" + Environment.NewLine);
                GlobalVariables.isErrorCommand = true;
                return;
            }

            FileSystem.GetMD5File(ref _sourceMd5, ref _sizeSource, ref _destinationMd5, ref _sizeDestination, sourceFile, true);
            if (!File.Exists(destinationFile))
            {
                File.Copy(sourceFile, destinationFile);
            }
            else
            {
                FileSystem.SuccessWriteLine($"Destination file '{destinationFile}' already exist!\nDo you want to copy with new file name? Yes[Y], No[N], Cancel[C]");
                var consoleInput = Console.ReadLine();
                if (consoleInput.Trim().ToLower() == "y")
                {
                    destinationFile = FileRename(destinationFile);
                    File.Copy(sourceFile, destinationFile);
                }
                else if (consoleInput.Trim().ToLower() == "n")
                {
                    File.Delete(destinationFile);
                    File.Copy(sourceFile, destinationFile);
                }
                else
                    return;
            }

            FileSystem.GetMD5File(ref _sourceMd5, ref _sizeSource, ref _destinationMd5, ref _sizeDestination, destinationFile, false);
            CheckMD5Destination(destinationFile);
        }

        /// <summary>
        /// Multiple files copy in same directory or other.
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="destinationDirectory"></param>
        /// <param name="currentLocation"></param>
        private void MultipleCopy(string sourceDirectory, string destinationDirectory, string currentLocation)
        {
            sourceDirectory = FileSystem.SanitizePath(sourceDirectory, currentLocation);
            destinationDirectory = FileSystem.SanitizePath(destinationDirectory, currentLocation);

            var files = Directory.GetFiles(sourceDirectory);
            GlobalVariables.eventKeyFlagX = true;
            foreach (var file in files)
            {
                if (GlobalVariables.eventCancelKey)
                {
                    FileSystem.SuccessWriteLine("Command stopped!");
                    break;
                }

                var fileInfo = new FileInfo(file);
                var fileName = fileInfo.Name;
                var fileDestinationName = $"{destinationDirectory}\\{fileName}";
                if (File.Exists(fileDestinationName))
                {
                    var fileDestination = FileRename(fileDestinationName);
                    FileSystem.GetMD5File(ref _sourceMd5, ref _sizeSource, ref _destinationMd5, ref _sizeDestination, file, true);
                    File.Copy(file, fileDestination);
                    FileSystem.GetMD5File(ref _sourceMd5, ref _sizeSource, ref _destinationMd5, ref _sizeDestination, fileDestination, false);
                    CheckMD5Destination(fileDestination);
                    _count = 0;
                }
                else
                {
                    FileSystem.GetMD5File(ref _sourceMd5, ref _sizeSource, ref _destinationMd5, ref _sizeDestination, file, true);
                    File.Copy(file, fileDestinationName);
                    FileSystem.GetMD5File(ref _sourceMd5, ref _sizeSource, ref _destinationMd5, ref _sizeDestination, fileDestinationName, false);
                    CheckMD5Destination(fileDestinationName);
                }
            }
            DisplayTotalCopiedFiles(sourceDirectory, destinationDirectory);
        }

        /// <summary>
        /// Display the total files copied on multycopy function use.
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="destinationDirectory"></param>
        private void DisplayTotalCopiedFiles(string sourceDirectory, string destinationDirectory)
        {
            if (sourceDirectory == destinationDirectory) return;
            string ErrorCopy = string.Join("\n\r", _errorCopy);
            var filesSrouce = Directory.GetFiles(sourceDirectory);
            var countFilesSource = filesSrouce.Count();
            var filesDestination = Directory.GetFiles(destinationDirectory);
            var countFilesDestination = filesDestination.Count();
            double sizeSourceRound = Math.Round(_sizeSource, 2);
            double sizeDestinationRound = Math.Round(_sizeDestination, 2);

            if (!string.IsNullOrWhiteSpace(ErrorCopy))
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Red, "List of files not copied. MD5 missmatch:\n\r" + ErrorCopy + Environment.NewLine);
                Console.WriteLine("Total Files Source Directory: " + countFilesSource.ToString() + " | Total Size: " + sizeSourceRound + " MB");
                Console.WriteLine("Total Files Destination Directory: " + countFilesDestination.ToString() + " | Total Size: " + sizeDestinationRound + " MB \n\r");
            }
            else
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Cyan, "\n\r----- All files are copied -----\n\r");
                Console.WriteLine("Total Files Source Directory: " + countFilesSource.ToString() + " | Total Size: " + sizeSourceRound + " MB");
                Console.WriteLine("Total Files Destination Directory: " + countFilesDestination.ToString() + " | Total Size: " + sizeDestinationRound + " MB \n\r");
            }
        }


        /// <summary>
        /// Check if MD5 of destination file is correct and return proper message or delete if not ok.
        /// </summary>
        /// <param name="destinationFile"></param>
        private void CheckMD5Destination(string destinationFile)
        {
            _errorCopy = new List<string>();
            if (IsSameMD5)
                FileSystem.ColorConsoleTextLine(ConsoleColor.Green, "MD5 match! File was copied OK!" + Environment.NewLine);
            else
            {
                _errorCopy.Add(destinationFile);
                if (File.Exists(destinationFile))
                    File.Delete(destinationFile);
                FileSystem.ColorConsoleTextLine(ConsoleColor.Red, "MD5 does not match! File was not copied." + Environment.NewLine);
            }
        }


        /// <summary>
        /// Check if source file has same MD5 has as destination file.
        /// </summary>
        private bool IsSameMD5 => _sourceMd5 == _destinationMd5;


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
            return newFileName;
        }
    }
}
