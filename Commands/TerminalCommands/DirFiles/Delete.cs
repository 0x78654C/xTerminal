using Core;
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
                DeleteFile(args, _currentLocation);
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
        private void DeleteFile(string arg, string currentLocation)
        {
            string input = string.Empty;
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount >0)
                input = FileSystem.SanitizePath(GlobalVariables.pipeCmdOutput.Trim(), currentLocation);
            else
                input = FileSystem.SanitizePath(arg, currentLocation);
            try
            {
                // Get the file attributes for file or directory
                FileAttributes attr = File.GetAttributes(input);

                if (attr.HasFlag(FileAttributes.Directory))
                {
                    var dir = new DirectoryInfo(input);
                    RecursiveDeleteDir(dir);
                    FileSystem.SuccessWriteLine($"Directory {input} deleted!");
                }
                else
                {
                    File.SetAttributes(input, FileAttributes.Normal);
                    File.Delete(input);
                    FileSystem.SuccessWriteLine($"File {input} deleted!");
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
