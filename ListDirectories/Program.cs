using Core;
using System;
using System.IO;

namespace ListDirectories
{
    /*List files and directories*/
    class Program
    {
        static void Main(string[] args)
        {
            string cDir = File.ReadAllText(GlobalVariables.currentLocation);
            if (Directory.Exists(cDir))
            {
                var files = Directory.GetFiles(cDir);
                var directories = Directory.GetDirectories(cDir);

                foreach (var dir in directories)
                {
                    Console.WriteLine(dir);
                }
                foreach (var file in files)
                {
                    Console.WriteLine(file);
                }
            }
            else
            {
                Console.WriteLine($"Directory '{cDir}' dose not exist!");
            }
        }
    }
}
