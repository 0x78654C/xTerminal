using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace Core
{
    public class NetWork
    {
        /// <summary>
        /// Verifies if IP is up or not
        /// </summary>
        /// <param name="ip"></param>
        /// <returns>verifies if IP is up or not</returns>

        public static bool pingH(string ip)
        {
            bool pingable = false;
            Ping pinger = null;
            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(ip);
                pingable = reply.Status == IPStatus.Success;

            }
            catch (PingException)
            {

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
        public static bool inetCK()
        {
            if (pingH("8.8.8.8") == false)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
