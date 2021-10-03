using System;

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
