using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NKFile
{
    class Program
    {
        static void Main(string[] args)
        {
            string CLocation = File.ReadAllText(@".\Data\curDir.ini");
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
                Console.WriteLine("File was not created. Check parameters!");
            }
        }
    }
}
