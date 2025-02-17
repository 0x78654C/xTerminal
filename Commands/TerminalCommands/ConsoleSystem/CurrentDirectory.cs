using Core;
using System.IO;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;


namespace Commands.TerminalCommands.ConsoleSystem
{
    /*
     Setting te current directory
     */
    [SupportedOSPlatform("Windows")]
    public class CurrentDirectory : ITerminalCommand
    {
        private static string s_newLocation = string.Empty;
        private static string s_currentLocation = string.Empty;
        private static string[] s_exceptionPath = { ".", "..\\", ".\\", "./", "...", "\\", "/",@"..\.." };
        public string Name => "cd";

        public void Execute(string arg)
        {
            try
            {
                GlobalVariables.isErrorCommand = false;
                int newPathLength = arg.Length - 3;
                if (GlobalVariables.isPipeCommand)
                    s_newLocation = GlobalVariables.pipeCmdOutput.Trim();
                else
                    s_newLocation = arg.Substring(3, newPathLength);
                s_currentLocation = File.ReadAllText(GlobalVariables.currentDirectory);
                string pathCombine = null;
                string pathSeparator;
                if (s_newLocation != "")
                {
                    if (s_newLocation.Length >= 2 && s_newLocation.EndsWith(":")) //check root path
                    {
                        if (Directory.Exists(s_newLocation))
                        {
                            pathSeparator = s_newLocation + "\\";
                            pathSeparator = pathSeparator.Replace("\\\\", "\\");
                            File.WriteAllText(GlobalVariables.currentDirectory, pathSeparator);
                            return;
                        }
                        FileSystem.ErrorWriteLine($"Directory '{s_newLocation}'\\ does not exist!");
                        GlobalVariables.isErrorCommand = true;
                    }
                    else if (s_newLocation == "..")
                    {
                        int parseLocation = Regex.Matches(s_currentLocation, @"\\").Count;

                        if (s_currentLocation.Length > 3)
                        {
                            string lastDirectory = s_currentLocation.Split('\\')[parseLocation];
                            if (parseLocation == 1)
                            {
                                string rootPartition = s_currentLocation.Split('\\')[parseLocation - 1] + "\\";
                                File.WriteAllText(GlobalVariables.currentDirectory, rootPartition);
                                return;
                            }
                            s_currentLocation = GetParentDir(s_currentLocation);
                            File.WriteAllText(GlobalVariables.currentDirectory, s_currentLocation);
                        }
                        else
                        {
                            s_currentLocation = s_currentLocation.Substring(0, 3);
                            File.WriteAllText(GlobalVariables.currentDirectory, s_currentLocation);
                            return;
                        }
                    }
                    else if (s_newLocation.StartsWith("../"))
                    {
                        BackWardNavigation();
                    }
                    else
                    {
                        if(s_newLocation.Contains("/"))
                        {
                            FileSystem.ErrorWriteLine($"Wrong path separator format!");
                            GlobalVariables.isErrorCommand = true;
                            return;
                        }

                        if (s_newLocation == "\\..")
                            return;

                        if (s_exceptionPath.ContainsParameter(s_newLocation))
                            return;

                        pathCombine = Path.Combine(s_currentLocation, s_newLocation); // combine locations
                        if (Directory.Exists(pathCombine))
                        {
                            pathSeparator = pathCombine + "\\";
                            pathSeparator = pathSeparator.Replace("\\\\", "\\");
                            File.WriteAllText(GlobalVariables.currentDirectory, pathSeparator);
                            return;
                        }
                        FileSystem.ErrorWriteLine($"Directory '{pathCombine}' does not exist!");
                        GlobalVariables.isErrorCommand = true;
                    }
                    return;
                }
                File.WriteAllText(GlobalVariables.currentDirectory, GlobalVariables.rootPath);
            }
            catch
            {
                File.WriteAllText(GlobalVariables.currentDirectory, GlobalVariables.rootPath);
            }
        }

        /// <summary>
        /// Getting parrent directory name from child one.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        private string GetParentDir(string dir)
        {
            string output;
            int parseCount = Regex.Matches(dir, @"\\").Count;
            string lastDir = dir.Split('\\')[parseCount - 1];
            int lastDirLength = lastDir.Length;
            int dirLenght = dir.Length;
            int parrentIndex = dirLenght - lastDirLength;
            output = dir.Substring(0, parrentIndex - 1);
            return output;
        }

        /// <summary>
        /// Backward multi directory navigation.
        /// </summary>
        private void BackWardNavigation()
        {
            int count = 1;
            int parseBackSlash = Regex.Matches(s_newLocation, "/..",RegexOptions.ExplicitCapture).Count + 1;
            int parseLocation = Regex.Matches(s_currentLocation, @"\\").Count;
            if (parseLocation < parseBackSlash)
                return;

            if (parseBackSlash == parseLocation)
            {
                s_currentLocation = s_currentLocation.Substring(0, 3);
                File.WriteAllText(GlobalVariables.currentDirectory, s_currentLocation);
                return;
            }

            if (s_currentLocation.Length > 3)
            {
                if (parseLocation > 0)
                {
                    while (count <= parseBackSlash)
                    {
                        count++;
                        s_currentLocation = GetParentDir(s_currentLocation);
                    }
                    File.WriteAllText(GlobalVariables.currentDirectory, s_currentLocation);
                    return;
                }
            }
            else
            {
                s_currentLocation = GetParentDir(s_currentLocation);
                File.WriteAllText(GlobalVariables.currentDirectory, s_currentLocation);
            }
        }
    }
}
