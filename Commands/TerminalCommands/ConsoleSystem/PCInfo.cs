using Core;
using System;
using System.Collections.Generic;
using System.IO;
using wmi = Core.Hardware.WMIDetails;

namespace Commands.TerminalCommands.ConsoleSystem
{
    public class PCInfo : ITerminalCommand
    {
        /*
         Display System Information
         */

        public string Name => "pcinfo";
        public void Execute(string args)
        {
            MachineInfo();
        }

        // WMI class detail grab and ouput.
        private void MachineInfo()
        {
            string pcInfo = wmi.GetWMIDetails("SELECT * FROM Win32_OperatingSystem");
            string gpuInfo = wmi.GetWMIDetails("SELECT * FROM Win32_VideoController");
            string modelInfo = wmi.GetWMIDetails("SELECT * FROM Win32_ComputerSystem");
            string coresInfo = wmi.GetWMIDetails("SELECT * FROM Win32_Processor");
            Console.WriteLine("\n----------------------System Info---------------------\n");
            FileSystem.ColorConsoleText(ConsoleColor.Green, "User logged");
            Console.WriteLine($": {GlobalVariables.accountName }");
            FileSystem.ColorConsoleText(ConsoleColor.Green, "Machine Name");
            Console.WriteLine($": {GlobalVariables.computerName }");
            GetMachineModel(modelInfo);
            Console.WriteLine("\n--------------------------OS--------------------------\n");
            GetOSInfo(pcInfo);
            Console.WriteLine("\n-----------------------Hardware-----------------------\n");
            GetProcesorInfo(modelInfo,coresInfo);
            GetRAMInfo();
            GetGPUInfo(gpuInfo);
            Console.WriteLine("");
        }

        /// <summary>
        /// Grab machine manufacturer and model info from WMI.
        /// </summary>
        /// <param name="pcInfo">WMI data.</param>
        private void GetMachineModel(string pcInfo)
        {
            List<string> machineParams = new List<string>() { "Manufacturer", "Model" };
            using (var sRead = new StringReader(pcInfo))
            {
                string lineOS;
                while ((lineOS = sRead.ReadLine()) != null)
                {
                    foreach (var param in machineParams)
                    {
                        if (lineOS.StartsWith(param))
                        {
                            string outParam = lineOS.Split(':')[1];
                            FileSystem.ColorConsoleText(ConsoleColor.Green, $"{param}");
                            Console.WriteLine($": {outParam }");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Grab Operating Sytem information from WMI output.
        /// </summary>
        /// <param name="pcInfo">WMI Data</param>
        private void GetOSInfo(string pcInfo)
        {
            List<string> osParams = new List<string>() { "BuildNumber", "Caption", "OSArchitecture", "Version" };
            using (var sRead = new StringReader(pcInfo))
            {
                string lineOS;
                while ((lineOS = sRead.ReadLine()) != null)
                {
                    foreach (var param in osParams)
                    {
                        if (lineOS.StartsWith(param))
                        {
                            string outParam = lineOS.Split(':')[1];
                            FileSystem.ColorConsoleText(ConsoleColor.Green, $"{param}");
                            Console.WriteLine($": {outParam }");
                        }
                    }
                }
            }
        }

        // Grap processor information from registry.
        private void GetProcesorInfo(string cpuInfoWMI, string coresInfo)
        {
            string procInfo = RegistryManagement.regKey_ReadMachine(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString");
            FileSystem.ColorConsoleText(ConsoleColor.Green, "CPU");
            Console.WriteLine($": {procInfo}");
            using (var sRead = new StringReader(cpuInfoWMI))
            {
                string lineCPUCount;
                while ((lineCPUCount = sRead.ReadLine()) != null)
                {
                    if (lineCPUCount.StartsWith("NumberOfProcessors"))
                    {
                        string outParam = "";
                        outParam += lineCPUCount.Split(':')[1];
                        FileSystem.ColorConsoleText(ConsoleColor.Green, $"Physical CPU's");
                        Console.WriteLine($": {outParam}");
                    }
                }
            }
            using (var sRead = new StringReader(coresInfo))
            {
                string lineCoresCount;
                while ((lineCoresCount = sRead.ReadLine()) != null)
                {
                    if (lineCoresCount.StartsWith("NumberOfCores"))
                    {
                        string outParam = "";
                        outParam += lineCoresCount.Split(':')[1];
                        FileSystem.ColorConsoleText(ConsoleColor.Green, $"CPU(s) Cores");
                        Console.WriteLine($": {outParam}");
                    }
                }
            }
            FileSystem.ColorConsoleText(ConsoleColor.Green, $"Logical CPU's");
            Console.WriteLine($": {Environment.ProcessorCount}");
        }

        // Grab RAM information.
        private void GetRAMInfo()
        {
            var ram = new Microsoft.VisualBasic.Devices.ComputerInfo();
            FileSystem.ColorConsoleText(ConsoleColor.Green, "RAM");
            string ramAvailable = FileSystem.GetSize(ram.AvailablePhysicalMemory.ToString(), false);
            string ramTotal = FileSystem.GetSize(ram.TotalPhysicalMemory.ToString(), false);
            Console.WriteLine($": { ramAvailable} Available / {ramTotal} Total");
        }

        /// <summary>
        /// Grab GPU information from WMI output.
        /// </summary>
        /// <param name="gpuInfoWMI">WMI Data.</param>
        private void GetGPUInfo(string gpuInfoWMI)
        {
            using (var sRead = new StringReader(gpuInfoWMI))
            {
                string lineGPU;
                int countGPU = 0;
                while ((lineGPU = sRead.ReadLine()) != null)
                {
                    if (lineGPU.StartsWith("Description"))
                    {
                        countGPU++;
                        string outParam = "";
                        outParam += lineGPU.Split(':')[1];
                        FileSystem.ColorConsoleText(ConsoleColor.Green, $"GPU{countGPU}");
                        Console.WriteLine($": {outParam}");
                    }
                }
            }
        }
    }
}
