﻿using System.Linq;
using System.Net.NetworkInformation;

namespace Core
{
    /*Network class for check Ping and Internet connection.*/
    public class NetWork
    {

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
            catch (PingException p)
            {
                FileSystem.ErrorWriteLine(p.Message);
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
