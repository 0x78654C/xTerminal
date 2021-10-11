using Core;
using System;
using System.IO;

namespace Commands.TerminalCommands.DirFiles
{
    public class MakeDirectory : ITerminalCommand
    {
        public string Name => "mkdir";
        public void Execute(string arg)
        {
            try
            {
                string input = arg.Split(' ')[1];              // geting  input        
                string newlocation = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory); ; //get the new location
                string locinput = newlocation + input; //new location+input
                if (input.Contains(":") && input.Contains(@"\"))
                {
                    try
                    {
                        Directory.CreateDirectory(input);
                        Console.WriteLine("Directory " + input + " is created!");
                    }
                    catch (Exception)
                    {
                        FileSystem.ErrorWriteLine("Something went wrong. Check path maybe!");
                    }
                }
                else
                {
                    try
                    {
                        Directory.CreateDirectory(locinput);
                        Console.WriteLine("Directory " + locinput + " is created!");
                    }
                    catch (Exception)
                    {
                        FileSystem.ErrorWriteLine("Something went wrong. Check path maybe!");
                    }
                }
            }
            catch
            {
                FileSystem.ErrorWriteLine("You must type the directory name!");
            }
        }
    }
}
