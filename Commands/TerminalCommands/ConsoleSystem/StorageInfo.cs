using Core;
using System;
using System.Runtime.Versioning;
using ping = Core.NetWork;
using Wmi = Core.Hardware.WMIDetails;
namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class StorageInfo : ITerminalCommand
    {
        /// <summary>
        /// Check Storeage device information (HDD,USB .. etc) localy or remote.
        /// </summary>

        public string Name => "sinfo";
        private static string[] s_itemNames = { "Model", "SerialNumber", "Size", "MediaType" };

        public void Execute(string arg)
        {
            try
            {
                if (arg.Split(' ')[1] == "-r")
                {
                    Console.Write("Type remote PC name or IP: ");
                    string pc = Console.ReadLine();
                    if (!ping.PingHost(pc))
                    {
                        FileSystem.ErrorWriteLine($"{pc} is offline!");
                        return;
                    }
                    string wmiDetails = Wmi.GetWMIDetails("SELECT * FROM Win32_DiskDrive", s_itemNames, @"\\" + pc + @"\root\cimv2");
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                        GlobalVariables.pipeCmdOutput = Wmi.SizeConvert(wmiDetails, false);
                    else
                        Console.WriteLine(Wmi.SizeConvert(wmiDetails, false));
                }
                else if (arg == $"{Name} -h")
                {
                    Console.WriteLine(HelpCommand());
                }
            }
            catch
            {
                string wmiDetails = Wmi.GetWMIDetails("SELECT * FROM Win32_DiskDrive", s_itemNames, @"\\.\root\cimv2");
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                    GlobalVariables.pipeCmdOutput = Wmi.SizeConvert(wmiDetails, false);
                else
                    Console.WriteLine(Wmi.SizeConvert(wmiDetails, false));
            }
        }
        private static string HelpCommand()
        {
            string help = @"Storage devices info grabber commands list:
  sinfo      : Displays Storage devices information.
  sinfo -r   : Displays Storage devices information on a remote pc.
  sinfo -h   : Displays this message.";
            return help;
        }
    }
}
