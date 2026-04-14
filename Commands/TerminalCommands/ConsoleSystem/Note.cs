using Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.Json;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class Note : ITerminalCommand
    {
        /*
            note — Persistent terminal sticky-notes.
            Notes survive session restarts, stored as JSON in the xTerminal work directory.
        */

        public string Name => "note";

        private static readonly string s_noteFile =
            Path.Combine(GlobalVariables.terminalWorkDirectory, "notes.json");

        private static readonly JsonSerializerOptions s_jsonOpts =
            new() { WriteIndented = true };

        private static readonly string s_helpMessage = @"Usage of note command:
    note add ""<text>""  : Add a new note.
    note list           : List all saved notes.
    note del <id>       : Delete a note by its ID.
    note clear          : Delete all notes.
    note -h             : Display this help message.

Examples:
    note add ""fix the login bug before Friday""
    note list
    note del 2
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
                string param = parts.Length > 2 ? parts[2].Trim('"') : string.Empty;

                switch (sub)
                {
                    case "add":
                        AddNote(param);
                        break;
                    case "list":
                        ListNotes();
                        break;
                    case "del":
                        if (int.TryParse(param, out int id))
                            DeleteNote(id);
                        else
                        {
                            FileSystem.ErrorWriteLine("Invalid ID. Usage: note del <id>");
                            GlobalVariables.isErrorCommand = true;
                        }
                        break;
                    case "clear":
                        ClearNotes();
                        break;
                    default:
                        FileSystem.ErrorWriteLine($"Unknown sub-command '{sub}'. Use 'note -h' for help.");
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

        private sealed class NoteEntry
        {
            public int    Id        { get; set; }
            public string Text      { get; set; }
            public string CreatedAt { get; set; }
        }

        private static List<NoteEntry> Load()
        {
            if (!File.Exists(s_noteFile)) return new();
            return JsonSerializer.Deserialize<List<NoteEntry>>(
                       File.ReadAllText(s_noteFile)) ?? new();
        }

        private static void Save(List<NoteEntry> notes) =>
            File.WriteAllText(s_noteFile, JsonSerializer.Serialize(notes, s_jsonOpts));

        // ── Operations ────────────────────────────────────────────────────────

        private static void AddNote(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                FileSystem.ErrorWriteLine("Note text cannot be empty.");
                return;
            }
            var notes  = Load();
            int nextId = notes.Count == 0 ? 1 : notes.Max(n => n.Id) + 1;
            notes.Add(new NoteEntry
            {
                Id        = nextId,
                Text      = text,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
            Save(notes);
            FileSystem.SuccessWriteLine($"Note #{nextId} added.");
        }

        private static void ListNotes()
        {
            var notes = Load();
            if (notes.Count == 0) { Console.WriteLine("  No notes found."); return; }

            Console.WriteLine();
            foreach (var n in notes)
            {
                FileSystem.ColorConsoleText(ConsoleColor.Cyan,    $"  [{n.Id}] ");
                FileSystem.ColorConsoleText(ConsoleColor.DarkGray, $"{n.CreatedAt}  ");
                Console.WriteLine(n.Text);
            }
            Console.WriteLine();

            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                GlobalVariables.pipeCmdOutput = string.Join(Environment.NewLine,
                    notes.Select(n => $"[{n.Id}] {n.CreatedAt}  {n.Text}"));
        }

        private static void DeleteNote(int id)
        {
            var notes  = Load();
            int before = notes.Count;
            notes.RemoveAll(n => n.Id == id);
            if (notes.Count == before)
            {
                FileSystem.ErrorWriteLine($"Note #{id} not found.");
                return;
            }
            Save(notes);
            FileSystem.SuccessWriteLine($"Note #{id} deleted.");
        }

        private static void ClearNotes()
        {
            Save(new List<NoteEntry>());
            FileSystem.SuccessWriteLine("All notes cleared.");
        }
    }
}
