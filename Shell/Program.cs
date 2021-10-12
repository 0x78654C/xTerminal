using Core;
using System;

namespace Shell
{

    class Program
    {
        static void Main(string[] args)
        {
            // confgure console
            Console.OutputEncoding = System.Text.Encoding.UTF8;//set utf8 encoding (for support Russian letters)
            Console.Title = GlobalVariables.terminalTitle;//setting up the new title
            
            var shell = new Shell();
            shell.Run();//Running the shell
        }
    }

}
