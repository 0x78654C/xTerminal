﻿using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using NetFwTypeLib;
using System.IO;
using System.Windows.Forms;

namespace Core.SystemTools
{
    [SupportedOSPlatform("Windows")]
    public class FirewallManager
    {
        public string RuleName { get; set; }
        public string Port { get; set; }

        private string RunArg { get; set; }

        public FirewallManager()
        {
        }
        public void AddRule(Action action, Protocol protocol)
        {
            RunArg = "advfirewall firewall add rule name=" + "\"" + "" + RuleName + "\"" + " action=" + action.ToString() + " protocol=" + protocol + " dir=in localport=" + Port + "";

            if (string.IsNullOrEmpty(RuleName))
            {
                FileSystem.ErrorWriteLine("You must add a name for the rule!");
                return;
            }

            if (string.IsNullOrEmpty(Port))
            {
                FileSystem.ErrorWriteLine("You must add a port for the rule!");
                return;
            }

            var outPut = ProcessStart.ExecuteAppWithOutput("netsh ", RunArg);
            FileSystem.SuccessWriteLine(outPut.ReadToEnd());
            INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
            firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;

        }

        /// <summary>
        /// Adds or removes a firewall rule.
        /// </summary>
        /// <param name="path">The path to the executable.</param>
        /// <param name="d">The affected connection type.</param>
        /// <param name="fwaction">Rule action.</param>
        /// <param name="action">"Add (1) or 
        /// remove (0) the specified rule."</param>
        private void AddApplication(string path, NET_FW_RULE_DIRECTION_ d,
        NET_FW_ACTION_ fwaction, ActionAdd actionAdd, InterfaceTypes interfaceType)
        {
            try
            {
                INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FWRule"));
                firewallRule.Action = fwaction;
                firewallRule.Enabled = true;
                firewallRule.InterfaceTypes = interfaceType.ToString();
                firewallRule.ApplicationName = path;
                firewallRule.Name = "CSwitch: " + Path.GetFileName(path);
                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance
                (Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
                firewallRule.Direction = d;
                if (actionAdd.ToString() == "Add")
                    firewallPolicy.Rules.Add(firewallRule);
                else
                    firewallPolicy.Rules.Remove(firewallRule.Name);
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine(ex.Message);
            }
        }

        /*
         
         
         FWRule(@"C:\test.exe", NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT, 
NET_FW_ACTION_.NET_FW_ACTION_BLOCK, "1"); 
         */

        private void AddPort(string path, NET_FW_RULE_DIRECTION_ d,
       NET_FW_ACTION_ fwaction, ActionAdd actionAdd, InterfaceTypes interfaceType)
        {
            try
            {
                INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FWRule"));
                firewallRule.Action = fwaction;
                firewallRule.Enabled = true;
                firewallRule.InterfaceTypes = interfaceType.ToString();
                firewallRule.ApplicationName = path;
                firewallRule.RemotePorts =
                firewallRule.Name = "CSwitch: " + Path.GetFileName(path);
                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance
                (Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
                firewallRule.Direction = d;
                if (actionAdd.ToString() == "Add")
                    firewallPolicy.Rules.Add(firewallRule);
                else
                    firewallPolicy.Rules.Remove(firewallRule.Name);
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine(ex.Message);
            }
        }

        // Reference: http://forums.purebasic.com/english/viewtopic.php?f=12&t=33608
        static string GetProfileString(int profile)
        {
            switch (profile)
            {
                case 1:
                    return "Domain";
                case 2:
                    return "Private";
                case 3:
                    return "Domain, Private";
                case 4:
                    return "Public";
                case 5:
                    return "Domain, Public";
                case 6:
                    return "Private, Public";
                case 7:
                case 2147483647:
                    return "All";
                default:
                    return profile.ToString();
            }
        }


        // Reference: https://github.com/TechnitiumSoftware/TechnitiumLibrary/blob/master/TechnitiumLibrary.Net.Firewall/WindowsFirewall.cs
        static string GetProtocolString(int protocol)
        {
            switch (protocol)
            {
                case -1:
                    return "Unknown";
                case 0:
                case 256:
                    return "ANY";
                case 1:
                    return "ICMPv4";
                case 2:
                    return "IGMP";
                case 4:
                    return "IPv4";
                case 6:
                    return "TCP";
                case 17:
                    return "UDP";
                case 41:
                    return "IPv6";
                case 47:
                    return "GRE";
                case 58:
                    return "ICMPv6";
                default:
                    return "Invalid"; // protocol.ToString();
            }
        }


        // Reference: https://docs.microsoft.com/en-us/windows/win32/api/icftypes/ne-icftypes-net_fw_rule_direction
        // Reference: http://forums.purebasic.com/english/viewtopic.php?f=12&t=33608
        static string GetDirectionString(int direction)
        {
            switch (direction)
            {
                case 1:
                    return "Inbound";
                case 2:
                    return "Outbound";
                default:
                    return "Invalid"; // direction.ToString();
            }
        }


        // Reference: https://docs.microsoft.com/en-us/windows/win32/api/icftypes/ne-icftypes-net_fw_rule_direction
        static string GetActionString(int action)
        {
            switch (action)
            {
                case 0:
                    return "Block";
                case 1:
                    return "Allow";
                default:
                    return "Invalid"; // action.ToString();
            }
        }


        // Source: https://stackoverflow.com/questions/3261451/using-a-bitmask-in-c-sharp
        [Flags]
        public enum Profile
        {
            None = 0,
            Domain = 1,
            Private = 2,
            Public = 4
        }
        public void ListRules()
        {
            try
            {
                Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);

                string[] columns = new string[] { "Action", "Protocol", "Source Address", "Source Ports", "Destination Address", "Destination Ports", "Application" };
                int col_spacer = 2;
                string direction;
                string profile;
                string protocol;
                string src_addr;
                string src_ports;
                string dest_addr;
                string dest_ports;
                string current_profile = GetProfileString(fwPolicy2.CurrentProfileTypes);


                foreach (INetFwRule rule in fwPolicy2.Rules)
                {
                    direction = GetDirectionString((int)rule.Direction);
                    protocol = GetProtocolString((int)rule.Protocol);

                    // Determine source / destination based on directionality of the traffic
                    // Because explicitly stating source and destination makes way more sense than manually determining it from local / remote and directionality of traffic
                    if (direction == "Inbound")
                    {
                        src_addr = rule.RemoteAddresses ?? "";
                        src_ports = rule.RemotePorts ?? "";
                        dest_addr = rule.LocalAddresses ?? "";
                        dest_ports = rule.LocalPorts ?? "";
                    }
                    else if (direction == "Outbound")
                    {
                        src_addr = rule.LocalAddresses ?? "";
                        src_ports = rule.LocalPorts ?? "";
                        dest_addr = rule.RemoteAddresses ?? "";
                        dest_ports = rule.RemotePorts ?? "";
                    }
                    else
                    {
                        src_addr = "";
                        src_ports = "";
                        dest_addr = "";
                        dest_ports = "";
                    }
                    // Skip disabled rules and invalid protocols in non-verbose mode
                    if (!rule.Enabled || protocol == "Invalid")
                    {
                        continue;
                    }

                    profile = GetProfileString((int)rule.Profiles);

                    // Only list rules from the current profile
                    if (profile == "All" || profile.Contains(current_profile))
                    {
                        Console.WriteLine($"{GetActionString((int)rule.Action),-8}{protocol,-10}{src_addr,-16}{src_ports,-14}{dest_addr,-21}{dest_ports,-19}{rule.ApplicationName}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[-] ERROR: {0}", e.Message);
            }
        }

        public void RemoveRule()
        {
            if (string.IsNullOrEmpty(RuleName))
            {
                FileSystem.ErrorWriteLine("You must add the rule name that you want to remove!");
                return;
            }

            RunArg = $"netsh advfirewall firewall delete rule {RuleName}";
            var outPut = ProcessStart.ExecuteAppWithOutput("netsh ", RunArg);
            FileSystem.SuccessWriteLine(outPut.ReadToEnd());
        }
        public enum Action
        {
            Allow,
            Block
        }

        public enum InterfaceTypes
        {
            Private,
            Public,
            All
        }

        public enum Protocol
        {
            TCP,
            UDP
        }

        public enum ActionAdd
        {
            Add,
            Remove
        }
    }
}
