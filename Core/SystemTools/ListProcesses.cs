using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Runtime.Versioning;
using System.Diagnostics;
namespace Core.SystemTools
{
    [SupportedOSPlatform("Windows")]
    public class ListProcesses
    {
        private static List<string> processList = new List<string> { };
        public static void GetProcessList()
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process"))
            {
                foreach (var process in searcher.Get())
                {
                    int id = Int32.Parse(process["ProcessID"].ToString());
                    string processName = Process.GetProcessById(id).ProcessName;
                    string item = $"Parent: [{processName}] [{id}]";
                    if (!processList.Contains(item))
                        processList.Add(item);
                    GetChildProcesses(id);
                }
            }
            string outList = string.Join("\n", processList);
            Console.WriteLine(outList);
        }

        private static void GetChildProcesses(int id)
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessId=" + id))
            {
                foreach (var process in searcher.Get())
                {
                    int idChildProcess = Int32.Parse(process["ProcessID"].ToString());
                    string processName = Process.GetProcessById(idChildProcess).ProcessName;
                    string item = $"    [{processName}] [{idChildProcess}]";
                    foreach(var i in processList)
                    {
                        if (i.Contains($"Parent: [{processName}] [{idChildProcess}]"));
                        processList.Remove(i);
                    }

                    if (!processList.Contains(item))
                        processList.Add(item);
                }
            }
        }
    }
}
