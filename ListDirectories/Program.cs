using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;

namespace ListDirectories
{
    /*List files and directories*/
    class Program
    {
        static void Main(string[] args)
        {
            string cDir = File.ReadAllText(FileSystem.CurrentLocation);
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
