using System.Diagnostics;

namespace AutoUpdater.Utils
{
    public class Prc
    {
        /// <summary>
        /// Process utility class for starting processes.
        /// </summary>
        public Prc()
        {
        }

        /// <summary>
        /// Start a process with the specified file name, arguments, and working directory.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="arguments"></param>
        /// <param name="workingDirectory"></param>
        public void StartProcess(string fileName, string arguments = "", string workingDirectory = "")
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                UI.ErrorWriteLine($"Failed to start process '{fileName}': {ex.Message}");
            }
        }
    }
}
