using System;
using Core;

namespace Commands.TerminalCommands.Network
{
    public class PortScanner : ITerminalCommand
    {
        /* Connection status check for an port (opened or closed) */

        public string Name => "cport";
        private static string s_helpMessage = @"Usage of cport command:

    Example 1: cport IPAddress/HostName -p 80   (checks if port 80 is open)
    Example 2: cport IPAddress/HostName -p 1-200   (checks if any port is open from 1 to 200)

    Port range scan can be canceled with CTRL+X key combination.    
";

        public void Execute(string args)
        {
            try
            {
                if (args.Length == 5)
                {
                    Console.WriteLine($"Use -h param for {Name} command usage!");
                    return;
                }
                if (args == $"{Name} -h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }
                string arg = args.Substring(6, args.Length - 6);
                string ipAddress = arg.SplitByText(" -p ", 0);
                if (!NetWork.PingHost(ipAddress))
                {
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"{ipAddress} is offline");
                    return;
                }
                string portData = arg.SplitByText(" -p ", 1);
                if (portData.Contains("-"))
                {
                    GlobalVariables.eventKeyFlagX = true;
                    string[] portsRange = portData.Split('-');
                    int minPort = Int32.Parse(portsRange[0].Trim());
                    int maxPort = Int32.Parse(portsRange[1].Trim());
                    PortScan.RunPortScan(ipAddress, minPort, maxPort);
                    return;
                }
                int port = Int32.Parse(portData.Trim());
                PortScan.RunPortScan(ipAddress, port, port);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }
    }
}
