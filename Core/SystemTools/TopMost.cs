using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace Core.SystemTools
{
    [SupportedOSPlatform("Windows")]
    public class TopMost
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vKey);

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(
             IntPtr processHandle,
             int processInformationClass,
             ref PROCESS_BASIC_INFORMATION processInformation,
             uint processInformationLength,
             out uint returnLength);

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            public IntPtr Reserved2_0;
            public IntPtr Reserved2_1;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            QueryInformation = 0x0400,
            VMRead = 0x0010
        }

        private const int ProcessBasicInformation = 0;
        private static int GetChildProcessIdByName(string parentProcessName, string targetProcessName)
        {
            Process[] parentProcesses = Process.GetProcessesByName(parentProcessName);
            if (parentProcesses.Length == 0)
            {
                return -1;
            }

            int parentProcessId = parentProcesses[0].Id;

            foreach (Process process in Process.GetProcesses())
            {
                int parentProcessIdOfProcess = GetParentProcessId(process.Id);

                if (parentProcessIdOfProcess == parentProcessId && process.ProcessName.Equals(targetProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    return process.Id;
                }
            }

            return -1;
        }

        private static int GetParentProcessId(int processId)
        {
            IntPtr processHandle = OpenProcess(ProcessAccessFlags.QueryInformation, false, processId);
            if (processHandle == IntPtr.Zero)
            {
                return -1;
            }

            PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
            uint returnLength;

            int status = NtQueryInformationProcess(processHandle, ProcessBasicInformation, ref pbi, (uint)Marshal.SizeOf(pbi), out returnLength);
            CloseHandle(processHandle);  // Ensure the handle is closed after the query

            if (status == 0)
            {
                return pbi.InheritedFromUniqueProcessId.ToInt32();
            }

            return -1;
        }

        /// <summary>
        /// Check Top most apps and compare with current running process id.
        /// </summary>
        /// <returns></returns>
        public static bool ApplicationIsActivated()
        {
            var activatedHandle = GetForegroundWindow();
            if (activatedHandle == IntPtr.Zero)
            {
                return false;       // No window is currently activated
            }

            var procId = Process.GetCurrentProcess().Id;
            int activeProcId;
            GetWindowThreadProcessId(activatedHandle, out activeProcId);
            Console.Write(activeProcId);
            return activeProcId == procId;
        }

        /// <summary>
        /// Key Press event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void KeyMonitorWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var currProcesName = Process.GetCurrentProcess().ProcessName;
            var currProcesId = Process.GetCurrentProcess().Id;
            var childProcessId = GetChildProcessIdByName("VsDebugConsole", currProcesName);
            int activeProcId=0;

            while (true)
            {
                var isNotForground = ApplicationIsActivated();
                //if (isNotForground)
                //   {
                if (GetAsyncKeyState(0x10) < 0)
                    {
                        if (!GlobalVariables.isKeyPressed)
                        {
                            Console.Write($"child {childProcessId} curr {currProcesId} foreground {isNotForground}");
                            Console.WriteLine();
                            GlobalVariables.isKeyPressed = true;
                        }
                    }
                    else
                    {
                    Console.WriteLine($"child {childProcessId} curr {currProcesId} foreground {isNotForground}");
                    GlobalVariables.isKeyPressed = false; // Reset flag when button is released
                    }

                    Thread.Sleep(50); // Small delay to reduce CPU usage
               // }
            }
        }
    }
}
