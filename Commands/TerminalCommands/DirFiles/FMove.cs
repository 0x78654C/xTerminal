using Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Commands.TerminalCommands.DirFiles
{
    [SupportedOSPlatform("windows")]
    public class FMove : ITerminalCommand
    {
        public string Name => "fmove";
        private string _sourceMd5 = string.Empty;
        private string _destinationMd5 = string.Empty;
        private double _sizeSource = 0;
        private double _sizeDestination = 0;
        private int _count = 0;
        private List<string> _errorCopy;
        private static string s_helpMessage = @"Usage of fmove command:
    fmove <source_file> -o <destination_file>

    Can be used with the following parameters:
    fmove -h : Displays this message
    fmove -ma <source_directory> -o <destination_directory> : Moves all files from the current directory to a specific directory

    fmove -ma command can be canceled with CTRL+X key combination.
";

        private string GetParam(string arg)
        {
            if (arg.Contains(" -ma"))
                arg = arg.Replace("fmove -ma ", string.Empty);
            else
                arg = arg.Replace("fmove ", string.Empty);
            return arg;
        }

        /// <summary>
        /// Main execution method.
        /// </summary>
        /// <param name="arg"></param>
        public void Execute(string arg)
        {
            try
            {
                FMoveRun(arg);
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
        /// Main function of simple move and multiple item move run.
        /// </summary>
        /// <param name="param"></param>
        private void FMoveRun(string param)
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

            if (param.Contains(" -ma"))
            {
                param = GetParam(param);
                if (param.Contains(" -o "))
                {
                    source = param.SplitByText(" -o ", 0).Trim();
                    destination = param.SplitByText(" -o ", 1).Trim();
                }
                MultipleMove(source, destination, currentLocation);
                GlobalVariables.eventCancelKey = false;
                ClearVars();
                return;
            }
            param = GetParam(param);
            source = param.SplitByText(" -o ", 0).Trim();
            destination = param.SplitByText(" -o ", 1).Trim();
            SimpleMove(source, destination, currentLocation);
            ClearVars();
        }



        /// <summary>
        /// Simple file move.
        /// </summary>
        /// <param name="param"></param>
        /// <param name="currentLocation"></param>
        private void SimpleMove(string sourceFile, string destinationFile, string currentLocation)
        {

            sourceFile = FileSystem.SanitizePath(sourceFile, currentLocation);
            destinationFile = FileSystem.SanitizePath(destinationFile, currentLocation);
            if(sourceFile == destinationFile)
            {
                FileSystem.ErrorWriteLine($"The source and destination file path are the same!" + Environment.NewLine);
                GlobalVariables.isErrorCommand = true;
                return;
            }
            if (!File.Exists(sourceFile))
            {
                FileSystem.ErrorWriteLine($"Source file '{sourceFile}' does not exist!" + Environment.NewLine);
                GlobalVariables.isErrorCommand = true;
                return;
            }

            FileSystem.GetMD5File(ref _sourceMd5,ref _sizeSource,ref _destinationMd5,ref _sizeDestination, sourceFile, true);

            if (!File.Exists(destinationFile))
            {
                File.Copy(sourceFile, destinationFile);
            }
            else
            {
                FileSystem.SuccessWriteLine($"Destination file '{destinationFile}' already exist!\nDo you want to move with new file name? Yes[Y], No[N], Cancel[C]");
                var consoleInput = Console.ReadLine();
                if (consoleInput.Trim().ToLower() == "y")
                {
                    CopyRenameFile(sourceFile, destinationFile);
                }
                else if (consoleInput.Trim().ToLower() == "n")
                {
                    File.Delete(destinationFile);
                    File.Copy(sourceFile, destinationFile);
                }
                else
                {
                    return;
                }
            }

            FileSystem.GetMD5File(ref _sourceMd5, ref _sizeSource, ref _destinationMd5, ref _sizeDestination, destinationFile, false);
            CheckMD5Destination(destinationFile,sourceFile);
        }


        private void CopyRenameFile(string sourceFile, string destinationFile)
        {
            destinationFile = FileRename(destinationFile);
            File.Copy(sourceFile, destinationFile);
        }

        /// <summary>
        /// Multiple files move in same directory or other.
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="destinationDirectory"></param>
        /// <param name="currentLocation"></param>
        private void MultipleMove(string sourceDirectory, string destinationDirectory, string currentLocation)
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
                    CheckMD5Destination(fileDestination,file);
                    _count = 0;
                }
                else
                {
                    FileSystem.GetMD5File(ref _sourceMd5, ref _sizeSource, ref _destinationMd5, ref _sizeDestination, file, true);
                    File.Copy(file, fileDestinationName);
                    FileSystem.GetMD5File(ref _sourceMd5, ref _sizeSource, ref _destinationMd5, ref _sizeDestination, fileDestinationName, false);
                    CheckMD5Destination(fileDestinationName,file);
                }
            }
            DisplayTotalCopiedFiles(sourceDirectory, destinationDirectory);
        }

        /// <summary>
        /// Check if MD5 of destination file is correct and return proper message or delete if not ok.
        /// </summary>
        /// <param name="destinationFile"></param>
        private void CheckMD5Destination(string destinationFile, string sourceFile)
        {
            _errorCopy = new List<string>();
            if (IsSameMD5)
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Green, "MD5 match! File was moved OK!" + Environment.NewLine);
                File.Delete(sourceFile);
            }
            else
            {
                _errorCopy.Add(destinationFile);
                if (File.Exists(destinationFile))
                    File.Delete(destinationFile);
                FileSystem.ColorConsoleTextLine(ConsoleColor.Red, "MD5 does not match! File was not moved." + Environment.NewLine);
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
        /// <summary>
        /// Display the total files copied on multi move function use.
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="destinationDirectory"></param>
        private void DisplayTotalCopiedFiles(string sourceDirectory, string destinationDirectory)
        {
            string ErrorCopy = string.Join("\n\r", _errorCopy);
            var filesSrouce = Directory.GetFiles(sourceDirectory);
            var countFilesSource = filesSrouce.Count();
            var filesDestination = Directory.GetFiles(destinationDirectory);
            var countFilesDestination = filesDestination.Count();
            double sizeSourceRound = Math.Round(_sizeSource, 2);
            double sizeDestinationRound = Math.Round(_sizeDestination, 2);

            if (!string.IsNullOrWhiteSpace(ErrorCopy))
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Red, "List of files not moved. MD5 missmatch:\n\r" + ErrorCopy + Environment.NewLine);
                Console.WriteLine("Total Files Source Directory: " + countFilesSource.ToString() + " | Total Size: " + sizeSourceRound + " MB");
                Console.WriteLine("Total Files Destination Directory: " + countFilesDestination.ToString() + " | Total Size: " + sizeDestinationRound + " MB \n\r");
            }
            else
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Cyan, "\n\r----- All files are moved -----\n\r");
                Console.WriteLine("Total Files Source Directory: " + countFilesSource.ToString() + " | Total Size: " + sizeSourceRound + " MB");
                Console.WriteLine("Total Files Destination Directory: " + countFilesDestination.ToString() + " | Total Size: " + sizeDestinationRound + " MB \n\r");
            }
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
    }
}
