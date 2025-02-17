using Core;
using System;
using System.Runtime.Versioning;
using ping = Core.NetWork;

namespace Commands.TerminalCommands.Network
{
    [SupportedOSPlatform("Windows")]
    public class CheckDomain : ITerminalCommand
    {
        /*
         Check if an IP dor domain is down.
         */
        public string Name => "icheck";
        private static string s_helpMessage = @"Check if an IP/ domain is down or up:

Example: icheck google.com 

";
        public void Execute(string arg)
        {
            try
            {
                GlobalVariables.isErrorCommand = false;
                if (arg == $"{Name} -h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }
                string domain = string.Empty;
                if (GlobalVariables.isPipeCommand)
                    domain = GlobalVariables.pipeCmdOutput.Trim();
                else
                    domain = arg.Split(' ')[1];
                if (ping.PingHost(domain))
                {
                    Console.Write($"{domain}:");
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Green, " ONLINE");
                    return;
                }
                Console.Write($"{domain}:");
                FileSystem.ColorConsoleTextLine(ConsoleColor.Red, " OFFLINE");
            }
            catch
            {
                FileSystem.ErrorWriteLine("You must specify a domain or an IP address to check. Eg.: icheck google.com");
                GlobalVariables.isErrorCommand = true;
            }
        }
    }
}
