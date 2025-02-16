/*
 *
 * reboot command.
 * 
 */

using System;
using System.Runtime.Versioning;
using Core;
using Core.Commands;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class Reboot : ITerminalCommand
    {
        public string Name => "reboot";
        private static string s_messageHelp = @"Usage of restart command:
    reboot    : reboots system normaly.
    reboot -f : force reboots system.
    reboot -m <remotePC>    : reboot a remote system normaly.
    reboot -f -m <remotePC> : force reboot a remote system normaly.
";

        public void Execute(string arg)
        {
            try
            {
                if (arg == "reboot")
                {
                    SystemCommands.RebootCmd(false);
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
                    SystemCommands.RebootCmd(true, remotePC);
                    return;
                }

                if (split[1].Trim() == "-f")
                {
                    if (!arg.Contains("-m"))
                    {
                        SystemCommands.RebootCmd(true);
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
                        SystemCommands.RebootCmd(true, remotePC);
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
