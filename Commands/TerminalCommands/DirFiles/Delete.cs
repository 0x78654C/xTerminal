using Core;
using System;
using System.IO;

namespace Commands.TerminalCommands.DirFiles
{
    public class Delete : ITerminalCommand
    {
        public string Name => "del";
        private string _currentLocation;
        private string _helpMessage = @"

    -h  : Displayes this message. 
    -a  : Deletes all files and directories in current directory. 
    -af : Deletes all files in current directory. 
    -ad : Deletes all directories in current directory. 
";

        public void Execute(string args)
        {
            _currentLocation = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory);
            string param = args.Split(' ').ParameterAfter("del");
            int argsLenght = args.Length - 4;
            args = args.Substring(4, argsLenght);
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
            else if (param == "-h")
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
            try
            {
                string input = arg;              // geting location input        

                //checking the cureent locaiton in folder
                if (input.Contains(":") && input.Contains(@"\"))
                {
                    try
                    {
                        // get the file attributes for file or directory
                        FileAttributes attr = File.GetAttributes(input);

                        if (attr.HasFlag(FileAttributes.Directory))
                        {
                            var dir = new DirectoryInfo(input);
                            RecursiveDeleteDir(dir);
                            Console.WriteLine("Directory " + input + " deleted!");
                        }
                        else
                        {
                            File.SetAttributes(input, FileAttributes.Normal);
                            File.Delete(input);
                            Console.WriteLine("File " + input + " deleted!");
                        }
                    }
                    catch (Exception e)
                    {
                        FileSystem.ErrorWriteLine(e.Message);
                    }
                }
                else
                {
                    try
                    {
                        // get the file attributes for file or directory
                        FileAttributes attr = File.GetAttributes(currentLocation + input);

                        if (attr.HasFlag(FileAttributes.Directory))
                        {
                            var dir = new DirectoryInfo(currentLocation + input);
                            RecursiveDeleteDir(dir);
                            Console.WriteLine("Directory " + currentLocation + input + " deleted!");
                        }
                        else
                        {
                            File.SetAttributes(currentLocation + input, FileAttributes.Normal);
                            File.Delete(currentLocation + input);
                            Console.WriteLine("File " + currentLocation + input + " deleted!");
                        }
                    }
                    catch (Exception e)
                    {
                        FileSystem.ErrorWriteLine(e.Message);
                    }
                }
            }
            catch
            {
                FileSystem.ErrorWriteLine("You must type the file/directory name!");
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
                FileSystem.ErrorWriteLine("Directory '{directory}' does not exist!");
                return;
            }

            foreach(var dir in directory.EnumerateDirectories())
            {
                RecursiveDeleteDir(dir);
            }
            var files = directory.GetFiles();
            foreach(var file in files)
            {
                file.IsReadOnly = false;
                file.Delete();
            }
            directory.Delete();
        }
    }
}
