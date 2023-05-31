using Core.SystemTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        /// Ouptus the final count and reset counters.
        /// </summary>
        private static void FinalReplayOutput()
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
        /// <param name="pingReplys"></param>
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
                Console.WriteLine($"Status: {s_pingReply.Status} | Buffer: {s_pingReply.Buffer.Length} | Time: {s_pingReply.RoundtripTime} ms | TTL: {options.Ttl} | Adress: {s_pingReply.Address}");
            }
            else
            {
                s_failure++;
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
                    nicOuptut += $"Description:".PadRight(15, ' ') + $"{networkInterface.Description}\n";
                    nicOuptut += $"IP Address: \n{ipAddress} \n";
                    nicOuptut += $"MASK: \n{mask}\n";
                    nicOuptut += $"Gateway: \n{gateway}\n";
                    nicOuptut += $"MAC Address: ".PadRight(15, ' ') + $"{mac}\n";
                    nicOuptut += $"DNS: \n{dnsAddr}\n";
                }
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
                            if(!unicastIPAddress.Address.IsIPv6LinkLocal)
                                 ipAddress += unicastIPAddress.Address;
            return ipAddress;
        }

        public static string GetMacAddress(string ipAddress)
        {
            string macAddress = string.Empty;
            System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
            pProcess.StartInfo.FileName = "arp";
            pProcess.StartInfo.Arguments = "-a " + ipAddress;
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.StartInfo.CreateNoWindow = true;
            pProcess.Start();
            string strOutput = pProcess.StandardOutput.ReadToEnd();
            string[] substrings = strOutput.Split('-');
            if (substrings.Length >= 8)
            {
                macAddress = substrings[3].Substring(Math.Max(0, substrings[3].Length - 2))
                         + "-" + substrings[4] + "-" + substrings[5] + "-" + substrings[6]
                         + "-" + substrings[7] + "-"
                         + substrings[8].Substring(0, 2);
                return macAddress;
            }

            else
            {
                return "not found";
            }
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
                    return new IPAndMac { IP = parts[0].Trim(), MAC = parts[1].Trim() };
                }).ToList();
        }

        /// <summary>
        /// Find MAC address form IP.
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static string FindMacFromIp(string ip)
        {
            var init = GetIPsAndMac("127.0.0.1");
            IPAndMac item = null;
            try
            {
                item = init.SingleOrDefault(x => x.IP == ip);
                if (item == null)
                    return null;
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
            return item.MAC;
        }

        /// <summary>
        /// Find Ip from MAC function.
        /// </summary>
        /// <param name="macAddress"></param>
        /// <returns></returns>
        public static string FindIPFromMacAddress(string macAddress)
        {
            try
            {
                string command = $"Get-NetNeighbor -LinkLayerAddress {macAddress} | Select -ExpandProperty ipaddress";
                string psOut = PSScript.RunScript(command).Split('\n')[1];
                return psOut;

            }
            catch (Exception)
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Possible spoof attack to be ongoning!");
            }
            return "";
        }

        public class IPAndMac
        {
            public string IP { get; set; }
            public string MAC { get; set; }
        }
    }
}
