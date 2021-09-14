using Core;
using System;
using System.IO;

namespace Delete
{
    /*Delete files and directories */
    class Program
    {
        static void Main(string[] args)
        {

            try
            {
                string input = args[0];              // geting location input        
                string newlocation = File.ReadAllText(GlobalVariables.currentLocation); //get the new location

                //checking the cureent locaiton in folder
                if (input.Contains(":") && input.Contains(@"\"))
                {
                    try
                    {
                        // get the file attributes for file or directory
                        FileAttributes attr = File.GetAttributes(input);

                        if (attr.HasFlag(FileAttributes.Directory))
                        {
                            Directory.Delete(input);
                            Console.WriteLine("Directory " + input + " deleted!");
                        }
                        else
                        {
                            File.Delete(input);
                            Console.WriteLine("File " + input + " deleted!");
                        }
                    }
                    catch
                    {
                        FileSystem.ErrorWriteLine("Directory/file " + input + " dose not exist!");
                    }
                }
                else
                {
                    try
                    {
                        // get the file attributes for file or directory
                        FileAttributes attr = File.GetAttributes(newlocation + @"\" + input);

                        if (attr.HasFlag(FileAttributes.Directory))
                        {
                            Directory.Delete(newlocation + @"\" + input);
                            Console.WriteLine("Directory " + newlocation + @"\" + input + " deleted!");
                        }
                        else
                        {
                            File.Delete(newlocation + @"\" + input);
                            Console.WriteLine("File " + newlocation + @"\" + input + " deleted!");
                        }
                    }
                    catch
                    {
                        FileSystem.ErrorWriteLine("Directory/file " + newlocation + @"\" + input + " dose not exist!");
                    }
                }
            }
            catch
            {
                FileSystem.ErrorWriteLine("You must type the file/directory name!");
            }
        }
    }
}
