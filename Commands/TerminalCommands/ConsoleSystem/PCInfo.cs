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
        private const int s_keyPadding = 14;
        private const int s_maxFrameWidth = 112;
        private const int s_meterWidth = 18;

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

        private void MachineInfo()
        {
            MachineSnapshot info = ReadMachineInfo();

            if (ShouldPipe())
            {
                WritePipeSnapshot(info);
                return;
            }

            WriteTechDashboard(info);
        }

        private static MachineSnapshot ReadMachineInfo()
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
            ulong totalPhysical = ram.TotalPhysicalMemory;
            ulong usedPhysical = totalPhysical - ram.AvailablePhysicalMemory;

            var snapshot = new MachineSnapshot
            {
                User = GlobalVariables.accountName,
                Host = GlobalVariables.computerName,
                ScanTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Manufacturer = manufacturer,
                Model = model,
                OsCaption = osCaption,
                Kernel = kernel,
                Architecture = osArch,
                CpuName = cpuName,
                LogicalProcessors = Environment.ProcessorCount.ToString(),
                Topology = $"{NormalizeValue(physicalCpuCount)} socket(s), {NormalizeValue(coreCount)} core(s)",
                MemoryUsed = FileSystem.GetSize(usedPhysical.ToString(), false),
                MemoryTotal = FileSystem.GetSize(totalPhysical.ToString(), false),
                MemoryUsedPercent = Percent(usedPhysical, totalPhysical),
                Gpus = GetWmiValues(gpuInfo, "Description"),
                Drives = GetDriveSnapshots()
            };

            if (snapshot.Gpus.Count == 0)
            {
                snapshot.Gpus.Add("N/A");
            }

            return snapshot;
        }

        private void WriteTechDashboard(MachineSnapshot info)
        {
            int width = GetFrameWidth();
            WriteRawLine(string.Empty);
            WriteRule(width, " XTERMINAL SYSTEM HUD ", ConsoleColor.Cyan);
            WriteLogoHeader(info, width);

            WriteSectionRule(width, "node");
            WriteMetricLine(width, "user", info.User);
            WriteMetricLine(width, "host", info.Host);
            WriteMetricLine(width, "vendor", info.Manufacturer);
            WriteMetricLine(width, "model", info.Model);

            WriteSectionRule(width, "operating system");
            WriteMetricLine(width, "os", info.OsCaption);
            WriteMetricLine(width, "kernel", info.Kernel);
            WriteMetricLine(width, "arch", info.Architecture);

            WriteSectionRule(width, "compute");
            WriteMetricLine(width, "cpu", info.CpuName);
            WriteMetricLine(width, "threads", info.LogicalProcessors);
            WriteMetricLine(width, "topology", info.Topology);
            WriteMeterLine(width, "memory", $"{info.MemoryUsed} used / {info.MemoryTotal} total", info.MemoryUsedPercent);

            WriteSectionRule(width, "graphics");
            for (int i = 0; i < info.Gpus.Count; i++)
            {
                string key = i == 0 ? "gpu" : $"gpu{i + 1}";
                WriteMetricLine(width, key, info.Gpus[i]);
            }

            WriteSectionRule(width, "storage");
            if (info.Drives.Count == 0)
            {
                WriteMetricLine(width, "disk", "N/A");
            }
            else
            {
                foreach (DriveSnapshot drive in info.Drives)
                {
                    WriteMeterLine(width, $"disk {drive.Mount}", $"{drive.Free} free / {drive.Total} total ({drive.Type})", drive.UsedPercent);
                }
            }

            WriteRule(width, string.Empty, ConsoleColor.DarkCyan);
            WriteRawLine(string.Empty);
        }

        private void WritePipeSnapshot(MachineSnapshot info)
        {
            WriteRawLine(string.Empty);
            WriteRawLine($"xTerminal pcinfo - {info.User}@{info.Host}");
            WriteRawLine($"scan          : {info.ScanTime}");
            WriteRawLine(string.Empty);

            WriteSection("system");
            WriteEntry("user", info.User);
            WriteEntry("host", info.Host);
            WriteEntry("vendor", info.Manufacturer);
            WriteEntry("model", info.Model);

            WriteSection("os");
            WriteEntry("os", info.OsCaption);
            WriteEntry("kernel", info.Kernel);
            WriteEntry("arch", info.Architecture);

            WriteSection("hardware");
            WriteEntry("cpu", info.CpuName);
            WriteEntry("threads", info.LogicalProcessors);
            WriteEntry("topology", info.Topology);
            WriteEntry("memory", $"{info.MemoryUsed} used / {info.MemoryTotal} total ({FormatPercent(info.MemoryUsedPercent)} used)");

            for (int i = 0; i < info.Gpus.Count; i++)
            {
                string key = i == 0 ? "gpu" : $"gpu{i + 1}";
                WriteEntry(key, info.Gpus[i]);
            }

            WriteSection("storage");
            if (info.Drives.Count == 0)
            {
                WriteEntry("disk", "N/A");
            }
            else
            {
                foreach (DriveSnapshot drive in info.Drives)
                {
                    WriteEntry($"disk {drive.Mount}", $"{drive.Free} free / {drive.Total} total ({drive.Type}, {FormatPercent(drive.UsedPercent)} used)");
                }
            }

            WriteRawLine(string.Empty);
        }

        private void WriteLogoHeader(MachineSnapshot info, int width)
        {
            int innerWidth = Math.Max(0, width - 4);
            int logoWidth = GetLogoWidth();
            string userHost = $"{NormalizeValue(info.User)}@{NormalizeValue(info.Host)}";
            string[] telemetryLines = new[]
            {
                $"NODE {userHost}",
                $"SCAN {info.ScanTime}",
                $"OS   {NormalizeValue(info.OsCaption)}",
                $"CPU  {NormalizeValue(info.CpuName)}",
                $"RAM  {FormatPercent(info.MemoryUsedPercent)} used"
            };

            if (innerWidth < logoWidth + 14)
            {
                WriteFrameLine(width, $"NODE {userHost}");
                WriteFrameLine(width, $"SCAN {info.ScanTime}");
                WriteFrameLine(width, $"OS   {NormalizeValue(info.OsCaption)}");
                return;
            }

            for (int i = 0; i < s_xTerminalLogo.Length; i++)
            {
                string logo = s_xTerminalLogo[i].PadRight(logoWidth);
                string telemetry = i < telemetryLines.Length ? telemetryLines[i] : string.Empty;
                int used = 0;

                Console.Write("| ");
                FileSystem.ColorConsoleText(ConsoleColor.Cyan, logo);
                used += logo.Length;

                if (used < innerWidth)
                {
                    Console.Write("  ");
                    used += 2;
                    string clippedTelemetry = Clip(telemetry, innerWidth - used);
                    FileSystem.ColorConsoleText(i == 0 ? ConsoleColor.White : ConsoleColor.Gray, clippedTelemetry);
                    used += clippedTelemetry.Length;
                }

                if (used < innerWidth)
                {
                    Console.Write(new string(' ', innerWidth - used));
                }

                Console.WriteLine(" |");
            }
        }

        private static List<DriveSnapshot> GetDriveSnapshots()
        {
            var drives = new List<DriveSnapshot>();
            try
            {
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                foreach (var drive in allDrives)
                {
                    if (!drive.IsReady)
                    {
                        continue;
                    }

                    long used = drive.TotalSize - drive.AvailableFreeSpace;
                    string mount = drive.Name.TrimEnd('\\');
                    drives.Add(new DriveSnapshot
                    {
                        Mount = mount,
                        Free = FileSystem.GetSize(drive.AvailableFreeSpace.ToString(), false),
                        Total = FileSystem.GetSize(drive.TotalSize.ToString(), false),
                        Type = drive.DriveType.ToString(),
                        UsedPercent = Percent(used, drive.TotalSize)
                    });
                }
            }
            catch
            {
            }

            return drives;
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

        private void WriteRule(int width, string title, ConsoleColor color)
        {
            FileSystem.ColorConsoleTextLine(color, BuildRule(width, title));
        }

        private static string BuildRule(int width, string title)
        {
            int innerWidth = Math.Max(0, width - 2);
            string normalizedTitle = title ?? string.Empty;
            if (normalizedTitle.Length == 0 || normalizedTitle.Length + 2 >= innerWidth)
            {
                return "+" + new string('-', innerWidth) + "+";
            }

            int left = (innerWidth - normalizedTitle.Length) / 2;
            int right = innerWidth - normalizedTitle.Length - left;
            return "+" + new string('-', left) + normalizedTitle + new string('-', right) + "+";
        }

        private void WriteSectionRule(int width, string section)
        {
            int innerWidth = Math.Max(0, width - 2);
            string title = $"-- {section.ToUpperInvariant()} ";
            string content = Clip(title, innerWidth).PadRight(innerWidth, '-');
            FileSystem.ColorConsoleTextLine(ConsoleColor.DarkGray, "|" + content + "|");
        }

        private void WriteFrameLine(int width, string content)
        {
            int innerWidth = Math.Max(0, width - 4);
            string clipped = Clip(content, innerWidth);
            Console.Write("| ");
            Console.Write(clipped);
            Console.Write(new string(' ', innerWidth - clipped.Length));
            Console.WriteLine(" |");
        }

        private void WriteMetricLine(int width, string key, string value)
        {
            int innerWidth = Math.Max(0, width - 4);
            string prefix = $"  {key.PadRight(s_keyPadding)} :: ";
            string normalizedValue = NormalizeValue(value);

            if (prefix.Length >= innerWidth)
            {
                WriteFrameLine(width, $"{key} :: {normalizedValue}");
                return;
            }

            int valueWidth = innerWidth - prefix.Length;
            string clippedValue = Clip(normalizedValue, valueWidth);
            Console.Write("| ");
            FileSystem.ColorConsoleText(ConsoleColor.DarkCyan, prefix);
            Console.Write(clippedValue);
            Console.Write(new string(' ', valueWidth - clippedValue.Length));
            Console.WriteLine(" |");
        }

        private void WriteMeterLine(int width, string key, string detail, double percent)
        {
            int innerWidth = Math.Max(0, width - 4);
            string prefix = $"  {key.PadRight(s_keyPadding)} :: ";
            string meter = $"{FormatPercent(percent),6} {BuildMeter(percent)}";
            string normalizedDetail = NormalizeValue(detail);

            if (prefix.Length >= innerWidth)
            {
                WriteFrameLine(width, $"{key} :: {meter} {normalizedDetail}");
                return;
            }

            int remaining = innerWidth - prefix.Length;
            Console.Write("| ");
            FileSystem.ColorConsoleText(ConsoleColor.DarkCyan, prefix);

            string clippedMeter = Clip(meter, remaining);
            FileSystem.ColorConsoleText(GetLoadColor(percent), clippedMeter);
            remaining -= clippedMeter.Length;

            if (remaining > 0)
            {
                string clippedDetail = Clip("  " + normalizedDetail, remaining);
                Console.Write(clippedDetail);
                remaining -= clippedDetail.Length;
            }

            if (remaining > 0)
            {
                Console.Write(new string(' ', remaining));
            }

            Console.WriteLine(" |");
        }

        private static string BuildMeter(double percent)
        {
            if (percent < 0)
            {
                return "[" + new string('?', s_meterWidth) + "]";
            }

            double clamped = Math.Max(0, Math.Min(100, percent));
            int filled = (int)Math.Round(clamped / 100 * s_meterWidth);
            filled = Math.Max(0, Math.Min(s_meterWidth, filled));
            return "[" + new string('#', filled) + new string('-', s_meterWidth - filled) + "]";
        }

        private static ConsoleColor GetLoadColor(double percent)
        {
            if (percent < 0)
            {
                return ConsoleColor.DarkGray;
            }

            if (percent >= 85)
            {
                return ConsoleColor.Red;
            }

            if (percent >= 65)
            {
                return ConsoleColor.Yellow;
            }

            return ConsoleColor.Green;
        }

        private static int GetLogoWidth()
        {
            int width = 0;
            foreach (string line in s_xTerminalLogo)
            {
                if (line.Length > width)
                {
                    width = line.Length;
                }
            }

            return width;
        }

        private static int GetFrameWidth()
        {
            try
            {
                int availableWidth = Console.WindowWidth - 1;
                if (availableWidth > 0)
                {
                    return Math.Min(availableWidth, s_maxFrameWidth);
                }
            }
            catch
            {
            }

            return 90;
        }

        private static double Percent(double used, double total)
        {
            if (total <= 0)
            {
                return -1;
            }

            return Math.Round(used / total * 100, 1);
        }

        private static string FormatPercent(double percent)
        {
            return percent < 0 ? "N/A" : $"{percent:0.0}%";
        }

        private static string Clip(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || maxLength <= 0)
            {
                return string.Empty;
            }

            if (value.Length <= maxLength)
            {
                return value;
            }

            if (maxLength <= 3)
            {
                return value.Substring(0, maxLength);
            }

            return value.Substring(0, maxLength - 3) + "...";
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

        private sealed class MachineSnapshot
        {
            public string User { get; set; } = string.Empty;
            public string Host { get; set; } = string.Empty;
            public string ScanTime { get; set; } = string.Empty;
            public string Manufacturer { get; set; } = string.Empty;
            public string Model { get; set; } = string.Empty;
            public string OsCaption { get; set; } = string.Empty;
            public string Kernel { get; set; } = string.Empty;
            public string Architecture { get; set; } = string.Empty;
            public string CpuName { get; set; } = string.Empty;
            public string LogicalProcessors { get; set; } = string.Empty;
            public string Topology { get; set; } = string.Empty;
            public string MemoryUsed { get; set; } = string.Empty;
            public string MemoryTotal { get; set; } = string.Empty;
            public double MemoryUsedPercent { get; set; }
            public List<string> Gpus { get; set; } = new List<string>();
            public List<DriveSnapshot> Drives { get; set; } = new List<DriveSnapshot>();
        }

        private sealed class DriveSnapshot
        {
            public string Mount { get; set; } = string.Empty;
            public string Free { get; set; } = string.Empty;
            public string Total { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public double UsedPercent { get; set; }
        }
    }
}
