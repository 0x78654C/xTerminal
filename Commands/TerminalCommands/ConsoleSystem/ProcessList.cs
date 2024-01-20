using Core;
using System;
using System.Runtime.Versioning;
using Core.SystemTools;
namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class ProcessList : ITerminalCommand
    {
        /*
         Get process tree.
        */

        public string Name => "plist";
        private string _helpMessage = @"List current running processes and their child processes.
Example: 
    Parent : [Idle] [0]   ---> parent process
        [Idle] [0]        ---> child process
        [System] [4]      ---> child process
    Parent (1) : [System] ---> parent process(was child process for parent process '[Idle] [0]')
        [Registry] [132]  ---> child process

";

        // Main run
        public void Execute(string args)
        {
            try
            {
                if (args == $"{Name} -h")
                {
                    Console.WriteLine(_helpMessage);
                    return;
                }

                ListProcesses.GetProcessList();

            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }

    }
}
