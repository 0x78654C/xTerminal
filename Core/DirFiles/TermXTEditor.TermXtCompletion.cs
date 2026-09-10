using System;
using System.Collections.Generic;
using System.Globalization;

namespace Core.DirFiles
{
    public sealed partial class TermXTEditor
    {
        private enum TermXtCompletionKind
        {
            None,
            Keyword,
            Expression,
            Function,
            Variable,
            LinesVariable,
            EachIn,
            EachSource,
            Condition
        }

        private TermXtCompletionKind _termXtCompletionKind;
        private bool _termXtCompletionManual;

        private static readonly Dictionary<string, string> s_termXtCompletionDetails =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["set"] = "set <name> = <value>",
                ["print"] = "print <text>",
                ["run"] = "run <command>",
                ["capture"] = "capture <name> = <command>",
                ["input"] = "input <name> = <prompt>",
                ["read"] = "read <name> = <path>",
                ["write"] = "write <path> <text>",
                ["append"] = "append <path> <text>",
                ["wait"] = "wait <milliseconds>",
                ["call"] = "call <function> [arguments]",
                ["if"] = "if <condition> ... end",
                ["elif"] = "elif <condition>",
                ["else"] = "else branch",
                ["end"] = "close the current block",
                ["loop"] = "loop <count> ... end",
                ["while"] = "while <condition> ... end",
                ["each"] = "each <name> in <values> ... end",
                ["func"] = "func <name> ... end",
                ["try"] = "try ... catch ... end",
                ["catch"] = "handle an error in try",
                ["break"] = "exit the current loop",
                ["continue"] = "next loop iteration",
                ["return"] = "return [value] from a function",
                ["exit"] = "stop the script",
                ["eval"] = "eval <arithmetic expression>",
                ["upper"] = "upper <text>",
                ["lower"] = "lower <text>",
                ["len"] = "len <text>",
                ["trim"] = "trim <text>",
                ["substr"] = "substr <text> <start> <length>",
                ["replace"] = "replace <text> <old> <new>",
                ["in"] = "each <name> in <values>",
                ["lines:"] = "lines:<variable> - iterate nonempty lines",
                ["not"] = "negate a condition",
                ["contains"] = "<text> contains <value>",
                ["startswith"] = "<text> startswith <prefix>",
                ["endswith"] = "<text> endswith <suffix>"
            };

