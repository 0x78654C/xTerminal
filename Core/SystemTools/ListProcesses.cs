using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using System.Diagnostics;

namespace Core.SystemTools
{
    [SupportedOSPlatform("Windows")]
    public class ListProcesses
    {
        private static List<string> s_processList = new List<string> { };

        /// <summary>
        /// Get process list with parent and child processes.
        /// </summary>
        public static void GetProcessList()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process");
            foreach (var process in searcher.Get())
            {
                int id = Int32.Parse(process["ProcessID"].ToString());
                string processName = Process.GetProcessById(id).ProcessName;
                string item = $"Parent : [{processName}] [{id}]";
                string searchPattern = $"    [{processName}] [{id}]";
                if (s_processList.Any(i => i.Contains(searchPattern)))
                {
                    int countExisting = s_processList.Where(i => i.Contains(searchPattern)).Count();
                    if (countExisting > 0)
                        item = $"Parent ({countExisting}) : [{processName}] [{id}]";
                }
                if (!s_processList.Contains(item))
                    s_processList.Add(item);
                GetChildProcesses(id);
            }

            string outList = string.Join("\n", s_processList);
            if (GlobalVariables.isPipeCommand)
                GlobalVariables.pipeCmdOutput = outList;
            else
            Console.WriteLine(outList);
        }


        /// <summary>
        /// Get child process.
        /// </summary>
        /// <param name="id"></param>
        private static void GetChildProcesses(int id)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessId=" + id);
            foreach (var process in searcher.Get())
            {
                int idChildProcess = Int32.Parse(process["ProcessID"].ToString());
                string processName = Process.GetProcessById(idChildProcess).ProcessName;
                string item = $"    [{processName}] [{idChildProcess}]";
                if (!s_processList.Contains(item))
                    s_processList.Add(item);
            }
        }
    }
}
