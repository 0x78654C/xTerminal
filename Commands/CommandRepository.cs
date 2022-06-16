using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Commands
{
    public static class CommandRepository
    {
        private static readonly Dictionary<string, ITerminalCommand> s_terminalCommands =
                    Assembly.GetExecutingAssembly().GetTypes()
                        .Where(t => typeof(ITerminalCommand).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                        .Select(t => Activator.CreateInstance(t))
                        .Cast<ITerminalCommand>()
                        .ToDictionary(command => command.Name, StringComparer.InvariantCulture);
        private static readonly List<string> s_shellCommands = new List<string>() { "reboot", "logoff", "lock", "shutdown" };

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

            // Get the first word from the parameters. This should be the command name
            string commandName = commandLine.Split().First();

            if (!s_terminalCommands.TryGetValue(commandName, out var terminalCommand)
                && !s_shellCommands.Contains(commandLine) && !commandLine.StartsWith("cmd") && !commandLine.StartsWith("ps"))
            {
                Console.WriteLine($"Unknown command: {commandLine}");
            }

            return terminalCommand;
        }
    }
}