        private static readonly Dictionary<string, string> s_termXtBuiltinVariables =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["argc"] = "argument count",
                ["DATE"] = "current date (yyyy-MM-dd)",
                ["TIME"] = "current time (HH:mm:ss)",
                ["USER"] = "current account name",
                ["PC"] = "computer name",
                ["CWD"] = "current working directory",
                ["i"] = "loop iteration (starting at 1)",
                ["result"] = "most recent function return value",
                ["error"] = "whether the last operation failed",
                ["error_message"] = "most recent error message"
            };

        private bool IsTermXtCompletionContext()
        {
            return _syntax == TermXTEditorSyntax.TermXt && !IsCSharpCompletionContext();
        }

        private bool RefreshTermXtCompletion(bool manual)
        {
            if (_mode != Mode.Insert || !TryGetTermXtCompletionContext(
                out TermXtCompletionKind kind, out int startColumn, out string prefix))
            {
                DismissCompletion();
                return false;
            }

            bool reuse = _completionActive && _termXtCompletionKind == kind &&
                _completionStartLine == _cursorLine && _completionStartCol == startColumn;
            manual |= reuse && _termXtCompletionManual;
            bool immediate = kind == TermXtCompletionKind.Variable ||
                kind == TermXtCompletionKind.LinesVariable || kind == TermXtCompletionKind.Function;
            if (!manual && !immediate && prefix.Length < 2)
            {
                DismissCompletion();
                return false;
            }

            string selectedLabel = reuse ? _completionItems[_completionSelectedIndex].Label : null;
            List<CompletionItem> allItems = reuse
                ? new List<CompletionItem>(_completionAllItems)
                : BuildTermXtCompletionItems(kind);

            // Positional arguments have no fixed upper limit.
            if ((kind == TermXtCompletionKind.Variable || kind == TermXtCompletionKind.LinesVariable) &&
                int.TryParse(prefix, NumberStyles.None, CultureInfo.InvariantCulture, out int argument) && argument > 0 &&
                !allItems.Exists(item => item.Label == prefix))
            {
                allItems.Add(new CompletionItem(prefix, prefix, "variable", "argument " + prefix, 20));
            }

            List<CompletionItem> items = FilterCompletionItems(allItems, prefix);
            if (items.Count == 0)
            {
                DismissCompletion();
                if (manual)
                    Status("No TermXT completions");
                return false;
            }

            SetCompletionSession(new CompletionSession(_cursorLine, startColumn, false, allItems, items), selectedLabel);
            _termXtCompletionKind = kind;
            _termXtCompletionManual = manual;
            if (manual)
                Status("IntelliSense " + items.Count + " " + Pluralize("item", items.Count));
            return true;
        }

        private bool TryGetTermXtCompletionContext(
            out TermXtCompletionKind kind, out int startColumn, out string prefix)
        {
            kind = TermXtCompletionKind.None;
            startColumn = _cursorCol;
            prefix = string.Empty;
            if (_cursorLine < 0 || _cursorLine >= _lines.Count || HasSelection())
                return false;

            string line = CurrentLine();
            if (_cursorCol < 0 || _cursorCol > line.Length || line.TrimStart().StartsWith("#", StringComparison.Ordinal))
                return false;

            // Interpolation is valid even inside quoted text.
            while (startColumn > 0 && IsTermXtVariablePart(line[startColumn - 1]))
                startColumn--;
            prefix = line.Substring(startColumn, _cursorCol - startColumn);
            if (startColumn > 0 && line[startColumn - 1] == '{')
            {
                kind = TermXtCompletionKind.Variable;
                return true;
            }

            string typed = line.Substring(0, _cursorCol).TrimStart();
            string command = FirstWord(typed).ToLowerInvariant();
            int commandStart = _cursorCol - typed.Length;
            int commandEnd = commandStart + command.Length;
            if (commandEnd == _cursorCol)
            {
                startColumn = commandStart;
                prefix = typed;
                kind = TermXtCompletionKind.Keyword;
                return IsTermXtVariableName(prefix) || prefix.Length == 0;
            }

            // All other completion contexts are outside double-quoted text.
            bool inQuotes = false;
            for (int i = 0; i < _cursorCol; i++)
                if (line[i] == '"') inQuotes = !inQuotes;
            if (inQuotes)
                return false;

            int argumentStart = commandEnd;
            while (argumentStart < _cursorCol && char.IsWhiteSpace(line[argumentStart]))
                argumentStart++;

            if (command == "call")
            {
                startColumn = argumentStart;
                prefix = line.Substring(startColumn, _cursorCol - startColumn);
                foreach (char c in prefix)
                    if (!IsTermXtCompletionPart(c, TermXtCompletionKind.Function)) return false;
                kind = TermXtCompletionKind.Function;
                return true;
            }

            if (command == "set")
            {
                int equals = line.IndexOf('=', commandEnd);
                if (equals < 0 || equals >= _cursorCol)
                    return false;
                int expressionStart = equals + 1;
                while (expressionStart < _cursorCol && char.IsWhiteSpace(line[expressionStart]))
                    expressionStart++;
                if (expressionStart != startColumn)
                    return false;
                kind = TermXtCompletionKind.Expression;
                return true;
            }

            string beforePrefix = line.Substring(argumentStart, Math.Max(0, startColumn - argumentStart)).Trim();
            if (command == "each")
            {
                string name = FirstWord(beforePrefix);
                if (!IsTermXtVariableName(name))
                    return false;
                string afterName = beforePrefix.Substring(name.Length).TrimStart();
                if (afterName.Length == 0 && startColumn > argumentStart + name.Length)
                    kind = TermXtCompletionKind.EachIn;
                else if (string.Equals(afterName, "in", StringComparison.OrdinalIgnoreCase) &&
                    startColumn > 0 && char.IsWhiteSpace(line[startColumn - 1]))
                    kind = TermXtCompletionKind.EachSource;
                else if (string.Equals(FirstWord(afterName), "in", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(afterName.Substring(2).Trim(), "lines:", StringComparison.OrdinalIgnoreCase))
                    kind = TermXtCompletionKind.LinesVariable;
                return kind != TermXtCompletionKind.None;
            }

            if ((command == "if" || command == "elif" || command == "while") &&
                startColumn > 0 && (char.IsWhiteSpace(line[startColumn - 1]) || line[startColumn - 1] == '('))
            {
                kind = TermXtCompletionKind.Condition;
                return true;
            }

            return false;
        }

        private List<CompletionItem> BuildTermXtCompletionItems(TermXtCompletionKind kind)
        {
            var items = new List<CompletionItem>();
            var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            switch (kind)
            {
                case TermXtCompletionKind.Keyword:
                    foreach (string keyword in s_termXtLineKeywords)
                        AddTermXtWordCompletion(items, keyword, "keyword");
                    break;
                case TermXtCompletionKind.Expression:
                    foreach (string function in s_functionKeywords)
                        if (function != "lines") AddTermXtWordCompletion(items, function, "function");
                    break;
                case TermXtCompletionKind.EachIn:
                    AddTermXtWordCompletion(items, "in", "keyword");
                    break;
                case TermXtCompletionKind.EachSource:
                    AddTermXtWordCompletion(items, "lines:", "source");
                    break;
                case TermXtCompletionKind.Condition:
                    foreach (string word in new[] { "not", "contains", "startswith", "endswith" })
                        AddTermXtWordCompletion(items, word, "operator");
                    break;
                case TermXtCompletionKind.Function:
                    foreach (string name in CollectTermXtFunctionNames())
                        items.Add(new CompletionItem(name, name, "function", "call " + name + " [arguments]", 0));
                    break;
                case TermXtCompletionKind.Variable:
                case TermXtCompletionKind.LinesVariable:
                    foreach (var variable in s_termXtBuiltinVariables)
                    {
                        labels.Add(variable.Key);
                        items.Add(new CompletionItem(variable.Key, variable.Key, "variable", variable.Value, 10));
                    }
                    for (int i = 1; i <= 9; i++)
                    {
                        string name = i.ToString(CultureInfo.InvariantCulture);
                        labels.Add(name);
                        items.Add(new CompletionItem(name, name, "variable", "argument " + name, 20));
                    }
                    for (int i = 0; i < _lines.Count; i++)
                    {
                        string line = _lines[i].Trim();
                        string command = FirstWord(line).ToLowerInvariant();
                        string name = string.Empty;
                        if (command == "set" || command == "input" || command == "read" || command == "capture")
                        {
                            int equals = line.IndexOf('=');
                            if (equals > command.Length)
                                name = line.Substring(command.Length, equals - command.Length).Trim();
                        }
                        else if (command == "each")
                        {
                            name = SecondWord(line);
                        }
                        if (IsTermXtVariableName(name) && labels.Add(name))
                            items.Add(new CompletionItem(name, name, "variable", command + " - line " + (i + 1), 0));
                    }
                    break;
            }
            return items;
        }

        private static void AddTermXtWordCompletion(List<CompletionItem> items, string word, string kind)
        {
            items.Add(new CompletionItem(word, word, kind, s_termXtCompletionDetails[word], 0));
        }

        private static bool IsTermXtCompletionPart(char c, TermXtCompletionKind kind)
        {
            return kind == TermXtCompletionKind.Function
                ? !char.IsWhiteSpace(c) && c != '"' && c != '{' && c != '}'
                : IsTermXtVariablePart(c);
        }

        private static bool IsTermXtVariableName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            foreach (char c in name)
                if (!IsTermXtVariablePart(c)) return false;
            return true;
        }

        private static bool IsTermXtVariablePart(char c)
        {
            UnicodeCategory category = char.GetUnicodeCategory(c);
            return char.IsLetterOrDigit(c) || category == UnicodeCategory.NonSpacingMark ||
                category == UnicodeCategory.ConnectorPunctuation;
        }
    }
}
