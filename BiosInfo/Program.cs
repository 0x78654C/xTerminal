using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using bios = Core.Hardware.PowerShell;
using ping = Core.NetWork;

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
            catch(Exception e)
            {
                Console.WriteLine("Type bios -h for help.");
            }
        }

        /// <summary>
        /// Display Bios information or configuration
        /// </summary>
        /// <param name="commandType"></param>
        private static void BiosInformation(string commandType)
        {
            string biosInfo = "get-wmiobject -class win32_bios";
            string sytemBiosInfo = "get-wmiobject -class win32_systembios";
            string pc;

            switch (commandType)
            {
                case "-s":
                    Console.WriteLine(bios.RunPowerShellScript(biosInfo));
                    break;
                case "-l":
                    Console.WriteLine(bios.RunPowerShellScript(sytemBiosInfo));
                    break;
                case "-rs":
                    Console.Write("Type remote PC name or IP: ");
                    pc = Console.ReadLine();
                    if (!ping.PingHost(pc))
                    {
                        Console.WriteLine($"{pc} is offline!");
                        return;
                    }
                    string biosInfoRemote = $"get-wmiobject -class win32_bios -computername {pc}";
                    Console.WriteLine(bios.RunPowerShellScript(biosInfoRemote));
                    break;
                case "-ls":
                    Console.Write("Type remote PC name or IP: ");
                    pc = Console.ReadLine();
                    if (!ping.PingHost(pc))
                    {
                        Console.WriteLine($"{pc} is offline!");
                        return;
                    }
                    string biosSytemInfoRemote = $"get-wmiobject -class win32_systembios -computername {pc}";
                    Console.WriteLine(bios.RunPowerShellScript(biosSytemInfoRemote));
                    break;
                case "-h":
                    Console.WriteLine(HelpCommand());
                    break;
                default:
                    Console.WriteLine("Wrong command. Type bios -h for help.");
                    break;
            }
        }

        //Display help command
        private static string HelpCommand()
        {
            string help = @"Bios info grabber commands list:
  -s  : Displays BIOS information.
  -l  : Displays BIOS configuration.
  -rs : Displays BIOS information on a remote pc.
  -rl : Displays BIOS configration on a remote pc.
  -h  : Displays this message.";
            return help;
        }
    }
}
