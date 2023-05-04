using Core;
using System;
using System.Net;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.Network
{
    [SupportedOSPlatform("Windows")]
    public class InternetSpeed : ITerminalCommand
    {
        public string Name => "ispeed";

        public void Execute(string arg)
        {
            Console.WriteLine("*******************************************");
            Console.WriteLine("**** Check internet speed with Google *****");
            Console.WriteLine("*******************************************");
            Console.WriteLine(" ");

            if (Core.NetWork.IntertCheck()) // Check internet connection.
            {
                // Create Object Of WebClient.
                WebClient wc = new WebClient();

                // Download Start Time.
                DateTime dt1 = DateTime.Now;

                // Number Of Bytes Downloaded.
                byte[] data = wc.DownloadData("http://www.google.com");

                // Download End Time.
                DateTime dt2 = DateTime.Now;

                // Calculate Speed in Kb Divide Value Of data by 1024 And Then by End Time Subtract Start Time To Know Download Per Second.
                Console.WriteLine(Math.Round((data.Length / 1024) / (dt2 - dt1).TotalSeconds, 2) + " Kb/s with Google");
            }
            else
            {
                FileSystem.ErrorWriteLine("No internet connection!");
            }
        }
    }
}
