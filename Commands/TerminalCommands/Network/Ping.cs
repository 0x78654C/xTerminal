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

        public void Execute(string args)
        {
            try
            {
                if (args.Length == 4)
                {
                    FileSystem.ErrorWriteLine($"You must provide an IP/Hostname.");
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
