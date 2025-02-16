using System;
using System.Runtime.Versioning;
using Core;

namespace Commands.TerminalCommands.Network
{
    [SupportedOSPlatform("Windows")]
    public class PortScanner : ITerminalCommand
    {
        /* Connection status check for an port (opened or closed) */

        public string Name => "cport";
        private static int s_timeOut = 0;
        private static bool s_pingCheck= false;
        private static string s_helpMessage = @"Usage of cport command:

    Example 1: cport IPAddress/HostName -p 80   (Checks if port 80 is open)
    Example 2: cport IPAddress/HostName -p 1-200   (Checks if any port from 1 to 200 is open)
    Example 3: cport stimeout 100   (Sets check port time out in milliseconds. Default value is 500)
    Example 4: cport rtimeout   (Reads the current time out value)
         
    cport check command can be used with --noping parameter to disable ping check on hostname/ip.
    Example: cport IPAddress/HostName -p 80 --noping

    Port range scan can be canceled with CTRL+X key combination.
";

        public void Execute(string args)
        {
            try
            {
                GlobalVariables.eventCancelKey = false;
                string cTimeOut = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCportTimeOut);
                if (FileSystem.IsNumberAllowed(cTimeOut) && !string.IsNullOrEmpty(cTimeOut))
                    s_timeOut = Int32.Parse(cTimeOut);
                else
                {
                    s_timeOut = 500;
                    RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCportTimeOut, "500");
                }

                if (args.Contains("--noping"))
                {
                    s_pingCheck = false;
                    args = args.Replace("--noping", string.Empty);
                }
                else
                {
                    s_pingCheck = true;
                }

                if (args.StartsWith($"{Name} stimeout"))
                {
                    string timeOut = args.SplitByText("stimeout ", 1);
                    RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCportTimeOut, timeOut);
                    Console.Write("New check port time out is: ");
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Green, timeOut+" ms");
                    return;
                }

                if (args == $"{Name} rtimeout")
                {
                    string timeOut = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCportTimeOut);
                    Console.Write("Check port time out is set to: ");
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Green, timeOut + " ms");
                    return;
                }

                if (args == Name)
                {
                    FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                    return;
                }
                if (args == $"{Name} -h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }
                string arg = args.Substring(6, args.Length - 6);
                string ipAddress = arg.SplitByText(" -p ", 0);
              

                if (s_pingCheck)
                {
                    if (!NetWork.PingHost(ipAddress))
                    {
                        FileSystem.SuccessWriteLine($"{ipAddress} is offline");
                        return;
                    }
                }
                string portData = arg.SplitByText(" -p ", 1);
                if (portData.Contains("-"))
                {
                    GlobalVariables.eventKeyFlagX = true;
                    string[] portsRange = portData.Split('-');
                    int minPort = Int32.Parse(portsRange[0].Trim());
                    int maxPort = Int32.Parse(portsRange[1].Trim());
                    PortScan.RunPortScan(ipAddress, minPort, maxPort, s_timeOut);
                    if (GlobalVariables.eventCancelKey)
                        FileSystem.SuccessWriteLine("Command stopped!");
                    GlobalVariables.eventCancelKey = false;
                    return;
                }
                int port = Int32.Parse(portData.Trim());
                PortScan.RunPortScan(ipAddress, port, port, s_timeOut);
                if (GlobalVariables.eventCancelKey)
                    FileSystem.SuccessWriteLine("Command stopped!");
                GlobalVariables.eventCancelKey = false;
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }
    }
}
