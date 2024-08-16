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
                if (split[1].Trim() == "-f")
                {
                    SystemCommands.RebootCmd(true);
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
