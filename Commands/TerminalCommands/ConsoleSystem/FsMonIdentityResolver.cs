using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Xml.Linq;

namespace Commands.TerminalCommands.ConsoleSystem
{
    // All state belongs to one monitoring session. File owners are fallback
    // information, never evidence that an account performed an operation.
    [SupportedOSPlatform("Windows")]
    internal sealed class FsMonIdentityResolver : IDisposable
    {
        private const int CacheLimit = 4096;
        private const uint WriteAccess = 0x2 | 0x4 | 0x10 | 0x100;
        private const uint DeleteAccess = 0x10000;
        private readonly string _root;
        private readonly bool _recursive;
        private readonly Func<string, string?> _getOwner;
        private readonly object _sync = new();
        private readonly Dictionary<string, (string Owner, DateTime Captured)> _owners = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<AuditEntry> _audits = new();
        private EventLogWatcher? _watcher;
        private volatile bool _securityLogAvailable;

        private sealed record AuditEntry(string Path, DateTime Time, uint Access, string Actor);

        internal FsMonIdentityResolver(string root, bool recursive, Func<string, string?>? getOwner = null)
        {
            _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            _recursive = recursive;
            _getOwner = getOwner ?? GetOwner;
        }

        internal bool SecurityLogAvailable => _securityLogAvailable;

        internal void Start()
        {
            try
            {
                // Event Log supports only a subset of XPath; contains() is not
                // supported. Compare full paths in managed code instead.
                var query = new EventLogQuery("Security", PathType.LogName,
                    "*[System[EventID=4663] and EventData[Data[@Name='ObjectType']='File']]");
                _watcher = new EventLogWatcher(query);
                _watcher.EventRecordWritten += OnAudit;
                _securityLogAvailable = true;
                _watcher.Enabled = true;
            }
            catch
            {
                _securityLogAvailable = false;
                _watcher?.Dispose();
                _watcher = null;
            }
        }

        private void OnAudit(object? sender, EventRecordWrittenEventArgs e)
        {
            using var record = e.EventRecord;
            if (e.EventException != null)
            {
                _securityLogAvailable = false;
                return;
            }
            try
            {
                if (record != null) RecordAudit(record.ToXml());
            }
            catch { /* An unreadable audit record must not stop monitoring. */ }
        }

        internal void RecordAudit(string xml)
        {
            try
            {
                var document = XDocument.Parse(xml);
                XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events/event";
                var system = document.Root?.Element(ns + "System");
                if ((string?)system?.Element(ns + "EventID") != "4663") return;
                if (!DateTime.TryParse((string?)system?.Element(ns + "TimeCreated")?.Attribute("SystemTime"),
                    CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var time)) return;

                var data = document.Root?.Element(ns + "EventData")?.Elements(ns + "Data")
                    .ToDictionary(e => (string)e.Attribute("Name")!, e => e.Value);
                if (data == null || !data.TryGetValue("ObjectType", out var objectType) || objectType != "File" ||
                    !data.TryGetValue("ObjectName", out var path) || !IsInScope(path) ||
                    !data.TryGetValue("AccessMask", out var mask)) return;

                bool hex = mask.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
                if (!uint.TryParse(hex ? mask.Substring(2) : mask,
                    hex ? NumberStyles.HexNumber : NumberStyles.Integer, CultureInfo.InvariantCulture, out uint access) ||
                    (access & (WriteAccess | DeleteAccess)) == 0) return;

                data.TryGetValue("SubjectUserName", out var user);
                data.TryGetValue("SubjectDomainName", out var domain);
                if (string.IsNullOrWhiteSpace(user) || user == "-")
                {
                    if (!data.TryGetValue("SubjectUserSid", out user) || string.IsNullOrWhiteSpace(user) || user == "-") return;
                    domain = null;
                }
                string actor = string.IsNullOrWhiteSpace(domain) || domain == "-" ? user : $"{domain}\\{user}";
                if (data.TryGetValue("ProcessName", out var process) && !string.IsNullOrWhiteSpace(process) && process != "-")
                    actor += $" ({Path.GetFileName(process)})";

                lock (_sync)
                {
                    _audits.RemoveAll(a => a.Time < DateTime.UtcNow.AddSeconds(-15));
                    if (_audits.Count >= CacheLimit) _audits.RemoveAt(0);
                    _audits.Add(new AuditEntry(path, time, access, actor));
                }
            }
            catch { /* Ignore malformed or incomplete event data. */ }
        }

