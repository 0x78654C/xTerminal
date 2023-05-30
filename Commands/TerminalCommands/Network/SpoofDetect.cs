using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace Commands.TerminalCommands.Network
{
    [SupportedOSPlatform("Windows")]
    public class SpoofDetect : ITerminalCommand
    {
        public string Name  => "dspoof";
        public void Execute(string arg)
        {
            var gateway = NetWork.GetGetewayIp();
            Console.WriteLine(gateway);
        }
    }
}
