using System;
using Core;
using Core.SystemTools;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Diagnostics.CodeAnalysis;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class Service : ITerminalCommand
    {
        public string Name => "service";
        private static string s_helpMessage = @"Usage of service command:
Local:
    -l : List all local services running on computer.
    -d <service_name> : Return the description for a specific service.
    -s <service_name> : Return the state for a specific service.
    -stop <service_name>  : Stops a specific service service.
    -start <service_name> : Starts a specific service.

Remote:
    -l -r <machine_name/IP> : List all local services running on a remote computer.
    -d <service_name> -r <machine_name/IP> : Return the description for a specific service.
    -s <service_name> -r <machine_name/IP> : Return the state for a specific service.
    -stop <service_name> -r <machine_name/IP>  : Stops a specific service service.
    -start <service_name> -r <machine_name/IP> : Starts a specific service.

Note: Requires administrator privileges.
";
        public void Execute(string arg)
        {
            try
            {
                arg = arg.Substring(8);

                // Display help message.
                if (arg.Trim() == "-h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                // List services.
                if (arg.StartsWith("-l"))
                {
                    var serviceC = new ServiceC();
                    if (arg.Contains("-r"))
                    {
                        var machine = arg.SplitByText("-r", 1).Trim();

                        var isMachineUp = NetWork.PingHost(machine);
                        if (!isMachineUp)
                        {
                            FileSystem.ErrorWriteLine($"Could not connect to {machine}. Seems down!");
                            return;
                        }

                        if (!string.IsNullOrEmpty(machine))
                            serviceC.MachineName = machine;
                        else
                            FileSystem.ErrorWriteLine("You need to provide the machine Name/IP after -r parameter!");
                        serviceC.Run(ServiceC.ActionService.List);
                        return;
                    }
                    serviceC.Run(ServiceC.ActionService.List);
                    return;
                }

                // Service description
                if (arg.StartsWith("-d "))
                {
                    string serviceName = arg.SplitByText("-d", 1).Trim();
                    var serviceC = new ServiceC();
                    if (arg.Contains("-r"))
                    {
                        RemoteCheck(arg, serviceName, ServiceC.ActionService.Description);
                        return;
                    }
                    serviceC.ServiceName = serviceName;
                    serviceC.Run(ServiceC.ActionService.Description);
                    return;
                }

                // Running State
                if (arg.StartsWith("-s "))
                {
                    string serviceName = arg.SplitByText("-s", 1).Trim();
                    var serviceC = new ServiceC();
                    if (arg.Contains("-r"))
                    {
                        RemoteCheck(arg, serviceName, ServiceC.ActionService.Status);
                        return;
                    }
                    serviceC.ServiceName = serviceName;
                    serviceC.Run(ServiceC.ActionService.Status);
                    return;
                }

                // Stop service
                if (arg.StartsWith("-stop "))
                {
                    string serviceName = arg.SplitByText("-stop", 1).Trim();
                    var serviceC = new ServiceC();
                    if (arg.Contains("-r"))
                    {
                        RemoteCheck(arg, serviceName, ServiceC.ActionService.Stop);
                        return;
                    }
                    serviceC.ServiceName = serviceName;
                    serviceC.Run(ServiceC.ActionService.Stop);
                    return;
                }

                // Stop service
                if (arg.StartsWith("-start "))
                {
                    string serviceName = arg.SplitByText("-start", 1).Trim();
                    if (arg.Contains("-r"))
                    {
                        RemoteCheck(arg, serviceName, ServiceC.ActionService.Start);
                        return;
                    }
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

        /// <summary>
        /// Use Remote machine service management.
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="serviceName"></param>
        private void RemoteCheck(string arg, string serviceName, ServiceC.ActionService actionService )
        {
            var serviceC = new ServiceC();
            var machine = arg.SplitByText("-r", 1).Trim();

            var isMachineUp = NetWork.PingHost(machine);
            if (!isMachineUp)
            {
                FileSystem.ErrorWriteLine($"Could not connect to {machine}. Seems down!");
                return;
            }

            serviceName = serviceName.Split(' ')[0].Trim();
            if (!string.IsNullOrEmpty(machine))
            {
                serviceC.MachineName = machine;
                serviceC.ServiceName = serviceName;
            }
            else
                FileSystem.ErrorWriteLine("You need to provide the machine Name/IP after -r parameter!");
            serviceC.Run(actionService);
        }
    }
}
