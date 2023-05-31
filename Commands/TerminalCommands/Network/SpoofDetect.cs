using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Commands.TerminalCommands.Network
{
    [SupportedOSPlatform("Windows")]
    public class SpoofDetect : ITerminalCommand
    {
        public string Name  => "dspoof";
        public void Execute(string arg)
        {
            CheckSpoof();
        }

        /// <summary>
        /// Run MITM ARP spoof check.
        /// </summary>
        private void CheckSpoof()
        {
            var localIP = NetWork.GetYourIp();
            var gatewayIP = NetWork.GetGetewayIp();
            var gatewayMAC = NetWork.GetMacAddress(gatewayIP);
            var arpTable = NetWork.GetIPsAndMac(localIP);
            int skip = 0;
            DisplayUI(gatewayIP, gatewayMAC);
            foreach (var arp in arpTable)
            {
                if(skip >= 1)
                {
                    if(arp.MAC == gatewayMAC)
                    {
                        FileSystem.ColorConsoleTextLine(ConsoleColor.Red, "ARP spoof detected from:");
                        Console.WriteLine($"{arp.IP} | {arp.MAC}");
                        break;
                    }
                }
                skip++;
            }
        }

        /// <summary>
        /// Display UI stuff.
        /// </summary>
        /// <param name="gatewayIP"></param>
        /// <param name="gatewayMAC"></param>
        public void DisplayUI(string gatewayIP, string gatewayMAC)
        {
            FileSystem.ColorConsoleTextLine(ConsoleColor.Green, "-------------------------------------");
            FileSystem.ColorConsoleText(ConsoleColor.Green, "Gateway IP: ");
            Console.WriteLine(gatewayIP);
            FileSystem.ColorConsoleText(ConsoleColor.Green, "Gateway MAC: ");
            Console.WriteLine(gatewayMAC);
            FileSystem.ColorConsoleTextLine(ConsoleColor.Green, "-------------------------------------");
        }
    }
}
