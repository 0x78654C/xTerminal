﻿using Core;
using System;
using ping = Core.NetWork;

namespace Commands.TerminalCommands.Network
{
    public class CheckDomain : ITerminalCommand
    {
        /*
         Check  if an IP dor domain is down.
         */
        public string Name => "icheck";

        public void Execute(string arg)
        {
            try
            {
                string domain = arg.Split(' ')[1];
                if (ping.PingHost(domain))
                {
                    Console.WriteLine($"{domain} is online!");
                    return;
                }
                Console.WriteLine($"{domain} is down!");
            }
            catch
            {
                FileSystem.ErrorWriteLine("You must specify a domain or an IP address to check . Ex.: icheck google.com");
            }
        }
    }
}
