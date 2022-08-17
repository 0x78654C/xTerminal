using System.Net.Sockets;

namespace Core
{
    public class WOLUdpClient : UdpClient
    {
        public WOLUdpClient(): base() { }

        /// <summary>
        /// Set up the UDP client to broadcast packets.
        /// </summary>
        /// <returns></returns>
        public bool IsClientInBroadcastMode()
        {
            bool broadcast = false;
            if (this.Active)
            {
                try
                {
                    this.Client.SetSocketOption(SocketOptionLevel.Socket,
                        SocketOptionName.Broadcast, 0);
                    broadcast = true;
                }
                catch
                {
                    broadcast = false;
                }
            }
            return broadcast;
        }
    }
}
