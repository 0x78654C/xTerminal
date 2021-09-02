using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using bios = Core.Hardware.PowerShell;
using ping = Core.NetWork;
using wmi = Core.Hardware.WMIDetails;

namespace BiosInfo
{
    class Program
    {
        /// <summary>
        /// Display information about BIOS.
        /// It can be used on remote computers too with the right privilage.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            try
            {
                string input = args[0];
                BiosInformation(input);
            }
            catch
            {
                BiosInformation("");
            }
        }

        /// <summary>
        /// Display Bios information or configuration
        /// </summary>
        /// <param name="commandType"></param>
        private static void BiosInformation(string commandType)
        {
            string pc;

            switch (commandType)
            {
                case "-r":
                    Console.Write("Type remote PC name or IP: ");
                    pc = Console.ReadLine();
                    if (!ping.PingHost(pc))
                    {
                        Console.WriteLine($"{pc} is offline!");
                        return;
                    }
                    Console.WriteLine(wmi.GetWMIDetails("SELECT * FROM Win32_BIOS", @"\\"+pc+@"\root\cimv2"));
                    break;
                case "-h":
                    Console.WriteLine(HelpCommand());
                    break;
                default:
                    Console.WriteLine(wmi.GetWMIDetails("SELECT * FROM Win32_BIOS", @"\\.\root\cimv2"));
                    break;
            }
        }

        //Display help command
        private static string HelpCommand()
        {
            string help = @"Bios info grabber commands list:
  bios      : Displays BIOS information.
  bios -r   : Displays BIOS information on a remote pc.
  bios -h   : Displays this message.";
            return help;
        }
    }
}
