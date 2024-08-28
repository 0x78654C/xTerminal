using System;
using System.Management;
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
                    ListServices(MachineName, ServiceName);
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
        }

        /// <summary>
        /// List all services from current pc.
        /// </summary>
        private void ListServices(string machineName, string serviceName)
        {
            ServiceController[] scServices = (!string.IsNullOrEmpty(machineName)) ? ServiceController.GetServices(machineName) : scServices = ServiceController.GetServices();

            foreach (var service in scServices)
            {
                var name = service.ServiceName;
                var status = service.Status;
                var displayName = service.DisplayName;
                var wmiService = new ManagementObject($"Win32_Service.Name='{name}'");
                wmiService.Get();
                var description = wmiService["Description"];
                var dataOut = $"{name.PadRight(50, ' ')} | {status} | {displayName}";
                Console.WriteLine(dataOut);
            }
        }

        /// <summary>
        /// Get service description by name.
        /// </summary>
        private void Description(string machineName, string serviceName)
        {
            var query = $"SELECT Description FROM Win32_Service WHERE Name='{serviceName}'"; ;
            var pc = (!string.IsNullOrEmpty(machineName)) ? @"\\" + machineName + @"\root\cimv2" : @"\\.\root\cimv2";
            var description = Wmi.GetWMIDetails(query, pc);
            FileSystem.SuccessWriteLine(description);
        }

        /// <summary>
        /// Stop a service.
        /// </summary>
        private void Stop(string machineName, string serviceName)
        {
            var sc = (!string.IsNullOrEmpty(machineName)) ? new ServiceController(serviceName, machineName) : new ServiceController(serviceName);

            if (sc.Status == ServiceControllerStatus.Running)
            {
                FileSystem.SuccessWriteLine($"Stopping {serviceName} ...");
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped);
                FileSystem.SuccessWriteLine($"Status: {sc.Status}");
            }
            else
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Red, $"Service {serviceName} is already stopped!");
            }
        }


        /// <summary>
        /// Start a service.
        /// </summary>
        private void Start(string machineName, string serviceName)
        {
            var sc = (!string.IsNullOrEmpty(machineName)) ? new ServiceController(serviceName, machineName) : new ServiceController(serviceName);

            if (sc.Status == ServiceControllerStatus.Stopped)
            {
                FileSystem.SuccessWriteLine($"Starting {serviceName} ...");
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running);
                FileSystem.SuccessWriteLine($"Status: {sc.Status}");
            }
            else
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Red, $"Service {serviceName} is already running!");
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
            var sc = (!string.IsNullOrEmpty(machineName)) ? new ServiceController(serviceName, machineName) : new ServiceController(serviceName);
            FileSystem.SuccessWriteLine(sc.Status);
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
