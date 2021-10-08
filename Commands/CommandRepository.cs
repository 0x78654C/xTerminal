﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Commands
{
    public static class CommandRepository
    {
        private static readonly List<ITerminalCommand> s_terminalCommands =
            new List<ITerminalCommand>();

        // Populate the list with all available ITerminalCommand classes
        static CommandRepository()
        {
            // System Commands
            s_terminalCommands.Add(new TerminalCommands.ConsoleSystem.ListDirectories());
            s_terminalCommands.Add(new TerminalCommands.ConsoleSystem.StartProccess());
            s_terminalCommands.Add(new TerminalCommands.ConsoleSystem.Help());
            s_terminalCommands.Add(new TerminalCommands.ConsoleSystem.HexViewer());
            s_terminalCommands.Add(new TerminalCommands.ConsoleSystem.CommandHistory());
            s_terminalCommands.Add(new TerminalCommands.ConsoleSystem.Clear());
            s_terminalCommands.Add(new TerminalCommands.ConsoleSystem.Clear());
            s_terminalCommands.Add(new TerminalCommands.ConsoleSystem.CurrentDirectory());
            s_terminalCommands.Add(new TerminalCommands.ConsoleSystem.CheckPermission());
            s_terminalCommands.Add(new TerminalCommands.ConsoleSystem.BiosInfo());
            s_terminalCommands.Add(new TerminalCommands.ConsoleSystem.StorageInfo());
            s_terminalCommands.Add(new TerminalCommands.ConsoleSystem.OpenDirectory());
            s_terminalCommands.Add(new TerminalCommands.ConsoleSystem.ClearHistory());

            // Network Commands
            s_terminalCommands.Add(new TerminalCommands.Network.NetworkInterfaceCheck());
            s_terminalCommands.Add(new TerminalCommands.Network.ExternalIp());
            s_terminalCommands.Add(new TerminalCommands.Network.InternetSpeed());
            s_terminalCommands.Add(new TerminalCommands.Network.CheckDomain());
            s_terminalCommands.Add(new TerminalCommands.Network.EmailClient());
            s_terminalCommands.Add(new TerminalCommands.Network.WGet());
            s_terminalCommands.Add(new TerminalCommands.Network.Ping());

            // File/Directory Commands
            s_terminalCommands.Add(new TerminalCommands.DirFiles.MD5Check());
            s_terminalCommands.Add(new TerminalCommands.DirFiles.MakeDirectory());
            s_terminalCommands.Add(new TerminalCommands.DirFiles.MakeFile());
            s_terminalCommands.Add(new TerminalCommands.DirFiles.FCopy());
            s_terminalCommands.Add(new TerminalCommands.DirFiles.FMove());
            s_terminalCommands.Add(new TerminalCommands.DirFiles.FRename());
            s_terminalCommands.Add(new TerminalCommands.DirFiles.Delete());
            s_terminalCommands.Add(new TerminalCommands.DirFiles.StringView());
            s_terminalCommands.Add(new TerminalCommands.DirFiles.Editor());

            // Games
            s_terminalCommands.Add(new TerminalCommands.Games.FlappyBirds());
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