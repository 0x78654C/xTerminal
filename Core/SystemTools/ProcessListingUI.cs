using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.SystemTools
{
    [SupportedOSPlatform("windows")]
    public sealed partial class ProcessListingUI
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

        // FIX 2: default-background escape — lets the terminal's own background
        //        show through for non-selected rows instead of painting BG_BASE (232 ≈ black).
        private const string BgReset = "\x1b[49m";

        private static readonly char[] Sparks = { ' ', '▏', '▎', '▍', '▌', '▋', '▊', '▉', '█' };
        private static readonly string[] Spin = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };

        // ── Shared state ─────────────────────────────────────────────────────
        private readonly object _lock = new();
        private volatile bool _exitRequested;
        private int _selectedIndex;
        private ProcessIdentity? _selectedIdentity;
        private bool _inSearchMode;
        private string _searchQuery = string.Empty;
        private string _lastSearchQuery = string.Empty;
        private SortMode _sortMode = SortMode.Name;

        private readonly Dictionary<ProcessIdentity, (long CpuTicks, long Timestamp)> _prevCpuTimes = new();
        private readonly Dictionary<ProcessIdentity, string> _userCache = new();
        private readonly HashSet<ProcessIdentity> _pendingUserLookups = new();
        private const int MaxUserLookups = 2;
        private const int MaxSearchLength = 256;
        private AutoResetEvent _sampleWake;
        private bool _hasSample;
        private bool _killPending;

        private double _cpuPct;
        private double _memPct;
        private double _memUsed;
        private double _memTotal;

        private double _cpuPctDraw;
        private double _memPctDraw;

        private string _status = string.Empty;
        private DateTime _statusExp = DateTime.MinValue;
        private int _spinIdx;

        // Frame buffer — entire frame is assembled here, then flushed in one write.
        // Eliminates per-row Console.Write syscalls and all mid-frame repaints.
        private readonly StringBuilder _fb = new(1 << 17);

        // Only immutable values cross threads. Process handles belong to the sampler
        // and are disposed before the next sample; drawing never queries a live process.
        private ProcessSnapshot[] _cachedProcs = Array.Empty<ProcessSnapshot>();
        private ProcessSnapshot[] _sortedProcs = Array.Empty<ProcessSnapshot>();
        private ProcessSnapshot[] _sortedSource;
        private SortMode _sortedMode;

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

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint access, bool inheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetProcessTimes(IntPtr process, out long created, out long exited, out long kernel, out long user);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(out long idle, out long kernel, out long user);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateProcess(IntPtr process, uint exitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

        // ── ANSI helpers ─────────────────────────────────────────────────────
        private static string F(int c) => $"{CSI}38;5;{c}m";
        private static string B(int c) => $"{CSI}48;5;{c}m";
        private static string Bold => $"{CSI}1m";
        private static string Faint => $"{CSI}2m";
        private static string At(int col, int row) => $"{CSI}{row + 1};{col + 1}H";

        // The calling thread owns all console input/output. Only sampling and
        // bounded owner lookups run in the background, so exit cannot leave a
        // renderer drawing over the shell or an input thread consuming its keys.
        public void Run()
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Windows only.");
            if (Console.IsInputRedirected || Console.IsOutputRedirected)
                throw new InvalidOperationException("wtop requires an interactive terminal.");

            using var virtualTerminalOutput = VirtualTerminalOutput.Enable();
            Encoding originalEncoding = Console.OutputEncoding;
            bool originalControlC = Console.TreatControlCAsInput;
            using var wake = new AutoResetEvent(false);
            var sampler = new Thread(SampleLoop) { IsBackground = true, Name = "wtop-Sample" };
            bool started = false;
            _exitRequested = false;
            _sampleWake = wake;

            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                Console.TreatControlCAsInput = true;
                Console.Write(AltScreen + HideCursor + CSI + "2J");
                RenderFrame();
                sampler.Start();
                started = true;

                long lastFrame = Stopwatch.GetTimestamp();
                while (!_exitRequested)
                {
                    bool inputChanged = false;
                    // Limit each input batch so a held key cannot starve rendering.
                    for (int i = 0; i < 32 && !_exitRequested && Console.KeyAvailable; i++)
                    {
                        HandleKey(Console.ReadKey(intercept: true));
                        inputChanged = true;
                    }

                    if (_exitRequested) break;
                    if (inputChanged || Stopwatch.GetElapsedTime(lastFrame).TotalMilliseconds >= 100)
                    {
                        RenderFrame();
                        lastFrame = Stopwatch.GetTimestamp();
                    }
                    Thread.Sleep(15);
                }
            }
            finally
            {
                _exitRequested = true;
                wake.Set();
                if (started) sampler.Join();
                lock (_lock) _sampleWake = null;
                try { Console.Write(R + ShowCursor + NormalScreen); } catch { }
                try { Console.TreatControlCAsInput = originalControlC; } catch { }
                try { Console.OutputEncoding = originalEncoding; } catch { }
            }
        }

        private void SampleLoop()
        {
            PerformanceCounter groupCounter = null;
            bool primed = false;
            (long Idle, long Kernel, long User) previous = default;
            bool useCounter = Environment.ProcessorCount > 64;

            // GetSystemTimes includes idle time in kernel time. For machines with
            // more than 64 CPUs, retain the all-processor counter: GetSystemTimes
            // only covers the calling thread's processor group on those machines.
            if (!useCounter)
                primed = GetSystemTimes(out previous.Idle, out previous.Kernel, out previous.User);

            try
            {
                while (!_exitRequested)
                {
                    try
                    {
                        ProcessSnapshot[] processes = CaptureProcesses();
                        if (_exitRequested) break;
                        var memory = GetMemInfo();
                        lock (_lock)
                        {
                            _memUsed = memory.used;
                            _memTotal = memory.total;
                            _memPct = Math.Clamp(memory.pct, 0, 100);
                            PublishProcesses(processes);
                        }

                        double cpu = 0;
                        bool hasCpu = false;
                        if (useCounter)
                            hasCpu = TryReadSystemCpu(ref groupCounter, ref primed, out cpu);
                        else if (GetSystemTimes(out long idle, out long kernel, out long user))
                        {
                            hasCpu = primed && TryCalculateSystemCpu(previous.Idle, previous.Kernel, previous.User,
                                idle, kernel, user, out cpu);
                            previous = (idle, kernel, user);
                            primed = true;
                        }

                        if (hasCpu)
                            lock (_lock) _cpuPct = Math.Clamp(cpu, 0, 100);
                    }
                    catch (Exception ex)
                    {
                        Status("Refresh failed  ·  " + Clip(ex.Message, 80));
                    }

                    if (!_exitRequested) _sampleWake.WaitOne(1000);
                }
            }
            finally
            {
                groupCounter?.Dispose();
                _prevCpuTimes.Clear();
            }
        }

        private ProcessSnapshot[] CaptureProcesses()
        {
            long parentSnapshotTime = DateTime.UtcNow.Ticks;
            Dictionary<int, int> parents = CaptureParentProcessIds();
            Process[] processes = Process.GetProcesses();
            var rows = new List<ProcessSnapshot>(processes.Length);
            var live = new HashSet<ProcessIdentity>();
            try
            {
                foreach (Process process in processes)
                {
                    if (_exitRequested) break;
                    try
                    {
                        int pid = process.Id;
                        string name = process.ProcessName;
                        long memory = 0, started = 0;
                        int threads = 0;
                        double cpu = 0;
                        try { memory = process.PrivateMemorySize64; } catch { }
                        try { threads = process.Threads.Count; } catch { }
                        try
                        {
                            started = process.StartTime.ToUniversalTime().Ticks;
                            long ticks = process.TotalProcessorTime.Ticks;
                            long timestamp = Stopwatch.GetTimestamp();
                            var identity = new ProcessIdentity(pid, started);
                            live.Add(identity);
                            if (_prevCpuTimes.TryGetValue(identity, out var previous))
                                cpu = CalculateProcessCpu(previous.CpuTicks, ticks,
                                    (timestamp - previous.Timestamp) / (double)Stopwatch.Frequency, Environment.ProcessorCount);
                            _prevCpuTimes[identity] = (ticks, timestamp);
                        }
                        catch { }
                        // A process created after the parent snapshot may have reused a
                        // PID from that snapshot. Leave its parent unknown until refresh.
                        int parentId = started <= parentSnapshotTime ? parents.GetValueOrDefault(pid) : 0;
                        rows.Add(new ProcessSnapshot(pid, name, memory, threads, started, cpu) { ParentId = parentId });
                    }
                    catch { } // An exiting process must not discard the rest of the sample.
                    finally { process.Dispose(); }
                }
            }
            finally
            {
                // Also release any objects not visited after cancellation.
                foreach (Process process in processes) process.Dispose();
            }
            foreach (var dead in _prevCpuTimes.Keys.Except(live).ToArray())
                _prevCpuTimes.Remove(dead);
            return rows.ToArray();
        }

        private void PublishProcesses(ProcessSnapshot[] processes)
        {
            lock (_lock)
            {
                _cachedProcs = processes;
                _hasSample = true;
                var live = processes.Select(p => p.Identity).ToHashSet();
                foreach (var dead in _userCache.Keys.Except(live).ToArray())
                    _userCache.Remove(dead);
                _collapsedProcesses.RemoveWhere(identity => !live.Contains(identity));
            }
        }

        private void HandleKey(ConsoleKeyInfo key)
        {
            lock (_lock)
            {
                if (key.Key == ConsoleKey.C && key.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    _exitRequested = true;
                    return;
                }
                if (_inSearchMode)
                {
                    HandleSearch(key);
                    return;
                }

                int pageSize = Math.Max(1, WinSize().h - 8);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow: MoveSelection(-1); break;
                    case ConsoleKey.DownArrow: MoveSelection(1); break;
                    case ConsoleKey.PageUp: MoveSelection(-pageSize); break;
                    case ConsoleKey.PageDown: MoveSelection(pageSize); break;
                    case ConsoleKey.Home: SelectIndex(0); break;
                    case ConsoleKey.End: SelectIndex(int.MaxValue); break;
                    case ConsoleKey.K:
                        // Capture the displayed identity now, before queuing work.
                        // Re-enumerating later could kill a different row after a sort.
                        ProcessSnapshot target = SelectedProcess();
                        if (target == null) Status("Nothing to kill");
                        else if (!_killPending)
                        {
                            _killPending = true;
                            Task.Run(() => KillSelected(target));
                        }
                        break;
                    case ConsoleKey.Q:
                    case ConsoleKey.Escape: _exitRequested = true; break;
                    case ConsoleKey.C: ChangeSort(SortMode.CPU); break;
                    case ConsoleKey.M: ChangeSort(SortMode.Memory); break;
                    case ConsoleKey.N: ChangeSort(SortMode.Name); break;
                    case ConsoleKey.T:
                    case ConsoleKey.F5: ToggleTreeView(); break;
                    case ConsoleKey.LeftArrow: NavigateTree(expand: false); break;
                    case ConsoleKey.RightArrow: NavigateTree(expand: true); break;
                    case ConsoleKey.Spacebar: ToggleSelectedBranch(); break;
                    case ConsoleKey.R: RequestSample(); Status("Refreshing processes…"); break;
                    case ConsoleKey.F3: JumpToProcess(_lastSearchQuery, next: true); break;
                    case ConsoleKey.Oem2:
                        _inSearchMode = true;
                        _searchQuery = string.Empty;
                        Status("Search  —  name or PID  ·  Enter to jump  ·  Esc to cancel");
                        break;
                }
            }
        }

        private void ChangeSort(SortMode mode)
        {
            _sortMode = mode;
            SortedProcesses();
            Status("Sort  ·  " + mode);
        }

        private void MoveSelection(int delta)
        {
            SortedProcesses();
            SelectIndex(_selectedIndex + delta);
        }

        private void SelectIndex(int index)
        {
            ProcessSnapshot[] processes = SortedProcesses();
            _selectedIndex = Math.Clamp(index, 0, Math.Max(0, processes.Length - 1));
            _selectedIdentity = processes.Length == 0 ? null : processes[_selectedIndex].Identity;
        }

        private void HandleSearch(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    string query = _searchQuery.Trim();
                    JumpToProcess(query.Length == 0 ? _lastSearchQuery : query, next: query.Length == 0);
                    _inSearchMode = false;
                    _searchQuery = string.Empty;
                    break;
                case ConsoleKey.Escape:
                    _inSearchMode = false;
                    _searchQuery = string.Empty;
                    Status("Search cancelled");
                    break;
                case ConsoleKey.Backspace:
                    if (_searchQuery.Length > 0) _searchQuery = _searchQuery[..^1];
                    break;
                default:
                    if (!char.IsControl(key.KeyChar) && _searchQuery.Length < MaxSearchLength &&
                        !key.Modifiers.HasFlag(ConsoleModifiers.Control) && !key.Modifiers.HasFlag(ConsoleModifiers.Alt))
                        _searchQuery += key.KeyChar;
                    break;
            }
        }

        private void JumpToProcess(string query, bool next)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                Status("Type a name or PID to search");
                return;
            }

            SortedProcesses();
            ProcessSnapshot[] processes = _treeView ? _treeAllProcs : _sortedProcs;
            _lastSearchQuery = query;
            int current = _selectedIdentity.HasValue
                ? Array.FindIndex(processes, p => p.Identity == _selectedIdentity.Value) : -1;
            int start = next ? current + 1 : 0;
            for (int offset = 0; offset < processes.Length; offset++)
            {
                int index = (start + offset) % processes.Length;
                ProcessSnapshot process = processes[index];
                if (process.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || process.Id.ToString() == query)
                {
                    if (_treeView)
                    {
                        _selectedIdentity = process.Identity;
                        _sortedSource = null;
                        SortedProcesses(); // Reveals the selected process's ancestors.
                    }
                    else SelectIndex(index);
                    Status($"Found  ·  {process.Name}  [{process.Id}]");
                    return;
                }
            }
            Status($"No match for {query}");
        }

        private ProcessSnapshot SelectedProcess()
        {
            // Use the visible view, without replacing it with a newer sample.
            return _selectedIndex >= 0 && _selectedIndex < _sortedProcs.Length
                ? _sortedProcs[_selectedIndex] : null;
        }

        private void KillSelected(ProcessSnapshot target)
        {
            IntPtr process = IntPtr.Zero;
            try
            {
                // Request only query, terminate and wait rights. Keep this same handle
                // through validation and termination, even if Windows recycles the PID.
                const uint access = 0x1000 | 0x0001 | 0x00100000;
                process = OpenProcess(access, false, target.Id);
                if (process == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                if (target.StartTimeTicks == 0 || !GetProcessTimes(process, out long created, out _, out _, out _) ||
                    DateTime.FromFileTimeUtc(created).Ticks != target.StartTimeTicks)
                    throw new InvalidOperationException("The selected process has exited or its identity cannot be verified.");
                if (!TerminateProcess(process, uint.MaxValue))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                uint wait = WaitForSingleObject(process, 2000);
                if (wait == uint.MaxValue)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                bool exited = wait == 0;
                Status($"{(exited ? "Killed" : "Termination requested")}  ·  {target.Name}  [{target.Id}]", 3500);
                RequestSample();
            }
            catch (Exception ex)
            {
                Status($"Kill failed  ·  {Clip(ex.Message, 80)}", 3500);
            }
            finally
            {
                if (process != IntPtr.Zero) CloseHandle(process);
                lock (_lock) _killPending = false;
            }
        }

        private void RequestSample()
        {
            lock (_lock)
                if (!_exitRequested) _sampleWake?.Set();
        }

        private void RenderFrame()
        {
            (int W, int H) = WinSize();
            Console.Write(BuildFrame(W, H));
        }

        private string BuildFrame(int W, int H)
        {
            ProcessSnapshot[] procs;
            double cpuPct, memPct, memUsed, memTotal;
            int sel;
            bool searching;
            string query, statusText;
            SortMode sort;
            int spin;
            bool hasSample;
            bool treeView;
            int totalCount;

            lock (_lock)
            {
                procs = SortedProcesses();
                cpuPct = _cpuPct;
                memPct = _memPct;
                memUsed = _memUsed;
                memTotal = _memTotal;
                sel = _selectedIndex;
                searching = _inSearchMode;
                query = _searchQuery;
                sort = _sortMode;
                spin = _spinIdx++ % Spin.Length;
                hasSample = _hasSample;
                treeView = _treeView;
                totalCount = _cachedProcs.Length;

                statusText = DateTime.UtcNow <= _statusExp
                    ? _status
                    : treeView ? "←/→ fold  T flat  / search  C/M/N sort  K kill  Q quit"
                        : "T tree  / search  C/M/N sort  R refresh  K kill  Q quit";
            }

            if (W < 60 || H < 12)
            {
                return $"{At(0, 0)}{F(C_WARN)}{B(BG_BASE)}{Clip("Terminal too small — resize to ≥ 60 × 12", Math.Max(0, W - 1))}{R}{EOL}";
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

            // ── Build entire frame into _fb, flush with one Console.Write ──
            // This eliminates all mid-frame repaints: the terminal only sees
            // a single write containing the finished frame.
            _fb.Clear();

            // ── Row 0 : top bar ────────────────────────────────────────────
            // Left and right portions go into the same buffer write — no gap
            // between them for the terminal to render, so the clock/spinner
            // no longer flicker independently of the left side.
            {
                string left =
                    $"{B(BG_TOPBAR)}{F(C_ACCENT)}{Bold} WTOP {R}" +
                    $"{B(BG_TOPBAR)}{F(FG_DIM)}  ·  {F(FG_PRIMARY)}{(treeView ? "Process tree" : "Process manager")}";

                string right =
                    $"{F(FG_DIM)}{Faint}{Spin[spin]}{R}" +
                    $"{B(BG_TOPBAR)}{F(FG_DIM)}  {DateTime.Now:HH:mm:ss}  ·  {F(FG_PRIMARY)}{totalCount}{F(FG_DIM)} procs {R}";

                int rightVisible = 23 + totalCount.ToString().Length;
                int rightCol = Math.Max(0, W - rightVisible);

                _fb.Append(At(0, 0)).Append(left).Append(EOL)
                   .Append(At(rightCol, 0)).Append(B(BG_TOPBAR)).Append(right);
            }

            // ── Row 1 : gauges ─────────────────────────────────────────────
            {
                bool showMemoryTotals = W >= 80;
                int gaugeW = Math.Max(4, (W - (showMemoryTotals ? 58 : 34)) / 2);

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

                _fb.Append(At(0, 1))
                   .Append(B(BG_TOPBAR)).Append(F(FG_DIM)).Append("  CPU  ")
                   .Append(cpuBar)
                   .Append(F(cpuC)).Append(Bold).Append("  ").Append(cpuStr)
                   .Append(B(BG_TOPBAR)).Append(F(FG_MUTED)).Append("    ")
                   .Append(F(FG_DIM)).Append("MEM  ")
                   .Append(memBar)
                   .Append(F(memC)).Append(Bold).Append("  ").Append(memStr);
                if (showMemoryTotals)
                    _fb.Append(B(BG_TOPBAR)).Append(F(FG_MUTED))
                       .Append("  ").Append(usedStr).Append(" / ").Append(totStr).Append(" MB");
                _fb.Append(R).Append(EOL);
            }

            // ── Row 2 : blank separator ────────────────────────────────────
            _fb.Append(At(0, 2)).Append(BgReset).Append(EOL);

            // ── Row 3 : column header ──────────────────────────────────────
            {
                int aw = pidW, bw = cpuW, cw = memW - 2, dw = thrW;

                string ColHdr(SortMode m, string label, int w, bool rightAlign = false)
                {
                    string suffix = sort == m ? (m == SortMode.Name ? " ↑" : " ↓") : "  ";
                    string padded = rightAlign
                        ? (label + suffix).PadLeft(w)
                        : (label + suffix).PadRight(w);

                    return sort == m
                        ? $"{F(C_ACCENT)}{Bold}{padded}{R}{B(BG_COLHDR)}{F(FG_DIM)}"
                        : padded;
                }

                _fb.Append(At(0, 3))
                   .Append(B(BG_COLHDR)).Append(F(FG_DIM))
                   .Append("  ")
                   .Append("PID".PadLeft(aw)).Append("  ")
                   .Append(ColHdr(SortMode.Name, "NAME", nameW)).Append("  ")
                   .Append(ColHdr(SortMode.CPU, "CPU%", bw, rightAlign: true)).Append("  ")
                   .Append(ColHdr(SortMode.Memory, "MEM MB", cw, rightAlign: true)).Append("  ")
                   .Append("THR".PadLeft(dw)).Append("  ")
                   .Append("USER".PadRight(userW))
                   .Append(R).Append(EOL);
            }

            // ── Row 4 : thin rule ──────────────────────────────────────────
            _fb.Append(At(0, 4)).Append(F(FG_MUTED)).Append(BgReset)
               .Append(new string('─', W)).Append(R).Append(EOL);

            // ── Process rows ───────────────────────────────────────────────
            int visCount = Math.Min(tableH, procs.Length);
            int startIdx = Math.Clamp(sel - visCount / 2, 0, Math.Max(0, procs.Length - visCount));

            for (int row = 0; row < tableH; row++)
            {
                int procIdx = startIdx + row;
                int y = tableTop + row;

                if (procIdx >= procs.Length)
                {
                    _fb.Append(At(0, y)).Append(BgReset);
                    if (row == 0 && procs.Length == 0)
                        _fb.Append(F(FG_DIM)).Append(hasSample ? "  No processes available" : "  Loading processes…").Append(R);
                    _fb.Append(EOL);
                    continue;
                }

                ProcessSnapshot p = procs[procIdx];
                bool isSel = procIdx == sel;
                string rowBgEsc = isSel ? B(BG_SEL) : BgReset;

                _fb.Append(At(0, y));

                try
                {
                    double procCpu = p.CpuPercent;
                    double procMem = p.MemoryBytes / 1_048_576.0;
                    string user = Clip(CachedUser(p), userW).PadRight(userW);
                    string name = ProcessDisplayName(p, nameW).PadRight(nameW);

                    int aw = pidW, bw = cpuW, cw = memW - 2, dw = thrW;

                    int pidC = isSel ? FG_PRIMARY : C_PID;
                    int cpuC = isSel ? FG_PRIMARY : LoadColour(procCpu * 3);
                    int memC = isSel ? FG_PRIMARY : C_MEM;
                    int thrC = isSel ? FG_PRIMARY : C_THR;
                    int userC = isSel ? FG_PRIMARY : C_USER;

                    if (isSel)
                        _fb.Append(F(C_ACCENT)).Append(B(BG_SEL2)).Append('▌').Append(rowBgEsc).Append(' ').Append(Bold);
                    else
                        _fb.Append(BgReset).Append("  ");

                    _fb.Append(F(pidC)).Append(rowBgEsc).Append(p.Id.ToString().PadLeft(aw)).Append("  ")
                       .Append(F(FG_PRIMARY)).Append(rowBgEsc).Append(name).Append("  ")
                       .Append(F(cpuC)).Append(rowBgEsc).Append(procCpu.ToString("0.0").PadLeft(bw)).Append("  ")
                       .Append(F(memC)).Append(rowBgEsc).Append(procMem.ToString("0.0").PadLeft(cw)).Append("  ")
                       .Append(F(thrC)).Append(rowBgEsc).Append(p.ThreadCount.ToString().PadLeft(dw)).Append("  ")
                       .Append(F(userC)).Append(rowBgEsc).Append(user)
                       .Append(R).Append(EOL);
                }
                catch
                {
                    int aw = pidW, bw = cpuW, cw = memW - 2, dw = thrW;

                    _fb.Append("  ").Append(Faint).Append(F(C_DEAD)).Append(BgReset)
                       .Append("?".PadLeft(aw)).Append("  ")
                       .Append("<access denied>".PadRight(nameW)).Append("  ")
                       .Append("·".PadLeft(bw)).Append("  ")
                       .Append("·".PadLeft(cw)).Append("  ")
                       .Append("·".PadLeft(dw)).Append("  ")
                       .Append("·".PadRight(userW))
                       .Append(R).Append(EOL);
                }
            }

            // ── Bottom rule ────────────────────────────────────────────────
            _fb.Append(At(0, bottomRule)).Append(F(FG_MUTED)).Append(BgReset)
               .Append(new string('─', W)).Append(R).Append(EOL);

            // ── Status / search row ────────────────────────────────────────
            _fb.Append(At(0, statusRow));
            if (searching)
            {
                int queryWidth = Math.Max(1, W - 40);
                string visibleQuery = query.Length <= queryWidth ? query : "…" + query[^(queryWidth - 1)..];
                _fb.Append(BgReset).Append(F(C_SEARCH)).Append(Bold).Append(" / ").Append(R)
                   .Append(F(C_SEARCH)).Append(BgReset).Append(visibleQuery)
                   .Append(F(FG_MUTED)).Append('█').Append(R)
                   .Append(F(FG_MUTED)).Append("   Enter to jump  ·  Esc to cancel").Append(R)
                   .Append(EOL);
            }
            else
            {
                _fb.Append(BgReset).Append(F(FG_DIM)).Append(' ')
                   .Append(Clip(statusText, W - 2)).Append(R).Append(EOL);
            }

            // ── Selected-process detail label ──────────────────────────────
            {
                string label = procs.Length == 0 || sel >= procs.Length
                    ? $"{F(FG_MUTED)}—{R}"
                    : F(FG_PRIMARY) + Clip(SafeLabel(procs[sel]), W - 4);

                _fb.Append(At(0, labelRow)).Append(BgReset)
                   .Append(F(C_ACCENT2)).Append(" ▸ ").Append(R)
                   .Append(BgReset).Append(label).Append(R).Append(EOL);
            }

            return _fb.ToString();
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

        private static ProcessSnapshot[] SortProcesses(ProcessSnapshot[] processes, SortMode sort)
        {
            return sort switch
            {
                SortMode.CPU => processes.OrderByDescending(p => p.CpuPercent)
                    .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Id).ToArray(),
                SortMode.Memory => processes.OrderByDescending(p => p.MemoryBytes)
                    .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Id).ToArray(),
                _ => processes.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Id).ToArray()
            };
        }

        private ProcessSnapshot[] SortedProcesses()
        {
            lock (_lock)
            {
                if (ReferenceEquals(_sortedSource, _cachedProcs) && _sortedMode == _sortMode)
                    return _sortedProcs;

                ProcessSnapshot[] ordered = SortProcesses(_cachedProcs, _sortMode);
                _sortedProcs = _treeView ? BuildTreeView(ordered) : ordered;
                _sortedSource = _cachedProcs;
                _sortedMode = _sortMode;
                int selected = _selectedIdentity.HasValue
                    ? Array.FindIndex(_sortedProcs, p => p.Identity == _selectedIdentity.Value) : -1;
                _selectedIndex = selected >= 0 ? selected : Math.Clamp(_selectedIndex, 0, Math.Max(0, _sortedProcs.Length - 1));
                _selectedIdentity = _sortedProcs.Length == 0 ? null : _sortedProcs[_selectedIndex].Identity;
                return _sortedProcs;
            }
        }

        private string CachedUser(ProcessSnapshot process)
        {
            if (process.StartTimeTicks == 0) return "—";
            ProcessIdentity identity = process.Identity;
            lock (_lock)
            {
                if (_userCache.TryGetValue(identity, out string user)) return user;
                if (!_exitRequested && _pendingUserLookups.Count < MaxUserLookups && _pendingUserLookups.Add(identity))
                    Task.Run(() => ResolveUser(identity));
            }
            return "…";
        }

        private void ResolveUser(ProcessIdentity identity)
        {
            string user = GetUser(identity);
            lock (_lock)
            {
                _pendingUserLookups.Remove(identity);
                if (!_exitRequested && _cachedProcs.Any(p => p.Identity == identity))
                    _userCache[identity] = user;
            }
        }

        private static string GetUser(ProcessIdentity identity)
        {
            const uint queryLimitedInformation = 0x1000;
            IntPtr process = IntPtr.Zero, token = IntPtr.Zero;
            try
            {
                process = OpenProcess(queryLimitedInformation, false, identity.Id);
                if (process == IntPtr.Zero ||
                    !GetProcessTimes(process, out long created, out _, out _, out _) ||
                    DateTime.FromFileTimeUtc(created).Ticks != identity.StartTimeTicks ||
                    !OpenProcessToken(process, TOKEN_QUERY, out token))
                    return "—";

                using var owner = new WindowsIdentity(token);
                string full = owner.Name;
                if (string.IsNullOrWhiteSpace(full)) return "—";
                int slash = full.IndexOf('\\');
                return slash >= 0 ? full[(slash + 1)..] : full;
            }
            catch { return "—"; }
            finally
            {
                if (token != IntPtr.Zero) CloseHandle(token);
                if (process != IntPtr.Zero) CloseHandle(process);
            }
        }

        private static double CalculateProcessCpu(long previousTicks, long currentTicks, double seconds, int processors)
        {
            if (seconds <= 0 || processors <= 0 || currentTicks < previousTicks) return 0;
            return Math.Clamp((currentTicks - previousTicks) / (double)TimeSpan.TicksPerSecond / seconds / processors * 100, 0, 100);
        }

        private static bool TryCalculateSystemCpu(long previousIdle, long previousKernel, long previousUser,
            long idle, long kernel, long user, out double cpu)
        {
            cpu = 0;
            if (idle < previousIdle || kernel < previousKernel || user < previousUser) return false;
            long total = (kernel - previousKernel) + (user - previousUser);
            if (total <= 0) return false;
            cpu = Math.Clamp((total - (idle - previousIdle)) / (double)total * 100, 0, 100);
            return true;
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

        private static bool TryReadSystemCpu(
            ref PerformanceCounter counter,
            ref bool primed,
            out double cpu)
        {
            cpu = 0;

            try
            {
                counter ??= new PerformanceCounter("Processor", "% Processor Time", "_Total", readOnly: true);
                double next = counter.NextValue();

                if (!primed)
                {
                    primed = true;
                    return false;
                }

                cpu = next;
                return true;
            }
            catch
            {
                try { counter?.Dispose(); } catch { }
                counter = null;
                primed = false;
                return false;
            }
        }

        private void Status(string msg, int ms = 2400)
        {
            lock (_lock)
            {
                if (_exitRequested) return;
                _status = msg;
                _statusExp = DateTime.UtcNow.AddMilliseconds(ms);
            }
        }

        private static string SafeLabel(ProcessSnapshot process)
        {
            return $"{process.Name}   pid {process.Id}  ·  mem {process.MemoryBytes / 1_048_576.0:0.0} MB  ·  thr {process.ThreadCount}";
        }

        private readonly record struct ProcessIdentity(int Id, long StartTimeTicks);

        private sealed record ProcessSnapshot(int Id, string Name, long MemoryBytes, int ThreadCount, long StartTimeTicks, double CpuPercent)
        {
            public ProcessIdentity Identity => new(Id, StartTimeTicks);
            public int ParentId { get; init; }
        }

        private static (int w, int h) WinSize()
        {
            try { return (Math.Max(1, Console.WindowWidth), Math.Max(1, Console.WindowHeight)); }
            catch { return (120, 30); }
        }

        // ── String helpers ───────────────────────────────────────────────────
        private static string Clip(string s, int max)
        {
            s ??= string.Empty;

            if (max <= 0)
                return string.Empty;

            if (s.Any(char.IsControl))
                s = new string(s.Select(c => char.IsControl(c) ? ' ' : c).ToArray());

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
