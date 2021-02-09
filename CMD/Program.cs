using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMD
{

    /*stating cmd with args*/
    class Program
    {
        static void Main(string[] args)
        {
            string input = Console.ReadLine();
            string[] parse = input.Split(' ');
            string i1 = parse[0];
            string i2== parse[1];
            if (input == "reboot")
            {
                var process = new Process();
                process.StartInfo = new ProcessStartInfo("cmd.exe")
                {
                    UseShellExecute = false,
                    Arguments = "/c shutdown /r /f /t 1"

                };

                process.Start();
                process.WaitForExit();

            }
        }
    }
}
