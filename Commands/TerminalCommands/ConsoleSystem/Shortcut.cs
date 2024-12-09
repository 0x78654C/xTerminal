using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class Shortcut: ITerminalCommand
    {
        public string Name => "ls";
        public void Execute(string args)
        {

        }
    }
}
