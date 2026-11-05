using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Core.DirFiles
{
    /// <summary>Line-oriented text search, independent of console and shell pipeline state.</summary>
    public static class GrepManage
    {
        public sealed class Options
        {
            public List<string> Patterns { get; } = new List<string>();
            public List<string> PatternFiles { get; } = new List<string>();
            public List<string> Files { get; } = new List<string>();
            public bool Help { get; set; }
            public bool IgnoreCase { get; set; }
            public bool Invert { get; set; }
            public bool LineNumbers { get; set; }
            public bool Recursive { get; set; }
            public bool FixedStrings { get; set; }
            public bool WholeWord { get; set; }
            public bool WholeLine { get; set; }
            public bool Count { get; set; }
            public bool Quiet { get; set; }
            public bool SuppressErrors { get; set; }
            public bool OnlyMatching { get; set; }
            public bool? FileNames { get; set; }
            public bool? ListFiles { get; set; }
            public long MaxCount { get; set; } = long.MaxValue;
            public int Before { get; set; }
            public int After { get; set; }
        }

        // Preserve backslashes for Windows paths and regex escapes. Use the opposite
        // quote style when a pattern contains a literal quote.
        public static Options ParseArguments(string arguments)
        {
            var tokens = new List<string>();
            var token = new StringBuilder();
            char quote = '\0';
            bool started = false;
            foreach (char ch in arguments ?? string.Empty)
            {
                if (quote != '\0')
                {
                    if (ch == quote) quote = '\0';
                    else token.Append(ch);
                }
                else if (ch == '\'' || ch == '"') { quote = ch; started = true; }
                else if (char.IsWhiteSpace(ch))
                {
                    if (started) { tokens.Add(token.ToString()); token.Clear(); started = false; }
                }
                else { token.Append(ch); started = true; }
            }
            if (quote != '\0') throw new ArgumentException("Unclosed quote.");
            if (started) tokens.Add(token.ToString());

            var options = new Options();
            var operands = new List<string>();
            bool endOptions = false;
            bool explicitPatterns = false;
            for (int i = 0; i < tokens.Count; i++)
            {
                string item = tokens[i];
                if (!endOptions && item == "--") { endOptions = true; continue; }
                if (endOptions || item == "-" || !item.StartsWith('-')) { operands.Add(item); continue; }

                string Value(string attached, string option)
                {
                    if (attached != null) return attached;
                    if (++i >= tokens.Count) throw new ArgumentException($"Option {option} requires a value.");
                    return tokens[i];
                }

                void Apply(char flag, string value = null)
                {
                    switch (flag)
                    {
                        case 'i': options.IgnoreCase = true; break;
                        case 'v': options.Invert = true; break;
                        case 'n': options.LineNumbers = true; break;
                        case 'r': options.Recursive = true; break;
                        case 'F': options.FixedStrings = true; break;
                        case 'E': options.FixedStrings = false; break;
                        case 'w': options.WholeWord = true; break;
                        case 'x': options.WholeLine = true; break;
                        case 'c': options.Count = true; break;
                        case 'l': options.ListFiles = true; break;
                        case 'L': options.ListFiles = false; break;
                        case 'q': options.Quiet = true; break;
                        case 's': options.SuppressErrors = true; break;
                        case 'o': options.OnlyMatching = true; break;
                        case 'H': options.FileNames = true; break;
                        case 'h': options.FileNames = false; break;
                        case 'e': options.Patterns.Add(value); explicitPatterns = true; break;
                        case 'f': options.PatternFiles.Add(value); explicitPatterns = true; break;
                        case 'm': options.MaxCount = Number(value, "-m"); break;
                        case 'A': options.After = ContextNumber(value, "-A"); break;
                        case 'B': options.Before = ContextNumber(value, "-B"); break;
                        case 'C': options.Before = options.After = ContextNumber(value, "-C"); break;
                        default: throw new ArgumentException($"Unknown option '-{flag}'. Use 'grep --help'.");
                    }
                }

                if (item.StartsWith("--", StringComparison.Ordinal))
                {
                    int equals = item.IndexOf('=');
                    string name = equals < 0 ? item : item[..equals];
                    string attached = equals < 0 ? null : item[(equals + 1)..];
                    char flag = name switch
                    {
                        "--ignore-case" => 'i', "--invert-match" => 'v', "--line-number" => 'n',
                        "--recursive" => 'r', "--fixed-strings" => 'F', "--extended-regexp" => 'E',
                        "--word-regexp" => 'w', "--line-regexp" => 'x', "--count" => 'c',
                        "--files-with-matches" => 'l', "--files-without-match" => 'L',
                        "--quiet" => 'q', "--silent" => 'q', "--no-messages" => 's',
                        "--only-matching" => 'o', "--with-filename" => 'H', "--no-filename" => 'h',
                        "--regexp" => 'e', "--file" => 'f', "--max-count" => 'm',
                        "--after-context" => 'A', "--before-context" => 'B', "--context" => 'C',
                        "--help" => '?',
                        _ => throw new ArgumentException($"Unknown option '{name}'. Use 'grep --help'.")
                    };
                    if ("efmABC".Contains(flag)) Apply(flag, Value(attached, name));
                    else
                    {
                        if (attached != null) throw new ArgumentException($"Option {name} does not take a value.");
                        if (flag == '?') options.Help = true;
                        else Apply(flag);
                    }
                }
                else
                {
                    for (int j = 1; j < item.Length; j++)
                    {
                        char flag = item[j];
                        if ("efmABC".Contains(flag))
                        {
                            Apply(flag, Value(j + 1 < item.Length ? item[(j + 1)..] : null, $"-{flag}"));
                            break;
                        }
                        Apply(flag);
                    }
                }
            }
            if (!options.Help && !explicitPatterns)
            {
                if (operands.Count == 0) throw new ArgumentException("Missing pattern. Use 'grep --help'.");
                options.Patterns.Add(operands[0]);
                operands.RemoveAt(0);
            }
            options.Files.AddRange(operands);
            return options;
        }

        private static long Number(string value, string option)
        {
            if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out long number))
                throw new ArgumentException($"Option {option} requires a non-negative integer.");
            return number;
        }

        private static int ContextNumber(string value, string option)
        {
            long number = Number(value, option);
            if (number > int.MaxValue) throw new ArgumentException($"Value for {option} is too large.");
            return (int)number;
        }

        /// <returns>0 for a selected line, 1 for no selected lines, 2 for an error, 130 for cancellation.</returns>
        public static int Search(Options options, string currentDirectory, TextReader input,
            TextWriter output, TextWriter error, Func<bool> cancelled = null)
        {
            return new SearchSession(options, currentDirectory, input, output, error, cancelled ?? (() => false)).Run();
        }

        private sealed class SearchSession
        {
            private readonly Options _options;
            private readonly string _directory;
            private readonly TextReader _input;
            private readonly TextWriter _output;
            private readonly TextWriter _error;
            private readonly Func<bool> _cancelled;
            private Regex[] _patterns;
            private bool _hadError;
            private bool _selected;
            private bool _wroteContextGroup;

            public SearchSession(Options options, string directory, TextReader input,
                TextWriter output, TextWriter error, Func<bool> cancelled)
            {
                _options = options;
                _directory = directory;
                _input = input;
                _output = output;
                _error = error;
                _cancelled = cancelled;
            }

            public int Run()
            {
                try
                {
                    var patterns = new List<string>();
                    foreach (string pattern in _options.Patterns)
                        patterns.AddRange(pattern.Split('\n').Select(p => p.TrimEnd('\r')));
                    foreach (string file in _options.PatternFiles)
                    {
                        if (file == "-")
                        {
                            if (_input == null) throw new ArgumentException("No input for pattern file '-'.");
                            string pattern;
                            while (!_cancelled() && (pattern = _input.ReadLine()) != null) patterns.Add(pattern);
                        }
                        else patterns.AddRange(File.ReadLines(Path.GetFullPath(file, _directory)));
                    }
                    RegexOptions flags = RegexOptions.CultureInvariant;
                    if (_options.IgnoreCase) flags |= RegexOptions.IgnoreCase;
                    _patterns = patterns.Select(pattern =>
                    {
                        string expression = _options.FixedStrings ? Regex.Escape(pattern) : pattern;
                        if (_options.WholeLine) expression = @"\A(?:" + expression + @")\z";
                        else if (_options.WholeWord) expression = @"(?<![\p{L}\p{N}_])(?:" + expression + @")(?![\p{L}\p{N}_])";
                        return new Regex(expression, flags, TimeSpan.FromSeconds(2));
                    }).ToArray();

                    var operands = new List<string>(_options.Files);
                    if (operands.Count == 0)
                    {
                        if (_options.Recursive) operands.Add(".");
                        else if (_input != null) operands.Add("-");
                        else throw new ArgumentException("Provide a file or pipe text into grep. Use 'grep --help'.");
                    }

                    // Expand top-level operands here; recursive traversal remains lazy.
                    var paths = new List<string>();
                    foreach (string operand in operands)
                    {
                        if (_cancelled()) return 130;
                        try
                        {
                            if (operand.IndexOfAny(new[] { '*', '?' }) < 0) { paths.Add(operand); continue; }
                            string fullPath = Path.GetFullPath(operand, _directory);
                            string parent = Path.GetDirectoryName(fullPath);
                            string[] matches = Directory.GetFiles(parent, Path.GetFileName(fullPath));
                            Array.Sort(matches, StringComparer.OrdinalIgnoreCase);
                            if (matches.Length == 0) Report($"{operand}: No files matched.");
                            foreach (string match in matches)
                                paths.Add(Path.IsPathRooted(operand) ? match : Path.GetRelativePath(_directory, match));
                        }
                        catch (Exception ex) when (IsInputError(ex)) { Report($"{operand}: {ex.Message}"); }
                    }
                    bool showFile = _options.FileNames ?? (paths.Count > 1 || operands.Count > 1 || _options.Recursive);
                    foreach (string path in paths)
                    {
                        foreach (string file in Expand(path))
                        {
                            if (_cancelled()) return 130;
                            try
                            {
                                if (file == "-")
                                {
                                    if (_input == null) throw new ArgumentException("No piped or redirected input for '-'.");
                                    SearchReader(_input, "(standard input)", showFile);
                                }
                                else
                                {
                                    using var reader = new StreamReader(Path.GetFullPath(file, _directory), Encoding.UTF8, true);
                                    SearchReader(reader, file, showFile);
                                }
                            }
                            catch (Exception ex) when (IsInputError(ex)) { Report($"{file}: {ex.Message}"); }
                            if (_options.Quiet && _selected) return 0;
                        }
                    }
                }
                catch (RegexMatchTimeoutException) { Report("Regular expression timed out.", true); }
                catch (Exception ex) when (IsInputError(ex)) { Report(ex.Message, ex is ArgumentException); }
                return _cancelled() ? 130 : _hadError ? 2 : _selected ? 0 : 1;
            }

            private IEnumerable<string> Expand(string path)
            {
                if (path == "-") { yield return path; yield break; }
                var pending = new Stack<(string Path, bool Root)>();
                pending.Push((path, true));
                while (pending.Count > 0 && !_cancelled())
                {
                    var entry = pending.Pop();
                    string[] children = null;
                    bool isFile = false;
                    try
                    {
                        string fullPath = Path.GetFullPath(entry.Path, _directory);
                        FileAttributes attributes = File.GetAttributes(fullPath);
                        // Discovered links are skipped because directory junctions can form cycles.
                        if (!entry.Root && (attributes & FileAttributes.ReparsePoint) != 0) continue;
                        if ((attributes & FileAttributes.Directory) == 0) isFile = true;
                        else if (!_options.Recursive) Report($"{entry.Path}: Is a directory (use -r).");
                        else
                        {
                            children = Directory.GetFileSystemEntries(fullPath);
                            Array.Sort(children, StringComparer.OrdinalIgnoreCase);
                        }
                    }
                    catch (Exception ex) when (IsInputError(ex)) { Report($"{entry.Path}: {ex.Message}"); }
                    if (isFile) yield return entry.Path;
                    if (children == null) continue;
                    for (int i = children.Length - 1; i >= 0; i--)
                        pending.Push((Path.Combine(entry.Path, Path.GetFileName(children[i])), false));
                }
            }

            private void SearchReader(TextReader reader, string name, bool showFile)
            {
                long lineNumber = 0, count = 0, lastWritten = 0, afterUntil = 0;
                bool context = (_options.Before > 0 || _options.After > 0) && !_options.OnlyMatching;
                var before = new Queue<(long Number, string Text)>();
                string line;
                while (!_cancelled() && (count < _options.MaxCount || lineNumber < afterUntil) &&
                    (line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    bool selected = count < _options.MaxCount && (_patterns.Any(p => p.IsMatch(line)) != _options.Invert);
                    if (selected)
                    {
                        count++;
                        _selected = true;
                        if (_options.Quiet || _options.ListFiles.HasValue) break;
                    }
                    if (_options.Count || _options.Quiet || _options.ListFiles.HasValue) continue;
                    if (selected)
                    {
                        if (_options.OnlyMatching)
                        {
                            if (!_options.Invert)
                                foreach (string match in MatchingParts(line)) WriteLine(match, lineNumber, ':');
                        }
                        else
                        {
                            long first = before.Count > 0 ? before.Peek().Number : lineNumber;
                            if (context && _wroteContextGroup && (lastWritten == 0 || first > lastWritten + 1))
                                _output.WriteLine("--");
                            while (before.Count > 0)
                            {
                                var previous = before.Dequeue();
                                WriteLine(previous.Text, previous.Number, '-');
                            }
                            WriteLine(line, lineNumber, ':');
                            lastWritten = lineNumber;
                            afterUntil = lineNumber + _options.After;
                            if (context) _wroteContextGroup = true;
                        }
                    }
                    else if (context && lineNumber <= afterUntil)
                    {
                        WriteLine(line, lineNumber, '-');
                        lastWritten = lineNumber;
                    }
                    else if (context && _options.Before > 0)
                    {
                        before.Enqueue((lineNumber, line));
                        if (before.Count > _options.Before) before.Dequeue();
                    }
                }
                if (_cancelled() || _options.Quiet) return;
                if (_options.ListFiles.HasValue)
                {
                    if ((count > 0) == _options.ListFiles.Value) _output.WriteLine(name);
                }
                else if (_options.Count) _output.WriteLine((showFile ? name + ":" : "") + count.ToString(CultureInfo.InvariantCulture));

                void WriteLine(string text, long number, char separator)
                {
                    if (showFile) { _output.Write(name); _output.Write(separator); }
                    if (_options.LineNumbers) { _output.Write(number.ToString(CultureInfo.InvariantCulture)); _output.Write(separator); }
                    _output.WriteLine(text);
                }
            }

            private IEnumerable<string> MatchingParts(string line)
            {
                int position = 0;
                while (position < line.Length && !_cancelled())
                {
                    Match best = null;
                    foreach (Regex pattern in _patterns)
                    {
                        Match match = pattern.Match(line, position);
                        while (match.Success && match.Length == 0) match = match.NextMatch();
                        if (match.Success && (best == null || match.Index < best.Index ||
                            (match.Index == best.Index && match.Length > best.Length))) best = match;
                    }
                    if (best == null) yield break;
                    yield return best.Value;
                    position = best.Index + best.Length;
                }
            }

            private void Report(string message, bool always = false)
            {
                _hadError = true;
                if (always || !_options.SuppressErrors) _error.WriteLine("grep: " + message);
            }

            private static bool IsInputError(Exception ex) => ex is IOException || ex is UnauthorizedAccessException ||
                ex is ArgumentException || ex is NotSupportedException || ex is System.Security.SecurityException;
        }
    }
}
