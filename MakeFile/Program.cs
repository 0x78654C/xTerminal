using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MakeFile
{
    /*Make file*/
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter name for new file:");
            string input = Console.ReadLine();              // geting  input        
            string newlocation = File.ReadAllText(@".\Data\curDir.ini"); //get the new location
            string locinput = newlocation + @"\" + input; //new location+input
            if (input.Contains(":") && input.Contains(@"\"))
            {
                string cmdCode = @"@echo off" +
                    "cls &&"+
" echo. &&" +
"echo To save press CTRL+Z then press enter &&" +
"echo. &&"+
"echo Make sure to include extension in file name &&"+
"set /p name=File Name: &&"+
"copy con %"+ input+ "% &&"+
"if exist %" + input + "% copy %" + input + "% + con";
                var process = new Process();
                process.StartInfo = new ProcessStartInfo("cmd.exe")
                {
                    UseShellExecute = false,
                    Arguments = cmdCode

                };
            }
            else
            {

                string cmdCode = @"/c echo. &&" +
                    "cls &&"+
"echo To save press CTRL+Z then press enter &&" +
"echo. &&" +
"echo Make sure to include extension in file name &&" +
"set /p name=File Name: &&" +
"copy con %" + locinput + "% &&" +
"if exist %" + locinput + "% copy %" + locinput + "% + con";
                var process = new Process();
                process.StartInfo = new ProcessStartInfo("cmd.exe")
                {
                    UseShellExecute = false,
                    Arguments = cmdCode

                };
            }
        }
    }
}
