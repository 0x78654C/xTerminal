using System;
using System.Runtime.Versioning;
using System.ServiceProcess;
using Wmi = Core.Hardware.WMIDetails;

namespace Core.SystemTools
{
    [SupportedOSPlatform("Windows")]
    public class ServiceC
    {
        public string ServiceName { get; set; }
        public string MachineName { get; set; }
        public bool IsDescription { get; set; } = true;
        public ServiceC()
        {
        }

        /// <summary>
        /// Runninc services actions.
        /// </summary>
        /// <param name="actionService"></param>
        public void Run(ActionService actionService)
        {
            switch (actionService)
            {
                case ActionService.Start:
                    Start(MachineName, ServiceName);
                    break;
                case ActionService.Stop:
                    Stop(MachineName, ServiceName);
                    break;
                case ActionService.List:
                    ListServices(MachineName, IsDescription);
                    break;
                case ActionService.Description:
                    Description(MachineName, ServiceName);
                    break;
                case ActionService.Status:
                    Status(MachineName, ServiceName);
                    break;
                case ActionService.Restart:
                    Restart(MachineName, ServiceName);
                    break;
            }
            MachineName = "";
            ServiceName = "";
            IsDescription = true;
        }

        /// <summary>
        /// List all services from current pc.
        /// </summary>
        private void ListServices(string machineName, bool isDescription)
        {;
            ServiceController[] scServices = (!string.IsNullOrEmpty(machineName)) ? ServiceController.GetServices(machineName) : scServices = ServiceController.GetServices();

            foreach (var service in scServices)
            {
                var name = service.ServiceName;
                var status = service.Status;
                var displayName = service.DisplayName;
                var dataOut = "";
                if(!string.IsNullOrEmpty(machineName))
                    dataOut = (isDescription) ? $"{name.PadRight(50, ' ')} | {status} | Remote | {displayName}" : name;
                else
                    dataOut = (isDescription) ? $"{name.PadRight(50, ' ')} | {status} | {displayName}" : name;
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                    GlobalVariables.pipeCmdOutput += dataOut+Environment.NewLine;
                else
                    Console.WriteLine(dataOut);
            }
        }

        /// <summary>
        /// Get service description by name.
        /// </summary>
        private void Description(string machineName, string serviceName)
        {
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                serviceName = GlobalVariables.pipeCmdOutput.Trim();
            var query = $"SELECT Description FROM Win32_Service WHERE Name='{serviceName}'"; ;
            var pc = (!string.IsNullOrEmpty(machineName)) ? @"\\" + machineName + @"\root\cimv2" : @"\\.\root\cimv2";
            var description = Wmi.GetWMIDetails(query, pc);
            var desc = (!string.IsNullOrEmpty(machineName)) ? $"{description}\n(Remote)" : description;
            FileSystem.SuccessWriteLine(desc);
        }

        /// <summary>
        /// Stop a service.
        /// </summary>
        private void Stop(string machineName, string serviceName)
        {
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                serviceName = GlobalVariables.pipeCmdOutput.Trim();
            var sc = (!string.IsNullOrEmpty(machineName)) ? new ServiceController(serviceName, machineName) : new ServiceController(serviceName);

            if (sc.Status == ServiceControllerStatus.Running)
            {
                FileSystem.SuccessWriteLine($"Stopping {serviceName} ...");
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped);
                var status = (!string.IsNullOrEmpty(machineName)) ? $"Status: {sc.Status} (Remote)" : $"Status: {sc.Status}";
                FileSystem.SuccessWriteLine(status);
            }
            else
            {
                var statusErr = (!string.IsNullOrEmpty(machineName)) ? $"Service {serviceName} is already stopped! (Remote)" : $"Service {serviceName} is already stopped!";
                FileSystem.ColorConsoleTextLine(ConsoleColor.Red, statusErr);
            }
        }


        /// <summary>
        /// Start a service.
        /// </summary>
        private void Start(string machineName, string serviceName)
        {
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                serviceName = GlobalVariables.pipeCmdOutput.Trim();
            var sc = (!string.IsNullOrEmpty(machineName)) ? new ServiceController(serviceName, machineName) : new ServiceController(serviceName);

            if (sc.Status == ServiceControllerStatus.Stopped)
            {
                FileSystem.SuccessWriteLine($"Starting {serviceName} ...");
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running);
                var status = (!string.IsNullOrEmpty(machineName)) ? $"Status: {sc.Status} (Remote)" : $"Status: {sc.Status}";
                FileSystem.SuccessWriteLine(status);
            }
            else
            {
                var statusErr = (!string.IsNullOrEmpty(machineName)) ? $"Service {serviceName} is already running! (Remote)" : $"Service {serviceName} is already runing!";
                FileSystem.ColorConsoleTextLine(ConsoleColor.Red, statusErr);
            }
        }

        /// <summary>
        /// Restart a service.
        /// </summary>
        /// <param name="machineName"></param>
        /// <param name="serviceName"></param>
        private void Restart(string machineName, string serviceName)
        {
            FileSystem.SuccessWriteLine($"Restarting {serviceName} ...\n--------------------------\n");
            Stop(machineName, serviceName);
            Start(machineName, serviceName);
        }

        /// <summary>
        /// Get service running state by name.
        /// </summary>
        private void Status(string machineName, string serviceName)
        {
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                serviceName = GlobalVariables.pipeCmdOutput.Trim();
            var sc = (!string.IsNullOrEmpty(machineName)) ? new ServiceController(serviceName, machineName) : new ServiceController(serviceName);
            var status = (!string.IsNullOrEmpty(machineName)) ? $"{sc.Status} (Remote)" : sc.Status.ToString();
            FileSystem.SuccessWriteLine(status);
        }

        /// <summary>
        /// Enumrate service action.
        /// </summary>
        public enum ActionService
        {
            List,
            Start,
            Stop,
            Description,
            Status,
            Restart
        }
    }
}
