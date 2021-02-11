using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;

namespace FRename
{
    class Program
    {
        /*File rename*/
        static void Main(string[] args)
        {
            try
            {

                //reading current location(for test no, after i make dynamic)
                string dlocation = File.ReadAllText(FileSystem.CurrentLocation);
                string cLocation = Directory.GetCurrentDirectory();
                //we grab the file names for source and destination
                string FileName = args[0];
                string NewName = args[1];

                //we check if is a diference between the too directories and see if is the current one set
                if (dlocation == cLocation || FileName.Contains(@"\"))
                {
                    //we check if file exists
                    if (File.Exists(FileName))
                    {
                        File.Move(FileName, NewName);
                    }
                    else
                    {
                        Console.WriteLine("File " + FileName + " dose not exist!");
                    }
                }
                else
                {
                    //we check if file exists
                    if (File.Exists(dlocation + @"\" + FileName))
                    {
                        File.Move(dlocation + @"\" + FileName, dlocation + @"\" + NewName);
                    }
                    else
                    {
                        Console.WriteLine("File " + dlocation + @"\" + FileName + " dose not exist!");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.ToString() + Environment.NewLine);
            }
        }
    }
}
