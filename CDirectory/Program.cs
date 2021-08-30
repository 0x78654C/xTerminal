
using Core;
using System;
using System.IO;

namespace CDirectory
{
    class Program
    {
        private static string nLocation = string.Empty;
        private static string llocation = string.Empty;
        private static string sLocation = string.Empty;
        /*Setting up the current directory*/
        static void Main(string[] args)
        {
            llocation = Directory.GetCurrentDirectory(); // get current location 
            try
            {
                nLocation = args[0];              // geting location input             
                sLocation = File.ReadAllText(GlobalVariables.currentLocation);// read location from ini

                string pathCombine = null;
                if (nLocation != "")
                {

                    if (nLocation.Length == 3 && nLocation.EndsWith(@":\")) //check root path
                    {

                        if (Directory.Exists(nLocation))
                        {
                            File.WriteAllText(GlobalVariables.currentLocation, nLocation);

                        }
                        else
                        {
                            Console.WriteLine($"Directory '{nLocation}'\\ dose not exist!");
                        }

                    }
                    else
                    {
                        if (nLocation.Length < 3)
                        {
                            Console.WriteLine(@"Root directory must contain ':\' at the end!");
                        }
                        else
                        {

                            pathCombine = Path.Combine(sLocation, nLocation); // combine locations
                            if (Directory.Exists(pathCombine))
                            {
                                File.WriteAllText(GlobalVariables.currentLocation, pathCombine);

                            }
                            else
                            {
                                Console.WriteLine($"Directory '{pathCombine}' dose not exist!");
                            }
                        }

                    }
                }
                else
                {
                    File.WriteAllText(GlobalVariables.currentLocation, llocation); //reset to current terminal locaton
                }
            }
            catch
            {
                File.WriteAllText(GlobalVariables.currentLocation, llocation); //reset to current terminal locaton

            }
        }
    }
}
