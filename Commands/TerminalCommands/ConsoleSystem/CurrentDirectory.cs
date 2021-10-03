using Core;
using System;
using System.IO;
using System.Text.RegularExpressions;


namespace Commands.TerminalCommands.ConsoleSystem
{
    public class CurrentDirectory: ITerminalCommand
    {
        private static string s_newLocation = string.Empty;
        private static string s_currentLocation = string.Empty;

        public string Name => "cd";

        public void Execute (string arg)
        {

            try
            {
                s_newLocation = arg.Split(' ')[1];              // geting location input             
                s_currentLocation = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory); // read location from ini
                string pathCombine = null;
                if (s_newLocation != "")
                {
                    if (s_newLocation.Length >= 2 && s_newLocation.EndsWith(":")) //check root path
                    {
                        if (Directory.Exists(s_newLocation))
                        {
                            RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory, s_newLocation + "\\");
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
                                RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory, GlobalVariables.rootPath);
                                return;
                            }
                            s_currentLocation = s_currentLocation.Replace("\\" + lastDirectory, "");
                            RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory, s_currentLocation);
                        }
                        else
                        {
                            File.WriteAllText(GlobalVariables.currentLocation, GlobalVariables.rootPath); //reset to current terminal locaton
                            RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory, GlobalVariables.rootPath);
                        }
                    }
                    else
                    {
                        pathCombine = Path.Combine(s_currentLocation, s_newLocation); // combine locations
                        if (Directory.Exists(pathCombine))
                        {
                            RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory, pathCombine);
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
    }
}
