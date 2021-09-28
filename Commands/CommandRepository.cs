using System;
using System.Collections.Generic;
using System.Linq;
using Commands.TerminalCommands;

namespace Commands
{
    public static class CommandRepository
    {
        private static readonly List<ITerminalCommand> s_terminalCommands =
            new List<ITerminalCommand>();

        // Populate the list with all available ITerminalCommand classes
        static CommandRepository()
        {
            s_terminalCommands.Add(new ListDirectories());
            s_terminalCommands.Add(new NetworkInterfaceCheck());
            s_terminalCommands.Add(new StartProccess());
            s_terminalCommands.Add(new CommandHistory());
        }

        public static ITerminalCommand GetCommand(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                // Maybe display an error message here
                return null;
            }

            return GetCommand(string.Join(" ", args));
        }

        public static ITerminalCommand GetCommand(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                return null;
            }

            // Get the first word from the parameters. This should be a command
            string commandName = commandLine.Split().First();

            return s_terminalCommands.FirstOrDefault(c => c.Name.Equals(commandName, StringComparison.InvariantCulture));
        }
    }
}