using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;

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
                string newlocation = File.ReadAllText(FileSystem.CurrentLocation); //get the new location

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
                    catch (Exception)
                    {
                        Console.WriteLine("Directory/file " + input + " dose not exist!");
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
                    catch (Exception)
                    {
                        Console.WriteLine("Directory/file " + newlocation + @"\" + input + " dose not exist!");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.ToString());
            }
        }
    }
}
