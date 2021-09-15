
using Core;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace CDirectory
{
    class Program
    {
        private static string s_newLocation = string.Empty;
        private static string s_currentLocation = string.Empty;


        /*Setting up the current directory*/

        static void Main(string[] args)
        {
            try
            {
                s_newLocation = args[0];              // geting location input             
                s_currentLocation = File.ReadAllText(GlobalVariables.currentLocation);// read location from ini
                string pathCombine = null;
                if (s_newLocation != "")
                {
                    if (s_newLocation.Length >= 2 && s_newLocation.EndsWith(":")) //check root path
                    {
                        if (Directory.Exists(s_newLocation))
                        {
                            File.WriteAllText(GlobalVariables.currentLocation, s_newLocation + "\\");
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
                                File.WriteAllText(GlobalVariables.currentLocation, GlobalVariables.rootPath);
                                return;
                            }
                            s_currentLocation = s_currentLocation.Replace("\\" + lastDirectory, "");
                            File.WriteAllText(GlobalVariables.currentLocation, s_currentLocation);
                        }
                        else
                        {
                            File.WriteAllText(GlobalVariables.currentLocation, GlobalVariables.rootPath); //reset to current terminal locaton
                        }
                    }
                    else
                    {
                        pathCombine = Path.Combine(s_currentLocation, s_newLocation); // combine locations
                        if (Directory.Exists(pathCombine))
                        {
                            File.WriteAllText(GlobalVariables.currentLocation, pathCombine);
                            return;
                        }
                        Console.WriteLine($"Directory '{pathCombine}' dose not exist!");
                    }
                    return;
                }

                File.WriteAllText(GlobalVariables.currentLocation, GlobalVariables.rootPath); //reset to current terminal locaton
            }
            catch
            {
                File.WriteAllText(GlobalVariables.currentLocation, GlobalVariables.rootPath); //reset to current terminal locaton
            }
        }
    }
}
