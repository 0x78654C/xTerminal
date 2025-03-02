using Core;
using System;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.Network
{
    [SupportedOSPlatform("Windows")]
    public class NetworkInterfaceCheck : ITerminalCommand
    {
        /*
         Display NIC's configuration. 
         */
        public string Name => "ifconfig";
        private static string s_helpMessage = @"Usage of ifconfig command:

Display onboard Network Interface Cards configuration (Ethernet and Wireless)
";


        public void Execute(string args)
        {
            args = args.Replace("ifconfig", "");

            if (args.Contains("-h") && args.Length == 2)
            {
                Console.WriteLine(s_helpMessage);
                return;
            }
            GlobalVariables.isErrorCommand = false;
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                GlobalVariables.pipeCmdOutput = NetWork.ShowNicConfiguragion();
            else
                Console.WriteLine(NetWork.ShowNicConfiguragion());
        }
    }
}
