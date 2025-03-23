using System;
using System.Runtime.Versioning;
using Core;

namespace Commands.TerminalCommands.Network
{
    /*Wake over LAN command class.*/
    [SupportedOSPlatform("Windows")]
    public class WakeOverLan : ITerminalCommand
    {
        private const int _port = 9;
        private static string s_helpMessage = @"Usage of wol (Wake Over LAN) command:
    wol -ip IPAddress/HostName -mac MAC_Address                   : sends wake packet for ip/mac.
    wol -ip IPAddress/HostName -mac MAC_Address -port number_port : sends wake packet for ip/mac and custom WOL port.
";
        public string Name => "wol";
        public void Execute(string args)
        {
            GlobalVariables.isErrorCommand = false;
            if (args.Trim()  == Name)
            {
                FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                return;
            }
            if (args == $"{Name} -h")
            {
                Console.WriteLine(s_helpMessage);
                return;
            }
            try
            {
                string ipAddress = args.MiddleString("-ip", "-mac").Trim();
                string macAddress = args.MiddleString("-mac", "-port").Trim();
                int port = _port;
                if (args.Contains("-port"))
                {
                    port = Int32.Parse(args.SplitByText("-port ", 1).Trim());
                }
                WakeOverLAN wakeOverLAN = new WakeOverLAN(ipAddress, macAddress, port);
                wakeOverLAN.Wake();
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine($"{e.Message}. Check parameters!");
                GlobalVariables.isErrorCommand = true;
            }
        }
    }
}
