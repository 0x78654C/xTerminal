﻿using System;
using System.Diagnostics;
using Core;

namespace Commands.TerminalCommands.ConsoleSystem
{
    /*
     Kill an active process by name.
     */
    class ProcessKill : ITerminalCommand
    {
        public string Name => "pkill";
        private string _helpMessage = @"
Kills a running process by name or id. Usage:
  pkill <process_name>
  pkill -i <process_id>
";
        public void Execute(string arg)
        {
            try
            {
                if(arg.ContainsText(" -h"))
                {
                    Console.WriteLine(_helpMessage);
                    return;
                }

                if (arg.ContainsText("-i"))
                {
                    KillProcess(arg.SplitByText("-i ", 1), true);
                    return;
                }
                KillProcess(arg.SplitByText("pkill ", 1), false);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }

        // Kill processes by name or id.
        private void KillProcess(string processName, bool id)
        {
            if (!id)
            {
                foreach (var process in Process.GetProcessesByName(processName))
                {
                    process.Kill();
                    Console.WriteLine("Process killed");
                    return;
                }
                FileSystem.ErrorWriteLine($"Process {processName} does not exist!");
                return;
            }
            Process.GetProcessById(Int32.Parse(processName)).Kill();
        }
    }
}
