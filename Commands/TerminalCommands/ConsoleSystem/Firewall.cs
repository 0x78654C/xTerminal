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
    public class Firewall: ITerminalCommand
    {
        public string Name => "fw";
        public void Execute(string arg)
        {
            var fw = new FirewallManager();
            fw.Port = "2122";
            fw.RuleName = "testRule";
            //fw.AddRule(FirewallManager.Action.Allow, FirewallManager.Protocol.TCP);
            fw.ListRules();
        }
    }
}
