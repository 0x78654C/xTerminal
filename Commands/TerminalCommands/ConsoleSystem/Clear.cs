using System;

namespace Commands.TerminalCommands.ConsoleSystem
{
    /*
     * Clears the current console window.
     */
    public class Clear : ITerminalCommand
    {
        public string Name => "clear";

        public void Execute(string args)
        {
            Console.Clear();
        }
    }
}
