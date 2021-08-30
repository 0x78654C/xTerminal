using Core;
using System;
using System.IO;

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

            File.WriteAllText(GlobalVariables.currentLocation, GlobalVariables.rootPath);
            Console.Title = "xTerminal v1.0";//setting up the new title
            var shell = new Shell();
            shell.Run();//Running the shell

        }
    }

}
