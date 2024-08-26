using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Management.Automation;
using System.Runtime.Versioning;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace Core.SystemTools
{
    [SupportedOSPlatform("Windows")]
    public class ServiceC
    {
        public string ServiceName { get; set; }
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
                    Start();
                    break;
                case ActionService.Stop:
                    Stop();
                    break;
                case ActionService.List:
                    ListServices();
                    break;
                case ActionService.Description:
                    Description();
                    break;
                case ActionService.Status:
                    Status();
                    break;
            }
        }

        /// <summary>
        /// List all services from current pc.
        /// </summary>
        private void ListServices()
        {
            var scServices = ServiceController.GetServices();
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
        private void Description()
        {
            var wmiService = new ManagementObject($"Win32_Service.Name='{ServiceName}'");
            wmiService.Get();
            var description = wmiService["Description"];
            FileSystem.SuccessWriteLine(description);
        }

        /// <summary>
        /// Stop a service.
        /// </summary>
        private void Stop()
        {
            var sc = new ServiceController();
            sc.ServiceName = ServiceName;
            if (sc.Status == ServiceControllerStatus.Running)
            {
                FileSystem.SuccessWriteLine($"Stopping {ServiceName} ...");
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped);
                FileSystem.SuccessWriteLine($"Status: {sc.Status}");
            }
            else
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Red, $"Service {ServiceName} is already stopped!");
            }
        }


        /// <summary>
        /// Start a service.
        /// </summary>
        private void Start()
        {
            var sc = new ServiceController();
            sc.ServiceName = ServiceName;
            if (sc.Status == ServiceControllerStatus.Stopped)
            {
                FileSystem.SuccessWriteLine($"Starting {ServiceName} ...");
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running);
                FileSystem.SuccessWriteLine($"Status: {sc.Status}");
            }
            else
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Red, $"Service {ServiceName} is already running!");
            }
        }
        /// <summary>
        /// Get service running state by name.
        /// </summary>
        private void Status()
        {
            var wmiService = new ManagementObject($"Win32_Service.Name='{ServiceName}'");
            wmiService.Get();
            var description = wmiService["State"];
            FileSystem.SuccessWriteLine(description);
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
        }
    }
}
