using Core;
using System;
using System.IO;
using System.Text.RegularExpressions;


namespace Commands.TerminalCommands.ConsoleSystem
{
    public class CurrentDirectory : ITerminalCommand
    {
        private static string s_newLocation = string.Empty;
        private static string s_currentLocation = string.Empty;

        public string Name => "cd";

        public void Execute(string arg)
        {
            try
            {
                int newPathLength = arg.Length - 3;
                s_newLocation = arg.Substring(3,newPathLength);             
                s_currentLocation = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory); // read location from ini
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
                            RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory, pathSeparator);
                            return;
                        }
                        Console.WriteLine($"Directory '{s_newLocation}'\\ dose not exist!");
                    }
                    else if (s_newLocation == "..")
                    {
                        int parseLocation = Regex.Matches(s_currentLocation, @"\\").Count;
                        if (s_currentLocation.Length != 3)
                        {
                            string lastDirectory = s_currentLocation.Split('\\')[parseLocation];
                            if (parseLocation == 1)
                            {
                                string rootPartition = s_currentLocation.Split('\\')[parseLocation - 1] + "\\";
                                RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory, rootPartition);
                                return;
                            }
                            s_currentLocation = GetParentDir(s_currentLocation);
                            RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory, s_currentLocation);
                        }
                        else
                        {
                            RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory, GlobalVariables.rootPath);
                        }
                    }
                    else
                    {
                        pathCombine = Path.Combine(s_currentLocation, s_newLocation); // combine locations
                        if (Directory.Exists(pathCombine))
                        {
                            pathSeparator = pathCombine + "\\";
                            pathSeparator = pathSeparator.Replace("\\\\", "\\");
                            RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory, pathSeparator);
                            return;
                        }
                        Console.WriteLine($"Directory '{pathCombine}' dose not exist!");
                    }
                    return;
                }

                RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory, GlobalVariables.rootPath);
            }
            catch
            {
                RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory, GlobalVariables.rootPath);
            }
        }

        // Getting parrent directory name from child one.
        private string GetParentDir(string dir)
        {
            string output;
            int parseCount = Regex.Matches(dir, @"\\").Count;
            string lastDir = dir.Split('\\')[parseCount-1];
            int lastDirLength = lastDir.Length;
            int dirLenght = dir.Length;
            int parrentIndex = dirLenght - lastDirLength;
            output = dir.Substring(0, parrentIndex-1);
            return output;
        }
    }
}
