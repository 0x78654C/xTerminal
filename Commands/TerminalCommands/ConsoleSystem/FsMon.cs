using Core;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Principal;
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

Attention: For best user information that made the operation, enable File System auditing for the monitored directory (and subdirs if -r) with these steps:
1. Open Local Security Policy (secpol.msc).
2. Navigate to Security Settings → Local Policies → Audit Policy.
3. Enable Audit object access' for Success and/or Failure.

or enable it via command line with these steps:
1. Open an elevated command prompt.
2. Run: auditpol /set /subcategory:""File System"" /success:enable /failure:enable


Press Q or Esc to quit.
";

        // Cache actor identity for files that are about to be deleted.
        // Changed/Created events fire before Delete, so we stash the actor
        // while the file still exists and the security log entry is fresh.
        private static readonly ConcurrentDictionary<string, string> s_actorCache = new(StringComparer.OrdinalIgnoreCase);

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

                s_actorCache.Clear();
                RunMonitor(watchPath, recursive, logFile);
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine(ex.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }

        private static void RunMonitor(string path, bool recursive, string? logFile)
        {
            bool secLog = CheckSecurityLogAccess();

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
                Console.WriteLine();
                FileSystem.ColorConsoleText(ConsoleColor.Cyan, "  Monitoring : ");
                Console.WriteLine(path);
                FileSystem.ColorConsoleText(ConsoleColor.DarkGray, $"  Recursive  : {recursive}   Press Q to quit\n");
                FileSystem.ColorConsoleText(ConsoleColor.Cyan, "  User source: ");
                if (secLog)
                    FileSystem.ColorConsoleText(ConsoleColor.Green, "Security Event Log\n");
                else
                    FileSystem.ColorConsoleText(ConsoleColor.DarkYellow,
                        "file owner (run elevated + enable Object Access auditing for actor tracking)\n");
                if (logFile != null)
                {
                    FileSystem.ColorConsoleText(ConsoleColor.Cyan, "  Log file   : ");
                    Console.WriteLine(logFile);

                    // Write session header to log file.
                    logWriter!.WriteLine(new string('─', 80));
                    logWriter.WriteLine($"  fsmon session started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    logWriter.WriteLine($"  Monitoring : {path}");
                    logWriter.WriteLine($"  Recursive  : {recursive}");
                    logWriter.WriteLine(new string('─', 80));
                }
                Console.WriteLine(new string('─', 80));

                using var preDeleteWatcher = new FileSystemWatcher(path)
                {
                    IncludeSubdirectories = recursive,
                    EnableRaisingEvents   = false,
                    NotifyFilter          = NotifyFilters.Attributes | NotifyFilters.Security | NotifyFilters.LastAccess,
                    InternalBufferSize    = 65536
                };

                preDeleteWatcher.Changed += (_, e) =>
                {
                    string actor = GetActor(e.FullPath);
                    if (actor != "—")
                        s_actorCache[e.FullPath] = actor;
                };

                using var watcher = new FileSystemWatcher(path)
                {
                    IncludeSubdirectories = recursive,
                    EnableRaisingEvents   = false,
                    NotifyFilter = NotifyFilters.FileName     | NotifyFilters.DirectoryName |
                                   NotifyFilters.LastWrite    | NotifyFilters.Size,
                    InternalBufferSize    = 65536
                };

                watcher.Created += (_, e) =>
                {
                    string actor = GetActor(e.FullPath);
                    Print("CREATED ", ConsoleColor.Green, e.FullPath, actor, logWriter);
                    if (actor != "—") s_actorCache[e.FullPath] = actor;
                };
                watcher.Changed += (_, e) =>
                {
                    string actor = GetActor(e.FullPath);
                    Print("MODIFIED", ConsoleColor.Yellow, e.FullPath, actor, logWriter);
                    if (actor != "—") s_actorCache[e.FullPath] = actor;
                };
                watcher.Deleted += (_, e) =>
                {
                    string actor;
                    if (s_actorCache.TryRemove(e.FullPath, out string? cached) && cached != "—")
                    {
                        actor = cached;
                    }
                    else
                    {
                        Thread.Sleep(500);
                        actor = TryQuerySecurityLog(e.FullPath) ?? "—";
                    }
                    Print("DELETED ", ConsoleColor.Red, e.FullPath, actor, logWriter);
                };
                watcher.Renamed += (_, e) =>
                {
                    if (s_actorCache.TryRemove(e.OldFullPath, out string? old))
                        s_actorCache[e.FullPath] = old;

                    Print("RENAMED ", ConsoleColor.Cyan,
                        $"{e.OldName}  →  {e.Name}", GetActor(e.FullPath), logWriter);
                };
                watcher.Error += (_, e) =>
                {
                    if (e.GetException() is InternalBufferOverflowException)
                        Print("OVERFLOW", ConsoleColor.Magenta,
                            "Event buffer overflowed — some events may be lost", "—", logWriter);
                };

                preDeleteWatcher.EnableRaisingEvents = true;
                watcher.EnableRaisingEvents = true;

                while (true)
                {
                    Thread.Sleep(100);
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true);
                        if (key.Key == ConsoleKey.Q || key.Key == ConsoleKey.Escape)
                            break;
                    }
                }

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

        // Cached once per monitor session — avoids repeated privilege checks.
        private static bool? s_secLogAvailable;

        private static bool CheckSecurityLogAccess()
        {
            if (s_secLogAvailable.HasValue) return s_secLogAvailable.Value;
            try
            {
                var q = new EventLogQuery("Security", PathType.LogName, "*[System[EventID=4663]]")
                { ReverseDirection = true };
                using var r = new EventLogReader(q);
                r.ReadEvent();
                s_secLogAvailable = true;
            }
            catch { s_secLogAvailable = false; }
            return s_secLogAvailable.Value;
        }

        // Queries Security Event Log for the most recent event touching this file.
        // EventID 4663 = Object Access, 4656 = Handle Request (fires on open-with-delete).
        private static string? TryQuerySecurityLog(string path)
        {
            if (!CheckSecurityLogAccess()) return null;
            try
            {
                string file = Path.GetFileName(path).Replace("'", "''");

                string xPath =
                    $"*[System[(EventID=4663 or EventID=4656) and " +
                    $"TimeCreated[timediff(@SystemTime) <= 15000]]" +
                    $" and EventData[Data[@Name='ObjectName'][contains(.,'{file}')]]]";

                var logQuery = new EventLogQuery("Security", PathType.LogName, xPath)
                { ReverseDirection = true };
                using var reader = new EventLogReader(logQuery);
                using var ev = reader.ReadEvent();
                if (ev == null) return null;

                string? user    = ev.Properties[1].Value?.ToString();
                string? domain  = ev.Properties[2].Value?.ToString();
                string? process = ev.Properties.Count > 11
                    ? ev.Properties[11].Value?.ToString() : null;

                if (string.IsNullOrEmpty(user) || user == "-") return null;

                string identity = (string.IsNullOrEmpty(domain) || domain == "-")
                    ? user : $"{domain}\\{user}";

                if (!string.IsNullOrEmpty(process) && process != "-")
                    identity += $" ({Path.GetFileName(process)})";

                return identity;
            }
            catch { return null; }
        }

        private static string GetOwner(string path)
        {
            try
            {
                if (File.Exists(path))
                    return new FileInfo(path).GetAccessControl()
                                            .GetOwner(typeof(NTAccount))?.ToString() ?? "—";
                if (Directory.Exists(path))
                    return new DirectoryInfo(path).GetAccessControl()
                                                  .GetOwner(typeof(NTAccount))?.ToString() ?? "—";
            }
            catch { }
            return "—";
        }

        // Preferred: Security log actor; fallback: ACL owner.
        private static string GetActor(string path)
            => TryQuerySecurityLog(path) ?? GetOwner(path);

        private static readonly object s_lock = new();

        private static void Print(string type, ConsoleColor color, string detail, string actor, StreamWriter? logWriter)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            lock (s_lock)
            {
                // Console output (coloured).
                FileSystem.ColorConsoleText(ConsoleColor.DarkGray, $"  {DateTime.Now:HH:mm:ss}  ");
                FileSystem.ColorConsoleText(color,                  $"{type}  ");
                FileSystem.ColorConsoleText(ConsoleColor.DarkCyan,  $"{actor,-32}");
                Console.WriteLine(detail);

                // Log file output (plain text).
                logWriter?.WriteLine($"  {timestamp}  {type}  {actor,-32}{detail}");
            }
        }
    }
}