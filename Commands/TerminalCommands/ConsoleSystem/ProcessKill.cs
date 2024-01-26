using Core;
using System;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    /*
     Kill an active process by name.
     */
    [SupportedOSPlatform("Windows")]
    class ProcessKill : ITerminalCommand
    {
        public string Name => "pkill";
        private string _helpMessage = @"
Kills a running process by name or id. Usage:
  pkill <process_name>
  pkill <process_name> -e : Kill entire process tree.
  pkill -i <process_id>
  pkill -i <process_id> -e : Kill entire process tree.
";
        public void Execute(string arg)
        {
            try
            {
                if (arg.Length == 3)
                {
                    FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                    return;
                }

                if (arg == $"{Name} -h")
                {
                    Console.WriteLine(_helpMessage);
                    return;
                }
                var argClean = string.Empty;


                if (arg.ContainsText("-i"))
                {
                    // Check if process tree kill param is added 
                    if (arg.ContainsText("-e"))
                    {
                        argClean = arg.Replace("-e", string.Empty).Trim();
                        KillProcess(argClean.SplitByText("-i ", 1), true, true);
                        return;
                    }
                    KillProcess(arg.SplitByText("-i ", 1), true);
                    return;
                }

                // Check if process tree kill param is added 
                if (arg.ContainsText("-e"))
                {
                    argClean = arg.Replace("-e", string.Empty).Trim();
                    KillProcess(argClean.SplitByText("pkill ", 1), false,true);
                    return;
                }
                KillProcess(arg.SplitByText("pkill ", 1), false);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }

        /// <summary>
        /// Kill processes by name or id.
        /// </summary>
        /// <param name="processName"></param>
        /// <param name="id"></param>
        /// <param name="entireProcessTree"></param>
        private void KillProcess(string processName, bool id, bool entireProcessTree = false)
        {
            if (!id)
            {
                foreach (var process in Process.GetProcessesByName(processName))
                {
                    process.Kill(entireProcessTree);
                    MessageProcessKill(processName, entireProcessTree);
                    return;
                }
                FileSystem.ErrorWriteLine($"Process {processName} does not exist!");
                return;
            }
            Process.GetProcessById(Int32.Parse(processName)).Kill(entireProcessTree);
            MessageProcessKill(processName, entireProcessTree);
        }

        /// <summary>
        /// Message display depeding if killing entire process tree or not.
        /// </summary>
        /// <param name="processName"></param>
        /// <param name="entireProcessTree"></param>
        private void MessageProcessKill(string processName, bool entireProcessTree)
        {
            if (entireProcessTree)
                FileSystem.SuccessWriteLine($"Process tree killed for: {processName}");
            else
                FileSystem.SuccessWriteLine($"Process killed: {processName}");
        }
    }
}
