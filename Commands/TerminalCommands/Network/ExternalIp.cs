using Core;
using System;
using System.Net.Http;
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
        private static string s_helpMessage = @" The command returns your public IP address provied by checkip.dyndns.org verification.";
        public void Execute(string arg)
        {
            try
            {
                GlobalVariables.isErrorCommand = false;
                if (arg == $"{Name} -h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                string externalIP;
                externalIP = (new HttpClient()).GetStringAsync("http://checkip.dyndns.org").Result;
                externalIP = (new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"))
                             .Matches(externalIP)[0].ToString();
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                    GlobalVariables.pipeCmdOutput = externalIP;
                else
                    FileSystem.SuccessWriteLine("Your external IP address is: " + externalIP);
            }
            catch
            {
                FileSystem.ErrorWriteLine("Cannot verify external IP address. Check your internet connection!");
                GlobalVariables.isErrorCommand = true;
            }
        }
    }
}
