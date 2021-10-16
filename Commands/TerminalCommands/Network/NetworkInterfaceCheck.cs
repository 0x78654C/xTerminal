using Core;
using System;

namespace Commands.TerminalCommands.Network
{
    public class NetworkInterfaceCheck : ITerminalCommand
    {
        /*
         Display NIC's configuration. 
         */
        public string Name => "ifconfig";

        public void Execute(string args)
        {
            Console.WriteLine(NetWork.ShowNicConfiguragion());
        }
    }
}
