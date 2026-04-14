using Core;
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class Watch : ITerminalCommand
    {
        /*
            watch — Re-run any xTerminal command on a fixed interval with live
            in-place refresh (like Linux `watch` but for Windows).
            Press Q or Esc to stop.
        */

        public string Name => "watch";

        private static readonly string s_helpMessage = @"Usage of watch command:
    watch -n <secs> ""<cmd>""  : Re-run <cmd> every <secs> seconds.
    watch ""<cmd>""            : Re-run with the default 2-second interval.
    watch -h                 : Display this help message.

Examples:
    watch -n 3 ""time""
    watch -n 5 ""plist""
    watch ""sinfo""

Press Q or Esc to quit.
";

        public void Execute(string args)
        {
            GlobalVariables.isErrorCommand = false;
            try
            {
                if (args == $"{Name} -h") { Console.WriteLine(s_helpMessage); return; }
                if (args == Name)         { FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!"); return; }

                int interval = 2;
                string command;

                string rest = args.SplitByText($"{Name} ", 1).Trim();

                if (rest.StartsWith("-n "))
                {
                    string[] nParts = rest.Split(' ', 3);
                    if (nParts.Length >= 3 && int.TryParse(nParts[1], out int n) && n >= 1)
                    {
                        interval = n;
                        command  = nParts[2].Trim('"');
                    }
                    else
                    {
                        FileSystem.ErrorWriteLine("Invalid interval. Usage: watch -n <seconds> \"<command>\"");
                        GlobalVariables.isErrorCommand = true;
                        return;
                    }
                }
                else
                {
                    command = rest.Trim('"');
                }

                if (string.IsNullOrWhiteSpace(command))
                {
                    FileSystem.ErrorWriteLine("No command provided. Usage: watch \"<command>\"");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }

                RunLoop(command, interval);
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine(ex.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }

        private static void RunLoop(string command, int intervalSec)
        {
            while (true)
            {
                Console.Clear();
                FileSystem.ColorConsoleText(ConsoleColor.DarkGray, "Every ");
                FileSystem.ColorConsoleText(ConsoleColor.White, $"{intervalSec}s");
                FileSystem.ColorConsoleText(ConsoleColor.DarkGray, ": ");
                FileSystem.ColorConsoleText(ConsoleColor.Cyan, command);
                Console.WriteLine();
                FileSystem.ColorConsoleText(ConsoleColor.DarkGray,
                    $"{DateTime.Now:ddd yyyy-MM-dd HH:mm:ss}  —  Press Q to quit\n");
                Console.WriteLine(new string('─', 57));

                GlobalVariables.isPipeCommand     = false;
                GlobalVariables.pipeCmdOutput     = string.Empty;
                GlobalVariables.pipeCmdCount      = 0;
                GlobalVariables.pipeCmdCountTemp  = 0;
                GlobalVariables.aliasInParameter.Clear();

                var cmd = CommandRepository.GetCommand(command);
                if (cmd != null)
                    cmd.Execute(command);
                else
                    Console.WriteLine($"Unknown command: {command}");

                // Poll for Q/Esc during the interval delay (100 ms ticks)
                for (int i = 0; i < intervalSec * 10; i++)
                {
                    Thread.Sleep(100);
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true);
                        if (key.Key == ConsoleKey.Q || key.Key == ConsoleKey.Escape)
                            return;
                    }
                }
            }
        }
    }
}
