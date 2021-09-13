using Core;
using System;
using System.IO;

namespace NKFile
{
    class Program
    {
        static void Main(string[] args)
        {
            string CLocation = File.ReadAllText(GlobalVariables.currentLocation);
            string file;
            try
            {
                file = args[0];
                if (Directory.Exists(CLocation))
                {
                    File.Create(CLocation + @"\" + file);
                }
                else
                {
                    Console.WriteLine("Directory dose not exist!");
                }
            }
            catch
            {
                FileSystem.ErrorWriteLine("You must type the file name!");
            }
        }
    }
}
