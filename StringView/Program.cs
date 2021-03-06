﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;

namespace StringView
{
    /*Cat function */
    class Program
    {
        static void Main(string[] args)
        {

            string cDir = File.ReadAllText(FileSystem.CurrentLocation);
         
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
                        Console.WriteLine("File " + cDir + @"\" + input + " dose not exist!");
                    }
                }
            }
            catch 
            {
                Console.WriteLine("You must type the file name!");
            }
        }      
    }
}
