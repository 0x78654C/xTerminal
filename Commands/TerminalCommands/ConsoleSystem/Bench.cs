using Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class Bench : ITerminalCommand
    {
        /*
            bench — Command benchmarker.
            Runs any xTerminal command N times and reports min / avg / max / total
            execution time. No shell has this built in.
        */

        public string Name => "bench";

        private static readonly string s_helpMessage = @"Usage of bench command:
    bench ""<command>""             : Benchmark with the default 5 runs.
    bench -n <count> ""<command>""  : Benchmark with a specific run count.
    bench -h                       : Display this help message.

Examples:
    bench ""time""
    bench -n 10 ""ls""
    bench -n 3 ""pcinfo""

Note: Command output is suppressed during measurement.
";

        public void Execute(string args)
        {
            GlobalVariables.isErrorCommand = false;
            try
            {
                if (args == $"{Name} -h") { Console.WriteLine(s_helpMessage); return; }
                if (args == Name)         { FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!"); return; }

                int    runs    = 5;
                string command;

                string rest = args.SplitByText($"{Name} ", 1).Trim();

                if (rest.StartsWith("-n "))
                {
                    string[] nParts = rest.Split(' ', 3);
                    if (nParts.Length >= 3 && int.TryParse(nParts[1], out int n) && n >= 1)
                    {
                        runs    = n;
                        command = nParts[2].Trim('"');
                    }
                    else
                    {
                        FileSystem.ErrorWriteLine("Invalid count. Usage: bench -n <count> \"<command>\"");
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
                    FileSystem.ErrorWriteLine("No command provided. Usage: bench \"<command>\"");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }

                var cmd = CommandRepository.GetCommand(command);
                if (cmd == null)
                {
                    FileSystem.ErrorWriteLine($"Unknown command: '{command}'");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }

                RunBenchmark(cmd, command, runs);
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine(ex.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }

        private static void RunBenchmark(ITerminalCommand cmd, string commandLine, int runs)
        {
            Console.WriteLine();
            FileSystem.ColorConsoleText(ConsoleColor.Cyan, "  Benchmarking: ");
            Console.WriteLine($"'{commandLine}'  ×{runs}");
            Console.WriteLine(new string('─', 57));

            var times        = new long[runs];
            var sw           = new Stopwatch();
            var originalOut  = Console.Out;

            Console.SetOut(TextWriter.Null);
            try
            {
                for (int i = 0; i < runs; i++)
                {
                    GlobalVariables.isPipeCommand    = false;
                    GlobalVariables.pipeCmdOutput    = string.Empty;
                    GlobalVariables.pipeCmdCount     = 0;
                    GlobalVariables.pipeCmdCountTemp = 0;
                    GlobalVariables.aliasInParameter.Clear();

                    sw.Restart();
                    try { cmd.Execute(commandLine); } catch { /* absorb per-run errors */ }
                    sw.Stop();
                    times[i] = sw.ElapsedMilliseconds;
                }
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            long   total = 0;
            long   min   = long.MaxValue;
            long   max   = long.MinValue;
            foreach (var t in times)
            {
                total += t;
                if (t < min) min = t;
                if (t > max) max = t;
            }
            double avg = (double)total / runs;

            Console.WriteLine();
            FileSystem.ColorConsoleText(ConsoleColor.DarkGray, "  Runs   : "); Console.WriteLine(runs);
            FileSystem.ColorConsoleText(ConsoleColor.Green,    "  Min    : "); Console.WriteLine($"{min} ms");
            FileSystem.ColorConsoleText(ConsoleColor.Yellow,   "  Avg    : "); Console.WriteLine($"{avg:F1} ms");
            FileSystem.ColorConsoleText(ConsoleColor.Red,      "  Max    : "); Console.WriteLine($"{max} ms");
            FileSystem.ColorConsoleText(ConsoleColor.Cyan,     "  Total  : "); Console.WriteLine($"{total} ms");
            Console.WriteLine();
        }
    }
}
