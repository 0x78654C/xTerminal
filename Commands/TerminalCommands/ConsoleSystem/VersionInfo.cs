using Core;
using Core.Encryption;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class VersionInfo : ITerminalCommand
    {
        public string Name => "ver";

        public void Execute(string args)
        {
            var version = string.IsNullOrWhiteSpace(GlobalVariables.version)
                ? Application.ProductVersion
                : GlobalVariables.version;

            var architecture = RuntimeInformation.ProcessArchitecture.ToString();
            var pathExecutable = Path.GetDirectoryName(Application.ExecutablePath);
            var commandsDll = @$"{pathExecutable}\Commands.dll";
            var coreDll = @$"{pathExecutable}\Core.dll";
            var sha256Commands = File.Exists(commandsDll) ? HashAlgo.GetSHA256(commandsDll) : "File does not exist!";
            var sha256Core = File.Exists(coreDll) ? HashAlgo.GetSHA256(coreDll) : "File does not exist!";
            var sha256 = HashAlgo.GetSHA256(Application.ExecutablePath).ToUpper();
            var output = $"xTerminal version: {version} ({architecture})\n" +
                "__________________________________\n"+
                $"SHA256 xTerminal.exe: {sha256}\n"+
                $"SHA256 Commands.dll : {sha256Commands}\n"+
                $"SHA256 Core.dll     : {sha256Core}\n";

            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                GlobalVariables.pipeCmdOutput = output;
            else
                FileSystem.SuccessWriteLine(output);

            GlobalVariables.isErrorCommand = false;
        }
    }
}
