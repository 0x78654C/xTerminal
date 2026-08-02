using Core;
using Core.Updater;
using System.Diagnostics;
using System.Reflection;

namespace AutoUpdater
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                KillXterm();
                var xtermPath = "";

                try
                {
                    xtermPath = args[0];
                }
                catch { }
                var pathExecutable = Path.GetDirectoryName(Application.ExecutablePath);
                //   var xterminalDll = @$"{xtermPath}\xTerminal.dll"; 
                var xterminalDll = "C:\\Users\\mrx\\Projects\\xTerminal\\Release\\net10.0-windows7.0\\xTerminal.dll";
                var verExe = File.Exists(xterminalDll) ? AssemblyName.GetAssemblyName(xterminalDll).Version.ToString() : "File does not exist!";
                var arch = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                var githubAPI = new GitHubAPI();
                Task.Run(() => githubAPI.CheckNewVersions(verExe, arch)).Wait();
                githubAPI.DownloadUpdate();
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine(ex.ToString());
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Kill xTerminal.
        /// </summary>
        /// <returns></returns>
        private static bool KillXterm()
        {
            var processes = Process.GetProcesses();

            foreach (var process in processes)
            {
                if (process.ProcessName == "xTerminal")
                {
                    process.Kill();
                    process.WaitForExit();
                    return true;
                }
            }
            return false;
        }
    }
}
