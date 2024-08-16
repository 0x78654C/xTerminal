/*
 *
 * shutdown command.
 * 
 */


using System;
using System.Runtime.Versioning;
using Core;
using Core.Commands;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class Shutdown : ITerminalCommand
    {
        public string Name => "shutdown";
        private static string s_messageHelp = @"Usage of shutdown command:
    shutdown    : shutdown system normaly.
    shutdown -f : force shutdown system;
";

        public void Execute(string arg)
        {
            try
            {
                if (arg == "shutdown")
                {
                    SystemCommands.ShutDownCmd(false);
                    return;
                }

                var split = arg.Split(' ');
                if (split[1].Trim() == "-f")
                {
                    SystemCommands.ShutDownCmd(true);
                    return;
                }

                if (split[1].Trim() == "-h")
                {
                    Console.WriteLine(s_messageHelp);
                    return;
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine($"{e.Message}. Use -h for more information!");
            }
        }
    }
}
