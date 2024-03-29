using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Core;
using Json = Core.SystemTools.JsonManage;
using AliasC = Core.SystemTools.AliasC;
using Core.SystemTools;
using System.Runtime.Versioning;
using System.Reflection.Metadata;

namespace Commands
{
    [SupportedOSPlatform("Windows")]
    public static class CommandRepository
    {
        private static string s_aliasFile = GlobalVariables.aliasFile;

        private static readonly Dictionary<string, ITerminalCommand> s_terminalCommands =
                    Assembly.GetExecutingAssembly().GetTypes()
                        .Where(t => typeof(ITerminalCommand).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                        .Select(t => Activator.CreateInstance(t))
                        .Cast<ITerminalCommand>()
                        .ToDictionary(command => command.Name, StringComparer.InvariantCulture);

        private static readonly List<string> s_shellCommands = new List<string>() { "reboot", "logoff", "lock", "shutdown", "+" };

        public static ITerminalCommand GetCommand(string[] args)
        {
            if (args == null || args.Length == 0)
                return null;

            return GetCommand(string.Join(" ", args));
        }

        public static ITerminalCommand GetCommand(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                GlobalVariables.aliasRunFlag = false;
                return null;
            }

            ITerminalCommand terminalCommandOut;

            // Get the first word from the parameters. This should be the command name
            string commandName = commandLine.Split().First();

            var commandLeng = commandName.Length;

            // Get the parameter for allias command.
            var subCommand = commandLine.Substring(commandLeng).Trim();
            if (!string.IsNullOrEmpty(subCommand))
            {
                var splitSubcommand = new string[] { };
                if (subCommand.Contains("!\""))
                {
                    splitSubcommand = subCommand.Split("\"");
                    foreach (var command in splitSubcommand)
                    {
                        if (!command.Contains(commandName) || string.IsNullOrEmpty(command))
                        {
                            var addCommand = "";
                            if (command.Contains('!'))
                            {
                                if (command.StartsWith('!'))
                                {
                                    addCommand = command.Substring(1);
                                    if (!string.IsNullOrEmpty(addCommand))
                                        GlobalVariables.aliasInParameter.Add($"{addCommand.Trim()}");
                                }
                                else if (command.EndsWith('!'))
                                {
                                    addCommand = command.Substring(0, command.Length - 1);
                                    if (!string.IsNullOrEmpty(addCommand))
                                        GlobalVariables.aliasInParameter.Add($"{addCommand.Trim()}");
                                }
                                else
                                {
                                    addCommand = command;
                                    if (!string.IsNullOrEmpty(addCommand))
                                        GlobalVariables.aliasInParameter.Add($"{addCommand.Trim()}");
                                }
                            }
                            else
                            {
                                addCommand = command;
                                if (!string.IsNullOrEmpty(addCommand))
                                    GlobalVariables.aliasInParameter.Add($"{addCommand.Trim()}");
                            }
                        }
                    }
                }
                else
                {
                    splitSubcommand = subCommand.Contains('"') ? subCommand.Split('"') : subCommand.Split(' ');
                    foreach (var command in splitSubcommand)
                        if (!command.Contains(commandName) && !string.IsNullOrEmpty(command))
                        {
                            if (subCommand.Contains('"'))
                                GlobalVariables.aliasInParameter.Add($"\"{command.Trim()}\"");
                            else 
                                GlobalVariables.aliasInParameter.Add(command.Trim());
                        }
                }
            }

            if (!s_terminalCommands.TryGetValue(commandName, out terminalCommandOut)
                && !s_shellCommands.Contains(commandLine))
            {
                string alias = GetAliasCommand(commandName, s_aliasFile);
                if (string.IsNullOrEmpty(alias) || !s_terminalCommands.TryGetValue(alias.Split().First(), out terminalCommandOut))
                {
                    if (!commandLine.StartsWith("cmd") && !commandLine.StartsWith("ps") && !GlobalVariables.aliasRunFlag)
                    {
                        Console.WriteLine($"Unknown command: {commandLine}");
                    }
                    GlobalVariables.aliasParameters = " ";
                }
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
                return string.Empty;

            var aliasCommands = Json.ReadJsonFromFile<AliasC[]>(aliasJsonFile);

            string command = aliasCommands.Where(f => f.CommandName == commandName).FirstOrDefault()?.Command?.Trim() ?? string.Empty;
            GlobalVariables.aliasRunFlag = !string.IsNullOrWhiteSpace(command);

            var countItems = GlobalVariables.aliasInParameter.Count;
            if (command.Contains("%"))
                for (int i = 0; i < countItems; i++)
                    command = command.Replace($"%{i + 1}", GlobalVariables.aliasInParameter[i]);

            GlobalVariables.aliasParameters = !string.IsNullOrWhiteSpace(command) ? command : GlobalVariables.aliasParameters;

            // Usage of cmd and ps with parameters in alias commands.
            if (command.StartsWith("cmd") || command.StartsWith("ps"))
            {
                ProcessStart.Execute(command, command);
                GlobalVariables.aliasInParameter.Clear();
                return string.Empty;
            }
            GlobalVariables.aliasInParameter.Clear();
            return command.Trim();
        }
    }
}