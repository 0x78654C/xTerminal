using Core;
using Core.DirFiles;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.DirFiles
{
    [SupportedOSPlatform("Windows")]
    public class xPlorer : ITerminalCommand
    {
        /*
         * Simple hex viewer.
         */

        public string Name => "fxp";
        public void Execute(string args)
        {
            var path = File.ReadAllText(GlobalVariables.currentDirectory);
            var currentDir = path.Substring(0, path.Length - 1);
            var fExplorer = new FileExplorer(currentDir);
            fExplorer.Run();
        }
    }
}
