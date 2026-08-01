using System.Reflection;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace AutoUpdater
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var pathExecutable = Path.GetDirectoryName(Application.ExecutablePath);
           // var xterminalDll = @$"{pathExecutable}\xTerminal.dll"; 
           var xterminalDll = Path.Combine("C:\\Users\\MrX\\Projects\\xTerminal\\Shell\\bin\\x64\\Debug\\net10.0-windows7.0\\xTerminal.dll");
            var verExe = File.Exists(xterminalDll) ? AssemblyName.GetAssemblyName(xterminalDll).Version.ToString() : "File does not exist!";
            var arch = Environment.Is64BitOperatingSystem ? "x64" : "x86";  
            var githubAPI = new GitHubAPI();
            var releases = Task.Run(()=>githubAPI.ListReporsetories(verExe, arch));
            Console.ReadKey();
        }
    }
}
