/*

Class for performing a traceroute operation using the Ping class. 
 
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Core.Network
{
    public class TraceRoute
    {
        public static IEnumerable<(int SentTtl, PingReply Reply)> GetTraceRoute(string address, int maxHops, int timeOut, bool isIPv6 = false)
        {
            IPAddress ipv4Address = null;
            if (!isIPv6)
            {
                // Force IPv4

                foreach (var ip in Dns.GetHostAddresses(address))
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4 only
                    {
                        ipv4Address = ip;
                        break;
                    }
                }

                if (ipv4Address == null)
                    throw new Exception("No IPv4 address found for the specified host.");
            }
            byte[] buffer = new byte[32];
            int ttl = 1;

            using var ping = new Ping();

            while (ttl < maxHops)
            {
                var pingOptions = new PingOptions(ttl, true);
                var reply = (!isIPv6) ? ping.Send(ipv4Address, timeOut, buffer, pingOptions) : ping.Send(address, timeOut, buffer, pingOptions);
                yield return (ttl, reply);

                if (reply.Status == IPStatus.Success)
                    break;

                ttl++;
            }
        }
    }
}
