using System;
using Core;
using Core.SystemTools;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class Service : ITerminalCommand
    {
        public string Name => "service";
        public void Execute(string arg)
        {
            try
            {
                arg = arg.Substring(8);

                // List services.
                if (arg == "-l")
                {
                    var serviceC = new ServiceC();
                    serviceC.Run(ServiceC.ActionService.List);
                    return;
                }

                // Service description
                if (arg.StartsWith("-d "))
                {
                    string serviceName = arg.SplitByText("-s", 1).Trim();
                    var serviceC = new ServiceC();
                    serviceC.ServiceName = serviceName;
                    serviceC.Run(ServiceC.ActionService.Description);
                    return;
                }

                // Running State
                if (arg.StartsWith("-s "))
                {
                    string serviceName = arg.SplitByText("-s",1).Trim();
                    var serviceC = new ServiceC();  
                    serviceC.ServiceName = serviceName;
                    serviceC.Run(ServiceC.ActionService.Status);
                    return;
                }

                // Stop service
                if (arg.StartsWith("-stop "))
                {
                    string serviceName = arg.SplitByText("-stop", 1).Trim();
                    var serviceC = new ServiceC();
                    serviceC.ServiceName = serviceName;
                    serviceC.Run(ServiceC.ActionService.Stop);
                    return;
                }

                // Stop service
                if (arg.StartsWith("-start "))
                {
                    string serviceName = arg.SplitByText("-start", 1).Trim();
                    var serviceC = new ServiceC();
                    serviceC.ServiceName = serviceName;
                    serviceC.Run(ServiceC.ActionService.Start);
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
