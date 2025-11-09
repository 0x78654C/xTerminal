using Core;
using Core.DirFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commands.TerminalCommands.DirFiles
{
    public class xPlorer : ITerminalCommand
    {
        /*
         * Simple hex viewer.
         */

        public string Name => "fxp";
        public void Execute(string args)
        {
            var currentDir = File.ReadAllText(GlobalVariables.currentDirectory);
            var fExplorer = new FileExplorer(currentDir);
            fExplorer.Run();
        }
    }
}
