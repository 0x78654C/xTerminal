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

namespace StorageDeviceCheck
{
    class Program
    {
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
                    Console.WriteLine(Wmi.GetWMIDetails("SELECT * FROM Win32_DiskDrive", @"\\"+pc+@"\root\cimv2"));
                }
            }
            catch
            {
                Console.WriteLine(Wmi.GetWMIDetails("SELECT * FROM Win32_DiskDrive", @"\\.\root\cimv2"));
            }
        }

        /* for future use
         * 
        private static string GetStorageInfo()
        {
            string infOutput=string.Empty;
           
            ManagementObjectSearcher moSearcher = new
    ManagementObjectSearcher("SELECT * FROM win32_diskdrive");

            foreach (ManagementObject wmi_HD in moSearcher.Get())
            {
                infOutput += "Model: "+wmi_HD["Model"].ToString()+Environment.NewLine;
                infOutput += "Serial Number: " + wmi_HD["SerialNumber"].ToString() + Environment.NewLine;
                infOutput += "Size: " + wmi_HD["Size"].ToString() + Environment.NewLine;
                infOutput += "Media Type: " + wmi_HD["MediaType"].ToString() + Environment.NewLine;
                infOutput += Environment.NewLine;
            }
            return infOutput;
        }
        */
    }
}
