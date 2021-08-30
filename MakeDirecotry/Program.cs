﻿using Core;
using System;
using System.IO;

namespace MakeDirecotry
{
    /*Creates directory*/
    class Program
    {
        static void Main(string[] args)
        {
            try
            {

                string input = args[0];              // geting  input        
                string newlocation = File.ReadAllText(GlobalVariables.currentLocation); //get the new location
                string locinput = newlocation + @"\" + input; //new location+input
                if (input.Contains(":") && input.Contains(@"\"))
                {
                    try
                    {
                        Directory.CreateDirectory(input);
                        Console.WriteLine("Directory " + input + " is created!");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Something went wrong. Check path maybe!");
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
                        Console.WriteLine("Something went wrong. Check path maybe!");
                    }
                }
            }
            catch
            {
                Console.WriteLine("You must type the directory name!");
            }
        }
    }
}
