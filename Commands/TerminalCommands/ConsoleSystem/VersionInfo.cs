using Core;
using System;
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
            string version = string.IsNullOrWhiteSpace(GlobalVariables.version)
                ? Application.ProductVersion
                : GlobalVariables.version;

            string architecture = RuntimeInformation.ProcessArchitecture.ToString();
            string output = $"xTerminal version: {version} ({architecture})";

            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                GlobalVariables.pipeCmdOutput = output;
            else
                FileSystem.SuccessWriteLine(output);

            GlobalVariables.isErrorCommand = false;
        }
    }
}
