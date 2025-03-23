using Core.SystemTools;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace Core
{
    [SupportedOSPlatform("Windows")]
    /*Network class for check Ping and Internet connection.*/
    public class NetWork
    {
        private static Ping s_myPing;
        private static PingReply s_pingReply;
        private static int s_success = 0;
        private static int s_failure = 0;

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int SendARP(int destIP, int srcIP, byte[] macAddr, ref uint physicalAddrLen);

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
                GlobalVariables.pipeCmdOutput = string.Empty;
                if (pingReplys == 0)
                {
                    while (true)
                    {
                        if (GlobalVariables.eventCancelKey)
                        {
                            GlobalVariables.eventCancelKey = false;
                            FinalReplayOutput();
                            return;
                        }
                        GetReply(address);
                    }
                }
                else
                {
                    for (int i = 0; i < pingReplys; i++)
                    {
                        if (GlobalVariables.eventCancelKey)
                        {
                            GlobalVariables.eventCancelKey = false;
                            FinalReplayOutput();
                            return;
                        }
                        GetReply(address);
                    }
                }
                FinalReplayOutput();
            }
            catch (TimeoutException)
            {
                FileSystem.ErrorWriteLine("Time out is to big");
                GlobalVariables.isErrorCommand = true;
                s_failure = 0;
                s_success = 0;
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.ToString());
                GlobalVariables.isErrorCommand = true;
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
        /// Ouptus the final count and reset counters.
        /// </summary>
        private static void FinalReplayOutput()
        {
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
            {
                GlobalVariables.pipeCmdOutput += $"\n---------------------------------------------------\n  Total status count: Success {s_success.ToString()} Failure {s_failure.ToString()}\n";
                GlobalVariables.eventKeyFlagX = false;
                s_failure = 0;
                s_success = 0;
            }
            else
            {
                Console.WriteLine("\n---------------------------------------------------\n");
                Console.Write($" Total status count: Success ");
                FileSystem.ColorConsoleText(ConsoleColor.Green, s_success.ToString());
                Console.Write(" Failure ");
                FileSystem.ColorConsoleTextLine(ConsoleColor.Red, s_failure.ToString() + "\n");
                GlobalVariables.eventKeyFlagX = false;
                s_failure = 0;
                s_success = 0;
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
        /// Output the ping result.
        /// </summary>
        /// <param name="address"></param>
        private static void GetReply(string address)
        {
            if (PingHost(address))
            {
                Thread.Sleep(500);
                s_myPing = new Ping();
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                PingOptions options = new PingOptions(64, true);
                s_pingReply = s_myPing.Send(address, 12000, buffer, options);
                if (!s_pingReply.Status.ToString().Contains("Success"))
                    s_failure++;
                else
                    s_success++;
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                    GlobalVariables.pipeCmdOutput += $"Status: {s_pingReply.Status} | Buffer: {s_pingReply.Buffer.Length} | Time: {s_pingReply.RoundtripTime} ms | TTL: {options.Ttl} | Adress: {s_pingReply.Address}\n";
                else
                    Console.WriteLine($"Status: {s_pingReply.Status} | Buffer: {s_pingReply.Buffer.Length} | Time: {s_pingReply.RoundtripTime} ms | TTL: {options.Ttl} | Adress: {s_pingReply.Address}");
            }
            else
            {
                s_failure++;
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                    GlobalVariables.pipeCmdOutput += $"{address} is down!\n";
                else
                    Console.WriteLine($"{address} is down!");
            }
        }

        /// <summary>
        /// Output NIC's configuration (Ethernet and Wireless).
        /// </summary>
        /// <returns>string</returns>
        public static string ShowNicConfiguragion()
        {
            string nicOuptut = string.Empty;
            string ipAddress = string.Empty;
            string ipAddressV6 = string.Empty;
            string gateway = string.Empty;
            string mask = string.Empty;
            string dnsAddr = string.Empty;
            var count = 0;

            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                {
                    foreach (GatewayIPAddressInformation gatewayIPAddress in networkInterface.GetIPProperties().GatewayAddresses)
                    {
                        if (gatewayIPAddress.Address.ToString().Trim().Length > 2)
                        {
                            if (!gatewayIPAddress.Address.ToString().Contains("::"))
                                gateway += "".PadRight(15, ' ') + gatewayIPAddress.Address.ToString() + "\n";
                        }
                    }
                    foreach (UnicastIPAddressInformation unicastIPAddress in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (unicastIPAddress.Address.ToString().Contains("."))
                            ipAddress += "".PadRight(15, ' ') + unicastIPAddress.Address + "\n";
                        if (unicastIPAddress.Address.ToString().Contains(":"))
                            ipAddressV6 += "".PadRight(15, ' ') + unicastIPAddress.Address + "\n";

                        if (unicastIPAddress.IPv4Mask.ToString() != "0.0.0.0")
                            mask += "".PadRight(15, ' ') + unicastIPAddress.IPv4Mask + "\n";
                    }
                    IPInterfaceProperties iPInterface = networkInterface.GetIPProperties();
                    IPAddressCollection dnsAddresses = iPInterface.DnsAddresses;
                    foreach (var dnsAddress in dnsAddresses)
                    {
                        if (!dnsAddress.ToString().Contains("::"))
                            dnsAddr += "".PadRight(15, ' ') + dnsAddress + "\n";
                    }

                    var mac = string.Join(":", (from z in networkInterface.GetPhysicalAddress().GetAddressBytes() select z.ToString("X2")).ToArray());
                    nicOuptut += $"\n-------------- {networkInterface.Name} --------------\n\n";
                    nicOuptut += $"Description:".PadRight(15, ' ') + $"{networkInterface.Description}\n";
                    nicOuptut += $"IPv4:".PadRight(15, ' ') + $"{ipAddress.Trim()} \n";
                    nicOuptut += $"IPv6:".PadRight(15, ' ') + $"{ipAddressV6.Trim()} \n";
                    nicOuptut += $"MASK:".PadRight(15, ' ') + $"{mask.Trim()}\n";
                    nicOuptut += $"Gateway:".PadRight(15, ' ') + $"{gateway.Trim()}\n";
                    nicOuptut += $"MAC Address:".PadRight(15, ' ') + $"{mac}\n";
                    nicOuptut += $"DNS:".PadRight(15, ' ') + $"{dnsAddr.Trim()}\n";
                    nicOuptut += $"\n-------------------------------------\n\n";

                }
                ipAddress = string.Empty;
                ipAddressV6 = string.Empty;
                gateway = string.Empty;
                mask = string.Empty;
                dnsAddr = string.Empty;
            }

            return nicOuptut;
        }

        /// <summary>
        /// Return gateweay
        /// </summary>
        /// <returns></returns>
        public static string GetGetewayIp()
        {
            string gateway = string.Empty;
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                    foreach (GatewayIPAddressInformation gatewayIPAddress in networkInterface.GetIPProperties().GatewayAddresses)
                        if (gatewayIPAddress.Address.ToString().Trim().Length > 2)
                            if (!gatewayIPAddress.Address.ToString().Contains(":"))
                                gateway += gatewayIPAddress.Address.ToString();
            return gateway;
        }

        /// <summary>
        /// Return you unicast IP Address
        /// </summary>
        /// <returns></returns>
        public static string GetYourIp()
        {
            string ipAddress = string.Empty;
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                    foreach (UnicastIPAddressInformation unicastIPAddress in networkInterface.GetIPProperties().UnicastAddresses)
                        if (!unicastIPAddress.Address.IsIPv6LinkLocal)
                            if (!unicastIPAddress.Address.ToString().Contains(":"))
                                ipAddress += unicastIPAddress.Address;
            return ipAddress;
        }

        /// <summary>
        /// Get MAC address from IP
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public static string GetMacAddress(string ipAddress)
        {
            string macAddress = string.Empty;
            try
            {
                IPAddress dst = IPAddress.Parse(ipAddress); // the destination IP address
                byte[] macAddr = new byte[6];
                uint macAddrLen = (uint)macAddr.Length;

                if (SendARP(BitConverter.ToInt32(dst.GetAddressBytes(), 0), 0, macAddr, ref macAddrLen) != 0)
                    FileSystem.ErrorWriteLine("SendARP failed.");

                string[] str = new string[(int)macAddrLen];
                for (int i = 0; i < macAddrLen; i++)
                    str[i] = macAddr[i].ToString("x2");

                macAddress = string.Join(":", str);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine($"[SendARP] Something went wrong: {e.Message}");
                macAddress = string.Empty;
            }
            return macAddress;
        }

        /// <summary>
        /// Return IP address from hostname.
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static string GetIPV4FromHostName(string host)
        {
            var ip = "";
            try
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(host);
                var firstIp = hostEntry.AddressList[0];
                if(!firstIp.ToString().Contains(":"))
                    ip = firstIp.ToString();
            }
            catch
            {
                //Ignore
            }
            return ip;
        }


        /// <summary>
        /// Inititialize the Get IP and MAC from IP
        /// </summary>
        public static List<IPAndMac> GetIPsAndMac(string localIp)
        {
            var arpStream = ProcessStart.ExecuteAppWithOutput("arp", $"-a -N {localIp}");
            List<string> result = new List<string>();
            while (!arpStream.EndOfStream)
            {
                var line = arpStream.ReadLine().Trim();
                result.Add(line);
            }

            return result.Where(x => !string.IsNullOrEmpty(x) && (x.Contains("dynamic")))
                .Select(x =>
                {
                    string[] parts = x.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    return new IPAndMac { IP = parts[0].Trim(), MAC = parts[1].Trim().Replace("-", ":") };
                }).ToList();
        }

        public class IPAndMac
        {
            public string IP { get; set; }
            public string MAC { get; set; }
        }
    }
}
