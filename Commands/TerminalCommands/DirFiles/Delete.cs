﻿using Core;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.DirFiles
{
    [SupportedOSPlatform("Windows")]
    public class Delete : ITerminalCommand
    {
        public string Name => "del";
        private string _currentLocation;
        private string _helpMessage = @"Usage of del command:
    -h  : Displays this message. 
    -a  : Deletes all files and directories in the current directory. 
    -af : Deletes all files in the current directory. 
    -ad : Deletes all directories in the current directory. 

Example1: del <dir_path>    
Example2: del <dir_path1;dir_path2;dir_path3>    
";

        public void Execute(string args)
        {
            _currentLocation = File.ReadAllText(GlobalVariables.currentDirectory);
            if (args == Name && !GlobalVariables.isPipeCommand)
            {
                FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                return;
            }
            string param = args.Split(' ').ParameterAfter("del");
            if (!GlobalVariables.isPipeCommand)
            {
                int argsLenght = args.Length - 4;
                args = args.Substring(4, argsLenght);
            }
            if (param == "-a")
            {
                DeleteAllFilesDris(_currentLocation, true, true);
            }
            else if (param == "-af")
            {
                DeleteAllFilesDris(_currentLocation, true, false);
            }
            else if (param == "-ad")
            {
                DeleteAllFilesDris(_currentLocation, false, true);
            }
            else if (args == "-h")
            {
                Console.WriteLine(_helpMessage);
            }
            else
            {
                args = args.Replace("del ", "");
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                    args = FileSystem.SanitizePath(GlobalVariables.pipeCmdOutput.Trim(), _currentLocation);
                else
                    args = FileSystem.SanitizePath(args, _currentLocation);

                // Multi dir delete
                if (args.Contains(";"))
                {
                    var dirs = args.Split(';');
                    foreach(var dir in dirs)
                        DeleteFile(dir);
                }
                else
                    DeleteFile(args);
            }
        }

        private void DeleteAllFilesDris(string currentLocation, bool fileDelete, bool dirDelete)
        {
            if (fileDelete)
            {
                var files = Directory.GetFiles(currentLocation);
                foreach (var file in files)
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
            }

            if (dirDelete)
            {
                var dirs = Directory.GetDirectories(currentLocation);
                foreach (var dir in dirs)
                {
                    if (Directory.Exists(dir))
                    {
                        var dirInfo = new DirectoryInfo(dir);
                        RecursiveDeleteDir(dirInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Delete File/Directory method.
        /// </summary>
        /// <param name="arg"></param>
        private void DeleteFile(string arg)
        {
            try
            {
                // Get the file attributes for file or directory
                FileAttributes attr = File.GetAttributes(arg);

                if (attr.HasFlag(FileAttributes.Directory))
                {
                    var dir = new DirectoryInfo(arg);
                    RecursiveDeleteDir(dir);
                    FileSystem.SuccessWriteLine($"Directory {arg} deleted!");
                }
                else
                {
                    File.SetAttributes(arg, FileAttributes.Normal);
                    File.Delete(arg);
                    FileSystem.SuccessWriteLine($"File {arg} deleted!");
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }

        /// <summary>
        /// Recursive directory delete with file atribute set.
        /// </summary>
        /// <param name="directory"></param>
        private void RecursiveDeleteDir(DirectoryInfo directory)
        {
            if (!directory.Exists)
            {
                FileSystem.ErrorWriteLine($"Directory '{directory}' does not exist!");
                return;
            }

            foreach (var dir in directory.EnumerateDirectories())
            {
                RecursiveDeleteDir(dir);
            }
            var files = directory.GetFiles();
            foreach (var file in files)
            {
                file.IsReadOnly = false;
                file.Delete();
            }
            directory.Delete();
        }
    }
}
