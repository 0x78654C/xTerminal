/*
 wTpp - A simple console-based process listing tool for Windows.
    * This tool provides a real-time view of running processes, their CPU and memory usage,
    * The first tool that is fully generated with AI and is designed to be simple and efficient.
 */


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace Core.SystemTools
{
    [SupportedOSPlatform("Windows")]

    public class ProcessListingUI
    {
        int selectedIndex = 0;
        int windowHeight = Console.WindowHeight - 4;
        DateTime prevSampleTime = DateTime.UtcNow;
        volatile bool exitRequested = false;
        Dictionary<int, double> cpuUsages = new();
        Dictionary<int, TimeSpan> prevCpuTimes = new();
        PerformanceCounter cpuCounter = new("Processor", "% Processor Time", "_Total");
        PerformanceCounter memAvailableCounter = new("Memory", "Available MBytes");
        string searchQuery = "";
        bool inSearchMode = false;

        // Shared fields updated by SampleCpuLoop
        double latestCpuPercent = 0;
        double latestMemPercent = 0;
        double latestMemUsed = 0;
        double latestMemTotal = 0;


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        (double usedMb, double totalMb, double percent) GetMemoryUsage()
        {
            MEMORYSTATUSEX memStatus = new();
            if (GlobalMemoryStatusEx(memStatus))
            {
                double totalMb = memStatus.ullTotalPhys / 1024.0 / 1024.0;
                double availMb = memStatus.ullAvailPhys / 1024.0 / 1024.0;
                double usedMb = totalMb - availMb;
                double percent = (usedMb / totalMb) * 100;
                return (usedMb, totalMb, percent);
            }
            return (0, 0, 0);
        }

        /// <summary>
        /// Entry point to run the process listing UI.
        /// </summary>
        public void Run()
        {
            Console.CursorVisible = false;

            new Thread(ReadInput) { IsBackground = true }.Start();
            new Thread(SampleCpuLoop) { IsBackground = true }.Start();
            new Thread(RenderLoop) { IsBackground = true }.Start();

            while (!exitRequested)
                Thread.Sleep(50); // keep main alive
        }

        /// <summary>
        /// Generates a visual usage bar as a string representation, with customizable width, color, and threshold.
        /// </summary>
        /// <remarks>The method sets the console's foreground color based on the specified threshold and
        /// percentage. Ensure that the console color is reset after using this method to avoid unintended color changes
        /// in subsequent output.</remarks>
        /// <param name="percent">The percentage value used to determine the filled portion of the bar. Must be between 0 and 100.</param>
        /// <param name="width">The total width of the usage bar, in characters.</param>
        /// <param name="colorLow">The console color used for the bar when <paramref name="percent"/> is below the <paramref
        /// name="threshold"/>.</param>
        /// <param name="colorHigh">The console color used for the bar when <paramref name="percent"/> is equal to or exceeds the <paramref
        /// name="threshold"/>.</param>
        /// <param name="threshold">The percentage threshold that determines whether <paramref name="colorHigh"/> or <paramref name="colorLow"/>
        /// is applied. Defaults to 80.0.</param>
        /// <returns>A string representing the usage bar, with the filled portion determined by <paramref name="percent"/>. The
        /// string is padded to match the specified <paramref name="width"/>.</returns>
        string GetUsageBar(double percent, int width, ConsoleColor colorLow, ConsoleColor colorHigh, double threshold = 80.0)
        {
            int filled = (int)(percent / 100 * width);
            var color = percent >= threshold ? colorHigh : colorLow;

            var bar = new string('█', filled).PadRight(width);

            Console.ForegroundColor = color;
            return bar;
        }

        /// <summary>
        /// Continuously samples CPU and memory usage, updating the shared fields.
        /// </summary>
        void SampleCpuLoop()
        {
            while (!exitRequested)
            {
                latestCpuPercent = cpuCounter.NextValue();
                (latestMemUsed, latestMemTotal, latestMemPercent) = GetMemoryUsage();

                var processes = Process.GetProcesses();
                DateTime now = DateTime.UtcNow;
                double interval = (now - prevSampleTime).TotalSeconds;
                prevSampleTime = now;

                var newUsages = new Dictionary<int, double>();

                foreach (var proc in processes)
                {
                    try
                    {
                        var cpuTime = proc.TotalProcessorTime;

                        if (prevCpuTimes.TryGetValue(proc.Id, out var prevCpu))
                        {
                            var delta = (cpuTime - prevCpu).TotalSeconds;
                            var percent = (delta / interval) / Environment.ProcessorCount * 100;
                            newUsages[proc.Id] = Math.Max(0.0, percent);
                        }
                        else
                        {
                            newUsages[proc.Id] = 0.0;
                        }

                        prevCpuTimes[proc.Id] = cpuTime;
                    }
                    catch { }
                }

                var currentIds = processes.Select(p => p.Id).ToHashSet();
                foreach (var dead in prevCpuTimes.Keys.Except(currentIds).ToList())
                    prevCpuTimes.Remove(dead);

                cpuUsages = newUsages;

                Thread.Sleep(1000); // sampling interval
            }
        }


        /// <summary>
        /// Continuously reads user input from the console and processes it to control application behavior.
        /// </summary>
        /// <remarks>This method listens for key presses and performs actions based on the input. It
        /// supports navigation using the arrow keys, process termination with the 'K' key, and application exit with
        /// the 'Q' key. The method runs in a loop until the exit condition is triggered.</remarks>
        void ReadInput()
        {
            while (!exitRequested)
            {
                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(10);
                    continue;
                }

                var keyInfo = Console.ReadKey(true);

                if (inSearchMode)
                {
                    if (keyInfo.Key == ConsoleKey.Enter)
                    {
                        inSearchMode = false;
                        SearchAndScrollToProcess(searchQuery.Trim());
                        searchQuery = "";
                    }
                    else if (keyInfo.Key == ConsoleKey.Escape)
                    {
                        inSearchMode = false;
                        searchQuery = "";
                    }
                    else if (keyInfo.Key == ConsoleKey.Backspace && searchQuery.Length > 0)
                    {
                        searchQuery = searchQuery[..^1];
                    }
                    else if (keyInfo.KeyChar != '\u0000')
                    {
                        searchQuery += keyInfo.KeyChar;
                    }

                    RenderSearchPrompt();
                    continue;
                }

                switch (keyInfo.Key)
                {
                    case ConsoleKey.UpArrow:
                        if (selectedIndex > 0) selectedIndex--;
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex++;
                        break;
                    case ConsoleKey.K:
                        KillSelectedProcess();
                        break;
                    case ConsoleKey.Q:
                        exitRequested = true;
                        Console.Clear();
                        break;
                    case ConsoleKey.Oem2: // '/' key
                        inSearchMode = true;
                        searchQuery = "";
                        RenderSearchPrompt();
                        break;
                }
            }
        }

        /// <summary>
        /// Searches for a process by name and scrolls to its position in the list.
        /// </summary>
        /// <remarks>If a matching process is found, its index is selected. If no match is found,  a
        /// message is displayed in the console indicating that no process matches the query.</remarks>
        /// <param name="query">The case-insensitive substring to search for in process names.</param>
        void SearchAndScrollToProcess(string query)
        {
            var processes = Process.GetProcesses()
                .OrderBy(p => p.ProcessName)
                .ToArray();

            for (int i = 0; i < processes.Length; i++)
            {
                try
                {
                    if (processes[i].ProcessName.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedIndex = i;
                        return;
                    }
                }
                catch { }
            }

            // If not found, optionally display message
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"No process found matching \"{query}\"".PadRight(Console.WindowWidth - 1));
            Console.ResetColor();
        }

        /// <summary>
        /// Renders a search prompt at the bottom of the console window.
        /// </summary>
        /// <remarks>The search prompt is displayed in yellow text and includes the current search query.
        /// The prompt is padded to fill the width of the console window.</remarks>
        void RenderSearchPrompt()
        {
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"/{searchQuery}".PadRight(Console.WindowWidth - 1));
            Console.ResetColor();
        }


        /// <summary>
        /// Terminates the process selected by the user.
        /// </summary>
        /// <remarks>This method retrieves all currently running processes, orders them by name, and
        /// attempts to terminate  the process at the specified index. If the process is successfully terminated, the
        /// method waits for  up to 2 seconds for the process to exit.</remarks>
        void KillSelectedProcess()
        {
            try
            {
                var processes = Process.GetProcesses()
                    .OrderBy(p => p.ProcessName)
                    .ToArray();

                if (selectedIndex >= 0 && selectedIndex < processes.Length)
                {
                    var proc = processes[selectedIndex];
                    proc.Kill();
                    proc.WaitForExit(2000); // wait max 2 seconds for process to exit
                }
            }
            catch (Exception ex)
            {
                // Optional: display an error somewhere or ignore if access denied
            }
        }

        /// <summary>
        /// Continuously renders the process list and system resource usage in the console. 
        /// </summary>
        void RenderLoop()
        {
            while (!exitRequested)
            {
                var processes = Process.GetProcesses()
                    .OrderBy(p => p.ProcessName)
                    .ToArray();

                int maxDisplay = Console.WindowHeight - 6; // Reserve lines for title + bars + header
                int visibleCount = Math.Min(maxDisplay, processes.Length);
                int start = Math.Clamp(selectedIndex - visibleCount / 2, 0, processes.Length - visibleCount);

                double cpuPercent = cpuCounter.NextValue(); // total CPU %
                (double memUsed, double memTotal, double memPercent) = GetMemoryUsage();

                // --- Draw Title ---
                Console.SetCursorPosition(0, 0);
                Console.Write(new string(' ', Console.WindowWidth)); // clear line 0
                Console.SetCursorPosition(0, 0);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("WTop - ↑/↓ navigate | 'Q' quit | 'K' kill selected process | '/' search".PadRight(Console.WindowWidth - 1));
                Console.ResetColor();

                // --- Draw CPU Bar ---
                Console.SetCursorPosition(0, 1);
                Console.Write(new string(' ', Console.WindowWidth)); // clear line 1
                Console.SetCursorPosition(0, 1);
                Console.Write("CPU: ");
                string cpuBar = GetUsageBar(cpuPercent, 30, ConsoleColor.Green, ConsoleColor.Red);
                Console.Write(cpuBar);
                Console.ResetColor();
                Console.Write($" {cpuPercent:0.0}%".PadLeft(7));

                // --- Draw Memory Bar ---
                Console.SetCursorPosition(0, 2);
                Console.Write(new string(' ', Console.WindowWidth)); // clear line 2
                Console.SetCursorPosition(0, 2);
                Console.Write("MEM: ");
                string memBar = GetUsageBar(memPercent, 30, ConsoleColor.Yellow, ConsoleColor.Red);
                Console.Write(memBar);
                Console.ResetColor();
                Console.Write($" {memUsed:0.0}MB / {memTotal:0.0}MB ({memPercent:0.0}%)".PadLeft(25));

                // --- Blank line ---
                Console.SetCursorPosition(0, 3);
                Console.Write(new string(' ', Console.WindowWidth)); // clear line 3

                // --- Draw process header ---
                Console.SetCursorPosition(0, 4);
                Console.Write(new string(' ', Console.WindowWidth)); // clear line 4
                Console.SetCursorPosition(0, 4);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($"{"PID",5} {"Name",-25} {"CPU%",6} {"Mem(MB)",8} {"Threads",8}".PadRight(Console.WindowWidth - 1));
                Console.ResetColor();

                // --- Draw process list ---
                for (int i = 0; i < visibleCount; i++)
                {
                    int index = start + i;
                    var proc = processes[index];
                    string line;

                    try
                    {
                        var cpu = cpuUsages.TryGetValue(proc.Id, out var value) ? value : 0.0;
                        var mem = proc.PrivateMemorySize64 / 1024.0 / 1024.0;
                        line = $"{proc.Id,5} {Truncate(proc.ProcessName, 25),-25} {cpu,6:0.0} {mem,8:0.0} {proc.Threads.Count,8}";
                    }
                    catch
                    {
                        line = $"{proc.Id,5} {"<Access Denied>",-25} {"0.0",6} {"N/A",8} {"N/A",8}";
                    }

                    Console.SetCursorPosition(0, i + 5);
                    if (index == selectedIndex)
                    {
                        Console.BackgroundColor = ConsoleColor.DarkBlue;
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write(line.PadRight(Console.WindowWidth - 1));
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.Write(line.PadRight(Console.WindowWidth - 1));
                    }
                }

                // --- Clear any leftover lines below the list ---
                for (int i = visibleCount; i < maxDisplay; i++)
                {
                    Console.SetCursorPosition(0, i + 5);
                    Console.Write(new string(' ', Console.WindowWidth - 1));
                }

                Thread.Sleep(100); // refresh rate
            }
        }

        /// <summary>
        /// Truncates the specified string to a maximum length, appending "..." if truncation occurs.
        /// </summary>
        /// <param name="value">The string to be truncated. Cannot be <see langword="null"/>.</param>
        /// <param name="maxLength">The maximum allowed length of the resulting string. Must be greater than 3.</param>
        /// <returns>The original string if its length is less than or equal to <paramref name="maxLength"/>;  otherwise, a
        /// truncated version of the string with "..." appended.</returns>
        string Truncate(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
        }
    }
}
