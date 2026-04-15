using Core;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Commands.TerminalCommands.Network
{
    [SupportedOSPlatform("Windows")]
    public class LatMon : ITerminalCommand
    {
        /*
            latmon — Real-time multi-host latency monitor dashboard.

            Pings any number of hosts concurrently and renders a live table with
            per-host sparkline history bars, current / avg / min / max / loss%,
            and colour-coding by latency tier.  Standard ping only handles one
            host at a time with no history visualisation — this fills that gap.

            Sparkline chars:  ▁▂▃▄▅▆▇█   ·  = timeout probe
            Colour tiers:     green <50 ms · yellow 50-150 ms · red >150 ms
        */

        public string Name => "latmon";

        private const int HistorySize   = 25;
        private const int PingTimeoutMs = 3000;

        private static readonly char[] s_blocks = { '▁', '▂', '▃', '▄', '▅', '▆', '▇', '█' };

        private static readonly string s_helpMessage = @"Usage of latmon command:
    latmon <host1> [host2] ...          : Monitor hosts at 1-second interval.
    latmon -n <ms> <host1> [host2] ...  : Custom interval in milliseconds (min 100).
    latmon -h                           : Display this help message.

Colour coding:
    Green   < 50 ms       (excellent)
    Yellow  50 - 150 ms   (acceptable)
    Red     > 150 ms      (degraded)
    Gray    timeout / unreachable

Sparkline history:  ▁▂▃▄▅▆▇█  (·= timeout)

Examples:
    latmon google.com cloudflare.com 8.8.8.8
    latmon -n 500 192.168.1.1 192.168.1.254 10.0.0.1

Press Q or Esc to quit.
";

        // ── Per-host state ────────────────────────────────────────────────────

        private sealed class HostState
        {
            public string  Host;
            public long?[] History = new long?[HistorySize]; // null = timeout
            public int     Head    = 0;  // circular-buffer write index
            public int     Filled  = 0;  // populated slots
            public long    Min     = long.MaxValue;
            public long    Max     = 0;
            public long    Sum     = 0;
            public int     Count   = 0;  // successful probes
            public int     Lost    = 0;  // timed-out probes
            public long?   Last;

            public void Record(long? ms)
            {
                History[Head] = ms;
                Head          = (Head + 1) % HistorySize;
                if (Filled < HistorySize) Filled++;
                Last = ms;

                if (ms.HasValue)
                {
                    Count++;
                    Sum += ms.Value;
                    if (ms.Value < Min) Min = ms.Value;
                    if (ms.Value > Max) Max = ms.Value;
                }
                else
                {
                    Lost++;
                }
            }

            public double? Avg     => Count == 0 ? null : (double)Sum / Count;
            public int     Total   => Count + Lost;
            public double  LossPct => Total == 0 ? 0.0 : Lost * 100.0 / Total;

            public string Sparkline()
            {
                if (Filled == 0) return string.Empty;

                int  start = Filled < HistorySize ? 0 : Head;
                long maxV  = BuildScale(start);

                var sb = new StringBuilder();
                for (int i = 0; i < Filled; i++)
                {
                    int idx = (start + i) % HistorySize;
                    sb.Append(!History[idx].HasValue
                        ? '╌'
                        : s_blocks[BlockIndex(History[idx].Value, maxV)]);
                }
                return sb.ToString();
            }

            // Returns (char, ms?) pairs so each bar can be coloured individually.
            public (char Ch, long? Ms)[] SparklineParts()
            {
                if (Filled == 0) return Array.Empty<(char, long?)>();

                int  start = Filled < HistorySize ? 0 : Head;
                long maxV  = BuildScale(start);

                var parts = new (char Ch, long? Ms)[Filled];
                for (int i = 0; i < Filled; i++)
                {
                    int idx = (start + i) % HistorySize;
                    if (!History[idx].HasValue)
                        parts[i] = ('╌', null);
                    else
                    {
                        long v = History[idx].Value;
                        parts[i] = (s_blocks[BlockIndex(v, maxV)], v);
                    }
                }
                return parts;
            }

            // Minimum scale ceiling of 50 ms prevents 1-ms hosts displaying all
            // bars at maximum height with no visible variation.
            private long BuildScale(int start)
            {
                long maxV = 50;
                for (int i = 0; i < Filled; i++)
                {
                    int idx = (start + i) % HistorySize;
                    if (History[idx].HasValue && History[idx].Value > maxV)
                        maxV = History[idx].Value;
                }
                return maxV;
            }

            private static int BlockIndex(long ms, long maxV) =>
                Math.Clamp((int)(ms * (s_blocks.Length - 1) / maxV), 0, s_blocks.Length - 1);
        }

        // ── Execute ───────────────────────────────────────────────────────────

        public void Execute(string args)
        {
            GlobalVariables.isErrorCommand = false;
            try
            {
                if (args == $"{Name} -h") { Console.WriteLine(s_helpMessage); return; }
                if (args == Name)         { FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!"); return; }

                string rest = args.SplitByText($"{Name} ", 1).Trim();

                int intervalMs = 1000;
                if (rest.StartsWith("-n "))
                {
                    string[] nParts = rest.Split(' ', 3);
                    if (nParts.Length < 3 || !int.TryParse(nParts[1], out intervalMs) || intervalMs < 100)
                    {
                        FileSystem.ErrorWriteLine("Invalid interval. Usage: latmon -n <ms> <host1> ...");
                        GlobalVariables.isErrorCommand = true;
                        return;
                    }
                    rest = nParts[2];
                }

                string[] hosts = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (hosts.Length == 0)
                {
                    FileSystem.ErrorWriteLine("No hosts specified. Usage: latmon <host1> [host2] ...");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }

                var states = hosts.Select(h => new HostState { Host = h }).ToArray();

                // ── Pipe mode: one probe round, emit labeled lines ────────────
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                {
                    PingAllAsync(states).GetAwaiter().GetResult();
                    GlobalVariables.pipeCmdOutput = BuildPipeText(states);
                    return;
                }

                // ── Interactive live dashboard ────────────────────────────────
                RunDashboard(states, intervalMs);
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine(ex.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }

        // ── Dashboard loop ────────────────────────────────────────────────────

        private static void RunDashboard(HostState[] states, int intervalMs)
        {
            Console.CursorVisible = false;
            try
            {
                int probe    = 0;
                int startTop = Console.CursorTop;

                while (true)
                {
                    PingAllAsync(states).GetAwaiter().GetResult();
                    probe++;

                    Console.SetCursorPosition(0, startTop);
                    DrawDashboard(states, intervalMs, probe);

                    // On the first draw, capture the end position so we know
                    // startTop is valid even if the first draw caused scrolling.
                    if (probe == 1)
                        startTop = Console.CursorTop - DashboardLineCount(states.Length);

                    // Poll for Q/Esc during the inter-probe sleep (100 ms ticks)
                    int ticks = Math.Max(1, intervalMs / 100);
                    for (int t = 0; t < ticks; t++)
                    {
                        Thread.Sleep(100);
                        if (Console.KeyAvailable)
                        {
                            var k = Console.ReadKey(intercept: true);
                            if (k.Key == ConsoleKey.Q || k.Key == ConsoleKey.Escape)
                                return;
                        }
                    }
                }
            }
            finally
            {
                Console.CursorVisible = true;
            }
        }

        // Total lines emitted by DrawDashboard:
        //   header \n  +  sep \n  +  colheader \n  +  sep \n  +  N host rows \n  +  sep \n
        private static int DashboardLineCount(int hostCount) => 5 + hostCount;

        private static void DrawDashboard(HostState[] states, int intervalMs, int probe)
        {
            const int colHost = 24;
            const int colStat =  9;
            // Separator exactly matches the widest row: indent + host + 5 stats + gap + sparkline
            int    sepLen = 2 + colHost + 5 * colStat + 2 + HistorySize;
            string sep    = new string('─', sepLen);

            // Header — build as a single padded line to avoid stale chars on redraw
            string headerLine = $"  latmon  interval:{intervalMs}ms  probes:{probe}  Q/Esc quit";
            FileSystem.ColorConsoleText(ConsoleColor.Cyan, "  latmon");
            FileSystem.ColorConsoleText(ConsoleColor.DarkGray,
                headerLine["  latmon".Length..].PadRight(sepLen - "  latmon".Length));
            Console.WriteLine();
            Console.WriteLine(sep);

            // Column headers
            string colLine =
                $"  {"Host",-colHost}{"Cur",colStat}{"Avg",colStat}" +
                $"{"Min",colStat}{"Max",colStat}{"Loss",colStat}  " +
                $"{"History",-HistorySize}";
            FileSystem.ColorConsoleText(ConsoleColor.DarkGray, colLine);
            Console.WriteLine();
            Console.WriteLine(sep);

            foreach (var s in states)
            {
                // Host
                FileSystem.ColorConsoleText(ConsoleColor.White,
                    $"  {Truncate(s.Host, colHost - 1),-colHost}");

                // Current
                if (s.Last.HasValue)
                    WriteLatency(s.Last.Value, colStat);
                else
                    FileSystem.ColorConsoleText(ConsoleColor.DarkGray, $"{"TIMEOUT",colStat}");

                // Avg
                if (s.Avg.HasValue)
                    WriteLatency((long)s.Avg.Value, colStat);
                else
                    FileSystem.ColorConsoleText(ConsoleColor.DarkGray, $"{"—",colStat}");

                // Min
                if (s.Count > 0)
                    WriteLatency(s.Min, colStat);
                else
                    FileSystem.ColorConsoleText(ConsoleColor.DarkGray, $"{"—",colStat}");

                // Max
                if (s.Count > 0)
                    WriteLatency(s.Max, colStat);
                else
                    FileSystem.ColorConsoleText(ConsoleColor.DarkGray, $"{"—",colStat}");

                // Loss %
                ConsoleColor lc = s.LossPct == 0  ? ConsoleColor.DarkGray
                                : s.LossPct < 10  ? ConsoleColor.Yellow
                                                  : ConsoleColor.Red;
                FileSystem.ColorConsoleText(lc, $"{s.LossPct:F0}%".PadLeft(colStat));

                // Sparkline — each bar coloured by its own latency value
                FileSystem.ColorConsoleText(ConsoleColor.DarkGray, "  ");
                int sparkLen = 0;
                foreach (var (ch, ms) in s.SparklineParts())
                {
                    ConsoleColor c = !ms.HasValue ? ConsoleColor.DarkRed
                                   : ms < 50      ? ConsoleColor.Green
                                   : ms < 150     ? ConsoleColor.Yellow
                                                  : ConsoleColor.Red;
                    FileSystem.ColorConsoleText(c, ch.ToString());
                    sparkLen++;
                }

                // Pad remaining sparkline columns with spaces to erase stale chars
                if (sparkLen < HistorySize)
                    Console.Write(new string(' ', HistorySize - sparkLen));

                Console.WriteLine();
            }

            Console.WriteLine(sep);
        }

        private static void WriteLatency(long ms, int width)
        {
            ConsoleColor c = ms <  50 ? ConsoleColor.Green
                           : ms < 150 ? ConsoleColor.Yellow
                                      : ConsoleColor.Red;
            FileSystem.ColorConsoleText(c, $"{ms}ms".PadLeft(width));
        }

        private static string Truncate(string s, int max) =>
            s.Length <= max ? s : s[..(max - 1)] + "…";

        // ── Concurrent ping ───────────────────────────────────────────────────

        private static async Task PingAllAsync(HostState[] states)
        {
            await Task.WhenAll(states.Select(PingOneAsync));
        }

        private static async Task PingOneAsync(HostState s)
        {
            try
            {
                var reply = await Task.Run(() =>
                {
                    var pinger  = new System.Net.NetworkInformation.Ping();
                    var buffer  = new byte[32];
                    var options = new PingOptions(64, true);
                    return pinger.Send(s.Host, PingTimeoutMs, buffer, options);
                });
                s.Record(reply.Status == IPStatus.Success ? reply.RoundtripTime : (long?)null);
            }
            catch
            {
                s.Record(null);
            }
        }

        // ── Pipe text ─────────────────────────────────────────────────────────

        private static string BuildPipeText(HostState[] states)
        {
            var sb = new StringBuilder();
            foreach (var s in states)
            {
                string cur = s.Last.HasValue ? $"{s.Last.Value}ms" : "timeout";
                string avg = s.Avg.HasValue  ? $"{s.Avg.Value:F0}ms" : "—";
                string min = s.Count > 0     ? $"{s.Min}ms" : "—";
                string max = s.Count > 0     ? $"{s.Max}ms" : "—";
                sb.AppendLine(
                    $"host: {s.Host}  cur: {cur}  avg: {avg}" +
                    $"  min: {min}  max: {max}  loss: {s.LossPct:F0}%");
            }
            return sb.ToString();
        }
    }
}
