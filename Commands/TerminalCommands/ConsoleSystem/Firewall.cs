using System;
using Core;
using Core.SystemTools;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using NetFwTypeLib;
using System.ComponentModel.DataAnnotations;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class Firewall : ITerminalCommand
    {
        public string Name => "fw";
        private static string s_helpMessage = @"Usage of firewall command parameters:
    -list : List all firewall rules.
    -list -in  : List all inbound firewall rules.
    -list -out : List all outbound firewall rules.

Protocols code:
-1     : Unknown
0, 256 : ANY (default)
1      : ICMPv4
2      : IGMP
4      : IPv4
6      : TCP
17     : UDP
41     : IPv6
47     : GRE
58     : ICMPv6

Profiles code:
1      : Domain
2      : Private
3      : Domain, Private
4      : Public
5      : Domain, Public
6      : Private, Public
7      : All

Note: Requires administrator privileges.
";
        public void Execute(string arg)
        {
            try
            {
                // No parameter.
                if (arg == Name && !GlobalVariables.isPipeCommand)
                {
                    FileSystem.SuccessWriteLine("Use -h for more information!");
                    return;
                }

                arg = arg.Substring(2);

                // Display help message.
                if (arg.Trim() == "-h" && !GlobalVariables.isPipeCommand)
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                var fw = new FirewallManager();

                // List firewall rules.
                if (arg.Trim().StartsWith("-list"))
                {
                    if (arg.Contains("-in"))
                    {
                        fw.ListRules(FirewallManager.Direction.Inbound);
                        return;
                    }
                    if (arg.Contains("-out"))
                    {
                        fw.ListRules(FirewallManager.Direction.Outbound);
                        return;
                    }
                    fw.ListRules(FirewallManager.Direction.AllDirections);
                    return;
                }

                // Add firewall rule.
                if (arg.Trim().StartsWith("-add"))
                {
                    fw.AddRule("TestFW","c:\\t.exe", 7, NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN, NET_FW_ACTION_.NET_FW_ACTION_ALLOW,"","","127.0.0.1","127.0.0.1",
                        6);
                }

                // Remove firewall rule
                if (arg.Trim().StartsWith("-del"))
                {
                   var roleName =  arg.Trim().SplitByText("-del",1).Trim();
                    if(string.IsNullOrEmpty(roleName))
                    {
                        FileSystem.ErrorWriteLine("You need to add the role name. Use -h for more information!");
                        return;
                    }
                    fw.RemoveRole(roleName);
                    return;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                FileSystem.ErrorWriteLine($"{ex.Message}. Requires administrator privileges. Use -h for more information!");
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine($"{ex.ToString()}. Use -h for more information!");
            }
        }
    }
}