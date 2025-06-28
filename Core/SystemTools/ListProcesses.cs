using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.Versioning;
using System.Diagnostics;

namespace Core.SystemTools
{
    [SupportedOSPlatform("Windows")]
    public class ListProcesses
    {
        /// <summary>
        /// Recursively prints a hierarchical tree structure of processes and their child processes.
        /// </summary>
        /// <remarks>This method outputs the tree structure to the console or appends it to a global
        /// string variable for piping, depending on the value of <c>GlobalVariables.isPipeCommand</c>. Each process is
        /// displayed with its name, process ID, and window title (if available). The tree structure uses visual
        /// connectors such as "├─" and "└─" to represent hierarchy.</remarks>
        /// <param name="pid">The process ID of the root process to start printing the tree from.</param>
        /// <param name="children">A dictionary mapping process IDs to their respective child processes. Each child process is represented as a
        /// list of <see cref="ManagementObject"/> instances.</param>
        /// <param name="indent">The string used to visually indent the tree structure for nested child processes.</param>
        /// <param name="last">A value indicating whether the current process is the last child in its parent's list of children. This
        /// affects the visual connector used in the tree.</param>
        private static void PrintTree(uint pid, Dictionary<uint, List<ManagementObject>> children, string indent, bool last)
        {
            if (!children.ContainsKey(pid)) return;

            var processes = children[pid];

            for (int i = 0; i < processes.Count; i++)
            {
                var proc = processes[i];
                uint childPid = (uint)proc["ProcessId"];
                string name = (string)proc["Name"];
                string windowTitle = GetWindowTitle(childPid);
                bool isLast = (i == processes.Count - 1);

                // Visual tree branch
                string connector = isLast ? "└─ " : "├─ ";
                var dataOut= $"{indent}{connector}{name} ({childPid}){(string.IsNullOrWhiteSpace(windowTitle) ? "" : $" - \"{windowTitle}\"")}";
                if (GlobalVariables.isPipeCommand)
                    GlobalVariables.pipeCmdOutput += $"{dataOut}\n";
                else
                    FileSystem.SuccessWriteLine($"{indent}{connector}{name} ({childPid}){(string.IsNullOrWhiteSpace(windowTitle) ? "" : $" - \"{windowTitle}\"")}");

                string newIndent = indent + (isLast ? "   " : "│  ");
                PrintTree(childPid, children, newIndent, isLast);
            }
        }

        /// <summary>
        /// Return windown title name.
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        private static string GetWindowTitle(uint pid)
        {
            try
            {
                var proc = Process.GetProcessById((int)pid);
                return proc.MainWindowTitle?.Trim() ?? "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Display the process tree of current running processes.
        /// </summary>
        public static void PrintProcessTree()
        {
            var query = "SELECT ProcessId, Name, ParentProcessId FROM Win32_Process";
            var searcher = new ManagementObjectSearcher(query);

            Dictionary<uint, List<ManagementObject>> children = new();
            Dictionary<uint, ManagementObject> allProcesses = new();

            foreach (ManagementObject proc in searcher.Get())
            {
                uint pid = (uint)proc["ProcessId"];
                uint ppid = (uint)proc["ParentProcessId"];

                allProcesses[pid] = proc;

                if (!children.ContainsKey(ppid))
                    children[ppid] = new List<ManagementObject>();

                children[ppid].Add(proc);
            }

            // Find root-level processes (whose parent isn't in the process list)
            foreach (var pid in children.Keys)
            {
                if (!allProcesses.ContainsKey(pid))
                {
                    PrintTree(pid, children, "", true);
                }
            }
        }
    }
}