        private bool IsInScope(string path)
        {
            if (!Path.IsPathFullyQualified(path)) return false;
            if (string.Equals(path, _root, StringComparison.OrdinalIgnoreCase)) return true;
            string prefix = Path.EndsInDirectorySeparator(_root) ? _root : _root + Path.DirectorySeparatorChar;
            return _recursive
                ? path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                : string.Equals(Path.GetDirectoryName(path), _root, StringComparison.OrdinalIgnoreCase);
        }

        internal string CaptureFallback(string path, WatcherChangeTypes change, string? oldPath = null)
        {
            lock (_sync)
            {
                // A new file at a reused path must not inherit a previous file's owner.
                if (change == WatcherChangeTypes.Created) _owners.Remove(path);
                string? cached = CachedOwner(path);
                if (oldPath != null)
                {
                    cached = CachedOwner(oldPath);
                    _owners.Remove(oldPath);
                    _owners.Remove(path);
                }

                // A deleted path may already refer to a different, newly created file.
                string? owner = change == WatcherChangeTypes.Deleted ? null : _getOwner(path);
                if (change == WatcherChangeTypes.Deleted) _owners.Remove(path);
                else if (owner != null || cached != null) RememberOwner(path, owner ?? cached!);

                if (owner != null) return $"{owner} [owner]";
                if (cached != null) return $"{cached} [cached owner]";

                // Short-lived files can disappear before even the Created callback.
                // The nearest readable parent's owner provides useful context, but
                // is explicitly labelled because it does not identify the actor.
                for (string? parent = Path.GetDirectoryName(path); parent != null; parent = Path.GetDirectoryName(parent))
                {
                    owner = _getOwner(parent);
                    if (owner != null)
                    {
                        RememberOwner(parent, owner);
                        return $"{owner} [parent owner]";
                    }
                    cached = CachedOwner(parent);
                    if (cached != null) return $"{cached} [cached parent owner]";
                }
                return "unknown";
            }
        }

        private string? CachedOwner(string path)
        {
            if (_owners.TryGetValue(path, out var entry))
            {
                if (DateTime.UtcNow - entry.Captured < TimeSpan.FromMinutes(2)) return entry.Owner;
                _owners.Remove(path);
            }
            return null;
        }

        private void RememberOwner(string path, string owner)
        {
            if (_owners.Count >= CacheLimit && !_owners.ContainsKey(path)) _owners.Clear();
            _owners[path] = (owner, DateTime.UtcNow);
        }

        internal string Resolve(string path, WatcherChangeTypes change, DateTime observedUtc, string fallback, string? oldPath = null)
        {
            lock (_sync)
            {
                AuditEntry? closest = null;
                double closestDistance = double.MaxValue;
                foreach (var entry in _audits)
                {
                    bool currentPath = string.Equals(entry.Path, path, StringComparison.OrdinalIgnoreCase);
                    bool previousPath = change == WatcherChangeTypes.Renamed &&
                        string.Equals(entry.Path, oldPath, StringComparison.OrdinalIgnoreCase);
                    uint requiredAccess = change == WatcherChangeTypes.Deleted || previousPath ? DeleteAccess : WriteAccess;
                    if ((!currentPath && !previousPath) || (entry.Access & requiredAccess) == 0) continue;

                    // Match this notification, not an unrelated old operation on
                    // the same file or a file with the same basename elsewhere.
                    double delta = (entry.Time - observedUtc).TotalSeconds;
                    if (delta < -2 || delta > 1 || Math.Abs(delta) >= closestDistance) continue;
                    closest = entry;
                    closestDistance = Math.Abs(delta);
                }
                return closest?.Actor ?? fallback;
            }
        }

        private static string? GetOwner(string path)
        {
            try
            {
                var security = Directory.Exists(path)
                    ? (System.Security.AccessControl.FileSystemSecurity)new DirectoryInfo(path).GetAccessControl()
                    : new FileInfo(path).GetAccessControl();
                var sid = security.GetOwner(typeof(SecurityIdentifier));
                try { return sid?.Translate(typeof(NTAccount)).Value; }
                catch (IdentityNotMappedException) { return sid?.Value; }
            }
            catch { return null; }
        }

        public void Dispose()
        {
            _securityLogAvailable = false;
            _watcher?.Dispose();
        }
    }
}
