using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Core
{
    // Shared by the interpreter and the editor so syntax checks stay consistent.
    public static class TermXtSyntax
    {
        public static string Keyword(string line)
        {
            int end = 0;
            while (end < line.Length && !char.IsWhiteSpace(line[end])) end++;
            return line[..end].ToLowerInvariant();
        }

        public static string Unquote(string text)
        {
            return text.Length >= 2 && text[0] == '"' && text[^1] == '"'
                ? text[1..^1] : text;
        }

        public static string ReadArgument(string input, ref int position)
        {
            while (position < input.Length && char.IsWhiteSpace(input[position])) position++;
            var result = new StringBuilder();
            bool quoted = false;
            while (position < input.Length)
            {
                char ch = input[position];
                if (ch == '"') quoted = !quoted;
                else if (!quoted && char.IsWhiteSpace(ch)) break;
                else result.Append(ch);
                position++;
            }
            if (quoted) throw new FormatException("Unclosed double quote in argument.");
            return result.ToString();
        }

        public static string[] ParseArguments(string input)
        {
            var result = new List<string>();
            int position = 0;
            while (position < input.Length)
            {
                while (position < input.Length && char.IsWhiteSpace(input[position])) position++;
                if (position < input.Length) result.Add(ReadArgument(input, ref position));
            }
            return result.ToArray();
        }

        public static int FindParameterMarker(string input)
        {
            bool quoted = false;
            for (int i = 0; i + 1 < input.Length; i++)
            {
                if (input[i] == '"') quoted = !quoted;
                if (!quoted && input[i] == '-' && input[i + 1] == 'p' &&
                    (i == 0 || char.IsWhiteSpace(input[i - 1])) &&
                    (i + 2 == input.Length || char.IsWhiteSpace(input[i + 2]))) return i;
            }
            return -1;
        }

        public static (int Index, string Operator) FindConditionOperator(string condition, string[] operators)
        {
            bool quoted = false;
            int nesting = 0;
            (int, string) found = (-1, null);
            for (int i = 0; i < condition.Length; i++)
            {
                char ch = condition[i];
                if (ch == '"') { quoted = !quoted; continue; }
                if (quoted) continue;
                if (ch == '(') { nesting++; continue; }
                if (ch == ')')
                {
                    if (--nesting < 0) throw new FormatException("Unmatched ')' in condition.");
                    continue;
                }
                if (nesting != 0 || found.Item1 >= 0) continue;
                foreach (string op in operators)
                {
                    if (i + op.Length > condition.Length ||
                        !condition.AsSpan(i, op.Length).Equals(op.AsSpan(), StringComparison.OrdinalIgnoreCase)) continue;
                    if (char.IsLetter(op[0]) &&
                        (i == 0 || !char.IsWhiteSpace(condition[i - 1]) ||
                         i + op.Length == condition.Length || !char.IsWhiteSpace(condition[i + op.Length]))) continue;
                    found = (i, op);
                    break;
                }
            }
            if (quoted) throw new FormatException("Unclosed double quote in condition.");
            if (nesting != 0) throw new FormatException("Unclosed '(' in condition.");
            return found;
        }

        // Inspect both branches without interpolation or evaluation. Runtime values
        // and short-circuiting must never execute commands during editor analysis.
        public static void ValidateCondition(string condition, int depth = 0)
        {
            if (depth > 128) throw new FormatException("Condition nesting is too deep.");
            condition = condition.Trim();
            foreach (string logical in new[] { "||", "&&" })
            {
                var (index, _) = FindConditionOperator(condition, new[] { logical });
                if (index < 0) continue;
                string left = condition[..index].Trim();
                string right = condition[(index + 2)..].Trim();
                if (left.Length == 0 || right.Length == 0)
                    throw new FormatException($"'{logical}' requires two conditions.");
                ValidateCondition(left, depth + 1);
                ValidateCondition(right, depth + 1);
                return;
            }
            if (condition.StartsWith("not", StringComparison.OrdinalIgnoreCase) &&
                (condition.Length == 3 || char.IsWhiteSpace(condition[3]) || condition[3] == '('))
            {
                if (condition.Length == 3) throw new FormatException("'not' requires a condition.");
                ValidateCondition(condition[3..], depth + 1);
                return;
            }
            if (condition.StartsWith('(') && condition.EndsWith(')'))
            {
                ValidateCondition(condition[1..^1], depth + 1);
                return;
            }
            var (opStart, op) = FindConditionOperator(condition,
                new[] { "==", "!=", ">=", "<=", ">", "<", "contains", "startswith", "endswith" });
            if (opStart >= 0 && (condition[..opStart].Trim().Length == 0 || condition[(opStart + op.Length)..].Trim().Length == 0))
                throw new FormatException($"'{op}' requires two operands.");
        }

        public static List<(int Line, string Message)> Validate(IReadOnlyList<string> lines)
        {
            var errors = new List<(int, string)>();
            var blocks = new Stack<(string Type, int Line, bool FinalBranch)>();
            var functions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith('#')) continue;
                string keyword = Keyword(line);
                string rest = line[keyword.Length..].Trim();
                void Error(string message) => errors.Add((i + 1, message));

                switch (keyword)
                {
                    case "if": case "while": case "loop": case "each": case "func": case "try":
                        if (keyword != "try" && rest.Length == 0)
                            Error($"'{keyword}' missing {(keyword == "func" ? "function name" : "expression")}.");
                        if (keyword == "func" && rest.Length > 0 && !functions.Add(rest))
                            Error($"Function '{rest}' is already defined.");
                        blocks.Push((keyword, i + 1, false));
                        break;
                    case "end":
                        if (blocks.Count == 0) Error("'end' without matching block opener.");
                        else blocks.Pop();
                        break;
                    case "elif": case "else": case "catch":
                        string expected = keyword == "catch" ? "try" : "if";
                        if (blocks.Count == 0 || blocks.Peek().Type != expected)
                            Error($"'{keyword}' without matching '{expected}'.");
                        else
                        {
                            var block = blocks.Pop();
                            if (block.FinalBranch) Error($"'{keyword}' cannot follow '{(expected == "if" ? "else" : "catch")}'.");
                            blocks.Push((block.Type, block.Line, block.FinalBranch || keyword != "elif"));
                        }
                        if (keyword == "elif" && rest.Length == 0) Error("'elif' missing condition.");
                        break;
                    case "set": case "capture": case "read": case "input":
                        int eq = rest.IndexOf('=');
                        if (eq < 0) Error($"'{keyword}' missing '=' assignment.");
                        else if (!Regex.IsMatch(rest[..eq].Trim(), @"^\w+$"))
                            Error($"'{keyword}' requires a valid variable name before '='.");
                        break;
                    case "break": case "continue":
                        if (!blocks.TakeWhile(b => b.Type != "func").Any(b => b.Type is "loop" or "each" or "while"))
                            Error($"'{keyword}' outside of a loop.");
                        break;
                    case "return":
                        if (!blocks.Any(b => b.Type == "func")) Error("'return' outside of a function.");
                        break;
                }
            }
            foreach (var block in blocks)
                errors.Add((block.Line, $"'{block.Type}' block never closed with 'end'."));
            return errors;
        }
    }
}
