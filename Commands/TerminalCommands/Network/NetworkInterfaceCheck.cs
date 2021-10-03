using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;

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
