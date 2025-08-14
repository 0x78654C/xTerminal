using System.Diagnostics;

namespace xInstaller
{
    internal class ProcessManager
    {

        /// <summary>
        /// CTor for process kill.
        /// </summary>
        public ProcessManager() { }

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

        /// <summary>
        /// Check if process is running.
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        public bool IsProcess(string processName)
        => Process.GetProcessesByName(processName).Length > 0;
    }
}