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
    shutdown -f : force shutdown system.
    shutdown -m <remotePC>    : shutdown a remote system normaly.
    shutdown -f -m <remotePC> : force shutdown a remote system normaly.
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

                if (split[1].Trim() == "-m")
                {
                    if (split.Length < 3)
                    {
                        FileSystem.ErrorWriteLine($"Remote PC parameter should not be empty. Use -h for more information!");
                        GlobalVariables.isErrorCommand = true;
                        return;
                    }

                    var remotePC = split[2].Trim();
                    SystemCommands.ShutDownCmd(true, remotePC);
                    return;
                }

                if (split[1].Trim() == "-f")
                {
                    if(!arg.Contains("-m"))
                    {
                        SystemCommands.ShutDownCmd(true);
                        return;
                    }

                    if (split[2].Trim() == "-m")
                    {
                        if (split.Length < 4)
                        {
                            FileSystem.ErrorWriteLine($"Remote PC parameter should not be empty. Use -h for more information!");
                            GlobalVariables.isErrorCommand = true;
                            return;
                        }
                        var remotePC = split[3].Trim();
                        SystemCommands.ShutDownCmd(true, remotePC);
                        return;
                    }
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
                GlobalVariables.isErrorCommand = true;
            }
        }
    }
}
