using System.Diagnostics;

namespace xUninstaller
{
    internal class ProcessKiller
    {
        /// <summary>
        /// CTor for process kill.
        /// </summary>
        public ProcessKiller() { }

        /// <summary>
        /// Kill processes by name or id.
        /// </summary>
        /// <param name="processName"></param>
        /// <param name="id"></param>
        /// <param name="entireProcessTree"></param>
        public void KillProcess(string processName)
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                process.Kill(true);
                return;
            }
            return;
        }
    }
}