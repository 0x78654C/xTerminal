using Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.Json;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class Ctx : ITerminalCommand
    {
        /*
            ctx — Terminal context manager.
            Save and restore named "contexts" (current working directory + process
            environment variables). Think kubectl config use-context but for your shell.
        */

        public string Name => "ctx";

        private static readonly string s_ctxFile =
            Path.Combine(GlobalVariables.terminalWorkDirectory, "contexts.json");

        private static readonly JsonSerializerOptions s_jsonOpts =
            new() { WriteIndented = true };

        private static readonly string s_helpMessage = @"Usage of ctx command:
    ctx save <name>   : Save current directory and env vars as a named context.
    ctx load <name>   : Restore a saved context (cwd + env vars).
    ctx list          : List all saved contexts.
    ctx show <name>   : Show details of a saved context.
    ctx del  <name>   : Delete a saved context.
    ctx -h            : Display this help message.

Examples:
    ctx save work
    ctx save homelab
    ctx list
    ctx load work
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
                string param = parts.Length > 2 ? parts[2].Trim() : string.Empty;

                switch (sub)
                {
                    case "save": SaveCtx(param);   break;
                    case "load": LoadCtx(param);   break;
                    case "list": ListCtx();        break;
                    case "show": ShowCtx(param);   break;
                    case "del":  DeleteCtx(param); break;
                    default:
                        FileSystem.ErrorWriteLine($"Unknown sub-command '{sub}'. Use 'ctx -h' for help.");
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

        private sealed class CtxEntry
        {
            public string                     Name      { get; set; }
            public string                     Directory { get; set; }
            public Dictionary<string, string> EnvVars   { get; set; }
            public string                     SavedAt   { get; set; }
        }

        private static Dictionary<string, CtxEntry> Load()
        {
            if (!File.Exists(s_ctxFile)) return new();
            return JsonSerializer.Deserialize<Dictionary<string, CtxEntry>>(
                       File.ReadAllText(s_ctxFile)) ?? new();
        }

        private static void SaveToDisk(Dictionary<string, CtxEntry> data) =>
            File.WriteAllText(s_ctxFile, JsonSerializer.Serialize(data, s_jsonOpts));

        // ── Operations ────────────────────────────────────────────────────────

        private static void SaveCtx(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { FileSystem.ErrorWriteLine("Usage: ctx save <name>"); return; }

            string cwd     = File.ReadAllText(GlobalVariables.currentDirectory);
            var    envVars = new Dictionary<string, string>();
            foreach (DictionaryEntry e in Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process))
                envVars[e.Key.ToString()] = e.Value?.ToString() ?? string.Empty;

            var data = Load();
            data[name] = new CtxEntry
            {
                Name      = name,
                Directory = cwd,
                EnvVars   = envVars,
                SavedAt   = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            SaveToDisk(data);
            FileSystem.SuccessWriteLine($"Context '{name}' saved  ({envVars.Count} vars, dir: {cwd})");
        }

        private static void LoadCtx(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { FileSystem.ErrorWriteLine("Usage: ctx load <name>"); return; }
            var data = Load();
            if (!data.TryGetValue(name, out var ctx))
            {
                FileSystem.ErrorWriteLine($"Context '{name}' not found.");
                return;
            }

            File.WriteAllText(GlobalVariables.currentDirectory, ctx.Directory);
            foreach (var kv in ctx.EnvVars)
                Environment.SetEnvironmentVariable(kv.Key, kv.Value, EnvironmentVariableTarget.Process);

            FileSystem.SuccessWriteLine(
                $"Context '{name}' loaded  (dir: {ctx.Directory}, {ctx.EnvVars.Count} vars restored)");
        }

        private static void ListCtx()
        {
            var data = Load();
            if (data.Count == 0) { Console.WriteLine("  No contexts saved."); return; }
            Console.WriteLine();
            foreach (var kv in data.Values)
            {
                FileSystem.ColorConsoleText(ConsoleColor.Cyan,    $"  {kv.Name,-20}");
                FileSystem.ColorConsoleText(ConsoleColor.DarkGray, $"{kv.SavedAt}  ");
                Console.WriteLine(kv.Directory);
            }
            Console.WriteLine();
        }

        private static void ShowCtx(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { FileSystem.ErrorWriteLine("Usage: ctx show <name>"); return; }
            var data = Load();
            if (!data.TryGetValue(name, out var ctx))
            {
                FileSystem.ErrorWriteLine($"Context '{name}' not found.");
                return;
            }

            var vars = ctx.EnvVars.ToList();

            // Header occupies: blank + separator + 4 fields + blank = 7 lines; footer = 2 lines.
            const int headerLines = 7;
            const int footerLines = 2;

            int viewHeight = Math.Max(1, Console.WindowHeight - headerLines - footerLines);

            if (vars.Count <= viewHeight)
            {
                // Everything fits — plain print, no interaction needed.
                PrintShowHeader(ctx);
                foreach (var kv in vars)
                {
                    FileSystem.ColorConsoleText(ConsoleColor.DarkGray, $"    {kv.Key}");
                    Console.WriteLine($"={kv.Value}");
                }
                Console.WriteLine(new string('─', 57));
                return;
            }

            // Interactive scrollable pager.
            int offset    = 0;
            int maxOffset = vars.Count - viewHeight;

            Console.CursorVisible = false;
            try
            {
                while (true)
                {
                    // Recalculate on each frame to handle terminal resize.
                    viewHeight = Math.Max(1, Console.WindowHeight - headerLines - footerLines);
                    maxOffset  = Math.Max(0, vars.Count - viewHeight);
                    if (offset > maxOffset) offset = maxOffset;

                    Console.Clear();
                    PrintShowHeader(ctx);

                    int end = Math.Min(offset + viewHeight, vars.Count);
                    for (int i = offset; i < end; i++)
                    {
                        FileSystem.ColorConsoleText(ConsoleColor.DarkGray, $"    {vars[i].Key}");
                        Console.WriteLine($"={vars[i].Value}");
                    }

                    Console.WriteLine(new string('─', 57));

                    int pct = (int)(Math.Min(end, vars.Count) * 100.0 / vars.Count);
                    FileSystem.ColorConsoleText(ConsoleColor.DarkGray,
                        $"  [{offset + 1}–{end} / {vars.Count}  {pct}%]" +
                        $"  ↑↓ scroll  PgUp/PgDn page  Home/End  Q/Esc quit");

                    var key = Console.ReadKey(intercept: true);
                    switch (key.Key)
                    {
                        case ConsoleKey.UpArrow:   offset = Math.Max(0, offset - 1);          break;
                        case ConsoleKey.DownArrow: offset = Math.Min(maxOffset, offset + 1);  break;
                        case ConsoleKey.PageUp:    offset = Math.Max(0, offset - viewHeight); break;
                        case ConsoleKey.PageDown:  offset = Math.Min(maxOffset, offset + viewHeight); break;
                        case ConsoleKey.Home:      offset = 0;         break;
                        case ConsoleKey.End:       offset = maxOffset; break;
                        case ConsoleKey.Q:
                        case ConsoleKey.Escape:    return;
                    }
                }
            }
            finally
            {
                Console.CursorVisible = true;
            }
        }

        private static void PrintShowHeader(CtxEntry ctx)
        {
            Console.WriteLine();
            Console.WriteLine(new string('─', 57));
            FileSystem.ColorConsoleText(ConsoleColor.Cyan, "  Name    : "); Console.WriteLine(ctx.Name);
            FileSystem.ColorConsoleText(ConsoleColor.Cyan, "  Dir     : "); Console.WriteLine(ctx.Directory);
            FileSystem.ColorConsoleText(ConsoleColor.Cyan, "  Saved   : "); Console.WriteLine(ctx.SavedAt);
            FileSystem.ColorConsoleText(ConsoleColor.Cyan, "  Env vars: "); Console.WriteLine(ctx.EnvVars.Count);
            Console.WriteLine();
        }

        private static void DeleteCtx(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { FileSystem.ErrorWriteLine("Usage: ctx del <name>"); return; }
            var data = Load();
            if (!data.Remove(name))
            {
                FileSystem.ErrorWriteLine($"Context '{name}' not found.");
                return;
            }
            SaveToDisk(data);
            FileSystem.SuccessWriteLine($"Context '{name}' deleted.");
        }
    }
}
