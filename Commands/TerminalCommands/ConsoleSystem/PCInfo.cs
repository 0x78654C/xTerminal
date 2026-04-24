using Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using wmi = Core.Hardware.WMIDetails;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class PCInfo : ITerminalCommand
    {
        public string Name => "pcinfo";
        private const int s_keyPadding = 12;
        private static readonly string[] s_xTerminalLogo = new[]
        {
            @" __  __ _____                   _             _ ",
            @" \ \/ /|_   _|__ _ __ _ __ ___ (_)_ __   __ _| |",
            @"  \  /   | |/ _ \ '__| '_ ` _ \| | '_ \ / _` | |",
            @"  /  \   | |  __/ |  | | | | | | | | | | (_| | |",
            @" /_/\_\  |_|\___|_|  |_| |_| |_|_|_| |_|\__,_|_|"
        };

        public void Execute(string args)
        {
            MachineInfo();
        }

        // WMI class detail grab and output in a Linux-like layout.
        private void MachineInfo()
        {
            string osInfo = wmi.GetWMIDetails("SELECT * FROM Win32_OperatingSystem");
            string gpuInfo = wmi.GetWMIDetails("SELECT * FROM Win32_VideoController");
            string machineInfo = wmi.GetWMIDetails("SELECT * FROM Win32_ComputerSystem");
            string cpuInfo = wmi.GetWMIDetails("SELECT * FROM Win32_Processor");

            string manufacturer = GetFirstWmiValue(machineInfo, "Manufacturer");
            string model = GetFirstWmiValue(machineInfo, "Model");
            string osCaption = GetFirstWmiValue(osInfo, "Caption");
            string osVersion = GetFirstWmiValue(osInfo, "Version");
            string osBuild = GetFirstWmiValue(osInfo, "BuildNumber");
            string osArch = GetFirstWmiValue(osInfo, "OSArchitecture");
            string physicalCpuCount = GetFirstWmiValue(machineInfo, "NumberOfProcessors");
            string coreCount = GetFirstWmiValue(cpuInfo, "NumberOfCores");
            string cpuName = RegistryManagement.regKey_ReadMachine(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString");
            string kernel = BuildKernelLabel(osVersion, osBuild);

            var ram = new Microsoft.VisualBasic.Devices.ComputerInfo();
            string totalRam = FileSystem.GetSize(ram.TotalPhysicalMemory.ToString(), false);
            string usedRam = FileSystem.GetSize((ram.TotalPhysicalMemory - ram.AvailablePhysicalMemory).ToString(), false);

            string userHost = $"{GlobalVariables.accountName}@{GlobalVariables.computerName}";

            WriteRawLine(string.Empty);
            WriteLogoHeader(userHost);
            WriteRawLine(string.Empty);

            WriteSection("system");
            WriteEntry("user", GlobalVariables.accountName);
            WriteEntry("host", GlobalVariables.computerName);
            WriteEntry("vendor", manufacturer);
            WriteEntry("model", model);

            WriteSection("os");
            WriteEntry("os", osCaption);
            WriteEntry("kernel", kernel);
            WriteEntry("arch", osArch);

            WriteSection("hardware");
            WriteEntry("cpu", cpuName);
            WriteEntry("cpu(s)", Environment.ProcessorCount.ToString());
            WriteEntry("topology", $"{NormalizeValue(physicalCpuCount)} socket(s), {NormalizeValue(coreCount)} core(s)");
            WriteEntry("memory", $"{usedRam} used / {totalRam} total");

            List<string> gpuList = GetWmiValues(gpuInfo, "Description");
            if (gpuList.Count == 0)
            {
                WriteEntry("gpu", "N/A");
            }
            else
            {
                for (int i = 0; i < gpuList.Count; i++)
                {
                    string key = i == 0 ? "gpu" : $"gpu{i + 1}";
                    WriteEntry(key, gpuList[i]);
                }
            }

            WriteSection("storage");
            WriteDrives();
            WriteRawLine(string.Empty);
        }

        private void WriteLogoHeader(string userHost)
        {
            string separator = new string('-', userHost.Length);
            int logoWidth = 0;
            foreach (string logoLine in s_xTerminalLogo)
            {
                if (logoLine.Length > logoWidth)
                {
                    logoWidth = logoLine.Length;
                }
            }

            int totalLines = Math.Max(s_xTerminalLogo.Length, 2);
            for (int i = 0; i < totalLines; i++)
            {
                string logoLine = i < s_xTerminalLogo.Length ? s_xTerminalLogo[i] : string.Empty;
                string infoLine = i == 0 ? userHost : i == 1 ? separator : string.Empty;

                WriteLogoLine(logoLine, "", logoWidth);
            }
        }

        private void WriteLogoLine(string logoLine, string infoLine, int logoWidth)
        {
            bool hasInfoLine = !string.IsNullOrEmpty(infoLine);
            string paddedLogo = logoLine.PadRight(logoWidth);

            if (ShouldPipe())
            {
                if (hasInfoLine)
                {
                    WriteRawLine($"{paddedLogo}  {infoLine}".TrimEnd());
                }
                else
                {
                    WriteRawLine(logoLine);
                }

                return;
            }

            if (hasInfoLine)
            {
                FileSystem.ColorConsoleText(ConsoleColor.Cyan, paddedLogo);
                Console.Write("  ");
                Console.WriteLine(infoLine);
                return;
            }

            FileSystem.ColorConsoleTextLine(ConsoleColor.Cyan, logoLine);
        }

        private void WriteDrives()
        {
            bool hasDrives = false;
            try
            {
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                foreach (var drive in allDrives)
                {
                    if (!drive.IsReady)
                    {
                        continue;
                    }

                    hasDrives = true;
                    string totalSize = wmi.SizeConvert($"Size {drive.TotalSize}", true);
                    string freeSize = wmi.SizeConvert($"Size {drive.AvailableFreeSpace}", true);
                    string mount = drive.Name.TrimEnd('\\');

                    WriteEntry($"disk {mount}", $"{freeSize} free / {totalSize} total ({drive.DriveType})");
                }
            }
            catch
            {
                hasDrives = false;
            }

            if (!hasDrives)
            {
                WriteEntry("disk", "N/A");
            }
        }

        private static string BuildKernelLabel(string version, string buildNumber)
        {
            string cleanVersion = NormalizeValue(version);
            string cleanBuild = NormalizeValue(buildNumber);
            if (cleanVersion == "N/A" && cleanBuild == "N/A")
            {
                return "N/A";
            }

            if (cleanVersion == "N/A" || cleanBuild == "N/A")
            {
                return cleanVersion == "N/A" ? cleanBuild : cleanVersion;
            }

            return $"{cleanVersion}.{cleanBuild}";
        }

        private static string GetFirstWmiValue(string wmiData, string key)
        {
            if (string.IsNullOrWhiteSpace(wmiData))
            {
                return string.Empty;
            }

            using (var reader = new StringReader(wmiData))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string[] split = line.Split(new[] { ':' }, 2);
                    if (split.Length < 2)
                    {
                        continue;
                    }

                    return split[1].Trim();
                }
            }

            return string.Empty;
        }

        private static List<string> GetWmiValues(string wmiData, string key)
        {
            var values = new List<string>();
            if (string.IsNullOrWhiteSpace(wmiData))
            {
                return values;
            }

            using (var reader = new StringReader(wmiData))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string[] split = line.Split(new[] { ':' }, 2);
                    if (split.Length < 2)
                    {
                        continue;
                    }

                    string value = split[1].Trim();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        values.Add(value);
                    }
                }
            }

            return values;
        }

        private void WriteSection(string section)
        {
            WriteRawLine($"[{section}]");
        }

        private void WriteEntry(string key, string value)
        {
            string normalizedValue = NormalizeValue(value);
            if (ShouldPipe())
            {
                GlobalVariables.pipeCmdOutput += $"{key.PadRight(s_keyPadding)}: {normalizedValue}{Environment.NewLine}";
            }
            else
            {
                FileSystem.ColorConsoleText(ConsoleColor.Green, $"{key.PadRight(s_keyPadding)}:");
                Console.WriteLine($" {normalizedValue}");
            }
        }

        private void WriteRawLine(string line)
        {
            if (ShouldPipe())
            {
                GlobalVariables.pipeCmdOutput += line + Environment.NewLine;
            }
            else
            {
                Console.WriteLine(line);
            }
        }

        private static string NormalizeValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "N/A";
            }

            return value.Trim();
        }

        private static bool ShouldPipe()
        {
            return GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0;
        }
    }
}
