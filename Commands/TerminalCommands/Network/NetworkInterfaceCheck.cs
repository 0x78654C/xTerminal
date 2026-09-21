using Core;
using Core.Network;
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
    -i  :  Display network interfaces names, Type, IP range and status.
    -h  :  Display this message.
";


        public void Execute(string args)
        {
            args = args.Replace("ifconfig", "");
            var isHWonly = false;
            if (args.Contains("-h"))
            {
                Console.WriteLine(s_helpMessage);
                return;
            }

            if (args.Contains("-i"))
            {
                isHWonly=true;
            }
            GlobalVariables.isErrorCommand = false;
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                GlobalVariables.pipeCmdOutput = NetWork.ShowNicConfiguragion(isHWonly);
            else
                FileSystem.SuccessWriteLine(NetWork.ShowNicConfiguragion(isHWonly));
        }
    }
}
