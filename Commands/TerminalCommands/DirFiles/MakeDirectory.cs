﻿using Core;
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
                int argLength = arg.Length - 6;

                string input = arg.Substring(6, argLength);
                string newlocation = File.ReadAllText(GlobalVariables.currentDirectory); ; // Get the new location
                string locinput = newlocation + input; // New location+input
                if (input.Contains(":") && input.Contains(@"\"))
                {
                    try
                    {
                        Directory.CreateDirectory(input);
                        Console.WriteLine($"Directory {input} is created!");
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
                        Console.WriteLine($"Directory {locinput} is created!");
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
