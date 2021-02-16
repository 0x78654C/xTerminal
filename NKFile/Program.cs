using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;

namespace NKFile
{
    class Program
    {
        static void Main(string[] args)
        {
            string CLocation = File.ReadAllText(FileSystem.CurrentLocation);
            string file;
            try
            {
                file = args[0];
                if (Directory.Exists(CLocation))
                {
                    File.Create(CLocation+@"\"+ file);
                }
                else
                {
                    Console.WriteLine("Directory dose not exist!");
                }
            }
            catch
            {
                Console.WriteLine("You must type the file name!");
            }
        }
    }
}
