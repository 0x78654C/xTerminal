using System;
using ping = Core.NetWork;

namespace CheckDomain
{

    /// <summary>
    /// Check if an Domain/IP is up.
    /// </summary>
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                string input = args[0];
                if (ping.PingHost(input))
                {
                    Console.WriteLine($"{input} is online!");
                    return;
                }
                Console.WriteLine($"{input} is down!");
            }
            catch
            {
                Console.WriteLine("You must specify a domain or an IP address to check . Ex.: icheck google.com");
            }
        }
    }
}
