/*
 Trace Command Implementation
 */

using Core;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.Network
{
    [SupportedOSPlatform("Windows")]
    public class Trace : ITerminalCommand
    {
        public string Name => "trace";
        private static string s_helpMessage = @"Usage of trace command:

Example 1: trace google.com  (for normal tracerout command)
Example 2: trace google.com -ipv6  (for IPv6 traceroute enabled)
Example 3: trace google.com -hops 50  (for traceroute with 50 hops)
Example 4: trace google.com -timeout 1000  (for traceroute with 1000 ms timeout)
Example 5: trace google.com -hops 50 -timeout 1000 -ipv6  (for traceroute with 50 hops, 1000 ms timeout and IPv6 traceroute enabled)

Default timeout is 500 ms and max hops is 100.

Command can be canceled with CTRL+X key combination.
";

        public void Execute(string arg)
        {
            try
            {
                if (arg == $"{Name} -h" && !GlobalVariables.isPipeCommand)
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }
                if (arg.Length == 5 && !GlobalVariables.isPipeCommand)
                {
                    FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                    return;
                }
                GlobalVariables.eventCancelKey = false;
                GlobalVariables.eventKeyFlagX = true;
                arg = arg.Replace(Name, "").Trim();
                var ip = arg.Split(' ')[0].Trim();

                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                {
                    ip = GlobalVariables.pipeCmdOutput.Trim();
                    GlobalVariables.pipeCmdOutput = "";
                }

                if (string.IsNullOrEmpty(ip))
                    throw new ArgumentException("Address cannot be null or empty!");

                bool isIPv6 = false;
                int maxHops = 100;
                int timeOut = 500;

                if (arg.Contains("-ipv6"))
                    isIPv6 = true;

                if (arg.Contains("-hops"))
                    maxHops = int.Parse(arg.GetParamValue("-hops"));

                if (arg.Contains("-timeout"))
                    timeOut = int.Parse(arg.GetParamValue("-timeout"));

                if (string.IsNullOrEmpty(ip))
                {
                    FileSystem.ErrorWriteLine("Please provide an IP address or hostname to trace!");
                    return;
                }
                if (!isIPv6)
                {
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                        GlobalVariables.pipeCmdOutput += $"Starting traceroute for: {ip}, Hops: {maxHops}, TimeOut: {timeOut}, IPv6: Disabled\n";
                    else
                        FileSystem.SuccessWriteLine($"Starting traceroute for: {ip}, Hops: {maxHops}, TimeOut: {timeOut}, IPv6: Disabled");
                }
                else
                {
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                        GlobalVariables.pipeCmdOutput += $"Starting traceroute for: {ip}, Hops: {maxHops}, TimeOut: {timeOut}, IPv6: Enabled\n";
                    else
                        FileSystem.SuccessWriteLine($"Starting traceroute for: {ip}, Hops: {maxHops}, TimeOut: {timeOut}, IPv6: Enabled");
                }

                var route = Core.Network.TraceRoute.GetTraceRoute(ip, maxHops, timeOut, isIPv6);
                foreach (var (sentTtl, reply) in route)
                {
                    var address = reply.Address?.ToString() ?? "N/A";
                    var hostname = "N/A";

                    if (reply.Address != null)
                    {
                        try
                        {
                            var hostEntry = Dns.GetHostEntry(reply.Address);
                            hostname = hostEntry.HostName;
                        }
                        catch
                        {
                            // Ignore if reverse DNS fails
                        }
                    }

                    var rtt = reply.RoundtripTime;
                    string status = reply.Status.ToString();
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                        GlobalVariables.pipeCmdOutput += $"Hop {sentTtl} - Host: {hostname} ({address}), RoundTrip: {rtt} ms, Status: {status}\n";
                    else
                        FileSystem.SuccessWriteLine($"Hop {sentTtl} - Host: {hostname} ({address}), RoundTrip: {rtt} ms, Status: {status}");



                    if (reply.Status == IPStatus.Success)
                    {
                        if (!GlobalVariables.isPipeCommand)
                            FileSystem.SuccessWriteLine($"Trace completed successfully to {ip}.");
                        return;
                    }
                    if (GlobalVariables.eventCancelKey)
                    {
                        FileSystem.SuccessWriteLine("Command stopped!");
                        break;
                    }
                    GlobalVariables.eventCancelKey = false;
                }
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine(ex.Message);
                return;
            }
        }
    }
}
