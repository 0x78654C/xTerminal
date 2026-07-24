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
            var sha256 = HashAlgo.GetSHA256(Application.ExecutablePath).ToUpper();
            var output = $"xTerminal version: {version} ({architecture})\n" +
                $"SHA256 executable: {sha256}";

            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                GlobalVariables.pipeCmdOutput = output;
            else
                FileSystem.SuccessWriteLine(output);

            GlobalVariables.isErrorCommand = false;
        }
    }
}
