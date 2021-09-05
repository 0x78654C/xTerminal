using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management;
using System.Management.Automation;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;
using Wmi = Core.Hardware.WMIDetails;
using ping = Core.NetWork;
using System.IO;

namespace StorageDeviceCheck
{
    class Program
    {
        private static string[] s_itemNames = { "Model", "SerialNumber", "Size", "MediaType" };
        /// <summary>
        /// Check Storeage device information (HDD,USB .. etc) localy or remote.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            try
            {
                if (args[0] == "-r")
                {
                    Console.Write("Type remote PC name or IP: ");
                    string pc = Console.ReadLine();
                    if (!ping.PingHost(pc))
                    {
                        Console.WriteLine($"{pc} is offline!");
                        return;
                    }
                    string wmiDetails = Wmi.GetWMIDetails("SELECT * FROM Win32_DiskDrive", s_itemNames, @"\\" + pc + @"\root\cimv2");
                    Console.WriteLine(SizeConvert(wmiDetails));
                }
                else if (args[0] == "-h")
                {
                    Console.WriteLine(HelpCommand());
                }
            }
            catch
            {
                string wmiDetails = Wmi.GetWMIDetails("SELECT * FROM Win32_DiskDrive", s_itemNames, @"\\.\root\cimv2");
                Console.WriteLine(SizeConvert(wmiDetails));
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

        /// <summary>
        /// Convert bytes to GB directly on WMI output
        /// </summary>
        /// <param name="data"> Input WMI data with Size(capacaty) parameter.</param>
        /// <returns>string</returns>
        private static string SizeConvert(string data)
        {
            string wmiOut = string.Empty;
            foreach (var wmiData in data.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (wmiData.Contains("Size"))
                {
                    double sizeDeviced = Convert.ToDouble(wmiData.Split(' ')[1]);
                    for (int i = 0; i < 3; i++)
                    {
                        sizeDeviced /= 1024;
                    }
                    sizeDeviced = Math.Round(sizeDeviced, 2);
                    wmiOut += $"Size: {sizeDeviced} GB" + Environment.NewLine;
                }
                wmiOut += wmiData + Environment.NewLine;
            }
            return wmiOut;
        }
    }
}
