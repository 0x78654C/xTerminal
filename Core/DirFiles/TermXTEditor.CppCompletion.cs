using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Core.DirFiles
{
    public sealed partial class TermXTEditor
    {
        private ClangdClient _clangd;
        private Task<CppCompletionResult> _cppCompletionTask;
        private CancellationTokenSource _cppCompletionCancellation;
        private DateTime _clangdRetryUtc;
        private int _cppDocumentVersion;
        private bool _cppCompletionManual;

        private bool IsCppCompletionContext()
        {
            return _syntax == TermXTEditorSyntax.C || _syntax == TermXTEditorSyntax.Cpp;
        }

        private bool RefreshCppCompletion(bool manual)
        {
            if (!IsCppCompletionContext() || _mode != Mode.Insert || HasSelection())
            {
                DismissCompletion();
                return false;
            }

            string line = CurrentLine();
            int start = Math.Min(_cursorCol, line.Length);
            while (start > 0 && IsCSharpWordPart(line[start - 1])) start--;
            string prefix = line.Substring(start, _cursorCol - start);
            string before = line.Substring(0, start).TrimEnd();
            bool member = before.EndsWith(".", StringComparison.Ordinal) ||
                before.EndsWith("->", StringComparison.Ordinal) || before.EndsWith("::", StringComparison.Ordinal);
            bool directive = before.Trim() == "#";
            Match header = Regex.Match(line.Substring(0, _cursorCol), @"^\s*#\s*(?:include|include_next)\s*([<""])[^>""]*$");
            if (header.Success)
            {
                start = header.Groups[1].Index + 1;
                for (int i = start; i < _cursorCol; i++)
                    if (line[i] == '/' || line[i] == '\\') start = i + 1;
                prefix = line.Substring(start, _cursorCol - start);
            }
            bool reuse = _completionActive && _completionStartLine == _cursorLine && _completionStartCol == start;
            manual |= reuse && _cppCompletionManual;
            string selected = reuse ? _completionItems[_completionSelectedIndex].Label : null;
            if ((!manual && !member && !directive && !header.Success && prefix.Length < 2) ||
                (!header.Success && prefix.Length > 0 && !IsCSharpWordStart(prefix[0])))
            {
                DismissCompletion();
                return false;
            }

            int lineOffset = 0;
            for (int i = 0; i < _cursorLine; i++) lineOffset += _lines[i].Length + 1;
            string source = string.Join("\n", _lines);
            HashSet<string> names = ScanCppCompletionNames(source, lineOffset + _cursorCol,
                header.Success ? lineOffset + header.Groups[1].Index : -1, out bool suppressed);
            if (suppressed)
            {
                DismissCompletion();
                return false;
            }

            var items = new List<CompletionItem>();
            var labels = new HashSet<string>(StringComparer.Ordinal);
            void Add(IEnumerable<string> words, string kind, string detail, int priority)
            {
                foreach (string word in words)
                    if (labels.Add(word)) items.Add(new CompletionItem(word, word, kind, detail, priority));
            }

            bool cpp = _syntax == TermXTEditorSyntax.Cpp;
            if (directive)
                Add(cpp ? s_cppPreprocessorDirectives : s_cPreprocessorDirectives, "directive", "preprocessor", 0);
            else if (!header.Success && !member)
            {
                Add(cpp ? s_cppTypeKeywords : s_cTypeKeywords, "type", "language type", 20);
                Add(cpp ? s_cppFlowKeywords : s_cFlowKeywords, "keyword", "control flow", 20);
                Add(cpp ? s_cppKeywords : s_cKeywords, "keyword", "language keyword", 20);
                Add(cpp ? s_cppLiteralKeywords : s_cLiteralKeywords, "constant", "language constant", 20);
                Add(s_cStdIdentifiers, "identifier", "C standard library", 30);
                if (cpp) Add(new[] { "std" }, "module", "C++ standard library", 20);
                names.Remove(prefix);
                Add(names, "identifier", "name in current file", 10);
            }
            else if (cpp && !header.Success && Regex.IsMatch(before, @"(?:^|[^\w:])(?:::)?std\s*::$"))
            {
                Add(s_cppStdIdentifiers, "identifier", "C++ standard library", 20);
                Add(s_cStdIdentifiers, "identifier", "C standard library", 30);
            }

            List<CompletionItem> filtered = FilterCompletionItems(items, prefix, maxItems: int.MaxValue);
            DismissCompletion();
            if (filtered.Count > 0)
                SetCompletionSession(new CompletionSession(_cursorLine, start, member, items, filtered), selected);
            _cppCompletionManual = manual;

            bool pending = QueueCppCompletion(source, start, manual);
            if (manual && !pending)
                Status(filtered.Count > 0 ? "IntelliSense " + filtered.Count + " items" : "No C/C++ completions");
            return filtered.Count > 0 || pending;
        }

        private bool QueueCppCompletion(string source, int start, bool manual)
        {
            if (DateTime.UtcNow < _clangdRetryUtc) return false;
            if (_clangd == null) _clangd = new ClangdClient(Path.GetDirectoryName(_path), _syntax == TermXTEditorSyntax.Cpp);
            ClangdClient client = _clangd;
            var result = new CppCompletionResult
            {
                Path = _path, Line = _cursorLine, Column = _cursorCol, StartColumn = start,
                Version = _cppDocumentVersion, Syntax = _syntax, Manual = manual
            };
            var cancellation = new CancellationTokenSource();
            _cppCompletionCancellation = cancellation;
            _cppCompletionTask = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(manual ? 0 : 150, cancellation.Token).ConfigureAwait(false);
                    result.Items = await client.CompleteAsync(result.Path, source, result.Version, result.Line, result.Column,
                        cancellation.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { result.Error = ex.Message; }
                return result;
            });
            if (manual) BottomStatus("Loading C/C++ suggestions...");
            return true;
        }

        private bool CheckCppCompletionOnIdle()
        {
            if (_cppCompletionTask == null || !_cppCompletionTask.IsCompleted) return false;
            CppCompletionResult result = _cppCompletionTask.GetAwaiter().GetResult();
            _cppCompletionTask = null;
            _cppCompletionCancellation.Dispose();
            _cppCompletionCancellation = null;
            if (result.Version != _cppDocumentVersion || result.Path != _path || result.Syntax != _syntax ||
                result.Line != _cursorLine || result.Column != _cursorCol || _mode != Mode.Insert || HasSelection()) return false;
            if (result.Error != null)
            {
                StopClangd();
                _clangdRetryUtc = DateTime.UtcNow.AddSeconds(30);
                if (result.Manual) BottomStatus("C/C++ completion: " + result.Error, warning: true);
                return result.Manual;
            }
            if (result.Items == null) return false;
            string prefix = CurrentLine().Substring(result.StartColumn, _cursorCol - result.StartColumn);
            List<CompletionItem> items = FilterCompletionItems(result.Items, prefix, maxItems: int.MaxValue);
            // Keep basic suggestions when the server has no result (e.g. incomplete code).
            if (items.Count == 0) return false;
            string selected = _completionActive ? _completionItems[_completionSelectedIndex].Label : null;
            SetCompletionSession(new CompletionSession(result.Line, result.StartColumn, false, result.Items, items), selected);
            _cppCompletionManual = result.Manual;
            if (result.Manual) BottomStatus("C/C++ IntelliSense: " + items.Count + " items");
            return true;
        }

        private void CancelCppCompletion()
        {
            var cancellation = _cppCompletionCancellation;
            var task = _cppCompletionTask;
            _cppCompletionCancellation = null;
            _cppCompletionTask = null;
            if (cancellation == null) return;
            cancellation.Cancel();
            _ = task.ContinueWith(_ => cancellation.Dispose(), CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        private void StopClangd()
        {
            CancelCppCompletion();
            _clangd?.Dispose();
            _clangd = null;
            _clangdRetryUtc = DateTime.MinValue;
        }

        private sealed class CppCompletionResult
        {
            public string Path;
            public int Line, Column, StartColumn, Version;
            public TermXTEditorSyntax Syntax;
            public bool Manual;
            public List<CompletionItem> Items;
            public string Error;
        }

        // Skip comments, character literals, ordinary strings and C++ raw strings.
        // Header paths are the one string context in which clangd should complete.
        private static HashSet<string> ScanCppCompletionNames(string source, int cursor, int headerStart, out bool suppressed)
        {
            suppressed = false;
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < source.Length;)
            {
                int start = i;
                bool ignored = false, closed = true;
                if (source[i] == '/' && i + 1 < source.Length && source[i + 1] == '/')
                {
                    ignored = true;
                    i += 2;
                    while (i < source.Length && (source[i] != '\n' || (i > 0 && source[i - 1] == '\\'))) i++;
                    closed = false;
                }
                else if (source[i] == '/' && i + 1 < source.Length && source[i + 1] == '*')
                {
                    ignored = true;
                    int end = source.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    closed = end >= 0;
                    i = closed ? end + 2 : source.Length;
                }
                else if (source[i] == '"' || source[i] == '\'')
                {
                    ignored = true;
                    char quote = source[i++];
                    int open = quote == '"' && start > 0 && source[start - 1] == 'R' ? source.IndexOf('(', i) : -1;
                    if (open >= i && open - i <= 16 && source.Substring(i, open - i).IndexOfAny(new[] { ' ', '\\', '\n', '\r', ')' }) < 0)
                    {
                        string terminator = ")" + source.Substring(i, open - i) + "\"";
                        int end = source.IndexOf(terminator, open + 1, StringComparison.Ordinal);
                        closed = end >= 0;
                        i = closed ? end + terminator.Length : source.Length;
                    }
                    else
                    {
                        closed = false;
                        while (i < source.Length)
                        {
                            if (source[i] == '\\') i = Math.Min(source.Length, i + 2);
                            else if (source[i++] == quote) { closed = true; break; }
                        }
                    }
                }
                else if (char.IsDigit(source[i]))
                {
                    while (i < source.Length && (IsCSharpWordPart(source[i]) || source[i] == '.' || source[i] == '\'')) i++;
                }
                else if (IsCSharpWordStart(source[i]))
                {
                    while (i < source.Length && IsCSharpWordPart(source[i])) i++;
                    names.Add(source.Substring(start, i - start));
                }
                else i++;
                if (ignored && start < cursor && (closed ? cursor < i : cursor <= i) && start != headerStart)
                    suppressed = true;
            }
            return names;
        }
    }
}
