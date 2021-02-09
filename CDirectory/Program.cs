﻿
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                sLocation = File.ReadAllText(@".\Data\curDir.ini");// read location from ini

                string pathCombine = null;
                if (nLocation != "")
                {

                    if (nLocation.Length == 3 && nLocation.EndsWith(@":\")) //check root path
                    {

                        if (Directory.Exists(nLocation))
                        {
                            File.WriteAllText(@".\Data\curDir.ini", nLocation);

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
                                File.WriteAllText(@".\Data\curDir.ini", pathCombine);

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
                    File.WriteAllText(@".\Data\curDir.ini", llocation); //reset to current terminal locaton
                }
            }
            catch
            {
                File.WriteAllText(@".\Data\curDir.ini", llocation); //reset to current terminal locaton
                
            }
        }
    }
}
