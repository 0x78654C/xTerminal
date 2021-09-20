using Core;
using System;
using System.IO;

namespace Shell
{

    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = GlobalVariables.terminalTitle;//setting up the new title
            var shell = new Shell();
            shell.Run();//Running the shell
        }
    }

}
