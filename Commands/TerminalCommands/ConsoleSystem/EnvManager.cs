using Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class EnvManager : ITerminalCommand
    {
        /*
            env — Environment variable manager.
            List, get, set, delete variables across Process / User / System scopes.
            Use -t user|system|process to target a specific scope (default: process).
            Load from .env files or export to them.
        */

        public string Name => "env";

        private static readonly string s_helpMessage = @"Usage of env command:
    env                                     : List all variables across Process, User and System.
    env -t <user|system|process>            : List variables for the specified scope.
    env [-t <scope>] get <NAME>             : Get a variable's value.
    env [-t <scope>] set <NAME> <VALUE>     : Set (overwrite) a variable.
    env [-t <scope>] del <NAME>             : Delete a variable.
    env [-t <scope>] addval <NAME> <VALUE>  : Append an entry (semicolon-separated list).
    env [-t <scope>] delval <NAME> <VALUE>  : Remove an entry (semicolon-separated list).
    env [-t <scope>] export <file>          : Export variables to a .env file.
    env [-t <scope>] load <file>            : Load variables from a .env file.
    env -h                                  : Display this help message.

Note: 'system' scope requires elevated (Administrator) privileges to write.

Examples:
    env
    env -t user
    env -t system get PATH
    env -t user set MY_VAR hello
    env -t user addval PATH C:\MyTool\bin
    env -t user delval PATH C:\OldTool\bin
    env -t system del LEGACY_VAR
    env load .env
    env -t user export snapshot.env
";

        public void Execute(string args)
        {
            GlobalVariables.isErrorCommand = false;
            try
            {
                if (args == $"{Name} -h") { Console.WriteLine(s_helpMessage); return; }

                // Extract optional -t <scope> flag, leaving the rest of args clean.
                string mutableArgs = args;
                EnvironmentVariableTarget? explicitTarget = ParseTarget(ref mutableArgs);
                EnvironmentVariableTarget  target         = explicitTarget ?? EnvironmentVariableTarget.Process;

                // Bare "env" or "env -t <scope>" — just list.
                if (mutableArgs == Name)
                {
                    if (explicitTarget.HasValue) ListVars(target);
                    else                         ListAllTargets();
                    return;
                }

                string[] parts = mutableArgs.Split(' ', 3);
                string sub   = parts.Length > 1 ? parts[1] : string.Empty;
                string param = parts.Length > 2 ? parts[2] : string.Empty;

                switch (sub)
                {
                    case "get":    GetVar(param.Trim(), target);    break;
                    case "set":    SetVar(param, target);            break;
                    case "del":    DelVar(param.Trim(), target);     break;
                    case "addval": AddVal(param, target);            break;
                    case "delval": DelVal(param, target);            break;
                    case "load":   LoadEnv(param.Trim(), target);    break;
                    case "export": ExportEnv(param.Trim(), target);  break;
                    default:
                        FileSystem.ErrorWriteLine($"Unknown sub-command '{sub}'. Use 'env -h' for help.");
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

        // ── Listing ──────────────────────────────────────────────────────────

        private static void ListAllTargets()
        {
            Console.WriteLine();
            foreach (var (label, tgt) in new[]
            {
                ("Process", EnvironmentVariableTarget.Process),
                ("User",    EnvironmentVariableTarget.User),
                ("System",  EnvironmentVariableTarget.Machine)
            })
            {
                FileSystem.ColorConsoleText(ConsoleColor.DarkGray, $"  [{label}]\n");
                PrintVars(tgt);
            }
        }

        private static void ListVars(EnvironmentVariableTarget target)
        {
            Console.WriteLine();
            FileSystem.ColorConsoleText(ConsoleColor.DarkGray, $"  [{TargetLabel(target)}]\n");
            PrintVars(target);
        }

        private static void PrintVars(EnvironmentVariableTarget target)
        {
            var entries = new List<(string Key, string Val)>();
            foreach (DictionaryEntry e in Environment.GetEnvironmentVariables(target))
                entries.Add((e.Key.ToString(), e.Value?.ToString() ?? string.Empty));
            entries.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase));
            foreach (var (key, val) in entries)
            {
                FileSystem.ColorConsoleText(ConsoleColor.Cyan, $"  {key}");
                Console.WriteLine($"={val}");
            }
            Console.WriteLine();
        }

        // ── CRUD ─────────────────────────────────────────────────────────────

        private static void GetVar(string name, EnvironmentVariableTarget target)
        {
            if (string.IsNullOrWhiteSpace(name)) { FileSystem.ErrorWriteLine("Usage: env get <NAME>"); return; }
            string val = Environment.GetEnvironmentVariable(name, target);
            if (val == null)
                FileSystem.ErrorWriteLine($"[{TargetLabel(target)}] Variable '{name}' not found.");
            else
            {
                FileSystem.ColorConsoleText(ConsoleColor.DarkGray, $"  [{TargetLabel(target)}] ");
                FileSystem.ColorConsoleText(ConsoleColor.Cyan, name);
                Console.WriteLine($"={val}");
            }
        }

        private static void SetVar(string param, EnvironmentVariableTarget target)
        {
            string[] kv = param.Split(' ', 2);
            if (kv.Length < 2 || string.IsNullOrWhiteSpace(kv[0]))
            {
                FileSystem.ErrorWriteLine("Usage: env set <NAME> <VALUE>");
                return;
            }
            Environment.SetEnvironmentVariable(kv[0], kv[1], target);
            FileSystem.SuccessWriteLine($"[{TargetLabel(target)}] Set {kv[0]}={kv[1]}");
        }

        private static void DelVar(string name, EnvironmentVariableTarget target)
        {
            if (string.IsNullOrWhiteSpace(name)) { FileSystem.ErrorWriteLine("Usage: env del <NAME>"); return; }
            if (Environment.GetEnvironmentVariable(name, target) == null)
            {
                FileSystem.ErrorWriteLine($"[{TargetLabel(target)}] Variable '{name}' not found.");
                return;
            }
            Environment.SetEnvironmentVariable(name, null, target);
            FileSystem.SuccessWriteLine($"[{TargetLabel(target)}] Deleted '{name}'.");
        }

        private static void AddVal(string param, EnvironmentVariableTarget target)
        {
            string[] kv = param.Split(' ', 2);
            if (kv.Length < 2 || string.IsNullOrWhiteSpace(kv[0]) || string.IsNullOrWhiteSpace(kv[1]))
            {
                FileSystem.ErrorWriteLine("Usage: env addval <NAME> <VALUE>");
                return;
            }
            string name    = kv[0].Trim();
            string entry   = kv[1].Trim();
            string current = Environment.GetEnvironmentVariable(name, target) ?? string.Empty;

            var parts = current.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (parts.Any(p => p.Equals(entry, StringComparison.OrdinalIgnoreCase)))
            {
                FileSystem.ColorConsoleText(ConsoleColor.DarkYellow,
                    $"[{TargetLabel(target)}] '{entry}' is already present in {name}.\n");
                return;
            }
            parts.Add(entry);
            Environment.SetEnvironmentVariable(name, string.Join(';', parts), target);
            FileSystem.SuccessWriteLine($"[{TargetLabel(target)}] Added '{entry}' to {name}.");
        }

        private static void DelVal(string param, EnvironmentVariableTarget target)
        {
            string[] kv = param.Split(' ', 2);
            if (kv.Length < 2 || string.IsNullOrWhiteSpace(kv[0]) || string.IsNullOrWhiteSpace(kv[1]))
            {
                FileSystem.ErrorWriteLine("Usage: env delval <NAME> <VALUE>");
                return;
            }
            string name    = kv[0].Trim();
            string entry   = kv[1].Trim();
            string current = Environment.GetEnvironmentVariable(name, target);
            if (current == null)
            {
                FileSystem.ErrorWriteLine($"[{TargetLabel(target)}] Variable '{name}' not found.");
                return;
            }

            var parts   = current.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            int removed = parts.RemoveAll(p => p.Equals(entry, StringComparison.OrdinalIgnoreCase));
            if (removed == 0)
            {
                FileSystem.ErrorWriteLine($"[{TargetLabel(target)}] '{entry}' not found in {name}.");
                return;
            }
            string updated = string.Join(';', parts);
            Environment.SetEnvironmentVariable(name, updated.Length > 0 ? updated : null, target);
            FileSystem.SuccessWriteLine($"[{TargetLabel(target)}] Removed '{entry}' from {name}.");
        }

        private static void LoadEnv(string filePath, EnvironmentVariableTarget target)
        {
            string currentDir = File.ReadAllText(GlobalVariables.currentDirectory);
            filePath = FileSystem.SanitizePath(filePath, currentDir);
            if (!File.Exists(filePath)) { FileSystem.ErrorWriteLine($"File not found: '{filePath}'"); return; }

            int count = 0;
            foreach (string line in File.ReadAllLines(filePath))
            {
                string t = line.Trim();
                if (string.IsNullOrEmpty(t) || t.StartsWith('#') || t.StartsWith("//")) continue;
                int eq = t.IndexOf('=');
                if (eq <= 0) continue;
                string key   = t[..eq].Trim();
                string value = t[(eq + 1)..].Trim().Trim('"').Trim('\'');
                Environment.SetEnvironmentVariable(key, value, target);
                count++;
            }
            FileSystem.SuccessWriteLine($"[{TargetLabel(target)}] Loaded {count} variable(s) from '{Path.GetFileName(filePath)}'.");
        }

        private static void ExportEnv(string filePath, EnvironmentVariableTarget target)
        {
            string currentDir = File.ReadAllText(GlobalVariables.currentDirectory);
            filePath = FileSystem.SanitizePath(filePath, currentDir);
            var lines = new List<string>();
            foreach (DictionaryEntry e in Environment.GetEnvironmentVariables(target))
                lines.Add($"{e.Key}={e.Value}");
            lines.Sort(StringComparer.OrdinalIgnoreCase);
            File.WriteAllLines(filePath, lines);
            FileSystem.SuccessWriteLine($"[{TargetLabel(target)}] Exported {lines.Count} variable(s) to '{Path.GetFileName(filePath)}'.");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static string TargetLabel(EnvironmentVariableTarget t) => t switch
        {
            EnvironmentVariableTarget.User    => "User",
            EnvironmentVariableTarget.Machine => "System",
            _                                  => "Process"
        };

        // Finds "-t <scope>" anywhere in args, removes it, and returns the resolved target.
        // Returns null if the flag was not present.
        private static EnvironmentVariableTarget? ParseTarget(ref string args)
        {
            int i = args.IndexOf(" -t ", StringComparison.OrdinalIgnoreCase);
            if (i < 0) return null;

            string after = args.Substring(i + 4).Trim();
            string word  = after.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                .FirstOrDefault()?.ToLowerInvariant() ?? string.Empty;

            EnvironmentVariableTarget target = word switch
            {
                "user"    => EnvironmentVariableTarget.User,
                "system"  => EnvironmentVariableTarget.Machine,
                "machine" => EnvironmentVariableTarget.Machine,
                _         => EnvironmentVariableTarget.Process
            };

            string remaining = after.Length > word.Length ? after.Substring(word.Length) : string.Empty;
            args = $"{args.Substring(0, i)}{remaining}".Trim();
            while (args.Contains("  ")) args = args.Replace("  ", " ");

            return target;
        }
    }
}
