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
        private static string s_helpMessage = @"Usage of fw command parameters:
    -list : List all firewall rules.
    -list -in  : List all inbound firewall rules.
    -list -out : List all outbound firewall rules.

    -add : Add firewall rule with following options:
         -n : Set rule name.
         -p : Set path to application executable.
         -prf : Set profile code. (See list bellow).
         -di : Set rule direction. Ex.: -di IN or -di OUT. (IN =  inbound, OUT = Outbound)
         -a  : Set action. Ex.: -a allow or -a block
         -lP : Set local port.
         -rP : Set remote port.
         -lA : Set local address.
         -rA : Set remote address.
         -pr : Set protocol code. (See list bellow).
         -de : Set description.

    -del : Removes a firewall rule by name.

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
                    var name = "";
                    var pathApp = "";
                    var profile = 0;
                    string direction="";
                    string action="";
                    var localPort = "";
                    var remotePort = "";
                    var remoteAddress = "";
                    var localAddress = "";
                    var protocol = 0;
                    var description = "";



                    
                    fw.AddRule(name,pathApp, profile, direction, action,localPort,remotePort,remoteAddress,localAddress,protocol,description);
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