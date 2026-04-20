using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace Core.SystemTools
{
    [SupportedOSPlatform("windows")]
    public sealed class ProcessListingUI
    {
        // ── VT / ANSI primitives ─────────────────────────────────────────────
        private const string CSI = "\x1b[";
        private const string AltScreen = "\x1b[?1049h";
        private const string NormalScreen = "\x1b[?1049l";
        private const string HideCursor = "\x1b[?25l";
        private const string ShowCursor = "\x1b[?25h";
        private const string R = "\x1b[0m";
        private const string EOL = "\x1b[K";

        // ── 256-colour palette ───────────────────────────────────────────────
        private const int BG_BASE = 232;
        private const int BG_TOPBAR = 233;
        private const int BG_COLHDR = 234;
        private const int BG_SEL = 23;
        private const int BG_SEL2 = 24;

        private const int FG_PRIMARY = 253;
        private const int FG_DIM = 244;
        private const int FG_MUTED = 239;

        private const int C_ACCENT = 45;
        private const int C_ACCENT2 = 38;
        private const int C_OK = 78;
        private const int C_WARN = 221;
        private const int C_DANGER = 203;
        private const int C_SEARCH = 227;

        private const int C_PID = 68;
        private const int C_MEM = 140;
        private const int C_THR = 179;
        private const int C_USER = 108;
        private const int C_DEAD = 238;

        private static readonly char[] Sparks = { ' ', '▏', '▎', '▍', '▌', '▋', '▊', '▉', '█' };
        private static readonly string[] Spin = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };

        // ── Shared state ─────────────────────────────────────────────────────
        private readonly object _lock = new();
        private volatile bool _exitRequested;
        private int _selectedIndex;
        private int _lastProcessCount;
        private bool _inSearchMode;
        private string _searchQuery = string.Empty;
        private SortMode _sortMode = SortMode.Name;

        private Dictionary<int, double> _cpuUsages = new();
        private readonly Dictionary<int, TimeSpan> _prevCpuTimes = new();
        private readonly Dictionary<int, string> _userCache = new();

        private readonly PerformanceCounter _cpuCounter =
            new("Processor", "% Processor Time", "_Total");

        private double _cpuPct;
        private double _memPct;
        private double _memUsed;
        private double _memTotal;

        private double _cpuPctDraw;
        private double _memPctDraw;

        private string _status = string.Empty;
        private DateTime _statusExp = DateTime.MinValue;
        private int _spinIdx;

        private const int TOKEN_QUERY = 0x0008;

        // ── P/Invoke ─────────────────────────────────────────────────────────
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public sealed class MEMORYSTATUSEX
        {
            public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            public uint dwMemoryLoad;
            public ulong ullTotalPhys, ullAvailPhys;
            public ulong ullTotalPageFile, ullAvailPageFile;
            public ulong ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr h, uint access, out IntPtr token);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr h);

        // ── ANSI helpers ─────────────────────────────────────────────────────
        private static string F(int c) => $"{CSI}38;5;{c}m";
        private static string B(int c) => $"{CSI}48;5;{c}m";
        private static string Bold => $"{CSI}1m";
        private static string Faint => $"{CSI}2m";
        private static string At(int col, int row) => $"{CSI}{row + 1};{col + 1}H";

        // ── Entry point ──────────────────────────────────────────────────────
        public void Run()
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Windows only.");

            Console.OutputEncoding = Encoding.UTF8;
            Console.TreatControlCAsInput = true;
            Console.Write(AltScreen + HideCursor);

            try
            {
                _cpuCounter.NextValue();
                Thread.Sleep(200);

                _cpuPctDraw = 0;
                _memPctDraw = 0;

                new Thread(SampleLoop) { IsBackground = true, Name = "wTpp-Sample" }.Start();
                new Thread(InputLoop) { IsBackground = true, Name = "wTpp-Input" }.Start();
                new Thread(RenderLoop) { IsBackground = true, Name = "wTpp-Render" }.Start();

                while (!_exitRequested)
                    Thread.Sleep(50);
            }
            finally
            {
                try { Console.Write(ShowCursor + NormalScreen); } catch { }
                _cpuCounter.Dispose();
            }
        }

        // ── Sampler thread ───────────────────────────────────────────────────
        private void SampleLoop()
        {
            DateTime prev = DateTime.UtcNow;

            while (!_exitRequested)
            {
                try
                {
                    double cpu = _cpuCounter.NextValue();
                    (double mUsed, double mTotal, double mPct) = GetMemInfo();

                    Process[] procs = Process.GetProcesses();
                    DateTime now = DateTime.UtcNow;
                    double dt = Math.Max((now - prev).TotalSeconds, 0.001);
                    prev = now;

                    var newCpu = new Dictionary<int, double>(procs.Length);
                    var liveIds = new HashSet<int>(procs.Length);

                    foreach (Process p in procs)
                    {
                        try
                        {
                            liveIds.Add(p.Id);
                            TimeSpan t = p.TotalProcessorTime;

                            newCpu[p.Id] = _prevCpuTimes.TryGetValue(p.Id, out TimeSpan prevT)
                                ? Math.Clamp((t - prevT).TotalSeconds / dt / Environment.ProcessorCount * 100.0, 0, 100)
                                : 0.0;

                            _prevCpuTimes[p.Id] = t;
                        }
                        catch { }
                    }

                    foreach (int dead in _prevCpuTimes.Keys.Except(liveIds).ToArray())
                        _prevCpuTimes.Remove(dead);

                    lock (_lock)
                    {
                        _cpuUsages = newCpu;
                        _cpuPct = Math.Clamp(cpu, 0, 100);
                        _memUsed = mUsed;
                        _memTotal = mTotal;
                        _memPct = Math.Clamp(mPct, 0, 100);

                        foreach (int dead in _userCache.Keys.Except(liveIds).ToArray())
                            _userCache.Remove(dead);
                    }
                }
                catch { }

                Thread.Sleep(1000);
            }
        }

        // ── Input thread ─────────────────────────────────────────────────────
        private void InputLoop()
        {
            while (!_exitRequested)
            {
                try
                {
                    if (!Console.KeyAvailable)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    ConsoleKeyInfo k = Console.ReadKey(intercept: true);

                    lock (_lock)
                    {
                        if (_inSearchMode)
                        {
                            HandleSearch(k);
                            continue;
                        }

                        int max = Math.Max(0, _lastProcessCount - 1);

                        switch (k.Key)
                        {
                            case ConsoleKey.UpArrow:
                                _selectedIndex = Math.Max(0, _selectedIndex - 1);
                                break;

                            case ConsoleKey.DownArrow:
                                _selectedIndex = Math.Min(max, _selectedIndex + 1);
                                break;

                            case ConsoleKey.PageUp:
                                _selectedIndex = Math.Max(0, _selectedIndex - 10);
                                break;

                            case ConsoleKey.PageDown:
                                _selectedIndex = Math.Min(max, _selectedIndex + 10);
                                break;

                            case ConsoleKey.Home:
                                _selectedIndex = 0;
                                break;

                            case ConsoleKey.End:
                                _selectedIndex = max;
                                break;

                            case ConsoleKey.K:
                                ThreadPool.QueueUserWorkItem(_ => KillSelected());
                                break;

                            case ConsoleKey.Q:
                            case ConsoleKey.Escape:
                                _exitRequested = true;
                                break;

                            case ConsoleKey.C:
                                _sortMode = SortMode.CPU;
                                Status("Sort  ·  CPU");
                                break;

                            case ConsoleKey.M:
                                _sortMode = SortMode.Memory;
                                Status("Sort  ·  Memory");
                                break;

                            case ConsoleKey.N:
                                _sortMode = SortMode.Name;
                                Status("Sort  ·  Name");
                                break;

                            case ConsoleKey.Oem2:
                                _inSearchMode = true;
                                _searchQuery = string.Empty;
                                Status("Search  —  type name  ·  Enter to jump  ·  Esc to cancel");
                                break;
                        }
                    }
                }
                catch { }
            }
        }

        private void HandleSearch(ConsoleKeyInfo k)
        {
            switch (k.Key)
            {
                case ConsoleKey.Enter:
                    JumpToProcess(_searchQuery.Trim());
                    _inSearchMode = false;
                    _searchQuery = string.Empty;
                    break;

                case ConsoleKey.Escape:
                    _inSearchMode = false;
                    _searchQuery = string.Empty;
                    Status("Search cancelled");
                    break;

                case ConsoleKey.Backspace:
                    if (_searchQuery.Length > 0)
                        _searchQuery = _searchQuery[..^1];
                    break;

                default:
                    if (!char.IsControl(k.KeyChar))
                        _searchQuery += k.KeyChar;
                    break;
            }
        }

        private void JumpToProcess(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                Status("Type a name to search");
                return;
            }

            Process[] procs = SortedProcesses();

            for (int i = 0; i < procs.Length; i++)
            {
                try
                {
                    if (procs[i].ProcessName.Contains(q, StringComparison.OrdinalIgnoreCase))
                    {
                        _selectedIndex = i;
                        Status($"Found  ·  {procs[i].ProcessName}  [{procs[i].Id}]");
                        return;
                    }
                }
                catch { }
            }

            Status($"No match for {q}");
        }

        private void KillSelected()
        {
            try
            {
                Process[] procs = SortedProcesses();

                int idx;
                lock (_lock)
                    idx = Math.Clamp(_selectedIndex, 0, Math.Max(0, procs.Length - 1));

                if (procs.Length == 0)
                {
                    Status("Nothing to kill");
                    return;
                }

                Process p = procs[idx];
                string name = p.ProcessName;
                int pid = p.Id;

                p.Kill(entireProcessTree: false);
                p.WaitForExit(2000);

                Status($"Killed  ·  {name}  [{pid}]", 3500);
            }
            catch (Exception ex)
            {
                Status($"Kill failed  ·  {Clip(ex.Message, 50)}", 3500);
            }
        }

        // ── Render thread ────────────────────────────────────────────────────
        private void RenderLoop()
        {
            while (!_exitRequested)
            {
                try { RenderFrame(); } catch { }
                Thread.Sleep(100);
            }
        }

        private void RenderFrame()
        {
            Process[] procs;
            Dictionary<int, double> cpuSnap;
            double cpuPct, memPct, memUsed, memTotal;
            int sel;
            bool searching;
            string query, statusText;
            SortMode sort;
            int spin;

            lock (_lock)
            {
                procs = SortedProcesses();
                _lastProcessCount = procs.Length;
                _selectedIndex = Math.Clamp(_selectedIndex, 0, Math.Max(0, procs.Length - 1));

                cpuSnap = _cpuUsages;
                cpuPct = _cpuPct;
                memPct = _memPct;
                memUsed = _memUsed;
                memTotal = _memTotal;
                sel = _selectedIndex;
                searching = _inSearchMode;
                query = _searchQuery;
                sort = _sortMode;
                spin = _spinIdx++ % Spin.Length;

                statusText = DateTime.UtcNow <= _statusExp
                    ? _status
                    : "↑↓  navigate    K  kill    /  search    C M N  sort    Q  quit";
            }

            (int W, int H) = WinSize();

            if (W < 60 || H < 12)
            {
                Console.Write($"{At(0, 0)}{F(C_WARN)}{B(BG_BASE)} Terminal too small — resize to ≥ 60 × 12 {R}{EOL}");
                return;
            }

            int tableTop = 5;
            int bottomRule = H - 3;
            int statusRow = H - 2;
            int labelRow = H - 1;
            int tableH = Math.Max(0, bottomRule - tableTop);

            int pidW = 7;
            int cpuW = 7;
            int memW = 10;
            int thrW = 5;
            int userW = Math.Clamp(W / 7, 8, 18);
            int nameW = Math.Max(10, W - (2 + pidW + 2 + cpuW + 2 + memW + 2 + thrW + 2 + userW));

            // ── Row 0 : top bar ────────────────────────────────────────────
            {
                string left =
                    $"{B(BG_TOPBAR)}{F(C_ACCENT)}{Bold} WTPP {R}" +
                    $"{B(BG_TOPBAR)}{F(FG_DIM)}  ·  {F(FG_PRIMARY)}System Monitor";

                string right =
                    $"{F(FG_DIM)}{Faint}{Spin[spin]}{R}" +
                    $"{B(BG_TOPBAR)}{F(FG_DIM)}  {DateTime.Now:HH:mm:ss}  ·  {F(FG_PRIMARY)}{procs.Length}{F(FG_DIM)} procs {R}";

                Console.Write($"{At(0, 0)}{B(BG_TOPBAR)}{new string(' ', W)}");
                Console.Write($"{At(0, 0)}{left}{EOL}");

                int rightVisible = 16 + procs.Length.ToString().Length;
                int rightCol = Math.Max(0, W - rightVisible);
                Console.Write($"{At(rightCol, 0)}{B(BG_TOPBAR)}{right}{EOL}");
            }

            // ── Row 1 : gauges ─────────────────────────────────────────────
            {
                int gaugeW = Math.Max(4, (W - 58) / 2);

                _cpuPctDraw = Smooth(_cpuPctDraw, cpuPct, 0.30);
                _memPctDraw = Smooth(_memPctDraw, memPct, 0.20);

                string cpuBar = SparkBar(_cpuPctDraw, gaugeW, BG_TOPBAR);
                string memBar = SparkBar(_memPctDraw, gaugeW, BG_TOPBAR);

                int cpuC = LoadColour(_cpuPctDraw);
                int memC = LoadColour(_memPctDraw);

                string cpuStr = _cpuPctDraw.ToString("0.0").PadLeft(5) + "%";
                string memStr = _memPctDraw.ToString("0.0").PadLeft(5) + "%";
                string usedStr = memUsed.ToString("0.0").PadLeft(8);
                string totStr = memTotal.ToString("0.0").PadLeft(8);

                Console.Write($"{At(0, 1)}{B(BG_TOPBAR)}{new string(' ', W)}");

                Console.Write(
                    $"{At(0, 1)}" +
                    $"{B(BG_TOPBAR)}{F(FG_DIM)}  CPU  " +
                    $"{cpuBar}" +
                    $"{F(cpuC)}{Bold}  {cpuStr}" +
                    $"{B(BG_TOPBAR)}{F(FG_MUTED)}    " +
                    $"{F(FG_DIM)}MEM  " +
                    $"{memBar}" +
                    $"{F(memC)}{Bold}  {memStr}" +
                    $"{B(BG_TOPBAR)}{F(FG_MUTED)}  {usedStr} / {totStr} MB" +
                    $"{R}{EOL}");
            }

            // ── Row 2 : blank separator ────────────────────────────────────
            Console.Write($"{At(0, 2)}{B(BG_BASE)}{EOL}");

            // ── Row 3 : column header ──────────────────────────────────────
            {
                int aw = pidW, bw = cpuW, cw = memW - 2, dw = thrW;

                string ColHdr(SortMode m, string label, int w, bool rightAlign = false)
                {
                    string suffix = sort == m ? " ↓" : "  ";
                    string padded = rightAlign
                        ? (label + suffix).PadLeft(w)
                        : (label + suffix).PadRight(w);

                    return sort == m
                        ? $"{F(C_ACCENT)}{Bold}{padded}{R}{B(BG_COLHDR)}{F(FG_DIM)}"
                        : padded;
                }

                Console.Write(
                    $"{At(0, 3)}" +
                    $"{B(BG_COLHDR)}{F(FG_DIM)}" +
                    $"  " +
                    $"{"PID".PadLeft(aw)}  " +
                    $"{ColHdr(SortMode.Name, "NAME", nameW)}  " +
                    $"{ColHdr(SortMode.CPU, "CPU%", bw, rightAlign: true)}  " +
                    $"{ColHdr(SortMode.Memory, "MEM MB", cw, rightAlign: true)}  " +
                    $"{"THR".PadLeft(dw)}  " +
                    $"{"USER".PadRight(userW)}" +
                    R + EOL);
            }

            // ── Row 4 : thin rule ──────────────────────────────────────────
            Console.Write($"{At(0, 4)}{F(FG_MUTED)}{B(BG_BASE)}{new string('─', W)}{R}{EOL}");

            // ── Process rows ───────────────────────────────────────────────
            int visCount = Math.Min(tableH, procs.Length);
            int startIdx = Math.Clamp(sel - visCount / 2, 0, Math.Max(0, procs.Length - visCount));

            for (int row = 0; row < tableH; row++)
            {
                int procIdx = startIdx + row;
                int y = tableTop + row;

                if (procIdx >= procs.Length)
                {
                    Console.Write($"{At(0, y)}{B(BG_BASE)}{EOL}");
                    continue;
                }

                Process p = procs[procIdx];
                bool isSel = procIdx == sel;
                int rowBg = isSel ? BG_SEL : BG_BASE;

                string line;

                try
                {
                    double procCpu = cpuSnap.TryGetValue(p.Id, out double c) ? c : 0.0;
                    double procMem = SafeMemMb(p);
                    string user = Clip(CachedUser(p), userW).PadRight(userW);
                    string name = Clip(p.ProcessName, nameW).PadRight(nameW);

                    int aw = pidW, bw = cpuW, cw = memW - 2, dw = thrW;

                    string gutter = isSel
                        ? $"{F(C_ACCENT)}{B(BG_SEL2)}▌{B(rowBg)} "
                        : $"{F(BG_BASE)}{B(BG_BASE)}  ";

                    int pidC = isSel ? FG_PRIMARY : C_PID;
                    int nameC = FG_PRIMARY;
                    int cpuC = isSel ? FG_PRIMARY : LoadColour(procCpu * 3);
                    int memC = isSel ? FG_PRIMARY : C_MEM;
                    int thrC = isSel ? FG_PRIMARY : C_THR;
                    int userC = isSel ? FG_PRIMARY : C_USER;
                    string boldSel = isSel ? Bold : string.Empty;

                    line =
                        $"{gutter}{boldSel}" +
                        $"{F(pidC)}{B(rowBg)}{p.Id.ToString().PadLeft(aw)}  " +
                        $"{F(nameC)}{B(rowBg)}{name}  " +
                        $"{F(cpuC)}{B(rowBg)}{procCpu.ToString("0.0").PadLeft(bw)}  " +
                        $"{F(memC)}{B(rowBg)}{procMem.ToString("0.0").PadLeft(cw)}  " +
                        $"{F(thrC)}{B(rowBg)}{p.Threads.Count.ToString().PadLeft(dw)}  " +
                        $"{F(userC)}{B(rowBg)}{user}" +
                        R;
                }
                catch
                {
                    int aw = pidW, bw = cpuW, cw = memW - 2, dw = thrW;

                    line =
                        $"  {Faint}{F(C_DEAD)}{B(BG_BASE)}" +
                        $"{"?".PadLeft(aw)}  " +
                        $"{"<access denied>".PadRight(nameW)}  " +
                        $"{"·".PadLeft(bw)}  " +
                        $"{"·".PadLeft(cw)}  " +
                        $"{"·".PadLeft(dw)}  " +
                        $"{"·".PadRight(userW)}" +
                        R;
                }

                Console.Write($"{At(0, y)}{line}{EOL}");
            }

            // ── Bottom rule ────────────────────────────────────────────────
            Console.Write($"{At(0, bottomRule)}{F(FG_MUTED)}{B(BG_BASE)}{new string('─', W)}{R}{EOL}");

            // ── Status / search row ────────────────────────────────────────
            if (searching)
            {
                Console.Write(
                    $"{At(0, statusRow)}" +
                    $"{B(BG_BASE)}{F(C_SEARCH)}{Bold} / {R}" +
                    $"{F(C_SEARCH)}{B(BG_BASE)}{query}" +
                    $"{F(FG_MUTED)}█{R}" +
                    $"{F(FG_MUTED)}   Enter to jump  ·  Esc to cancel{R}" +
                    EOL);
            }
            else
            {
                Console.Write(
                    $"{At(0, statusRow)}" +
                    $"{B(BG_BASE)}{F(FG_DIM)} {Clip(statusText, W - 2)}{R}" +
                    EOL);
            }

            // ── Selected-process detail label ──────────────────────────────
            {
                string label = procs.Length == 0 || sel >= procs.Length
                    ? $"{F(FG_MUTED)}—{R}"
                    : SafeLabel(procs, sel);

                Console.Write($"{At(0, labelRow)}{B(BG_BASE)}{F(C_ACCENT2)} ▸ {R}{B(BG_BASE)}{label}{R}{EOL}");
            }
        }

        // ── Visual primitives ────────────────────────────────────────────────
        private static string SparkBar(double percent, int width, int bg)
        {
            int eighths = (int)Math.Round(percent / 100.0 * width * 8);
            int fullCells = eighths / 8;
            int remainder = eighths % 8;
            int empty = Math.Max(0, width - fullCells - (remainder > 0 ? 1 : 0));

            int fillC = percent >= 85 ? C_DANGER : percent >= 60 ? C_WARN : C_OK;

            var sb = new StringBuilder(width + 32);
            sb.Append(B(bg));
            sb.Append(F(fillC));
            sb.Append(new string('█', fullCells));

            if (remainder > 0)
                sb.Append(Sparks[remainder]);

            sb.Append(F(FG_MUTED));
            sb.Append(new string('░', empty));
            sb.Append(B(bg));

            return sb.ToString();
        }

        private static double Smooth(double current, double target, double factor)
        {
            if (factor <= 0) return current;
            if (factor >= 1) return target;

            double next = current + (target - current) * factor;
            return Math.Abs(next - target) < 0.05 ? target : next;
        }

        private static int LoadColour(double pct) =>
            pct >= 85 ? C_DANGER : pct >= 40 ? C_WARN : C_OK;

        // ── Process helpers ──────────────────────────────────────────────────
        private Process[] SortedProcesses()
        {
            SortMode sort;
            Dictionary<int, double> snap;

            lock (_lock)
            {
                sort = _sortMode;
                snap = _cpuUsages;
            }

            Process[] all = Process.GetProcesses();

            return sort switch
            {
                SortMode.CPU => all.OrderByDescending(p => snap.TryGetValue(p.Id, out double c) ? c : 0)
                                   .ThenBy(SafeName).ToArray(),

                SortMode.Memory => all.OrderByDescending(SafeMemBytes)
                                      .ThenBy(SafeName).ToArray(),

                _ => all.OrderBy(SafeName).ToArray()
            };
        }

        private string CachedUser(Process p)
        {
            lock (_lock)
            {
                if (_userCache.TryGetValue(p.Id, out string? u))
                    return u;
            }

            string user = GetUser(p);

            lock (_lock)
                _userCache[p.Id] = user;

            return user;
        }

        private string GetUser(Process p)
        {
            IntPtr tok = IntPtr.Zero;

            try
            {
                if (!OpenProcessToken(p.Handle, TOKEN_QUERY, out tok))
                    return "—";

                using var id = new WindowsIdentity(tok);
                string? full = id.Name;

                if (string.IsNullOrWhiteSpace(full))
                    return "—";

                int slash = full.IndexOf('\\');
                return slash >= 0 ? full[(slash + 1)..] : full;
            }
            catch
            {
                return "—";
            }
            finally
            {
                if (tok != IntPtr.Zero)
                    CloseHandle(tok);
            }
        }

        private static (double used, double total, double pct) GetMemInfo()
        {
            MEMORYSTATUSEX s = new();

            if (!GlobalMemoryStatusEx(s) || s.ullTotalPhys == 0)
                return (0, 0, 0);

            double tot = s.ullTotalPhys / 1_048_576.0;
            double avl = s.ullAvailPhys / 1_048_576.0;
            double used = Math.Max(0, tot - avl);

            return (used, tot, tot <= 0 ? 0 : used / tot * 100.0);
        }

        private void Status(string msg, int ms = 2400)
        {
            _status = msg;
            _statusExp = DateTime.UtcNow.AddMilliseconds(ms);
        }

        private string SafeLabel(Process[] procs, int idx)
        {
            try
            {
                Process p = procs[idx];
                return
                    $"{F(FG_PRIMARY)}{p.ProcessName}{R}" +
                    $"{F(FG_DIM)}   pid {p.Id}  ·  mem {SafeMemMb(p):0.0} MB  ·  thr {p.Threads.Count}{R}";
            }
            catch
            {
                return $"{F(FG_MUTED)}—{R}";
            }
        }

        private static string SafeName(Process p)
        {
            try { return p.ProcessName; }
            catch { return "~"; }
        }

        private static long SafeMemBytes(Process p)
        {
            try { return p.PrivateMemorySize64; }
            catch { return 0L; }
        }

        private static double SafeMemMb(Process p)
        {
            try { return p.PrivateMemorySize64 / 1_048_576.0; }
            catch { return 0.0; }
        }

        private static (int w, int h) WinSize()
        {
            try { return (Math.Max(1, Console.WindowWidth), Math.Max(1, Console.WindowHeight)); }
            catch { return (120, 30); }
        }

        // ── String helpers ───────────────────────────────────────────────────
        private static string Clip(string? s, int max)
        {
            s ??= string.Empty;

            if (max <= 0)
                return string.Empty;

            if (s.Length <= max)
                return s;

            return max <= 1 ? s[..max] : s[..(max - 1)] + "…";
        }

        private enum SortMode
        {
            Name,
            Memory,
            CPU
        }
    }
}