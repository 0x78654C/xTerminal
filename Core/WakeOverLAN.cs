using System;
using System.Net;
using System.Globalization;
using Log = Core.FileSystem;
using System.Text.RegularExpressions;
using System.Runtime.Versioning;

namespace Core
{
    /* Class for send WOL magic packet. */
    [SupportedOSPlatform("Windows")]
    public class WakeOverLAN
    {
        private string IP { get; set; }
        private string MAC { get; set; }
        private int Port { get; set; }

        /// <summary>
        /// Wake over LAN.
        /// </summary>
        /// <param name="ip">Destination IP address.</param>
        /// <param name="mac">Destination MAC address.</param>
        /// <param name="port">WOL port (default is 9).</param>
        public WakeOverLAN(string ip, string mac, int port)
        {
            IP = ip;
            MAC = mac;
            Port = port;
        }


        /// <summary>
        /// Send Wake over LAN magic packet to destination machine.
        /// </summary>
        public void Wake()
        {
            if (string.IsNullOrEmpty(IP) || string.IsNullOrEmpty(MAC) || Port == 0)
            {
                Log.ErrorWriteLine("Parameters should not be empty!");
                return;
            }
            
            var host = NetWork.GetIPV4FromHostName(IP);
            if(!string.IsNullOrEmpty(host))
                IP = host.Trim();

            int countSymbolIP = Regex.Matches(IP, "\\.").Count;
            if (countSymbolIP != 3)
            {
                Log.ErrorWriteLine("IP address format is incorrect!");
                return;
            }

            int countSymbolMAC = Regex.Matches(MAC, ":").Count;
            if (MAC.Length != 17 || countSymbolMAC != 5)
            {
                Log.ErrorWriteLine("MAC address format is incorrect!");
                return;
            }

            try
            {
                string macAddress = MAC.Replace(":", "");
                IPAddress iPAddress = IPAddress.Parse(IP);
                WOLUdpClient client = new WOLUdpClient();
                client.Connect(IP, Port);

                if (client.IsClientInBroadcastMode())
                {
                    int byteCount = 0;
                    byte[] bytes = new byte[102];
                    for (int trailer = 0; trailer < 6; trailer++)
                    {
                        bytes[byteCount++] = 0xFF;
                    }
                    for (int macPackets = 0; macPackets < 16; macPackets++)
                    {
                        int i = 0;
                        for (int macBytes = 0; macBytes < 6; macBytes++)
                        {
                            bytes[byteCount++] = byte.Parse(macAddress.Substring(i, 2), NumberStyles.HexNumber);
                            i += 2;
                        }
                    }
                    client.Send(bytes, byteCount);
                    Log.ColorConsoleTextLine(ConsoleColor.Green, $"WOL packet is sent to IP: {IP} / MAC: {MAC}");
                    client.Close();
                }
                else
                {
                    Log.ErrorWriteLine("Remote client could not be set in broadcast mode. Please check the settings!");
                }
            }
            catch (Exception e)
            {
                Log.ErrorWriteLine(e.Message);
            }
        }
    }
}
