using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commands.TerminalCommands.ConsoleSystem
{
    public class Clear : ITerminalCommand
    {
        public string Name => "clear";

        public void Execute(string args)
        {
            Console.Clear();
        }
    }
}
