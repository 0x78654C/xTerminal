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
                if (args.ContainsText("-r"))
                {
                    int pingReplays = Int32.Parse(arg.ParameterAfter("-r"));
                    NetWork.PingMain(arg.ParameterAfter("ping"), pingReplays);
                    return;
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
