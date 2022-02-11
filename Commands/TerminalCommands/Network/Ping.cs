using Core;
using System;

namespace Commands.TerminalCommands.Network
{
    public class Ping : ITerminalCommand
    {
        /*
         Display the echo newtork respons from other machine.
         */

        public string Name => "ping";
        private static string s_helpMessage = @"Usage of ping command:

Example 1: ping google.com  (for normal ping with 4 replies)
Example 2: ping google.com -t 10  (for 10 replies)
Example 3: ping google.com -t  (infinite replies)

Ping with -t can be canceled with CTRL+X key combination.    
";

        public void Execute(string args)
        {
            try
            {
                if (args.Length == 4)
                {
                    Console.WriteLine($"Use -h param for {Name} command usage!");
                    return;
                }
                if (args == $"{Name} -h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }
                string[] arg = args.Split(' ');
                if (args.ContainsText("-t"))
                {
                    string pingRetry = args.SplitByText("-t", 1);
                    int pingReplays = 0;
                    if (!string.IsNullOrEmpty(pingRetry))
                        pingReplays = Int32.Parse(pingRetry);

                    if (pingReplays > 0) {
                        GlobalVariables.eventKeyFlagX = true;
                        NetWork.PingMain(arg.ParameterAfter("ping"), pingReplays);
                        return;
                    }

                    if (string.IsNullOrEmpty(pingRetry))
                    {
                        GlobalVariables.eventKeyFlagX = true;
                        NetWork.PingMain(arg.ParameterAfter("ping"), 0);
                        return;
                    }
                }
                NetWork.PingMain(arg.ParameterAfter("ping"), 4);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }
    }
}
