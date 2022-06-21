using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Core;
using Json = Core.SystemTools.JsonManage;
using AliasC = Core.SystemTools.AliasC;

namespace Commands
{
    public static class CommandRepository
    {
        private static string s_aliasFile = GlobalVariables.aliasFile;

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

            ITerminalCommand terminalCommandOut;

            // Get the first word from the parameters. This should be the command name
            string commandName = commandLine.Split().First();
            if (!s_terminalCommands.TryGetValue(commandName, out terminalCommandOut)
                && !s_shellCommands.Contains(commandLine) && !commandLine.StartsWith("cmd") && !commandLine.StartsWith("ps"))
            {
                string alias= GetAliasCommand(commandName, s_aliasFile);
                if (string.IsNullOrEmpty(alias) || !s_terminalCommands.TryGetValue(alias.Split().First(),out terminalCommandOut))
                    Console.WriteLine($"Unknown command: {commandLine}");
            }
            return terminalCommandOut;
        }

        /// <summary>
        /// Get alias command name and param.
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="aliasJsonFile"></param>
        /// <returns></returns>
        private static string GetAliasCommand(string commandName, string aliasJsonFile)
        {
            if (!File.Exists(aliasJsonFile))
            {
                FileSystem.ErrorWriteLine("Alias file does not exist!");
                return string.Empty;
            }
            string command = string.Empty;
            var aliasCommands = Json.ReadJsonFromFile<AliasC[]>(aliasJsonFile);
            foreach (var alias in aliasCommands)
            {
                if (alias.CommandName == commandName)
                {
                    command = alias.Command;
                    GlobalVariables.aliasParameters = command;
                }
            }
            return command;
        }
    }
}