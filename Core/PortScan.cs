using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Threading;

namespace Core
{
    // <summary>
    /// Use Sockets to Scan the Ports on a Machine.
    /// </summary>
    /// <remarks>
    /// From the Oreilly C# 6.0 Cookbook
    /// https://github.com/oreillymedia/c_sharp_6_cookbook
    /// http://shop.oreilly.com/product/0636920037347.do
    /// edited by x_coding
    /// </remarks>

    public class PortScan
    {
        /// <summary>
        /// Run port scan for check active/ non active.
        /// </summary>
        /// <param name="ipAddress">IP Address/Hostname</param>
        /// <param name="minPort">min port</param>
        /// <param name="maxPort">Max port (max 65535)</param>
        /// <param name="timeOut">check port time out (default 500 ms)</param>
        public static void RunPortScan(string ipAddress, int minPort, int maxPort, int timeOut)
        {
            if (minPort == maxPort)
            {
                Console.Write($"> Checking port {minPort} on ");
                FileSystem.ColorConsoleText(ConsoleColor.Cyan, $"{ipAddress}");
                Console.WriteLine($"...\n");
            }
            else
            {
                Console.Write($"> Checking ports {minPort}-{maxPort} on ");
                FileSystem.ColorConsoleText(ConsoleColor.Cyan, $"{ipAddress}");
                Console.WriteLine($"...\n");
            }

            PortScanner cps = new PortScanner(ipAddress, minPort, maxPort);
            var progress = new Progress<PortScanner.PortScanResult>();

            progress.ProgressChanged += (sender, args) =>
            {
                if (args.IsPortOpen)
                {
                    Console.Write($" {args.PortNum} | ");
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Green, $"Open");
                }
                else
                {
                    if (minPort == maxPort)
                    {
                        Console.Write($" {args.PortNum} | ");
                        FileSystem.ColorConsoleTextLine(ConsoleColor.Red, $"Close");
                    }
                }
            };

            cps.Scan(progress, timeOut);
            Thread.Sleep(5);
            cps.LastPortScanSummary();
        }


        internal class PortScanner
        {
            private const int PORT_MIN_VALUE = 1;
            private const int PORT_MAX_VALUE = 65535;

            private List<int> _openPorts;

            public ReadOnlyCollection<int> OpenPorts => new ReadOnlyCollection<int>(_openPorts);

            public int MinPort { get; } = PORT_MIN_VALUE;
            public int MaxPort { get; } = PORT_MAX_VALUE;

            public string Host { get; } = "127.0.0.1"; // localhost

            public PortScanner()
            {
                // defaults are already set for ports & localhost
                SetupLists();
            }

            public PortScanner(string host, int minPort, int maxPort)
            {
                if (minPort > maxPort)
                    throw new ArgumentException("Min port cannot be greater than max port");

                if (minPort < PORT_MIN_VALUE || minPort > PORT_MAX_VALUE)
                    throw new ArgumentOutOfRangeException(
                        $"Min port cannot be less than {PORT_MIN_VALUE} " +
                        $"or greater than {PORT_MAX_VALUE}");

                if (maxPort < PORT_MIN_VALUE || maxPort > PORT_MAX_VALUE)
                    throw new ArgumentOutOfRangeException(
                        $"Max port cannot be less than {PORT_MIN_VALUE} " +
                        $"or greater than {PORT_MAX_VALUE}");

                Host = host;
                MinPort = minPort;
                MaxPort = maxPort;

                SetupLists();
            }

            private void SetupLists()
            {
                // set up lists with capacity to hold half of range
                // since we can't know how many ports are going to be open
                // so we compromise and allocate enough for half

                // rangeCount is max - min + 1
                int rangeCount = (MaxPort - MinPort) + 1;

                // if there are an odd number, bump by one to get one extra slot
                if (rangeCount % 2 != 0)
                {
                    rangeCount += 1;
                }

                // reserve half the ports in the range for each
                _openPorts = new List<int>(rangeCount / 2);
            }

            internal class PortScanResult
            {
                public int PortNum { get; set; }
                public bool IsPortOpen { get; set; }
            }

            private void CheckPort(int port, int timeOut, IProgress<PortScanResult> progress)
            {
                if (IsPortOpen(port, timeOut))
                {
                    // if we got here it is open
                    _openPorts.Add(port);

                    // notify anyone paying attention
                    progress?.Report(new PortScanResult { PortNum = port, IsPortOpen = true });
                }
                else
                {
                    // server doesn't have that port open
                    progress?.Report(new PortScanResult() { PortNum = port, IsPortOpen = false });
                }
            }

            private bool IsPortOpen(int port, int timeOut)
            {
                Socket socket = null;
                try
                {

                    TcpClient tcpClient = new TcpClient();
                    IAsyncResult ar = tcpClient.BeginConnect(Host, port, null, true);
                    return ar.AsyncWaitHandle.WaitOne(timeOut, false);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.ConnectionRefused)
                    {
                        return false;
                    }
                }
                finally
                {
                    if (socket?.Connected ?? false)
                    {
                        socket?.Disconnect(false);
                    }
                    socket?.Close();
                }

                return false;
            }

            public void Scan(IProgress<PortScanResult> progress, int timeOut)
            {
                for (int port = MinPort; port <= MaxPort; port++)
                {
                    if (GlobalVariables.eventCancelKey)
                    {
                        GlobalVariables.eventCancelKey = false;
                        return;
                    }
                    CheckPort(port,timeOut, progress);
                }
            }

            public void LastPortScanSummary()
            {
                // display "0" or comma delimited list of open ports
                string openPorts = (_openPorts.Count == 0)
                    ? "0"
                    : string.Join(",", _openPorts);

                Console.WriteLine();
                Console.WriteLine("-----------------");
                Console.WriteLine("Port Scan Results");
                Console.WriteLine("-----------------");
                Console.WriteLine();
                Console.WriteLine($"Open Ports......: {openPorts}");
                Console.WriteLine();
                GlobalVariables.eventKeyFlagX = false;
            }
        }
    }
}
