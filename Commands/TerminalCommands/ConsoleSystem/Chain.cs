using Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class Chain : ITerminalCommand
    {
        /*
            chain — Named reusable command chains (macro sequences).
            Define a sequence of xTerminal commands under a name and run them
            all in order with a single command. Each step is shown as it runs,
            and execution stops on the first failure.
        */

        public string Name => "chain";

        private static readonly string s_chainFile =
            Path.Combine(GlobalVariables.terminalWorkDirectory, "chains.json");

        private static readonly JsonSerializerOptions s_jsonOpts =
            new() { WriteIndented = true };

        private static readonly string s_helpMessage = @"Usage of chain command:
    chain create <name> ""cmd1"" ""cmd2"" ...  : Create a named command chain.
    chain run    <name>                        : Execute the chain sequentially.
    chain add    <name> ""cmd""                : Append a command to an existing chain.
    chain show   <name>                        : Show the steps of a chain.
    chain list                                 : List all saved chains.
    chain del    <name>                        : Delete a chain.
    chain -h                                   : Display this help message.

Examples:
    chain create deploy ""ls"" ""time"" ""sinfo""
    chain run deploy
    chain add deploy ""pcinfo""
    chain show deploy
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
                    case "create": CreateChain(param); break;
                    case "run":    RunChain(param);    break;
                    case "add":    AddToChain(param);  break;
                    case "show":   ShowChain(param);   break;
                    case "list":   ListChains();       break;
                    case "del":    DeleteChain(param); break;
                    default:
                        FileSystem.ErrorWriteLine($"Unknown sub-command '{sub}'. Use 'chain -h' for help.");
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

        private sealed class ChainEntry
        {
            public string       Name      { get; set; }
            public List<string> Commands  { get; set; }
            public string       CreatedAt { get; set; }
        }

        private static Dictionary<string, ChainEntry> Load()
        {
            if (!File.Exists(s_chainFile)) return new();
            return JsonSerializer.Deserialize<Dictionary<string, ChainEntry>>(
                       File.ReadAllText(s_chainFile)) ?? new();
        }

        private static void SaveToDisk(Dictionary<string, ChainEntry> data) =>
            File.WriteAllText(s_chainFile, JsonSerializer.Serialize(data, s_jsonOpts));

        // ── Operations ────────────────────────────────────────────────────────

        private static void CreateChain(string param)
        {
            string[] nameParts = param.Split(' ', 2);
            if (nameParts.Length < 2)
            {
                FileSystem.ErrorWriteLine("Usage: chain create <name> \"cmd1\" \"cmd2\" ...");
                return;
            }
            string name = nameParts[0];
            var    cmds = ParseQuotedCommands(nameParts[1]);
            if (cmds.Count == 0) { FileSystem.ErrorWriteLine("No commands provided."); return; }

            var data = Load();
            data[name] = new ChainEntry
            {
                Name      = name,
                Commands  = cmds,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            SaveToDisk(data);
            FileSystem.SuccessWriteLine($"Chain '{name}' created with {cmds.Count} command(s).");
        }

        private static void RunChain(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { FileSystem.ErrorWriteLine("Usage: chain run <name>"); return; }
            var data = Load();
            if (!data.TryGetValue(name, out var chain))
            {
                FileSystem.ErrorWriteLine($"Chain '{name}' not found.");
                return;
            }

            // ── Pipe output: capture each step's stdout ───────────────────────
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
            {
                GlobalVariables.pipeCmdOutput = RunChainCapture(name, chain);
                return;
            }

            // ── Console output with colors ────────────────────────────────────
            Console.WriteLine();
            FileSystem.ColorConsoleText(ConsoleColor.Cyan, $"  Running chain '{name}'  ({chain.Commands.Count} steps)\n");
            Console.WriteLine(new string('─', 57));

            for (int i = 0; i < chain.Commands.Count; i++)
            {
                string cmdLine = chain.Commands[i];
                Console.WriteLine();
                FileSystem.ColorConsoleText(ConsoleColor.DarkGray, $"  [{i + 1}/{chain.Commands.Count}] ");
                FileSystem.ColorConsoleText(ConsoleColor.Yellow, cmdLine + "\n");
                Console.WriteLine();

                GlobalVariables.isPipeCommand    = false;
                GlobalVariables.pipeCmdOutput    = string.Empty;
                GlobalVariables.pipeCmdCount     = 0;
                GlobalVariables.pipeCmdCountTemp = 0;
                GlobalVariables.aliasInParameter.Clear();

                var cmd = CommandRepository.GetCommand(cmdLine);
                if (cmd != null)
                    cmd.Execute(cmdLine);
                else
                    Console.WriteLine($"  Unknown command: {cmdLine}");

                if (GlobalVariables.isErrorCommand)
                {
                    Console.WriteLine();
                    FileSystem.ColorConsoleText(ConsoleColor.Red,
                        $"  Chain stopped at step {i + 1}: '{cmdLine}' failed.\n");
                    return;
                }
            }

            Console.WriteLine();
            Console.WriteLine(new string('─', 57));
            FileSystem.SuccessWriteLine($"Chain '{name}' completed successfully.");
        }

        private static string RunChainCapture(string name, ChainEntry chain)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"chain: {name}  steps: {chain.Commands.Count}");
            sb.AppendLine(new string('-', 57));

            // Save the outer pipe state so the shell's pipe counter stays correct
            // when we return — the outer loop still needs to decrement pipeCmdCount.
            bool   savedIsPipe    = GlobalVariables.isPipeCommand;
            int    savedCount     = GlobalVariables.pipeCmdCount;
            int    savedCountTemp = GlobalVariables.pipeCmdCountTemp;

            TextWriter originalOut = Console.Out;

            for (int i = 0; i < chain.Commands.Count; i++)
            {
                string cmdLine = chain.Commands[i];
                sb.AppendLine($"step: {i + 1}  cmd: {cmdLine}");

                // Each sub-command runs in normal (non-pipe) mode so it prints
                // to stdout normally; we capture that stdout via StringWriter.
                GlobalVariables.isPipeCommand    = false;
                GlobalVariables.pipeCmdOutput    = string.Empty;
                GlobalVariables.pipeCmdCount     = 0;
                GlobalVariables.pipeCmdCountTemp = 0;
                GlobalVariables.aliasInParameter.Clear();

                using var sw = new StringWriter();
                Console.SetOut(sw);
                try
                {
                    var cmd = CommandRepository.GetCommand(cmdLine);
                    if (cmd != null)
                        cmd.Execute(cmdLine);
                    else
                        Console.WriteLine($"Unknown command: {cmdLine}");
                }
                finally
                {
                    Console.SetOut(originalOut);
                }

                foreach (var line in sw.ToString().Split('\n'))
                {
                    string trimmed = line.TrimEnd('\r');
                    if (!string.IsNullOrWhiteSpace(trimmed))
                        sb.AppendLine($"output: {trimmed}");
                }

                string status = GlobalVariables.isErrorCommand ? "failed" : "ok";
                sb.AppendLine($"status: {status}");

                if (GlobalVariables.isErrorCommand)
                {
                    sb.AppendLine($"chain stopped at step {i + 1}");
                    break;
                }
            }

            // Restore outer pipe state before returning so the shell loop can
            // decrement pipeCmdCount correctly and pass pipeCmdOutput to the
            // next stage (e.g. cat).
            GlobalVariables.isPipeCommand    = savedIsPipe;
            GlobalVariables.pipeCmdCount     = savedCount;
            GlobalVariables.pipeCmdCountTemp = savedCountTemp;

            return sb.ToString();
        }

        private static void AddToChain(string param)
        {
            string[] nameParts = param.Split(' ', 2);
            if (nameParts.Length < 2) { FileSystem.ErrorWriteLine("Usage: chain add <name> \"cmd\""); return; }
            string name     = nameParts[0];
            string cmdToAdd = nameParts[1].Trim().Trim('"');

            var data = Load();
            if (!data.TryGetValue(name, out var chain))
            {
                FileSystem.ErrorWriteLine($"Chain '{name}' not found.");
                return;
            }
            chain.Commands.Add(cmdToAdd);
            SaveToDisk(data);
            FileSystem.SuccessWriteLine(
                $"Added '{cmdToAdd}' to chain '{name}'  (now {chain.Commands.Count} commands).");
        }

        private static void ShowChain(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { FileSystem.ErrorWriteLine("Usage: chain show <name>"); return; }
            var data = Load();
            if (!data.TryGetValue(name, out var chain))
            {
                FileSystem.ErrorWriteLine($"Chain '{name}' not found.");
                return;
            }

            // ── Pipe output: plain labeled lines ─────────────────────────────
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
            {
                GlobalVariables.pipeCmdOutput = BuildShowText(chain);
                return;
            }

            // ── Console output with colors ────────────────────────────────────
            Console.WriteLine();
            Console.WriteLine(new string('─', 57));
            FileSystem.ColorConsoleText(ConsoleColor.Cyan, "  Chain   : "); Console.WriteLine(chain.Name);
            FileSystem.ColorConsoleText(ConsoleColor.Cyan, "  Created : "); Console.WriteLine(chain.CreatedAt);
            FileSystem.ColorConsoleText(ConsoleColor.Cyan, "  Steps   : "); Console.WriteLine(chain.Commands.Count);
            Console.WriteLine();
            for (int i = 0; i < chain.Commands.Count; i++)
            {
                FileSystem.ColorConsoleText(ConsoleColor.DarkGray, $"  {i + 1,2}. ");
                Console.WriteLine(chain.Commands[i]);
            }
            Console.WriteLine(new string('─', 57));
        }

        private static string BuildShowText(ChainEntry chain)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"chain: {chain.Name}");
            sb.AppendLine($"created: {chain.CreatedAt}");
            sb.AppendLine($"steps: {chain.Commands.Count}");
            sb.AppendLine(new string('-', 57));
            for (int i = 0; i < chain.Commands.Count; i++)
                sb.AppendLine($"step: {i + 1}  cmd: {chain.Commands[i]}");
            return sb.ToString();
        }

        private static void ListChains()
        {
            var data = Load();

            // ── Pipe output: plain labeled lines ─────────────────────────────
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
            {
                if (data.Count == 0) { GlobalVariables.pipeCmdOutput = "no chains saved"; return; }
                var sb = new StringBuilder();
                foreach (var kv in data.Values)
                    sb.AppendLine($"chain: {kv.Name}  steps: {kv.Commands.Count}  created: {kv.CreatedAt}");
                GlobalVariables.pipeCmdOutput = sb.ToString();
                return;
            }

            // ── Console output with colors ────────────────────────────────────
            if (data.Count == 0) { Console.WriteLine("  No chains saved."); return; }
            Console.WriteLine();
            foreach (var kv in data.Values)
            {
                FileSystem.ColorConsoleText(ConsoleColor.Cyan,    $"  {kv.Name,-20}");
                FileSystem.ColorConsoleText(ConsoleColor.DarkGray, $"{kv.CreatedAt}  ");
                Console.WriteLine($"{kv.Commands.Count} command(s)");
            }
            Console.WriteLine();
        }

        private static void DeleteChain(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { FileSystem.ErrorWriteLine("Usage: chain del <name>"); return; }
            var data = Load();
            if (!data.Remove(name))
            {
                FileSystem.ErrorWriteLine($"Chain '{name}' not found.");
                return;
            }
            SaveToDisk(data);
            FileSystem.SuccessWriteLine($"Chain '{name}' deleted.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static List<string> ParseQuotedCommands(string input)
        {
            var result = new List<string>();
            int i = 0;
            while (i < input.Length)
            {
                while (i < input.Length && input[i] == ' ') i++;
                if (i >= input.Length) break;

                if (input[i] == '"')
                {
                    int end = input.IndexOf('"', i + 1);
                    if (end == -1) { result.Add(input[(i + 1)..].Trim()); break; }
                    result.Add(input[(i + 1)..end]);
                    i = end + 1;
                }
                else
                {
                    int end = input.IndexOf(' ', i);
                    if (end == -1) { result.Add(input[i..]); break; }
                    result.Add(input[i..end]);
                    i = end + 1;
                }
            }
            return result;
        }
    }
}
