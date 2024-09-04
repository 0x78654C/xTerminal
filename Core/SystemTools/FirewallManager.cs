using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using NetFwTypeLib;
using System.IO;
using System.Windows.Forms;
using System.Data;

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
        public void AddRule1(Action action, Protocol protocol)
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
        /// Adds or removes a firewall application rule.
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="pathApp"></param>
        /// <param name="profile"></param>
        /// <param name="direction"></param>
        /// <param name="fwaction"></param>
        /// <param name="localPort"></param>
        /// <param name="remotePort"></param>
        /// <param name="remoteAddress"></param>
        /// <param name="localAddress"></param>
        /// <param name="protocol"></param>
        /// <param name="description"></param>
        public void AddRule(string roleName, string pathApp, int profile, string direction,
            string fwaction, string localPort = "", string remotePort = "", string remoteAddress = "",
            string localAddress = "", int protocol = 256, string description = "")
        {
            var directionSet = (direction.ToLower().Contains("IN")) ? NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN : NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
            var actionSet = (fwaction.ToLower().Contains("allow")) ? NET_FW_ACTION_.NET_FW_ACTION_ALLOW : NET_FW_ACTION_.NET_FW_ACTION_BLOCK;

            INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
            firewallRule.Action = actionSet;
            firewallRule.Enabled = true;
            firewallRule.Protocol = protocol;
            firewallRule.Profiles = profile;
            firewallRule.ApplicationName = pathApp;
            firewallRule.Name = roleName;
            firewallRule.Description = description;
            if (!string.IsNullOrEmpty(localAddress))
                firewallRule.LocalAddresses = localAddress;
            if (!string.IsNullOrEmpty(remoteAddress))
                firewallRule.RemoteAddresses = remoteAddress;
            if (!string.IsNullOrEmpty(localPort))
            {
                if (protocol == 256)
                {
                    FileSystem.ErrorWriteLine("You cannot set local port when Protocol is set to ANY");
                    return;
                }
                firewallRule.LocalPorts = localPort;
            }
            if (!string.IsNullOrEmpty(remotePort))
            {
                if (protocol == 256)
                {
                    FileSystem.ErrorWriteLine("You cannot set remote port when Protocol is set to ANY");
                    return;
                }
                firewallRule.RemotePorts = remotePort;
            }
            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance
            (Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
            firewallRule.Direction = directionSet;
            firewallPolicy.Rules.Add(firewallRule);
        }


        public void AddPort(string path, NET_FW_RULE_DIRECTION_ d,
       NET_FW_ACTION_ fwaction, ActionAdd actionAdd, InterfaceTypes interfaceType)
        {

            INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(
            Type.GetTypeFromProgID("HNetCfg.FWRule"));
            firewallRule.Action = fwaction;
            firewallRule.Enabled = true;
            firewallRule.InterfaceTypes = interfaceType.ToString();
            firewallRule.ApplicationName = path;
            firewallRule.LocalPorts =
            firewallRule.Name = Path.GetFileName(path);
            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance
            (Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
            firewallRule.Direction = d;
            if (actionAdd.ToString() == "Add")
                firewallPolicy.Rules.Add(firewallRule);
            else
                firewallPolicy.Rules.Remove(firewallRule.Name);
        }


        /// <summary>
        /// Remove firewall role.
        /// </summary>
        /// <param name="roleName"></param>
        public void RemoveRole(string ruleName)
        {
            if (!IsRulePresent(ruleName))
            {
                FileSystem.ErrorWriteLine($"Firewall rule(s) does not exist: {ruleName}");
                return;
            }
            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance
          (Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

            var count = 0;
            foreach (INetFwRule rule in firewallPolicy.Rules)
                if (rule.Name == ruleName)
                {
                    firewallPolicy.Rules.Remove(ruleName);
                    count++;
                }

            if (!IsRulePresent(ruleName))
                FileSystem.SuccessWriteLine($"Firewall rule(s) was removed: {ruleName}. Total: {count} rules");
        }


        /// <summary>
        /// Check if firewall rule exist.
        /// </summary>
        /// <param name="ruleName"></param>
        /// <returns></returns>
        private bool IsRulePresent(string ruleName)
        {
            bool isPresent = false;
            Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);
            foreach (INetFwRule rule in fwPolicy2.Rules)
            {
                if (rule.Name == ruleName)
                {
                    isPresent = true;
                    break;
                }
            }
            return isPresent;
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
        public void ListRules(Direction directionSet)
        {
            try
            {
                Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);
                var columns = new string[] { "Action", "Protocol", "Source Address", "Source Ports", "Destination Address", "Destination Ports", "Application" };
                var direction = "";
                var profile = "";
                var protocol = "";
                var src_addr = "";
                var src_ports = "";
                var dest_addr = "";
                var dest_ports = "";
                var current_profile = GetProfileString(fwPolicy2.CurrentProfileTypes);


                foreach (INetFwRule rule in fwPolicy2.Rules)
                {
                    direction = GetDirectionString((int)rule.Direction);
                    protocol = GetProtocolString((int)rule.Protocol);

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

                    profile = GetProfileString((int)rule.Profiles);


                    if (direction == directionSet.ToString())
                        DisplayData(rule, profile, direction, protocol, src_addr, src_ports, dest_addr, dest_ports);

                    if (directionSet.ToString() == "AllDirections")
                        DisplayData(rule, profile, direction, protocol, src_addr, src_ports, dest_addr, dest_ports);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[-] ERROR: {0}", e.Message);
            }
        }

        /// <summary>
        /// Display firewall data.
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="profile"></param>
        /// <param name="direction"></param>
        /// <param name="protocol"></param>
        /// <param name="src_addr"></param>
        /// <param name="src_ports"></param>
        /// <param name="dest_addr"></param>
        /// <param name="dest_ports"></param>
        private void DisplayData(INetFwRule rule, string profile, string direction, string protocol, string src_addr, string src_ports,
            string dest_addr, string dest_ports)
        {
            var info = @$"-----------------------------------------------
{rule.Name,-60}|{GetActionString((int)rule.Action),-8}|{profile,-19}|{direction,-9}|{protocol,-10}|{src_addr,-16}|{src_ports,-14}|{dest_addr,-21}|{dest_ports,-19}|{rule.ApplicationName}";

            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                GlobalVariables.pipeCmdOutput += info + Environment.NewLine;
            else
                Console.WriteLine(info);
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

        public enum Direction
        {
            Inbound,
            Outbound,
            AllDirections
        }
    }
}
