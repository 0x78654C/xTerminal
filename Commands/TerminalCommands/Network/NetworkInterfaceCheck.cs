using Core;
using System;

namespace Commands.TerminalCommands.Network
{
    public class NetworkInterfaceCheck : ITerminalCommand
    {
        public string Name => "ifconfig";

        public void Execute(string args)
        {
            Console.WriteLine(NetWork.ShowNicConfiguragion());
        }
    }
}
