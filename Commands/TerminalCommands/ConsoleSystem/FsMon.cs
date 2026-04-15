using Core;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Principal;
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
    fsmon [path]     : Monitor the current (or given) directory for changes.
    fsmon -r [path]  : Monitor recursively including all sub-directories.
    fsmon -h         : Display this help message.

Examples:
    fsmon
    fsmon -r C:\Projects\myapp
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
                bool recursive = false;
                string rest = args == Name ? string.Empty : args.SplitByText($"{Name} ", 1).Trim();

                if (rest.StartsWith("-r"))
                {
                    recursive = true;
                    rest = rest.SplitByText("-r", 1).Trim();
                }

                string watchPath = string.IsNullOrWhiteSpace(rest)
                    ? currentDir
                    : FileSystem.SanitizePath(rest, currentDir);

                if (!Directory.Exists(watchPath))
                {
                    FileSystem.ErrorWriteLine($"Directory not found: '{watchPath}'");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }

                s_actorCache.Clear();
                RunMonitor(watchPath, recursive);
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine(ex.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }

        private static void RunMonitor(string path, bool recursive)
        {
            bool secLog = CheckSecurityLogAccess();

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
            Console.WriteLine(new string('─', 80));

            // Secondary watcher dedicated to catching file access *before* deletion.
            // NotifyFilters.Security | LastAccess fire when a process opens a handle
            // with DELETE intent — the file still exists at that point so we can
            // resolve the actor and cache it for the upcoming Deleted event.
            using var preDeleteWatcher = new FileSystemWatcher(path)
            {
                IncludeSubdirectories = recursive,
                EnableRaisingEvents = false,
                NotifyFilter = NotifyFilters.Attributes | NotifyFilters.Security | NotifyFilters.LastAccess,
                InternalBufferSize = 65536
            };

            preDeleteWatcher.Changed += (_, e) =>
            {
                // File still exists here — cache the actor for a possible upcoming delete.
                string actor = GetActor(e.FullPath);
                if (actor != "—")
                    s_actorCache[e.FullPath] = actor;
            };

            using var watcher = new FileSystemWatcher(path)
            {
                IncludeSubdirectories = recursive,
                EnableRaisingEvents = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName |
                               NotifyFilters.LastWrite | NotifyFilters.Size,
                InternalBufferSize = 65536
            };

            watcher.Created += (_, e) =>
            {
                string actor = GetActor(e.FullPath);
                Print("CREATED ", ConsoleColor.Green, e.FullPath, actor);
                // Cache in case the file is deleted immediately after creation.
                if (actor != "—") s_actorCache[e.FullPath] = actor;
            };
            watcher.Changed += (_, e) =>
            {
                string actor = GetActor(e.FullPath);
                Print("MODIFIED", ConsoleColor.Yellow, e.FullPath, actor);
                // Update cache — the most recent modifier is the likely deleter.
                if (actor != "—") s_actorCache[e.FullPath] = actor;
            };
            watcher.Deleted += (_, e) =>
            {
                // 1) Try the pre-cached actor (captured while file still existed).
                // 2) Fall back to querying the Security Event Log with a delay.
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
                Print("DELETED ", ConsoleColor.Red, e.FullPath, actor);
            };
            watcher.Renamed += (_, e) =>
            {
                // Migrate cache entry to the new name.
                if (s_actorCache.TryRemove(e.OldFullPath, out string? old))
                    s_actorCache[e.FullPath] = old;

                Print("RENAMED ", ConsoleColor.Cyan,
                    $"{e.OldName}  →  {e.Name}", GetActor(e.FullPath));
            };
            watcher.Error += (_, e) =>
            {
                if (e.GetException() is InternalBufferOverflowException)
                    Print("OVERFLOW", ConsoleColor.Magenta, "Event buffer overflowed — some events may be lost", "—");
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
                r.ReadEvent(); // throws UnauthorizedAccessException if not allowed
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

                // Property order: SubjectUserSid[0], SubjectUserName[1],
                //   SubjectDomainName[2], SubjectLogonId[3], ..., ProcessName[11]
                string? user = ev.Properties[1].Value?.ToString();
                string? domain = ev.Properties[2].Value?.ToString();
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

        private static void Print(string type, ConsoleColor color, string detail, string actor)
        {
            lock (s_lock)
            {
                FileSystem.ColorConsoleText(ConsoleColor.DarkGray, $"  {DateTime.Now:HH:mm:ss}  ");
                FileSystem.ColorConsoleText(color, $"{type}  ");
                FileSystem.ColorConsoleText(ConsoleColor.DarkCyan, $"{actor,-32}");
                Console.WriteLine(detail);
            }
        }
    }
}