using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

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

        public enum Protocol
        {
            TCP,
            UDP
        }

    }
}
