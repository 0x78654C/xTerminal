using Core;
using System;
using System.IO;

namespace StringView
{
    /*Cat function */
    class Program
    {
        static void Main(string[] args)
        {

            string cDir = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory);;

            try
            {
                string input = args[0];
                string output = null;
                if (input.Contains(":") && input.Contains(@"\"))
                {
                    if (File.Exists(input))
                    {
                        output = File.ReadAllText(input);
                        Console.WriteLine(output);
                    }
                    else
                    {
                        Console.WriteLine("File " + input + " dose not exist!");
                    }
                }
                else
                {
                    if (File.Exists(cDir + @"\" + input))
                    {
                        output = File.ReadAllText(cDir + @"\" + input);
                        Console.WriteLine(output);
                    }
                    else
                    {
                        FileSystem.ErrorWriteLine("File " + cDir + @"\" + input + " dose not exist!");
                    }
                }
            }
            catch
            {
                FileSystem.ErrorWriteLine("You must type the file name!");
            }
        }
    }
}
