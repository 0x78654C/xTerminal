using AutoUpdater.Utils;
using System.Diagnostics;
using System.Reflection;

namespace AutoUpdater
{
    internal class Program
    {
        private static readonly string[] s_xTerminalLogo = new[]
        {
    @"       _____                   _             _ ",
    @"__  __|_   _|__ _ __ _ __ ___ (_)_ __   __ _| |",
    @"\ \/ /  | |/ _ \ '__| '_ ` _ \| | '_ \ / _` | |",
    @" >  <   | |  __/ |  | | | | | | | | | | (_| | |",
    @"/_/\_\  |_|\___|_|  |_| |_| |_|_|_| |_|\__,_|_|"
};

        /// <summary>
        /// Main entry point of the application.
        /// </summary>
        /// <param name="args"></param>
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
                Console.WriteLine(string.Join(Environment.NewLine, s_xTerminalLogo));
                Console.WriteLine("");
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                Console.WriteLine("================================================");
                Console.WriteLine($"=  AutoUpdater for xTerminal version: {version}  =");
                Console.WriteLine("================================================");
                Console.WriteLine("");
                Console.WriteLine($"xTerminal path: {xtermPath}");
                var xterminalDll = @$"{xtermPath}\xTerminal.dll";
                //var xterminalDll = "C:\\Users\\mrx\\Projects\\xTerminal\\Release\\net10.0-windows7.0\\xTerminal.dll";
                var verExe = File.Exists(xterminalDll) ? AssemblyName.GetAssemblyName(xterminalDll).Version.ToString() : "File does not exist!";
                var arch = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                var githubAPI = new GitHubAPI();
                Task.Run(() => githubAPI.CheckNewVersions(verExe, arch)).Wait();
                githubAPI.DownloadUpdate(xtermPath);
                return;
            }
            catch (Exception ex)
            {
                UI.ErrorWriteLine(ex.ToString());
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
