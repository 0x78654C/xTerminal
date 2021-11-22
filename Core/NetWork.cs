using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace Core
{
    /*Network class for check Ping and Internet connection.*/
    public class NetWork
    {
        private static Ping s_myPing;
        private static PingReply s_pingReply;
        private static int s_success = 0;
        private static int s_failure = 0;

        /// <summary>
        /// Verifies if IP is up or not
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns>verifies if IP is up or not</returns>
        public static bool PingHost(string ipAddress)
        {
            bool pingable = false;
            Ping pinger = null;
            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(ipAddress);
                pingable = reply.Status == IPStatus.Success;

            }
            catch
            {
                // We handle erros in other functions.
            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }

            }
            return pingable;

        }

        /// <summary>
        /// Ping function for ping command line.
        /// </summary>
        /// <param name="address">IP/Hostanme for ping.</param>
        public static void PingMain(string address, int pingReplys)
        {
            try
            {
                for (int i = 0; i < pingReplys; i++)
                {
                    if (PingHost(address))
                    {
                        Thread.Sleep(500);
                        s_myPing = new Ping();
                        string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                        byte[] buffer = Encoding.ASCII.GetBytes(data);
                        PingOptions options = new PingOptions(64, true);
                        s_pingReply = s_myPing.Send(address, 12000, buffer, options);
                        Console.WriteLine($"Status: {s_pingReply.Status} | Buffer: {s_pingReply.Buffer.Length} | Time: {s_pingReply.RoundtripTime} ms | TTL: {options.Ttl} |  Adress: {s_pingReply.Address}");
                        s_success++;
                    }
                    else
                    {
                        s_failure++;
                        Console.WriteLine($"{address} is down!");
                    }
                }
                Console.WriteLine("\n---------------------------------------------------\n");
                Console.Write($" Total status count: Success ");
                FileSystem.ColorConsoleText(ConsoleColor.Green, s_success.ToString());
                Console.Write(" Failure ");
                FileSystem.ColorConsoleTextLine(ConsoleColor.Red, s_failure.ToString() + "\n");
                s_failure = 0;
                s_success = 0;
            }
            catch (TimeoutException)
            {
                FileSystem.ErrorWriteLine("Time out is to big");
                s_failure = 0;
                s_success = 0;
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.ToString());
                s_failure = 0;
                s_success = 0;
            }
            finally
            {
                if (s_myPing != null)
                {
                    s_myPing.Dispose();
                    s_failure = 0;
                    s_success = 0;
                }
            }
        }
        /// <summary>
        /// Checking internet connection with Google DNS 8.8.8.8
        /// </summary>
        /// <returns></returns>
        public static bool IntertCheck()
        {
            return PingHost("8.8.8.8");
        }

        /// <summary>
        /// Output NIC's configuration (Ethernet and Wireless).
        /// </summary>
        /// <returns>string</returns>
        public static string ShowNicConfiguragion()
        {
            string nicOuptut = string.Empty;
            string ipAddress = string.Empty;
            string gateway = string.Empty;
            string mask = string.Empty;
            string dnsAddr = string.Empty;

            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                {
                    foreach (GatewayIPAddressInformation gatewayIPAddress in networkInterface.GetIPProperties().GatewayAddresses)
                    {
                        if (gatewayIPAddress.Address.ToString().Trim().Length > 2)
                        {
                            gateway += "".PadRight(15, ' ') + gatewayIPAddress.Address.ToString() + "\n";
                        }
                    }
                    foreach (UnicastIPAddressInformation unicastIPAddress in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        ipAddress += "".PadRight(15, ' ') + unicastIPAddress.Address + "\n";
                        mask += "".PadRight(15, ' ') + unicastIPAddress.IPv4Mask + "\n";
                    }
                    IPInterfaceProperties iPInterface = networkInterface.GetIPProperties();
                    IPAddressCollection dnsAddresses = iPInterface.DnsAddresses;
                    foreach (var dnsAddress in dnsAddresses)
                    {
                        dnsAddr += "".PadRight(15, ' ') + dnsAddress + "\n";
                    }

                    var mac = string.Join(":", (from z in networkInterface.GetPhysicalAddress().GetAddressBytes() select z.ToString("X2")).ToArray());
                    nicOuptut += $"\n-------------- {networkInterface.Name} --------------\n\n";
                    nicOuptut += $"Description:".PadRight(15, ' ') + $"{ networkInterface.Description}\n";
                    nicOuptut += $"IP Address: \n{ ipAddress} \n";
                    nicOuptut += $"MASK: \n{ mask}\n";
                    nicOuptut += $"Gateway: \n{gateway}\n";
                    nicOuptut += $"MAC Address: ".PadRight(15, ' ') + $"{mac}\n";
                    nicOuptut += $"DNS: \n{dnsAddr}\n";
                }
            }
            return nicOuptut;
        }
    }
}
