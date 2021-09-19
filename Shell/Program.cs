using Core;
using System;
using System.IO;

namespace Shell
{

    class Program
    {
        static void Main(string[] args)
        {
            RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory, @"C:\"); // write root path of c
            Console.Title = GlobalVariables.terminalTitle;//setting up the new title
            var shell = new Shell();
            shell.Run();//Running the shell
        }
    }

}
