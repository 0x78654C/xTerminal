using System;
using Core;
using Core.SystemTools;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class Service : ITerminalCommand
    {
        public string Name => "sc";
        private static string s_helpMessage = @"Usage of sc command parameters:
Local:
    -list : List all local services names, status and description running on computer.
    -list --noinfo: List all local services names running on computer.
    -des <service_name> : Return the description for a specific service.
    -status <service_name> : Return the state for a specific service.
    -stop <service_name>  : Stops a specific service.
    -start <service_name> : Starts a specific service.
    -restart <service_name> : Restarts a specific service.

Remote:
    -list -r <machine_name/IP> : List all local services names, status and description running on a remote computer.
    -list --noinfo: List all local services names running on a remote computer.
    -des <service_name> -r <machine_name/IP> : Return the description for a specific service.
    -status <service_name> -r <machine_name/IP> : Return the state for a specific service.
    -stop <service_name> -r <machine_name/IP>  : Stops a specific service.
    -start <service_name> -r <machine_name/IP> : Starts a specific service.
    -restart <service_name> -r <machine_name/IP> : Restarts a specific service.

Note: Requires administrator privileges. (-list works with privileges as wel)
";
        public void Execute(string arg)
        {
            try
            {
                GlobalVariables.isErrorCommand = false;
                if (arg == Name && !GlobalVariables.isPipeCommand)
                {
                    FileSystem.SuccessWriteLine("Use -h for more information!");
                    return;
                }

                arg = arg.Substring(3);

                // Display help message.
                if (arg.Trim() == "-h" && !GlobalVariables.isPipeCommand)
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                // List services.
                if (arg.StartsWith("-list"))
                {
                    var serviceC = new ServiceC();
                    if (arg.Contains("--noinfo"))
                    {
                        arg = arg.Replace("--noinfo", "");
                        serviceC.IsDescription = false;
                    }

                    if (arg.Contains("-r"))
                    {
                        var machine = arg.SplitByText("-r", 1).Trim();
                        if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                            machine = GlobalVariables.pipeCmdOutput.Trim();
                        var isMachineUp = NetWork.PingHost(machine);
                        if (!isMachineUp)
                        {
                            FileSystem.ErrorWriteLine($"Could not connect to {machine}. Seems down!");
                            GlobalVariables.isErrorCommand = true;
                            return;
                        }

                        if (!string.IsNullOrEmpty(machine))
                            serviceC.MachineName = machine;
                        else
                        {
                            FileSystem.ErrorWriteLine("You need to provide the machine Name/IP after -r parameter!");
                            GlobalVariables.isErrorCommand = true;
                        }
                        serviceC.Run(ServiceC.ActionService.List);
                        return;
                    }
                    serviceC.Run(ServiceC.ActionService.List);
                    return;
                }

                // Service description
                if (arg.StartsWith("-des"))
                {
                    RunParameter(arg, "-des", ServiceC.ActionService.Description);
                    return;
                }

                // Running State
                if (arg.StartsWith("-status"))
                {
                    RunParameter(arg, "-status", ServiceC.ActionService.Status);
                    return;
                }

                // Stop service
                if (arg.StartsWith("-stop"))
                {
                    RunParameter(arg, "-stop", ServiceC.ActionService.Stop);
                    return;
                }

                // Start service
                if (arg.StartsWith("-start"))
                {
                    RunParameter(arg, "-start", ServiceC.ActionService.Start);
                    return;
                }


                // Restart service
                if (arg.StartsWith("-restart"))
                {
                    RunParameter(arg, "-restart", ServiceC.ActionService.Restart);
                    return;
                }
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine($"{ex.Message}. Use -h for more information!");
                GlobalVariables.isErrorCommand = true;
            }
        }

        /// <summary>
        /// Run parameters.
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="param"></param>
        /// <param name="actionService"></param>
        private void RunParameter(string arg, string param, ServiceC.ActionService actionService)
        {
            string serviceName = arg.SplitByText(param, 1).Trim();

            if (serviceName.Contains("-r"))
            {
                RemoteCheck(serviceName, serviceName, actionService);
                return;
            }
            var serviceC = new ServiceC();
            serviceC.ServiceName = serviceName;
            serviceC.Run(actionService);
        }

        /// <summary>
        /// Use Remote machine service management.
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="serviceName"></param>
        private void RemoteCheck(string arg, string serviceName, ServiceC.ActionService actionService)
        {
            var serviceC = new ServiceC();
            var machine = arg.SplitByText("-r", 1).Trim();

            var isMachineUp = NetWork.PingHost(machine);
            if (!isMachineUp)
            {
                FileSystem.ErrorWriteLine($"Could not connect to {machine}. Seems down!");
                GlobalVariables.isErrorCommand = true;
                return;
            }

            serviceName = serviceName.Split(' ')[0].Trim();
            if (!string.IsNullOrEmpty(machine))
            {
                serviceC.MachineName = machine;
                serviceC.ServiceName = serviceName;
            }
            else
            {
                FileSystem.ErrorWriteLine("You need to provide the machine Name/IP after -r parameter!");
                GlobalVariables.isErrorCommand = true;
            }
            serviceC.Run(actionService);
        }
    }
}
