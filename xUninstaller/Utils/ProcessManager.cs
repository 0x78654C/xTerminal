using System.Diagnostics;

namespace xUninstaller
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
        /// Delete after process ended.
        /// </summary>
        /// <param name="path"></param>
        public void Delete(string path)
        {
            Process.Start(new ProcessStartInfo()
            {
                Arguments = $"/C choice /C Y /N /D Y /T & rmdir /S /Q {path}",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName ="cmd.exe"
            });
        }
    }
}