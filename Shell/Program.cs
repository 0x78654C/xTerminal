using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shell
{
    
    class Program
    {
        static void Main(string[] args)
        {
            //Checking data directory
            if (!Directory.Exists(Directory.GetCurrentDirectory() + @"\Data"))
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Data");
            }
            //-------------------------------

            File.WriteAllText(@"./Data/curDir.ini", Directory.GetCurrentDirectory());
            Console.Title="xTerminal v1.0";//setting up the new title
            var shell = new Shell();
            shell.Run();//Running the shell

        }
    }

}
