using Core;
using System;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.Network
{
    [SupportedOSPlatform("Windows")]
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
                GlobalVariables.eventCancelKey = false;

                if (args == $"{Name} -h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }
                if (args.Length == 4 && !GlobalVariables.isPipeCommand)
                {
                    FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                    return;
                }
                
                if(GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 && !args.ContainsText("-t"))
                    args = $"{args} {GlobalVariables.pipeCmdOutput.Trim()}";

                string[] arg = args.Split(' ');
                if (args.ContainsText("-t"))
                {
                    string pingRetry = args.SplitByText("-t", 1);
                    int pingReplays = 0;
                    if (!string.IsNullOrEmpty(pingRetry.Trim()))
                        pingReplays = Int32.Parse(pingRetry);

                    if (pingReplays > 0) {
                        GlobalVariables.eventKeyFlagX = true;
                        if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                            NetWork.PingMain(GlobalVariables.pipeCmdOutput.Trim(), pingReplays);
                        else
                            NetWork.PingMain(arg.ParameterAfter("ping"), pingReplays);
                        if (GlobalVariables.eventCancelKey)
                            FileSystem.SuccessWriteLine("Command stopped!");
                        GlobalVariables.eventCancelKey = false;
                        return;
                    }

                    if (string.IsNullOrEmpty(pingRetry))
                    {
                        GlobalVariables.eventKeyFlagX = true;
                        if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                            NetWork.PingMain(GlobalVariables.pipeCmdOutput.Trim(), pingReplays);
                        else
                            NetWork.PingMain(arg.ParameterAfter("ping"), 0);
                        if (GlobalVariables.eventCancelKey)
                            FileSystem.SuccessWriteLine("Command stopped!");
                        GlobalVariables.eventCancelKey = false;
                        return;
                    }
                }
                GlobalVariables.eventKeyFlagX = true;
                NetWork.PingMain(arg.ParameterAfter("ping"), 4);
                if (GlobalVariables.eventCancelKey)
                    FileSystem.SuccessWriteLine("Command stopped!");
                GlobalVariables.eventCancelKey = false;
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }
    }
}
