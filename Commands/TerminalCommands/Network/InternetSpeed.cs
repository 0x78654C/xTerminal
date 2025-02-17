using Core;
using System;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Threading;

namespace Commands.TerminalCommands.Network
{
    [SupportedOSPlatform("Windows")]
    public class InternetSpeed : ITerminalCommand
    {
        public string Name => "ispeed";

        public void Execute(string arg)
        {
            GlobalVariables.isErrorCommand = false;
            Console.WriteLine("*******************************************");
            Console.WriteLine("**** Check internet speed with Google *****");
            Console.WriteLine("*******************************************");
            Console.WriteLine(" ");

            if (NetWork.IntertCheck()) // Check internet connection.
            {
                var inetSpeed = GetInternetSpeedAsync();
                Console.WriteLine($"{inetSpeed} Kb/s with Google");
            }
            else
            {
                FileSystem.ErrorWriteLine("No internet connection!");
                GlobalVariables.isErrorCommand = true;
            }
        }

        /// <summary>
        /// Get internet speed with httpClient.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public int GetInternetSpeedAsync(CancellationToken ct = default)
        {
            const double kb = 1024;

            // do not use compression
            using var client = new HttpClient();

            int numberOfBytesRead = 0;

            var buffer = new byte[10240].AsMemory();

            // create request
            var stream = client.GetStreamAsync("https://www.google.com", ct).Result;

            // start timer
            DateTime dt1 = DateTime.UtcNow;

            // download stuff
            while (true)
            {
                var i = stream.ReadAsync(buffer, ct).Result;
                if (i < 1)
                    break;

                numberOfBytesRead += i;
            }

            // end timer
            DateTime dt2 = DateTime.UtcNow;

            double kilobytes = numberOfBytesRead / kb;
            double time = (dt2 - dt1).TotalSeconds;

            // speed in Kb per Second.
            return (int)(kilobytes / time);
        }
    }
}
