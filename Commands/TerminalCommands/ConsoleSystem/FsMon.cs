using Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class FsMon : ITerminalCommand
    {
        /*
            fsmon — Real-time filesystem monitor.
            Uses FileSystemWatcher to display created/modified/deleted/renamed
            events as they happen. Nothing like this exists natively on Windows.
            Press Q or Esc to stop.
        */

        public string Name => "fsmon";

        private static readonly string s_helpMessage = @"Usage of fsmon command:
    fsmon [path]                 : Monitor the current (or given) directory for changes.
    fsmon -r [path]              : Monitor recursively including all sub-directories.
    fsmon -l <logfile> [path]    : Log events to a file in addition to the console.
    fsmon -r -l <logfile> [path] : Recursive monitoring with logging.
    fsmon -h                     : Display this help message.

Flags -r and -l can appear in any order, before or after the path.

Examples:
    fsmon
    fsmon -r C:\Projects\myapp
    fsmon -l C:\logs\fsmon.log C:\Windows\Temp
    fsmon -r -l changes.log
    fsmon -r C:\Users\<username>\Downloads -l C:\Users\<username>\audit.log
    fsmon C:\Windows\Temp

User information:
    Audit records identify the account and process that accessed the file.
    [owner], [cached owner] and [parent owner] are ownership fallbacks, not actors.
    Short-lived files may only have a parent owner available; unknown means no identity could be read.
    Audit matching is best effort and can delay display by up to one second.

For actor information, run elevated and enable File System auditing:
1. Open Local Security Policy (secpol.msc).
2. Navigate to Security Settings → Local Policies → Audit Policy.
3. Enable 'Audit object access' for Success.

or enable it via command line with these steps:
1. Open an elevated command prompt.
2. Run: auditpol /set /subcategory:""File System"" /success:enable

Also configure the monitored folder: Properties > Security > Advanced > Auditing.
Add a Success auditing entry for the users to monitor, with write/delete permissions.
Apply it to this folder, subfolders and files when monitoring recursively.
Enabling the audit policy alone does not create file access records.

Press Q or Esc to quit.
";

        public void Execute(string args)
        {
            GlobalVariables.isErrorCommand = false;
            try
            {
                if (args == $"{Name} -h") { Console.WriteLine(s_helpMessage); return; }

                string currentDir = File.ReadAllText(GlobalVariables.currentDirectory);
                bool   recursive  = false;
                string? logFile   = null;

                string rest = args == Name ? string.Empty : args.SplitByText($"{Name} ", 1).Trim();

                // Parse flags in any order from the argument tokens.
                var tokens = new System.Collections.Generic.List<string>(
                    rest.Split(' ', StringSplitOptions.RemoveEmptyEntries));

                for (int i = 0; i < tokens.Count;)
                {
                    if (tokens[i] == "-r")
                    {
                        recursive = true;
                        tokens.RemoveAt(i);
                    }
                    else if (tokens[i] == "-l")
                    {
                        tokens.RemoveAt(i);
                        if (i >= tokens.Count)
                        {
                            FileSystem.ErrorWriteLine("No log file specified. Usage: fsmon -l <logfile> [path]");
                            GlobalVariables.isErrorCommand = true;
                            return;
                        }
                        logFile = FileSystem.SanitizePath(tokens[i], currentDir);
                        tokens.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }

                // Whatever remains is the watch path (may contain spaces if it was
                // a single token, but after splitting it could be multiple tokens
                // that form a path — rejoin them).
                string pathArg = string.Join(' ', tokens).Trim();
                string watchPath = string.IsNullOrWhiteSpace(pathArg)
                    ? currentDir
                    : FileSystem.SanitizePath(pathArg, currentDir);

                if (!Directory.Exists(watchPath))
                {
                    FileSystem.ErrorWriteLine($"Directory not found: '{watchPath}'");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }

                RunMonitor(Path.GetFullPath(watchPath), recursive, logFile == null ? null : Path.GetFullPath(logFile));
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine(ex.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }

        private static void RunMonitor(string path, bool recursive, string? logFile)
        {
            // Open log file stream if requested.
            StreamWriter? logWriter = null;
            if (logFile != null)
            {
                try
                {
                    logWriter = new StreamWriter(logFile, append: true, Encoding.UTF8)
                    {
                        AutoFlush = true
                    };
                }
                catch (Exception ex)
                {
                    FileSystem.ErrorWriteLine($"Cannot open log file: {ex.Message}");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }
            }

            try
            {
                using var identities = new FsMonIdentityResolver(path, recursive);
                identities.Start();
                Console.WriteLine();
                FileSystem.ColorConsoleText(ConsoleColor.Cyan, "  Monitoring : ");
                Console.WriteLine(path);
                FileSystem.ColorConsoleText(ConsoleColor.DarkGray, $"  Recursive  : {recursive}   Press Q to quit\n");
                FileSystem.ColorConsoleText(ConsoleColor.Cyan, "  User source: ");
                if (identities.SecurityLogAvailable)
                    FileSystem.ColorConsoleText(ConsoleColor.Green, "Security Event Log when audited; ownership fallback otherwise\n");
                else
                    FileSystem.ColorConsoleText(ConsoleColor.DarkYellow,
                        "ownership fallback (see fsmon -h for audit setup)\n");
                FileSystem.ColorConsoleText(ConsoleColor.DarkGray,
                    "  [owner]/[cached owner]/[parent owner] identify owners, not the user making the change.\n");
                if (logFile != null)
                {
                    FileSystem.ColorConsoleText(ConsoleColor.Cyan, "  Log file   : ");
                    Console.WriteLine(logFile);

                    // Write session header to log file.
                    logWriter!.WriteLine(new string('─', 80));
                    logWriter.WriteLine($"  fsmon session started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    logWriter.WriteLine($"  Monitoring : {path}");
                    logWriter.WriteLine($"  Recursive  : {recursive}");
                    logWriter.WriteLine("  User: audit actor when available; [owner]/[cached owner]/[parent owner] are ownership fallbacks.");
                    logWriter.WriteLine(new string('─', 80));
                }
                Console.WriteLine(new string('─', 80));

                const int pendingLimit = 8192;
                var pending = new Queue<PendingChange>();
                var pendingLock = new object();
                bool accepting = true;
                int overflowed = 0;

                void Enqueue(FileSystemEventArgs e)
                {
                    // Logging inside the watched tree must not generate an endless
                    // stream of events caused by fsmon's own log writes.
                    if (string.Equals(e.FullPath, logFile, StringComparison.OrdinalIgnoreCase)) return;
                    DateTime observed = DateTime.UtcNow;
                    long readyAt = Environment.TickCount64 + (identities.SecurityLogAvailable ? 1000 : 0);
                    string? oldPath = (e as RenamedEventArgs)?.OldFullPath;
                    lock (pendingLock)
                    {
                        if (!accepting) return;
                        if (pending.Count >= pendingLimit)
                        {
                            Interlocked.Exchange(ref overflowed, 1);
                            return;
                        }
                        string fallback = identities.CaptureFallback(e.FullPath, e.ChangeType, oldPath);
                        pending.Enqueue(new PendingChange(e.ChangeType, e.FullPath, oldPath, observed, readyAt, fallback));
                    }
                }

                void Drain(bool flush)
                {
                    // Bound each batch to keep Q/Esc responsive under heavy load.
                    for (int i = 0; flush || i < 256; i++)
                    {
                        PendingChange change;
                        lock (pendingLock)
                        {
                            if (pending.Count == 0 || (!flush && pending.Peek().ReadyAt > Environment.TickCount64)) return;
                            change = pending.Dequeue();
                        }
                        string actor = identities.Resolve(change.Path, change.Type, change.ObservedUtc, change.Fallback, change.OldPath);
                        var (label, color) = change.Type switch
                        {
                            WatcherChangeTypes.Created => ("CREATED ", ConsoleColor.Green),
                            WatcherChangeTypes.Deleted => ("DELETED ", ConsoleColor.Red),
                            WatcherChangeTypes.Renamed => ("RENAMED ", ConsoleColor.Cyan),
                            _ => ("MODIFIED", ConsoleColor.Yellow)
                        };
                        string detail = change.OldPath == null ? change.Path : $"{change.OldPath}  →  {change.Path}";
                        Print(label, color, detail, actor, logWriter, change.ObservedUtc.ToLocalTime());
                    }
                }

                using var watcher = new FileSystemWatcher(path)
                {
                    IncludeSubdirectories = recursive,
                    EnableRaisingEvents   = false,
                    NotifyFilter = NotifyFilters.FileName     | NotifyFilters.DirectoryName |
                                   NotifyFilters.LastWrite    | NotifyFilters.Size,
                    InternalBufferSize    = 65536
                };

                watcher.Created += (_, e) => Enqueue(e);
                watcher.Changed += (_, e) => Enqueue(e);
                watcher.Deleted += (_, e) => Enqueue(e);
                watcher.Renamed += (_, e) => Enqueue(e);
                watcher.Error += (_, e) =>
                {
                    if (e.GetException() is InternalBufferOverflowException)
                        Interlocked.Exchange(ref overflowed, 1);
                };

                watcher.EnableRaisingEvents = true;

                try
                {
                    while (true)
                    {
                        Thread.Sleep(50);
                        Drain(flush: false);
                        if (Interlocked.Exchange(ref overflowed, 0) != 0)
                            Print("OVERFLOW", ConsoleColor.Magenta,
                                "Event buffer overflowed — some events may be lost", "unknown", logWriter, DateTime.Now);
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(intercept: true);
                            if (key.Key == ConsoleKey.Q || key.Key == ConsoleKey.Escape) break;
                        }
                    }
                }
                finally
                {
                    watcher.EnableRaisingEvents = false;
                    lock (pendingLock) accepting = false;
                }

                // Callbacks no longer write to the console or log. Drain accepted
                // notifications before the footer and before disposing the writer.
                Drain(flush: true);

                // Write session footer to log file.
                logWriter?.WriteLine(new string('─', 80));
                logWriter?.WriteLine($"  fsmon session ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                logWriter?.WriteLine(new string('─', 80));
            }
            finally
            {
                logWriter?.Dispose();
            }
        }

        private sealed record PendingChange(WatcherChangeTypes Type, string Path, string? OldPath,
            DateTime ObservedUtc, long ReadyAt, string Fallback);

        private static readonly object s_lock = new();

        private static void Print(string type, ConsoleColor color, string detail, string actor, StreamWriter? logWriter, DateTime observed)
        {
            string timestamp = observed.ToString("yyyy-MM-dd HH:mm:ss");

            lock (s_lock)
            {
                // Console output (coloured).
                FileSystem.ColorConsoleText(ConsoleColor.DarkGray, $"  {observed:HH:mm:ss}  ");
                FileSystem.ColorConsoleText(color,                  $"{type}  ");
                FileSystem.ColorConsoleText(ConsoleColor.DarkCyan,  $"{actor,-32}  ");
                Console.WriteLine(detail);

                // Log file output (plain text).
                logWriter?.WriteLine($"  {timestamp}  {type}  {actor,-32}  {detail}");
            }
        }
    }
}
