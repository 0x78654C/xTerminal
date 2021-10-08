using Core;
using System;
using System.IO;

namespace Commands.TerminalCommands.DirFiles
{
    public class Delete : ITerminalCommand
    {
        public string Name => "del";
        public void Execute(string arg)
        {

            try
            {
                string input = arg.Split(' ')[1];              // geting location input        
                string newlocation = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory); ; //get the new location

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
                    catch(Exception e)
                    {
                        FileSystem.ErrorWriteLine(e.Message);
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
                    catch(Exception e)
                    {
                        FileSystem.ErrorWriteLine(e.Message);
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
