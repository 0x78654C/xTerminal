using Core;
using System;
using System.Net;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace Commands.TerminalCommands.Network
{
    [SupportedOSPlatform("Windows")]
    public class ExternalIp : ITerminalCommand
    {
        /*
         * Display machine's external IP.
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
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                    GlobalVariables.pipeCmdOutput = externalIP;
                else
                    Console.WriteLine("Your external IP address is: " + externalIP);
            }
            catch { FileSystem.ErrorWriteLine("Cannot verify external IP address. Check your internet connection!"); }
        }
    }
}
