using Core;
using System;
using System.Runtime.Versioning;
using Ping = Core.Network.NetWork;
using Wmi = Core.Hardware.WMIDetails;

namespace Commands.TerminalCommands.ConsoleSystem
{
    /*
     Bios information display for local or remote user using WMI.
     */
    [SupportedOSPlatform("Windows")]
    public class BiosInfo : ITerminalCommand
    {
        public string Name => "bios";
        public void Execute(string arg)
        {
            try
            {
                string input = arg.Split(' ')[1];
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
                    if (!Ping.PingHost(pc))
                    {
                        Console.WriteLine($"{pc} is offline!");
                        return;
                    }
                    if (GlobalVariables.isPipeCommand)
                    {
                        if (GlobalVariables.pipeCmdCount > 0)
                            GlobalVariables.pipeCmdOutput = Wmi.GetWMIDetails("SELECT * FROM Win32_BIOS", @"\\" + pc + @"\root\cimv2");
                        else
                            Console.WriteLine(Wmi.GetWMIDetails("SELECT * FROM Win32_BIOS", @"\\" + pc + @"\root\cimv2"));
                    }
                    else
                        Console.WriteLine(Wmi.GetWMIDetails("SELECT * FROM Win32_BIOS", @"\\" + pc + @"\root\cimv2"));
                    break;
                case "-h":
                    Console.WriteLine(HelpCommand());
                    break;
                default:
                    if (GlobalVariables.isPipeCommand)
                    {
                        if(GlobalVariables.pipeCmdCount > 0)
                            GlobalVariables.pipeCmdOutput = Wmi.GetWMIDetails("SELECT * FROM Win32_BIOS", @"\\.\root\cimv2");
                        else
                            Console.WriteLine(Wmi.GetWMIDetails("SELECT * FROM Win32_BIOS", @"\\.\root\cimv2"));
                    }
                    else
                        Console.WriteLine(Wmi.GetWMIDetails("SELECT * FROM Win32_BIOS", @"\\.\root\cimv2"));
                    break;
            }
        }

        // Display help command.
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
