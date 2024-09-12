using System;
using Core;
using Core.SystemTools;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class Firewall : ITerminalCommand
    {
        public string Name => "fw";
        private List<string> _params = ["-n","-p","-pf","-di","-a","-lP", "-rP", "-lA", "-rA", "-pr", "-de","-e"];
        private static string s_helpMessage = @"Usage of fw command parameters:
    -list : List all firewall rules.
    -list -in  : List all inbound firewall rules.
    -list -out : List all outbound firewall rules.

    -add : Add firewall rule with following options:
         -n : Set rule name.
         -e : Enable or disable the rule.  Ex.: -e true or -e false. (Default true)
         -p : Set path to application executable.
         -pf : Set profile code. (See list bellow).
         -di : Set rule direction. Ex.: -di IN or -di OUT. (IN =  inbound, OUT = Outbound)
         -a  : Set action. Ex.: -a allow or -a block
         -lP : Set local port.
         -rP : Set remote port.
         -lA : Set local address.
         -rA : Set remote address.
         -pr : Set protocol code. (See list bellow).
         -de : Set description.
       Example : fw -add -n New Rule -p c:\a b\test.exe -e true -pf 3 -pr 17 -di IN -a block -de Block test.exe for private connections type UDP.

    -del : Removes firewall rule by name. 
    -en  : Enable firewall rule by name.
    -dis : Disable firewall rule by name.

Profiles code:
1      : Domain
2      : Private
3      : Domain, Private
4      : Public
5      : Domain, Public
6      : Private, Public
7      : All

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
                    var localPort = "*";
                    var remotePort = "*";
                    var remoteAddress = "*";
                    var localAddress = "*";
                    var protocol = 0;
                    var description = "";
                    var enable = "";

                    if (arg.Contains("-n "))
                    {
                        var desData = arg.SplitByText("-n ", 1);
                        var isParamPresent = _params.Any(param => desData.Contains(param));
                        if (isParamPresent)
                        {
                            var paramPresent = _params.Where(param => desData.Contains(param)).Select(x => x).FirstOrDefault();
                            name = desData.SplitByText(paramPresent, 0).Trim();
                        }
                        else
                            name = desData.Trim();
                    }

                    if (arg.Contains("-p "))
                    {
                        var desData = arg.SplitByText("-p ", 1);
                        var isParamPresent = _params.Any(param => desData.Contains(param));
                        if (isParamPresent)
                        {
                            var paramPresent = _params.Where(param => desData.Contains(param)).Select(x => x).FirstOrDefault();
                            pathApp = desData.SplitByText(paramPresent, 0).Trim();
                        }
                        else
                            pathApp = desData.Trim();
                    }

                    if (arg.Contains("-pf "))
                        profile = Int32.Parse(arg.GetParamValue("-pf "));

                    if (arg.Contains("-di "))
                        direction = arg.GetParamValue("-di ");

                    if (arg.Contains("-a "))
                        action = arg.GetParamValue("-a ");

                    if (arg.Contains("-lP "))
                        localPort = arg.GetParamValue("-lP ");

                    if (arg.Contains("-rP "))
                        remotePort = arg.GetParamValue("-rP ");
                    
                    if (arg.Contains("-lA "))
                        localAddress = arg.GetParamValue("-lA ");

                    if (arg.Contains("-rA "))
                        remoteAddress = arg.GetParamValue("-rA ");

                    if (arg.Contains("-pr "))
                        protocol = Int32.Parse(arg.GetParamValue("-pr "));
                    if (arg.Contains("-e "))
                        enable = arg.GetParamValue("-e ");

                    if (arg.Contains("-de "))
                    {
                        var desData = arg.SplitByText("-de", 1);
                        var isParamPresent = _params.Any(param => desData.Contains(param));
                        if (isParamPresent)
                        {
                            var paramPresent = _params.Where(param => desData.Contains(param)).Select(x => x).FirstOrDefault();
                            description = desData.SplitByText(paramPresent,0).Trim();
                        }
                        else
                            description = desData.Trim();
                    }

                    // Add role.
                    fw.AddRule(name,pathApp, profile, direction, action,localPort,remotePort,remoteAddress,localAddress,protocol,description,enable);
                }

                // Remove firewall rule
                if (arg.Trim().StartsWith("-del"))
                {
                    var roleName = arg.SplitByText("-del ", 1);
                    
                    if (string.IsNullOrEmpty(roleName))
                    {
                        FileSystem.ErrorWriteLine("You need to add the role name. Use -h for more information!");
                        return;
                    }

                    // Rmove rule from firewall.
                    fw.RemoveRole(roleName);
                }

                // Disable Rule.
                if (arg.Trim().StartsWith("-dis"))
                {
                    var roleName = arg.SplitByText("-dis ", 1);

                    if (string.IsNullOrEmpty(roleName))
                    {
                        FileSystem.ErrorWriteLine("You need to add the role name. Use -h for more information!");
                        return;
                    }

                    fw.SetEnableDisbale(roleName,false);
                }

                // Disable Rule.
                if (arg.Trim().StartsWith("-en"))
                {
                    var roleName = arg.SplitByText("-en ", 1);

                    if (string.IsNullOrEmpty(roleName))
                    {
                        FileSystem.ErrorWriteLine("You need to add the role name. Use -h for more information!");
                        return;
                    }

                    fw.SetEnableDisbale(roleName, true);
                }
            }

            catch (UnauthorizedAccessException ex)
            {
                FileSystem.ErrorWriteLine($"{ex.Message}. Requires administrator privileges. Use -h for more information!");
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine($"{ex.Message}. Use -h for more information!");
            }
        }
    }
}