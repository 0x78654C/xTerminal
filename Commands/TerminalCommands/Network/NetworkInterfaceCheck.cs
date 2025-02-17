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

        public void Execute(string args)
        {
            GlobalVariables.isErrorCommand = false;
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                GlobalVariables.pipeCmdOutput = NetWork.ShowNicConfiguragion();
            else
                Console.WriteLine(NetWork.ShowNicConfiguragion());
        }
    }
}
