using Core;
using System;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.Network
{
    [SupportedOSPlatform("Windows")]
    public class SpoofDetect : ITerminalCommand
    {
        private static string s_helpMessage = @"The command detects MITM(man in the middle) attacks using ARP spoof method. Here is a short description:

ARP spoofing refers to an attacker with access to the LAN pretending to be Host B. 
The attacker sends messages to Host A with the goal of tricking Host A into saving the attacker’s address as Host B’s address. 
Host A will ultimately send communications intended for Host B to the attacker instead. 
Once the attacker becomes this middle man, each time Host A communicates with Host B, that host will in fact be communicating first with the attacker.
Host B will typically be the default gateway, or the router.
   
";
        public string Name  => "dspoof";
        public void Execute(string args)
        {
            if (args == $"{Name} -h")
            {
                Console.WriteLine(s_helpMessage);
                return;
            }

            // Run MITM ARP spoof detection.
            CheckSpoof();
        }

        /// <summary>
        /// MITM ARP spoof check.
        /// </summary>
        private void CheckSpoof()
        {
            var localIP = NetWork.GetYourIp();
            var gatewayIP = NetWork.GetGetewayIp();
            var gatewayMAC = NetWork.GetMacAddress(gatewayIP);
            var arpTable = NetWork.GetIPsAndMac(localIP);
            bool isAttack = false;
            int skip = 0;
            DisplayUIMain(gatewayIP, gatewayMAC);
            foreach (var arp in arpTable)
            {
                if(skip >= 1)
                {
                    if(arp.MAC == gatewayMAC)
                    {
                        FileSystem.ColorConsoleTextLine(ConsoleColor.Red, "[-]Possbile ARP spoof detection from:");
                        DisplayUIAttack(arp.IP, arp.MAC);
                        isAttack = true;
                        break;
                    }
                }
                skip++;
            }
            if (!isAttack)
                FileSystem.ColorConsoleTextLine(ConsoleColor.Green, "[+] Seems all good!");
        }

        /// <summary>
        /// Display MAIN UI stuff.
        /// </summary>
        /// <param name="gatewayIP"></param>
        /// <param name="gatewayMAC"></param>
        public void DisplayUIMain(string gatewayIP, string gatewayMAC)
        {
            FileSystem.ColorConsoleTextLine(ConsoleColor.Green, "-------------------------------------");
            FileSystem.ColorConsoleText(ConsoleColor.Green, "Gateway IP: ");
            Console.WriteLine(gatewayIP);
            FileSystem.ColorConsoleText(ConsoleColor.Green, "Gateway MAC: ");
            Console.WriteLine(gatewayMAC);
            FileSystem.ColorConsoleTextLine(ConsoleColor.Green, "-------------------------------------");
        }

        /// <summary>
        /// Display UI stuff.
        /// </summary>
        /// <param name="gatewayIP"></param>
        /// <param name="gatewayMAC"></param>
        public void DisplayUIAttack(string ipAddress, string macAddress)
        {
            FileSystem.ColorConsoleText(ConsoleColor.Yellow, "IP: ");
            Console.Write(ipAddress);
            FileSystem.ColorConsoleText(ConsoleColor.Yellow, " | MAC: ");
            Console.WriteLine(macAddress);
        }
    }
}
