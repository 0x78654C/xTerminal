using Core;
using System;
using System.IO;

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
                string dlocation = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory);;
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
                        FileSystem.ErrorWriteLine("File " + FileName + " dose not exist!");
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
                        FileSystem.ErrorWriteLine("File " + dlocation + @"\" + FileName + " dose not exist!");
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
