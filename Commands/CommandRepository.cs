using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Commands
{
    public static class CommandRepository
    {

        private static readonly List<ITerminalCommand> s_terminalCommands =
                    Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(ITerminalCommand).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                        .Select(t => Activator.CreateInstance(t))
                        .Cast<ITerminalCommand>().ToList();
        private static List<string> s_shellCommands = new List<string>() { "reboot", "logoff", "shutdown", "speedtest" };
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

            var t = s_terminalCommands.FirstOrDefault(c => c.Name.Equals(commandName, StringComparison.InvariantCulture));
            if (t == null && !s_shellCommands.Contains(commandLine) && !commandLine.StartsWith("cmd") && !commandLine.StartsWith("ps"))
                Console.WriteLine($"Unknown command: {commandLine}");

            return s_terminalCommands.FirstOrDefault(c => c.Name.Equals(commandName, StringComparison.InvariantCulture));
        }
    }
}