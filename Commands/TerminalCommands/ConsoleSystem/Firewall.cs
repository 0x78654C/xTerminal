using System;
using Core;
using Core.SystemTools;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Versioning;

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



Note: Requires administrator privileges.
";
        public void Execute(string arg)
        {
            try
            {
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
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine($"{ex.Message}. Use -h for more information!");
            }
        }
    }
}