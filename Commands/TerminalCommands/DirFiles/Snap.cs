using Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Commands.TerminalCommands.DirFiles
{
    [SupportedOSPlatform("Windows")]
    public class Snap : ITerminalCommand
    {
        /*
            snap — Directory state snapshot + diff.
            Take a lightweight snapshot of any directory (file list + MD5 hashes)
            and later diff it to see exactly what was added, deleted or modified.
            Like a zero-setup git-status for any folder — no repo required.
        */

        public string Name => "snap";

        private static readonly string s_snapFile =
            Path.Combine(GlobalVariables.terminalWorkDirectory, "snaps.json");

        private static readonly JsonSerializerOptions s_jsonOpts =
            new() { WriteIndented = true };

        private static readonly string s_helpMessage = @"Usage of snap command:
    snap save [name]   : Snapshot the current directory (default name: 'default').
    snap diff [name]   : Compare current state against a saved snapshot.
    snap list          : List all saved snapshots.
    snap del  <name>   : Delete a snapshot.
    snap -h            : Display this help message.

Examples:
    snap save
    snap save before-install
    snap diff before-install
    snap list
";

        public void Execute(string args)
        {
            GlobalVariables.isErrorCommand = false;
            try
            {
                if (args == $"{Name} -h") { Console.WriteLine(s_helpMessage); return; }
                if (args == Name)         { FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!"); return; }

                string[] parts = args.Split(' ', 3);
                string sub   = parts.Length > 1 ? parts[1] : string.Empty;
                string param = (parts.Length > 2 ? parts[2].Trim() : null) ?? "default";

                switch (sub)
                {
                    case "save": SaveSnap(param);   break;
                    case "diff": DiffSnap(param);   break;
                    case "list": ListSnaps();        break;
                    case "del":  DeleteSnap(param); break;
                    default:
                        FileSystem.ErrorWriteLine($"Unknown sub-command '{sub}'. Use 'snap -h' for help.");
                        GlobalVariables.isErrorCommand = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine(ex.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }

        // ── Data model ────────────────────────────────────────────────────────

        private sealed class SnapEntry
        {
            public string          Name      { get; set; }
            public string          Directory { get; set; }
            public List<SnapFile>  Files     { get; set; }
            public string          SavedAt   { get; set; }
        }

        private sealed class SnapFile
        {
            public string RelPath  { get; set; }
            public long   Size     { get; set; }
            public string Hash     { get; set; }
            public string Modified { get; set; }
        }

        private static Dictionary<string, SnapEntry> Load()
        {
            if (!File.Exists(s_snapFile)) return new();
            return JsonSerializer.Deserialize<Dictionary<string, SnapEntry>>(
                       File.ReadAllText(s_snapFile)) ?? new();
        }

        private static void SaveToDisk(Dictionary<string, SnapEntry> data) =>
            File.WriteAllText(s_snapFile, JsonSerializer.Serialize(data, s_jsonOpts));

        // ── Operations ────────────────────────────────────────────────────────

        private static void SaveSnap(string name)
        {
            string cwd = File.ReadAllText(GlobalVariables.currentDirectory);
            FileSystem.SuccessWriteLine($"Scanning '{cwd}'…");

            var files = BuildFileList(cwd);
            var data  = Load();
            data[name] = new SnapEntry
            {
                Name      = name,
                Directory = cwd,
                Files     = files,
                SavedAt   = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            SaveToDisk(data);
            FileSystem.SuccessWriteLine($"Snapshot '{name}' saved  ({files.Count} files).");
        }

        private static void DiffSnap(string name)
        {
            var data = Load();
            if (!data.TryGetValue(name, out var snap))
            {
                FileSystem.ErrorWriteLine($"Snapshot '{name}' not found. Run 'snap save {name}' first.");
                return;
            }

            string cwd = File.ReadAllText(GlobalVariables.currentDirectory);

            var current  = BuildFileList(cwd);
            var snapDict = snap.Files.ToDictionary(f => f.RelPath, StringComparer.OrdinalIgnoreCase);
            var currDict = current.ToDictionary(f => f.RelPath,    StringComparer.OrdinalIgnoreCase);

            var added    = currDict.Keys.Except(snapDict.Keys, StringComparer.OrdinalIgnoreCase)
                                   .OrderBy(x => x).ToList();
            var deleted  = snapDict.Keys.Except(currDict.Keys, StringComparer.OrdinalIgnoreCase)
                                   .OrderBy(x => x).ToList();
            var modified = currDict.Keys.Intersect(snapDict.Keys, StringComparer.OrdinalIgnoreCase)
                                   .Where(k => currDict[k].Hash != snapDict[k].Hash)
                                   .OrderBy(x => x).ToList();

            // ── Pipe output: plain labeled lines ─────────────────────────────
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
            {
                GlobalVariables.pipeCmdOutput = BuildDiffText(name, snap.SavedAt, cwd, added, deleted, modified);
                return;
            }

            // ── Console output with colors ────────────────────────────────────
            FileSystem.SuccessWriteLine($"Comparing '{cwd}' against snapshot '{name}' ({snap.SavedAt})…");
            Console.WriteLine();
            Console.WriteLine(new string('─', 57));
            FileSystem.ColorConsoleText(ConsoleColor.Cyan, "  Snapshot : ");
            Console.WriteLine($"'{name}'  ({snap.SavedAt})");
            FileSystem.ColorConsoleText(ConsoleColor.Cyan, "  Directory: ");
            Console.WriteLine(cwd);
            Console.WriteLine(new string('─', 57));

            if (added.Count == 0 && deleted.Count == 0 && modified.Count == 0)
            {
                Console.WriteLine();
                FileSystem.ColorConsoleText(ConsoleColor.Green, "  No changes detected.\n");
                return;
            }

            if (added.Count > 0)
            {
                Console.WriteLine();
                FileSystem.ColorConsoleText(ConsoleColor.Green, $"  Added ({added.Count}):\n");
                foreach (var f in added)
                    Console.WriteLine($"    + {f}");
            }

            if (deleted.Count > 0)
            {
                Console.WriteLine();
                FileSystem.ColorConsoleText(ConsoleColor.Red, $"  Deleted ({deleted.Count}):\n");
                foreach (var f in deleted)
                    Console.WriteLine($"    - {f}");
            }

            if (modified.Count > 0)
            {
                Console.WriteLine();
                FileSystem.ColorConsoleText(ConsoleColor.Yellow, $"  Modified ({modified.Count}):\n");
                foreach (var f in modified)
                    Console.WriteLine($"    ~ {f}");
            }
            Console.WriteLine();
        }

        private static string BuildDiffText(string name, string savedAt, string dir,
            List<string> added, List<string> deleted, List<string> modified)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"snapshot: {name}  ({savedAt})");
            sb.AppendLine($"directory: {dir}");
            sb.AppendLine(new string('-', 57));

            if (added.Count == 0 && deleted.Count == 0 && modified.Count == 0)
            {
                sb.AppendLine("no changes detected");
                return sb.ToString();
            }

            foreach (var f in added)    sb.AppendLine($"added: {f}");
            foreach (var f in deleted)  sb.AppendLine($"deleted: {f}");
            foreach (var f in modified) sb.AppendLine($"modified: {f}");
            return sb.ToString();
        }

        private static void ListSnaps()
        {
            var data = Load();

            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
            {
                if (data.Count == 0) { GlobalVariables.pipeCmdOutput = "no snapshots saved"; return; }
                var sb = new StringBuilder();
                foreach (var s in data.Values)
                    sb.AppendLine($"{s.Name}  {s.SavedAt}  {s.Files.Count} files  {s.Directory}");
                GlobalVariables.pipeCmdOutput = sb.ToString();
                return;
            }

            if (data.Count == 0) { Console.WriteLine("  No snapshots saved."); return; }
            Console.WriteLine();
            foreach (var s in data.Values)
            {
                FileSystem.ColorConsoleText(ConsoleColor.Cyan,    $"  {s.Name,-20}");
                FileSystem.ColorConsoleText(ConsoleColor.DarkGray, $"{s.SavedAt}  {s.Files.Count,5} files  ");
                Console.WriteLine(s.Directory);
            }
            Console.WriteLine();
        }

        private static void DeleteSnap(string name)
        {
            var data = Load();
            if (!data.Remove(name))
            {
                FileSystem.ErrorWriteLine($"Snapshot '{name}' not found.");
                return;
            }
            SaveToDisk(data);
            FileSystem.SuccessWriteLine($"Snapshot '{name}' deleted.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static List<SnapFile> BuildFileList(string rootDir)
        {
            var result = new List<SnapFile>();
            try
            {
                foreach (string file in Directory.EnumerateFiles(rootDir, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        result.Add(new SnapFile
                        {
                            RelPath  = Path.GetRelativePath(rootDir, file),
                            Size     = fi.Length,
                            Hash     = ComputeMd5(file),
                            Modified = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                        });
                    }
                    catch { /* skip inaccessible files */ }
                }
            }
            catch { /* skip inaccessible root */ }
            return result;
        }

        private static string ComputeMd5(string path)
        {
            using var md5    = MD5.Create();
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(md5.ComputeHash(stream)).ToLowerInvariant();
        }
    }
}
