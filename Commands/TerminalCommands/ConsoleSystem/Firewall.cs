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
    -list : List firewall rules.



Note: Requires administrator privileges.
";
        public void Execute(string arg)
        {
            try
            {
                arg = arg.Substring(2);

                // Display help message.
                if (arg.Trim() == "-h" && !GlobalVariables.isPipeCommand)
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                var fw = new FirewallManager();

                // List firewall rules.
                if (arg.StartsWith("-list"))
                {
                    fw.ListRules();
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