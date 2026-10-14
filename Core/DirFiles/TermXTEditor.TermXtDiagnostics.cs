using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Core.DirFiles
{
    public sealed partial class TermXTEditor
    {
        private void CollectTermXtDiagnostics()
        {
            foreach (var diagnostic in TermXtSyntax.Validate(_lines))
            {
                string source = _lines[diagnostic.Line - 1];
                int start = source.Length - source.TrimStart().Length;
                AddDiagnostic(diagnostic.Line - 1, start, start + FirstWord(source.TrimStart()).Length,
                    string.Empty, diagnostic.Message);
            }

            HashSet<string> functions = CollectTermXtFunctionNames();
            var variables = CollectTermXtDeclaredVariables();
            for (int i = 0; i < _lines.Count; i++)
            {
                string raw = _lines[i];
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                int start = raw.Length - raw.TrimStart().Length;
                string keyword = TermXtSyntax.Keyword(line);
                int argumentStart = start + keyword.Length;
                while (argumentStart < raw.Length && char.IsWhiteSpace(raw[argumentStart])) argumentStart++;
                string rest = raw.Substring(argumentStart).TrimEnd();
                void Error(string code, string message) => AddDiagnostic(i, argumentStart, raw.TrimEnd().Length, code, message);
                void Warning(int from, int to, string code, string message) =>
                    AddDiagnostic(i, from, to, code, message, EditorDiagnosticSeverity.Warning);

                if (keyword == "if" || keyword == "elif" || keyword == "while")
                {
                    try { TermXtSyntax.ValidateCondition(rest); }
                    catch (FormatException ex) { Error("XT1001", ex.Message); }
                }
                else if (keyword == "each" && rest.Length > 0)
                {
                    Match each = Regex.Match(rest, @"^(\w+)\s+in\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    if (!each.Success) Error("XT1002", "Invalid 'each' syntax. Expected: each <variable> in <values>.");
                    else
                    {
                        string values = each.Groups[2].Value;
                        Match range = Regex.Match(values, @"^(-?\d+)\.\.(-?\d+)$");
                        if (range.Success && (!int.TryParse(range.Groups[1].Value, out _) || !int.TryParse(range.Groups[2].Value, out _)))
                            Error("XT1002", "Each range endpoints must be 32-bit integers.");
                        Match lines = Regex.Match(values, @"^lines:(\w+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                        if (lines.Success)
                        {
                            Group name = lines.Groups[1];
                            WarnUnknownVariable(name.Value, argumentStart + each.Groups[2].Index + name.Index, name.Length);
                        }
                    }
                }
                else if (keyword == "call")
                {
                    try
                    {
                        string[] arguments = TermXtSyntax.ParseArguments(rest);
                        if (arguments.Length == 0) Error("XT1003", "'call' missing function name.");
                        else if (!HasTermXtInterpolation(arguments[0]) && !functions.Contains(arguments[0]))
                        {
                            int position = 0;
                            TermXtSyntax.ReadArgument(rest, ref position);
                            AddDiagnostic(i, argumentStart, argumentStart + position, "XT1003", "Function '" + arguments[0] + "' not found.");
                        }
                    }
                    catch (FormatException ex) { Error("XT1003", ex.Message); }
                }
                else if (keyword == "loop" && rest.Length > 0 && !HasTermXtInterpolation(rest))
                {
                    if (!int.TryParse(rest, out int count) || count <= 0)
                        Error("XT1004", "'loop' expects a positive 32-bit integer.");
                }
                else if (keyword == "set")
                {
                    int equals = raw.IndexOf('=', argumentStart);
                    if (equals >= 0)
                    {
                        string name = raw.Substring(argumentStart, equals - argumentStart).Trim();
                        if (name.Equals("DATE", StringComparison.OrdinalIgnoreCase) || name.Equals("TIME", StringComparison.OrdinalIgnoreCase) ||
                            name.Equals("CWD", StringComparison.OrdinalIgnoreCase))
                            Warning(argumentStart, argumentStart + name.Length, "XT2002", "Assignment to '" + name + "' is ignored by interpolation; this built-in is computed dynamically.");
                        int valueStart = equals + 1;
                        while (valueStart < raw.Length && char.IsWhiteSpace(raw[valueStart])) valueStart++;
                        string expression = raw.Substring(valueStart).TrimEnd();
                        if (!HasTermXtInterpolation(expression))
                        {
                            string error = ValidateTermXtConstantExpression(expression);
                            if (error != null) AddDiagnostic(i, valueStart, raw.TrimEnd().Length, "XT1005", error);
                        }
                    }
                }
                else if (keyword == "write" || keyword == "append")
                {
                    try
                    {
                        int position = 0;
                        string file = TermXtSyntax.ReadArgument(rest, ref position);
                        if (file.Length == 0 || position >= rest.Length)
                            Error("XT1006", "'" + keyword + "' expects: " + keyword + " <file> \"text\".");
                    }
                    catch (FormatException ex) { Error("XT1006", ex.Message); }
                }
                else if (keyword == "wait" && !HasTermXtInterpolation(rest))
                {
                    if (!int.TryParse(rest, out int milliseconds) || milliseconds <= 0)
                        Warning(argumentStart, raw.TrimEnd().Length, "XT2004", "This wait has no effect; use a positive integer number of milliseconds.");
                }
                else if (keyword == "end" || keyword == "else" || keyword == "try" || keyword == "catch" ||
                    keyword == "break" || keyword == "continue" || keyword == "exit")
                {
                    if (rest.Length > 0) Warning(argumentStart, raw.TrimEnd().Length, "XT2003", "'" + keyword + "' ignores trailing arguments.");
                }
                else if (!_termXtCommandNames.Contains(keyword) && TrySuggestTermXtKeyword(keyword, line, functions, out string suggestion))
                {
                    // Unrecognized lines are also valid external commands. A likely
                    // spelling mistake is advisory, not a definite language error.
                    Warning(start, start + keyword.Length, "XT2005", "Possible TermXT keyword typo '" + keyword + "'. Did you mean '" + suggestion + "'?");
                }

                foreach (Match match in Regex.Matches(raw, @"\{(\w+)\}"))
                    WarnUnknownVariable(match.Groups[1].Value, match.Index, match.Length);

                void WarnUnknownVariable(string name, int column, int length)
                {
                    if (variables.ContainsKey(name) || s_termXtBuiltinVariables.ContainsKey(name) || IsTermXtPositionalArgument(name)) return;
                    Warning(column, column + length, "XT2001", "Variable '" + name + "' has no declaration in this script.");
                }
            }
        }

        private static bool HasTermXtInterpolation(string text)
        {
            return Regex.IsMatch(text, @"\{\w+\}");
        }

        private static bool IsTermXtPositionalArgument(string name)
        {
            foreach (char c in name)
                if (!char.IsDigit(c)) return false;
            return name.Length > 0;
        }

        private static string ValidateTermXtConstantExpression(string expression)
        {
            string function = FirstWord(expression).ToLowerInvariant();
            // These are string prefixes in the interpreter. A bare name is a
            // literal value, and ordinary text is deliberately not parsed here.
            if (expression.Length <= function.Length || expression[function.Length] != ' ') return null;
            string arguments = expression.Substring(function.Length + 1).Trim();
            try
            {
                if (function == "eval")
                {
                    if (!Regex.IsMatch(arguments, @"^[\d\s\+\-\*\/\%\(\)\.]+$")) return "Math expression contains invalid characters.";
                    using var calculator = new DataTable();
                    object result = calculator.Compute(arguments, null);
                    if (!double.IsFinite(Convert.ToDouble(result, CultureInfo.InvariantCulture))) return "Math result is not finite.";
                }
                else if (function == "substr")
                {
                    string[] tokens = TermXtSyntax.ParseArguments(arguments);
                    if (tokens.Length < 3) return "substr expects: substr <text> <start> <length>.";
                    if (!int.TryParse(tokens[tokens.Length - 2], out _) || !int.TryParse(tokens[tokens.Length - 1], out int length))
                        return "substr: start and length must be numbers.";
                    if (length < 0) return "substr: length must not be negative.";
                }
                else if (function == "replace")
                {
                    string[] tokens = TermXtSyntax.ParseArguments(arguments);
                    if (tokens.Length < 3) return "replace expects: replace <text> <old> <new>.";
                    if (tokens[1].Length == 0) return "replace: the text to replace must not be empty.";
                }
            }
            catch (Exception ex) when (ex is FormatException || ex is EvaluateException || ex is SyntaxErrorException || ex is OverflowException || ex is InvalidCastException || ex is DivideByZeroException)
            {
                return ex.Message;
            }
            return null;
        }
    }
}
