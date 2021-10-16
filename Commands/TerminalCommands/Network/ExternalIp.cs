using Core;
using System;
using System.Net;
using System.Text.RegularExpressions;

namespace Commands.TerminalCommands.Network
{
    public class ExternalIp : ITerminalCommand
    {
        /*
         * Display machine external IP.
         */
        public string Name => "extip";

        public void Execute(string arg)
        {
            try
            {
                string externalIP;
                externalIP = (new WebClient()).DownloadString("http://checkip.dyndns.org");
                externalIP = (new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"))
                             .Matches(externalIP)[0].ToString();
                Console.WriteLine("Your external IP address is: " + externalIP);
            }
            catch { FileSystem.ErrorWriteLine("Canno't verify external IP. Check your internet connection!"); }
        }
    }
}
