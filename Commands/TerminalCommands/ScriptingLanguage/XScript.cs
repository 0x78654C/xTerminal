using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Core;

namespace Commands.TerminalCommands.ScriptingLanguage
{
    [SupportedOSPlatform("Windows")]
    public class XTScript : ITerminalCommand
    {
        /*
            xt — TermXT Script: a simple, easy-to-use scripting language for xTerminal.

            Runs .xt script files containing xTerminal commands with variables,
            conditionals, loops, functions, error handling and output capture.
            Every xTerminal command works as-is inside a script.
        */
        private static readonly Version s_version = new(1, 0, 2);
        public string Name => "xt";

        private static readonly string s_helpMessage = @"Usage of xt command:
    xt <script.xt>              : Run a TermXT Script file.
    xt <script.xt> -p <args>    : Run with parameters ({1}, {2}... in script).
    xt -h                       : Display this help message.
    xt -new <script.xt>         : Create a new empty script template.
    xt -edit <script.xt>        : Open script in the built-in Vim-style TermXT editor.
    xt -check <script.xt>       : Validate script syntax without running.
    xt -ver                     : Display TermXT version.

TermXT Script Language Reference:
    # comment                          : Line comment.
    set <var> = <value>                : Set a variable.
    set <var> = eval <expr>            : Math expression (+ - * / % with parentheses).
    set <var> = upper <text>           : Convert to uppercase.
    set <var> = lower <text>           : Convert to lowercase.
    set <var> = len <text>             : Get string length.
    set <var> = substr <text> <s> <l>  : Substring (start, length).
    set <var> = replace <t> <old> <new>: Replace text.
    set <var> = trim <text>            : Trim whitespace.
    print ""text with {var}""            : Print with variable interpolation.
    run <command>                      : Run any xTerminal command (supports pipes).
    capture <var> = <command>           : Run command, store stdout in variable (supports pipes).
    input <var> = ""prompt""             : Read user input into a variable.
    if <a> <op> <b> / elif / else / end : Conditional block.
       Operators: ==  !=  >  <  >=  <=  contains  startswith  endswith
       Logical:   not, then && (and), then || (or); parentheses group conditions.
       Operators outside quoted text support compact forms such as {n}>=2&&{n}<5.
    loop <n> / end                     : Repeat block N times. {i} = iteration.
    while <condition> / end            : Repeat while condition is true.
    each <var> in <a,b,c> / end        : Iterate comma-separated values.
    each <var> in <start>..<end> / end  : Iterate a numeric range (inclusive).
    each <var> in lines:<varname> / end : Iterate over lines of a variable.
    func <name> / end                  : Define a reusable function.
    call <name> [args]                 : Call function. {1},{2}.. for args.
    return <value>                     : Return value from function (stored in {result}).
    try / catch / end                  : Error handling.
    break                              : Exit current loop.
    continue                           : Skip to next loop iteration.
    read <var> = <file>                    : Read file contents into a variable.
    write <file> ""text""                  : Write text to file (overwrite).
    append <file> ""text""                 : Append text to file.
    wait <ms>                          : Pause execution in milliseconds.
    exit                               : Stop script.

Built-in variables:
    {DATE}   : Current date (yyyy-MM-dd).
    {TIME}   : Current time (HH:mm:ss).
    {USER}   : Current username.
    {PC}     : Computer name.
    {CWD}    : Current working directory.
    {i}      : Current loop iteration (1-based).
    {argc}   : Number of arguments in the current script or function call.
    {result} : Return value from last function call.
    {error}  : Set to ""true"" when last command failed.
    {error_message} : Error message from last caught exception.

Examples:
    xt deploy.xt
    xt deploy.xt -p production 8080
    xt -new myscript.xt

Script file example (deploy.xt):
    set env = {1}
    set port = {2}
    print ""Deploying to {env} on port {port}""
    run time
    each host in server1,server2,server3
        print ""Checking {host}""
        run ping {host}
    end

    # Math
    set total = eval 10 + {port} * 2
    print ""Computed: {total}""

    # String ops
    set upper_env = upper {env}
    set envLen = len {env}
    print ""{upper_env} has {envLen} characters""

    # While loop
    set countdown = 3
    while {countdown} > 0
        print ""T-{countdown}""
        set countdown = eval {countdown} - 1
    end

    # Iterate captured output
    capture files = ls
    each f in lines:{files}
        if {f} contains .exe
            print ""Executable: {f}""
        end
    end

    # Pipe in capture
    capture exeFiles = ls | cat -s exe
    print ""{exeFiles}""

    print ""Done!""
";

        private static readonly string s_template = @"# ── xTermXT Script template ────────────────────────
# Created: {DATE}

# Variables
set name = ""my-script""

# Main logic
print ""Running {name}...""
run time

# Math example
set x = eval 2 + 3 * 4
print ""Result: {x}""

# String example
set greeting = upper hello world
print ""{greeting}""

print ""Done!""
";

        public void Execute(string args)
        {
            GlobalVariables.isErrorCommand = false;
            try
            {
                if (args == $"{Name} -h") { Console.WriteLine(s_helpMessage); return; }
                if (args == Name) { FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!"); return; }

                string currentDir = File.ReadAllText(GlobalVariables.currentDirectory).Trim();
                string rest = args.Substring(Name.Length).TrimStart();

                if (rest.StartsWith("-ver"))
                {
                    FileSystem.SuccessWriteLine($"TermXT version: {s_version}");
                    return;
                }

                // xt -new <file>
                if (rest.StartsWith("-new "))
                {
                    string newFile = FileSystem.SanitizePath(TermXtSyntax.Unquote(rest.Substring(5).Trim()), currentDir);
                    File.WriteAllText(newFile, s_template.Replace("{DATE}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    FileSystem.SuccessWriteLine($"Script template created: {newFile}");
                    return;
                }

                // xt -edit <file>
                if (rest.StartsWith("-edit", StringComparison.OrdinalIgnoreCase))
                {
                    string editorArgs = rest.Length > 5 ? rest.Substring(5).TrimStart() : string.Empty;
                    TermXTEditorCommand.OpenFromArguments(editorArgs, currentDir);
                    return;
                }

                // xt -check <file>
                if (rest.StartsWith("-check "))
                {
                    string checkFile = FileSystem.SanitizePath(TermXtSyntax.Unquote(rest.Substring(7).Trim()), currentDir);
                    GlobalVariables.isErrorCommand = CheckScript(checkFile) > 0;
                    return;
                }

                // xt <file> [-p <args>]
                string[] scriptArgs = Array.Empty<string>();
                string scriptPath;

                int pIdx = TermXtSyntax.FindParameterMarker(rest);
                if (pIdx >= 0)
                {
                    scriptPath = FileSystem.SanitizePath(TermXtSyntax.Unquote(rest[..pIdx].Trim()), currentDir);
                    string paramStr = rest[(pIdx + 2)..].Trim();
                    scriptArgs = ParseArgs(paramStr);
                }
                else
                {
                    scriptPath = FileSystem.SanitizePath(TermXtSyntax.Unquote(rest.Trim()), currentDir);
                }

                if (!File.Exists(scriptPath))
                {
                    FileSystem.ErrorWriteLine($"Script not found: {scriptPath}");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }

                // Auto-validate before running — abort if syntax errors found.
                if (CheckScript(scriptPath, silent: true) > 0)
                {
                    GlobalVariables.isErrorCommand = true;
                    return;
                }

                var engine = new ScriptEngine(scriptPath, scriptArgs);
                engine.Run();
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine(ex.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }

        private static string[] ParseArgs(string input)
        {
            return TermXtSyntax.ParseArguments(input);
        }

        /// <summary>
        /// Validates script syntax. Returns the number of errors found.
        /// When silent is true, no success message is printed (used for auto-check before run).
        /// </summary>
        private static int CheckScript(string path, bool silent = false)
        {
            if (!File.Exists(path))
            {
                FileSystem.ErrorWriteLine($"Script not found: {path}");
                return 1;
            }

            string[] lines = File.ReadAllLines(path);
            var diagnostics = TermXtSyntax.Validate(lines);
            int errors = diagnostics.Count;
            foreach (var diagnostic in diagnostics)
            {
                FileSystem.ColorConsoleText(ConsoleColor.Red, $"  Line {diagnostic.Line}: ");
                Console.WriteLine(diagnostic.Message);
            }

            if (errors == 0 && !silent)
                FileSystem.ColorConsoleText(ConsoleColor.Green, $"  ✓ Script '{path}' is valid ({lines.Length} lines).\n");
            else if (errors > 0)
                FileSystem.ColorConsoleText(ConsoleColor.Red, $"\n  ✗ Found {errors} error(s). Script not executed.\n");

            return errors;
        }

        // ── Script Engine ────────────────────────────────────────────────────

        private sealed class ScriptEngine
        {
            private readonly string[] _lines;
            private readonly Dictionary<string, string> _vars = new(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, (int Start, int End)> _funcs = new(StringComparer.OrdinalIgnoreCase);
            private int _pc;
            private bool _stopRequested;
            private bool _breakRequested;
            private bool _continueRequested;
            private bool _returnRequested;
            private int _tryDepth;
            private int _callDepth;
            private int _loopDepth;


            private readonly DataTable _calc = new DataTable();

            public ScriptEngine(string path, string[] scriptArgs)
            {
                _lines = File.ReadAllLines(path);
                for (int i = 0; i < scriptArgs.Length; i++)
                    _vars[$"{i + 1}"] = scriptArgs[i];

                _vars["USER"] = GlobalVariables.accountName;
                _vars["PC"] = GlobalVariables.computerName;
                _vars["result"] = "";
                _vars["argc"] = scriptArgs.Length.ToString(CultureInfo.InvariantCulture);
                _vars["error"] = "false";
                _vars["error_message"] = "";

                PreScanFunctions();
            }

            public void Run()
            {
                try { ExecuteBlock(0, _lines.Length - 1); }
                finally { _calc.Dispose(); }
            }

            private void ExecuteBlock(int start, int end)
            {
                _pc = start;
                while (_pc <= end && !_stopRequested && !_breakRequested && !_continueRequested && !_returnRequested)
                {
                    string raw = _lines[_pc].Trim();

                    if (string.IsNullOrEmpty(raw) || raw.StartsWith('#'))
                    {
                        _pc++;
                        continue;
                    }

                    string line = Interpolate(raw);
                    string keyword = TermXtSyntax.Keyword(line);

                    switch (keyword)
                    {
                        case "set": ExecSet(line); _pc++; break;
                        case "print": ExecPrint(raw); _pc++; break;
                        case "run": ExecRun(line); _pc++; break;
                        case "capture": ExecCapture(line); _pc++; break;
                        case "input": ExecInput(line); _pc++; break;
                        case "read": ExecRead(line); _pc++; break;
                        case "wait": ExecWait(line); _pc++; break;
                        case "write": ExecFileWrite(raw, append: false); _pc++; break;
                        case "append": ExecFileWrite(raw, append: true); _pc++; break;
                        case "exit": _stopRequested = true; break;
                        case "break": _breakRequested = true; _pc++; break;
                        case "continue": _continueRequested = true; _pc++; break;
                        case "return": ExecReturn(line); break;
                        case "if": ExecIf(end); break;
                        case "loop": ExecuteLoop(ExecLoop, end); break;
                        case "while": ExecuteLoop(ExecWhile, end); break;
                        case "each": ExecuteLoop(ExecEach, end); break;
                        case "call": ExecCall(raw); _pc++; break;
                        case "try": ExecTry(end); break;
                        case "func": SkipBlock(); break;
                        default:
                            ExecRun("run " + line);
                            _pc++;
                            break;
                    }
                }
            }

            // ── Variable interpolation ───────────────────────────────────────

            private string Interpolate(string text)
            {
                return Regex.Replace(text, @"\{(\w+)\}", m =>
                {
                    string key = m.Groups[1].Value;

                    // Dynamic built-in variables — always return current values.
                    if (key.Equals("DATE", StringComparison.OrdinalIgnoreCase))
                        return DateTime.Now.ToString("yyyy-MM-dd");
                    if (key.Equals("TIME", StringComparison.OrdinalIgnoreCase))
                        return DateTime.Now.ToString("HH:mm:ss");
                    if (key.Equals("CWD", StringComparison.OrdinalIgnoreCase))
                        return File.ReadAllText(GlobalVariables.currentDirectory).Trim();

                    return _vars.TryGetValue(key, out string val) ? val : m.Value;
                });
            }

            // ── Keyword implementations ──────────────────────────────────────

            private void ExecSet(string line)
            {
                int eq = line.IndexOf('=');
                if (eq < 0) { PrintError(_pc + 1, "'set' missing '='"); return; }
                string varName = line[3..eq].Trim();
                string rhs = line[(eq + 1)..].Trim();
                _vars[varName] = EvalRhs(rhs);
            }

            private string EvalRhs(string rhs)
            {
                if (rhs.StartsWith("eval ", StringComparison.OrdinalIgnoreCase))
                    return EvalMath(rhs[5..].Trim());
                if (rhs.StartsWith("upper ", StringComparison.OrdinalIgnoreCase))
                    return rhs[6..].Trim().Trim('"').ToUpper();
                if (rhs.StartsWith("lower ", StringComparison.OrdinalIgnoreCase))
                    return rhs[6..].Trim().Trim('"').ToLower();
                if (rhs.StartsWith("len ", StringComparison.OrdinalIgnoreCase))
                    return rhs[4..].Trim().Trim('"').Length.ToString();
                if (rhs.StartsWith("trim ", StringComparison.OrdinalIgnoreCase))
                    return rhs[5..].Trim().Trim('"').Trim();
                if (rhs.StartsWith("substr ", StringComparison.OrdinalIgnoreCase))
                    return EvalSubstr(rhs[7..].Trim());
                if (rhs.StartsWith("replace ", StringComparison.OrdinalIgnoreCase))
                    return EvalReplace(rhs[8..].Trim());
                return rhs.Trim('"');
            }

            private string EvalMath(string expression)
            {
                try
                {
                    if (!Regex.IsMatch(expression, @"^[\d\s\+\-\*\/\%\(\)\.]+$"))
                    {
                        throw new FormatException("Math expression contains invalid characters.");
                    }
                    object result = _calc.Compute(expression, null);
                    double value = Convert.ToDouble(result, CultureInfo.InvariantCulture);
                    if (!double.IsFinite(value)) throw new ArithmeticException("Math result is not finite.");
                    return value.ToString(CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {
                    PrintError(_pc + 1, $"Math error: {ex.Message} in '{expression}'");
                    return "0";
                }
            }

            private string EvalSubstr(string args)
            {
                string[] tokens = TermXtSyntax.ParseArguments(args);
                if (tokens.Length < 3)
                {
                    PrintError(_pc + 1, "substr expects: substr <text> <start> <length>");
                    return args;
                }
                if (!int.TryParse(tokens[^1], out int length) || !int.TryParse(tokens[^2], out int start))
                {
                    PrintError(_pc + 1, "substr: start and length must be numbers.");
                    return args;
                }
                string text = string.Join(' ', tokens.Take(tokens.Length - 2)).Trim('"');
                if (length < 0)
                {
                    PrintError(_pc + 1, "substr: length must not be negative.");
                    return "";
                }
                if (start < 0) start = 0;
                if (start >= text.Length) return "";
                if (length > text.Length - start) length = text.Length - start;
                return text.Substring(start, length);
            }

            private string EvalReplace(string args)
            {
                var parts = ParseQuotedTokens(args);
                if (parts.Count >= 3)
                    return parts[0].Replace(parts[1], parts[2], StringComparison.OrdinalIgnoreCase);
                PrintError(_pc + 1, "replace expects: replace <text> <old> <new>");
                return args;
            }

            private static List<string> ParseQuotedTokens(string input)
            {
                return TermXtSyntax.ParseArguments(input).ToList();
            }

            private void ExecPrint(string rawLine)
            {
                string text = rawLine[5..].Trim().Trim('"');
                text = Interpolate(text);
                text = ProcessEscapes(text);
                Console.WriteLine(text);
            }

            private static string ProcessEscapes(string text)
            {
                if (string.IsNullOrEmpty(text))
                    return text;

                var result = new System.Text.StringBuilder(text.Length);

                for (int i = 0; i < text.Length; i++)
                {
                    if (IsWindowsPathStart(text, i))
                    {
                        int end = FindWindowsPathEnd(text, i);
                        result.Append(text, i, end - i);
                        i = end - 1;
                        continue;
                    }

                    if (text[i] == '\\' && i + 1 < text.Length)
                    {
                        switch (text[i + 1])
                        {
                            case '\\':
                                result.Append('\\');
                                i++;
                                continue;
                            case 'n':
                                result.Append('\n');
                                i++;
                                continue;
                            case 't':
                                result.Append('\t');
                                i++;
                                continue;
                        }
                    }

                    result.Append(text[i]);
                }

                return result.ToString();
            }

            private static bool IsWindowsPathStart(string text, int index)
            {
                bool boundary = index == 0 ||
                    char.IsWhiteSpace(text[index - 1]) ||
                    text[index - 1] == '"' ||
                    text[index - 1] == '\'' ||
                    text[index - 1] == '(';

                if (!boundary)
                    return false;

                if (index + 2 < text.Length &&
                    char.IsLetter(text[index]) &&
                    text[index + 1] == ':' &&
                    (text[index + 2] == '\\' || text[index + 2] == '/'))
                {
                    return true;
                }

                return index + 1 < text.Length &&
                    text[index] == '\\' &&
                    text[index + 1] == '\\';
            }

            private static int FindWindowsPathEnd(string text, int start)
            {
                int end = start;
                while (end < text.Length && !char.IsWhiteSpace(text[end]))
                    end++;

                return end;
            }

            private void ExecRun(string line)
            {
                string cmdLine = line[3..].Trim();
                if (string.IsNullOrEmpty(cmdLine)) return;

                ResetCommandState();
                ExecutePipeline(cmdLine);
                _vars["error"] = GlobalVariables.isErrorCommand ? "true" : "false";
                if (GlobalVariables.isErrorCommand)
                    PrintError(_pc + 1, $"Command failed: {cmdLine}");
                else _vars["error_message"] = "";
            }

            private void ExecCapture(string line)
            {
                int eq = line.IndexOf('=');
                if (eq < 0) { PrintError(_pc + 1, "'capture' missing '='"); return; }
                string varName = line[7..eq].Trim();
                string cmdLine = line[(eq + 1)..].Trim();

                ResetCommandState();
                TextWriter originalOut = Console.Out;
                string captured;
                using (var sw = new StringWriter())
                {
                    Console.SetOut(sw);
                    try
                    {
                        ExecutePipeline(cmdLine);
                    }
                    finally
                    {
                        Console.SetOut(originalOut);
                    }
                    captured = sw.ToString().Trim();
                }

                if (string.IsNullOrEmpty(captured) && !string.IsNullOrEmpty(GlobalVariables.pipeCmdOutput))
                    captured = GlobalVariables.pipeCmdOutput.Trim();

                _vars[varName] = captured;
                _vars["error"] = GlobalVariables.isErrorCommand ? "true" : "false";

                GlobalVariables.pipeCmdOutput = string.Empty;
                GlobalVariables.pipeCmdCount = 0;
                GlobalVariables.pipeCmdCountTemp = 0;
                GlobalVariables.isPipeCommand = false;
                if (GlobalVariables.isErrorCommand)
                    PrintError(_pc + 1, $"Command failed: {cmdLine}");
                else _vars["error_message"] = "";
            }

            private void ExecInput(string line)
            {
                int eq = line.IndexOf('=');
                if (eq < 0) { PrintError(_pc + 1, "'input' missing '='"); return; }
                string varName = line[5..eq].Trim();
                string prompt = line[(eq + 1)..].Trim().Trim('"');
                FileSystem.ColorConsoleText(ConsoleColor.Cyan, $"  {prompt}: ");
                string val = Console.ReadLine() ?? string.Empty;
                _vars[varName] = val.Trim();
            }

            private void ExecWait(string line)
            {
                string msStr = line[4..].Trim();
                if (int.TryParse(msStr, out int ms) && ms > 0)
                    System.Threading.Thread.Sleep(ms);
            }

            private void ExecRead(string line)
            {
                int eq = line.IndexOf('=');
                if (eq < 0) { PrintError(_pc + 1, "'read' missing '='"); return; }
                string varName = line[4..eq].Trim();
                string filePath = line[(eq + 1)..].Trim().Trim('"');

                string currentDir = File.ReadAllText(GlobalVariables.currentDirectory).Trim();
                filePath = FileSystem.SanitizePath(filePath, currentDir);

                if (!File.Exists(filePath))
                {
                    PrintError(_pc + 1, $"File not found: {filePath}");
                    return;
                }

                _vars[varName] = File.ReadAllText(filePath);
                _vars["error"] = "false";
                _vars["error_message"] = "";
                GlobalVariables.isErrorCommand = false;
            }

            private void ExecFileWrite(string rawLine, bool append)
            {
                string keyword = append ? "append" : "write";
                string rest = rawLine[keyword.Length..].Trim();

                int position = 0;
                string filePath = Interpolate(TermXtSyntax.ReadArgument(rest, ref position));
                if (position >= rest.Length)
                {
                    PrintError(_pc + 1, $"'{keyword}' expects: {keyword} <file> \"text\"");
                    return;
                }

                string text = TermXtSyntax.Unquote(rest[position..].Trim());
                text = ProcessEscapes(text);
                text = Interpolate(text);

                string currentDir = File.ReadAllText(GlobalVariables.currentDirectory).Trim();
                filePath = FileSystem.SanitizePath(filePath, currentDir);


                if (append)
                    File.AppendAllText(filePath, text + Environment.NewLine);
                else
                    File.WriteAllText(filePath, text + Environment.NewLine);
                GlobalVariables.isErrorCommand = false;
                _vars["error"] = "false";
                _vars["error_message"] = "";
            }

            private void ExecReturn(string line)
            {
                string val = line.Length > 6 ? line[6..].Trim().Trim('"') : "";
                _vars["result"] = val;
                _returnRequested = true;
                _pc++;
            }

            // ── If / elif / else / end ───────────────────────────────────────

            private void ExecIf(int blockEnd)
            {
                var branches = new List<(int Line, string Type, string Condition)>();
                int endLine = FindMatchingEnd(_pc, blockEnd, branches);

                bool executed = false;
                for (int b = 0; b < branches.Count && !executed && !_stopRequested; b++)
                {
                    var (bLine, bType, bCond) = branches[b];
                    int bodyStart = bLine + 1;
                    int bodyEnd = b + 1 < branches.Count ? branches[b + 1].Line - 1 : endLine - 1;

                    if (bType == "else")
                    {
                        ExecuteBlock(bodyStart, bodyEnd);
                        executed = true;
                    }
                    else
                    {
                        if (EvalCondition(bCond))
                        {
                            ExecuteBlock(bodyStart, bodyEnd);
                            executed = true;
                        }
                    }
                }

                _pc = endLine + 1;
            }

            private int FindMatchingEnd(int start, int blockEnd, List<(int, string, string)> branches)
            {
                string firstLine = _lines[start].Trim();
                string firstKeyword = TermXtSyntax.Keyword(firstLine);
                string firstCond = firstLine.Length > firstKeyword.Length ? firstLine[(firstKeyword.Length + 1)..].Trim() : "";
                branches.Add((start, firstKeyword, firstCond));

                int depth = 1;
                int i = start + 1;
                while (i <= blockEnd && depth > 0)
                {
                    string trimmed = _lines[i].Trim();
                    string kw = TermXtSyntax.Keyword(trimmed);

                    if (kw is "if" or "loop" or "each" or "func" or "try" or "while")
                        depth++;
                    else if (kw == "end")
                    {
                        depth--;
                        if (depth == 0) return i;
                    }
                    else if (depth == 1 && (kw == "elif" || kw == "else" || kw == "catch"))
                    {
                        string cond = trimmed.Length > kw.Length ? trimmed[(kw.Length + 1)..].Trim() : "";
                        branches.Add((i, kw, cond));
                    }
                    i++;
                }
                return i;
            }

            private bool EvalCondition(string condition, int depth = 0)
            {
                if (depth > 128) throw new FormatException("Condition nesting is too deep.");
                condition = condition.Trim();

                // Parse syntax before interpolation: operators inside variable values are data.
                foreach (string logical in new[] { "||", "&&" })
                {
                    var (index, _) = FindConditionOperator(condition, new[] { logical });
                    if (index >= 0)
                    {
                        string leftCondition = condition[..index].Trim();
                        string rightCondition = condition[(index + 2)..].Trim();
                        if (leftCondition.Length == 0 || rightCondition.Length == 0)
                            throw new FormatException($"'{logical}' requires two conditions.");
                        return logical == "||"
                            ? EvalCondition(leftCondition, depth + 1) || EvalCondition(rightCondition, depth + 1)
                            : EvalCondition(leftCondition, depth + 1) && EvalCondition(rightCondition, depth + 1);
                    }
                }

                if (condition.StartsWith("not", StringComparison.OrdinalIgnoreCase) &&
                    (condition.Length == 3 || char.IsWhiteSpace(condition[3]) || condition[3] == '('))
                {
                    if (condition.Length == 3) throw new FormatException("'not' requires a condition.");
                    return !EvalCondition(condition[3..], depth + 1);
                }
                if (condition.StartsWith('(') && condition.EndsWith(')'))
                    return EvalCondition(condition[1..^1], depth + 1);

                var (opStart, foundOp) = FindConditionOperator(condition,
                    new[] { "==", "!=", ">=", "<=", ">", "<", "contains", "startswith", "endswith" });
                if (opStart < 0)
                {
                    string val = Interpolate(TermXtSyntax.Unquote(condition));
                    return val.Length > 0 && val != "0" && !val.Equals("false", StringComparison.OrdinalIgnoreCase);
                }

                string leftRaw = condition[..opStart].Trim();
                string rightRaw = condition[(opStart + foundOp.Length)..].Trim();
                if (leftRaw.Length == 0 || rightRaw.Length == 0)
                    throw new FormatException($"'{foundOp}' requires two operands.");
                string left = Interpolate(TermXtSyntax.Unquote(leftRaw));
                string right = Interpolate(TermXtSyntax.Unquote(rightRaw));
                double numL = 0, numR = 0;
                bool isNumeric = double.TryParse(left, NumberStyles.Float, CultureInfo.InvariantCulture, out numL)
                              && double.TryParse(right, NumberStyles.Float, CultureInfo.InvariantCulture, out numR);

                return foundOp switch
                {
                    "==" => left.Equals(right, StringComparison.OrdinalIgnoreCase),
                    "!=" => !left.Equals(right, StringComparison.OrdinalIgnoreCase),
                    ">" => isNumeric ? numL > numR : string.Compare(left, right, StringComparison.OrdinalIgnoreCase) > 0,
                    "<" => isNumeric ? numL < numR : string.Compare(left, right, StringComparison.OrdinalIgnoreCase) < 0,
                    ">=" => isNumeric ? numL >= numR : string.Compare(left, right, StringComparison.OrdinalIgnoreCase) >= 0,
                    "<=" => isNumeric ? numL <= numR : string.Compare(left, right, StringComparison.OrdinalIgnoreCase) <= 0,
                    "contains" => left.Contains(right, StringComparison.OrdinalIgnoreCase),
                    "startswith" => left.StartsWith(right, StringComparison.OrdinalIgnoreCase),
                    "endswith" => left.EndsWith(right, StringComparison.OrdinalIgnoreCase),
                    _ => false
                };
            }

            private static (int Index, string Operator) FindConditionOperator(string condition, string[] operators)
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

            // ── Loop N / end ─────────────────────────────────────────────────

            private void ExecuteLoop(Action<int> execute, int blockEnd)
            {
                bool hadIteration = _vars.TryGetValue("i", out string savedIteration);
                _loopDepth++;
                try { execute(blockEnd); }
                finally
                {
                    _loopDepth--;
                    if (_loopDepth > 0)
                    {
                        if (hadIteration) _vars["i"] = savedIteration;
                        else _vars.Remove("i");
                    }
                    _breakRequested = false;
                    _continueRequested = false;
                }
            }

            private void ExecLoop(int blockEnd)
            {
                string line = Interpolate(_lines[_pc].Trim());
                string countStr = line[4..].Trim();
                if (!int.TryParse(countStr, out int count) || count <= 0)
                {
                    PrintError(_pc + 1, $"'loop' expects a positive number, got '{countStr}'");
                    SkipBlock();
                    return;
                }
                var branches = new List<(int, string, string)>();
                int endLine = FindMatchingEnd(_pc, blockEnd, branches);
                int bodyStart = _pc + 1;
                int bodyEnd = endLine - 1;

                for (long iter = 1; iter <= count && !_stopRequested; iter++)
                {
                    _vars["i"] = iter.ToString();
                    _continueRequested = false;
                    ExecuteBlock(bodyStart, bodyEnd);

                    if (_breakRequested) { _breakRequested = false; break; }
                    if (_returnRequested) break;
                }
                _continueRequested = false;

                _pc = endLine + 1;
            }

            // ── While <condition> / end ──────────────────────────────────────

            private void ExecWhile(int blockEnd)
            {
                int condLine = _pc;
                var branches = new List<(int, string, string)>();
                int endLine = FindMatchingEnd(_pc, blockEnd, branches);
                int bodyStart = _pc + 1;
                int bodyEnd = endLine - 1;

                long iter = 0;

                while (!_stopRequested)
                {
                    iter++;
                    string condRaw = _lines[condLine].Trim();
                    string condStr = condRaw.Length > 5 ? condRaw[5..].Trim() : "";

                    if (!EvalCondition(condStr))
                        break;

                    _vars["i"] = iter.ToString();
                    _continueRequested = false;
                    ExecuteBlock(bodyStart, bodyEnd);

                    if (_breakRequested) { _breakRequested = false; break; }
                    if (_returnRequested) break;
                }
                _continueRequested = false;

                _pc = endLine + 1;
            }

            // ── Each <var> in <a,b,c> / end ──────────────────────────────────

            private void ExecEach(int blockEnd)
            {
                string raw = _lines[_pc].Trim();

                // Check for lines:<varname> pattern BEFORE interpolation
                var linesMatch = Regex.Match(raw, @"^each\s+(\w+)\s+in\s+lines:\{?(\w+)\}?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (linesMatch.Success)
                {
                    string varName = linesMatch.Groups[1].Value;
                    string srcVar = linesMatch.Groups[2].Value;
                    string multiline = _vars.TryGetValue(srcVar, out string v) ? v : "";
                    string[] values = multiline.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    var branches = new List<(int, string, string)>();
                    int endLine = FindMatchingEnd(_pc, blockEnd, branches);
                    int bodyStart = _pc + 1;
                    int bodyEnd = endLine - 1;

                    int idx = 0;
                    foreach (string val in values)
                    {
                        if (_stopRequested) break;
                        idx++;
                        _vars[varName] = val.Trim();
                        _vars["i"] = idx.ToString();
                        _continueRequested = false;
                        ExecuteBlock(bodyStart, bodyEnd);

                        if (_breakRequested) { _breakRequested = false; break; }
                        if (_returnRequested) break;
                    }
                    _continueRequested = false;

                    _pc = endLine + 1;
                    return;
                }

                // Standard each: interpolate for comma-separated values
                string line = Interpolate(raw);
                var match = Regex.Match(line, @"^each\s+(\w+)\s+in\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (!match.Success)
                {
                    PrintError(_pc + 1, "Invalid 'each' syntax. Expected: each <var> in <a,b,c>");
                    SkipBlock();
                    return;
                }

                string eachVar = match.Groups[1].Value;
                string valuesRaw = match.Groups[2].Value.Trim();

                // Check for numeric range pattern: <start>..<end>
                var rangeMatch = Regex.Match(valuesRaw, @"^(-?\d+)\.\.(-?\d+)$");
                if (rangeMatch.Success)
                {
                    int rangeStart = int.Parse(rangeMatch.Groups[1].Value);
                    int rangeEnd = int.Parse(rangeMatch.Groups[2].Value);

                    int step = rangeStart <= rangeEnd ? 1 : -1;

                    var branchesRange = new List<(int, string, string)>();
                    int endLineRange = FindMatchingEnd(_pc, blockEnd, branchesRange);
                    int bodyStartRange = _pc + 1;
                    int bodyEndRange = endLineRange - 1;

                    long idxRange = 0;
                    for (long n = rangeStart; step > 0 ? n <= rangeEnd : n >= rangeEnd; n += step)
                    {
                        if (_stopRequested) break;
                        idxRange++;
                        _vars[eachVar] = n.ToString();
                        _vars["i"] = idxRange.ToString();
                        _continueRequested = false;
                        ExecuteBlock(bodyStartRange, bodyEndRange);

                        if (_breakRequested) { _breakRequested = false; break; }
                        if (_returnRequested) break;
                    }
                    _continueRequested = false;

                    _pc = endLineRange + 1;
                    return;
                }

                string[] items = valuesRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                var branchesStd = new List<(int, string, string)>();
                int endLineStd = FindMatchingEnd(_pc, blockEnd, branchesStd);
                int bodyStartStd = _pc + 1;
                int bodyEndStd = endLineStd - 1;

                int idxStd = 0;
                foreach (string val in items)
                {
                    if (_stopRequested) break;
                    idxStd++;
                    _vars[eachVar] = val.Trim('"').Trim();
                    _vars["i"] = idxStd.ToString();
                    _continueRequested = false;
                    ExecuteBlock(bodyStartStd, bodyEndStd);

                    if (_breakRequested) { _breakRequested = false; break; }
                    if (_returnRequested) break;
                }
                _continueRequested = false;

                _pc = endLineStd + 1;
            }

            // ── Functions ────────────────────────────────────────────────────

            private void PreScanFunctions()
            {
                for (int i = 0; i < _lines.Length; i++)
                {
                    string trimmed = _lines[i].Trim();
                    if (TermXtSyntax.Keyword(trimmed) == "func")
                    {
                        string funcName = trimmed[5..].Trim();
                        int depth = 1;
                        int j = i + 1;
                        while (j < _lines.Length && depth > 0)
                        {
                            string kw = TermXtSyntax.Keyword(_lines[j].Trim());
                            if (kw is "if" or "loop" or "each" or "func" or "try" or "while") depth++;
                            else if (kw == "end") depth--;
                            if (depth > 0) j++;
                        }
                        _funcs[funcName] = (i + 1, j - 1);
                    }
                }
            }

            private void ExecCall(string line)
            {
                string rest = line[4..].Trim();
                var parts = ParseQuotedTokens(rest).Select(Interpolate).ToList();
                if (parts.Count == 0) { PrintError(_pc + 1, "'call' missing function name."); return; }

                string funcName = parts[0];
                if (!_funcs.TryGetValue(funcName, out var range))
                {
                    PrintError(_pc + 1, $"Function '{funcName}' not found.");
                    return;
                }

                int argCount = parts.Count - 1;
                if (_callDepth >= 128)
                {
                    PrintError(_pc + 1, "Function call nesting exceeds the limit of 128.");
                    return;
                }
                var savedArgs = _vars.Where(kv => IsPositionalArgument(kv.Key)).ToArray();
                string savedArgc = _vars["argc"];
                int savedPc = _pc;
                _callDepth++;
                try
                {
                    foreach (var kv in savedArgs) _vars.Remove(kv.Key);
                    for (int a = 1; a <= argCount; a++)
                        _vars[a.ToString(CultureInfo.InvariantCulture)] = parts[a];
                    _vars["argc"] = argCount.ToString(CultureInfo.InvariantCulture);
                    _vars["result"] = "";
                    _returnRequested = false;
                    ExecuteBlock(range.Start, range.End);
                }
                finally
                {
                    foreach (string key in _vars.Keys.Where(IsPositionalArgument).ToArray())
                        _vars.Remove(key);
                    foreach (var kv in savedArgs) _vars[kv.Key] = kv.Value;
                    _vars["argc"] = savedArgc;
                    _returnRequested = false;
                    _pc = savedPc;
                    _callDepth--;
                }
            }

            private static bool IsPositionalArgument(string key) => key.Length > 0 && key.All(char.IsDigit);

            // ── Try / catch / end ────────────────────────────────────────────

            private void ExecTry(int blockEnd)
            {
                var branches = new List<(int, string, string)>();
                int endLine = FindMatchingEnd(_pc, blockEnd, branches);

                int tryBodyStart = _pc + 1;
                int tryBodyEnd = branches.Count > 1 ? branches[1].Item1 - 1 : endLine - 1;
                int catchBodyStart = branches.Count > 1 ? branches[1].Item1 + 1 : -1;
                int catchBodyEnd = endLine - 1;

                GlobalVariables.isErrorCommand = false;
                _vars["error"] = "false";
                _vars["error_message"] = "";
                bool failed = false;

                _tryDepth++;
                try
                {
                    ExecuteBlock(tryBodyStart, tryBodyEnd);
                }
                catch (Exception ex)
                {
                    failed = true;
                    GlobalVariables.isErrorCommand = true;
                    _vars["error"] = "true";
                    _vars["error_message"] = ex.Message;
                }
                finally { _tryDepth--; }

                if (failed && catchBodyStart >= 0)
                {
                    GlobalVariables.isErrorCommand = false;
                    ExecuteBlock(catchBodyStart, catchBodyEnd);
                }
                else if (failed && _tryDepth > 0)
                    throw new InvalidOperationException(_vars["error_message"]);

                _vars["error"] = GlobalVariables.isErrorCommand ? "true" : "false";
                _pc = endLine + 1;
            }

            // ── Helpers ──────────────────────────────────────────────────────

            private void SkipBlock()
            {
                int depth = 1;
                _pc++;
                while (_pc < _lines.Length && depth > 0)
                {
                    string kw = TermXtSyntax.Keyword(_lines[_pc].Trim());
                    if (kw is "if" or "loop" or "each" or "func" or "try" or "while") depth++;
                    else if (kw == "end") depth--;
                    _pc++;
                }
            }

            private static void ResetCommandState()
            {
                GlobalVariables.isPipeCommand = false;
                GlobalVariables.pipeCmdOutput = string.Empty;
                GlobalVariables.pipeCmdCount = 0;
                GlobalVariables.pipeCmdCountTemp = 0;
                GlobalVariables.aliasInParameter.Clear();
                GlobalVariables.isErrorCommand = false;
            }

            private void PrintError(int lineNum, string msg)
            {
                GlobalVariables.isErrorCommand = true;
                _vars["error"] = "true";
                _vars["error_message"] = msg;
                FileSystem.ColorConsoleText(ConsoleColor.Red, $"  [xt line {lineNum}] ");
                Console.WriteLine(msg);
                if (_tryDepth > 0) throw new InvalidOperationException(msg);
            }

            /// <summary>
            /// Splits a command line on single pipe <c>|</c> characters,
            /// leaving double-pipe <c>||</c> sequences intact.
            /// </summary>
            private static string[] SplitPipes(string cmdLine)
            {
                var segments = new List<string>();
                int segStart = 0;
                bool quoted = false;
                for (int i = 0; i < cmdLine.Length; i++)
                {
                    if (cmdLine[i] == '"') quoted = !quoted;
                    if (!quoted && cmdLine[i] == '|')
                    {
                        if (i + 1 < cmdLine.Length && cmdLine[i + 1] == '|')
                        {
                            i++; // skip ||
                            continue;
                        }
                        segments.Add(cmdLine[segStart..i]);
                        segStart = i + 1;
                    }
                }
                segments.Add(cmdLine[segStart..]);
                return segments.ToArray();
            }

            /// <summary>
            /// Executes a command line, handling pipe chains when present.
            /// </summary>
            private static void ExecutePipeline(string cmdLine)
            {
                var stages = SplitPipes(cmdLine);
                if (stages.Length > 1)
                {
                    GlobalVariables.isPipeCommand = true;
                    GlobalVariables.pipeCmdCount = stages.Length - 1;
                    GlobalVariables.pipeCmdCountTemp = GlobalVariables.pipeCmdCount;

                    try
                    {
                        foreach (var stage in stages)
                        {
                            string stageTrimmed = stage.Trim();
                            if (stageTrimmed.Length == 0)
                                throw new FormatException("A pipeline stage cannot be empty.");
                            var cmd = CommandRepository.GetCommand(stageTrimmed);
                            if (cmd != null) cmd.Execute(stageTrimmed);
                            GlobalVariables.pipeCmdCount--;
                            if (GlobalVariables.isErrorCommand) break;
                        }
                    }
                    finally
                    {
                        GlobalVariables.isPipeCommand = false;
                        GlobalVariables.pipeCmdOutput = string.Empty;
                        GlobalVariables.pipeCmdCount = 0;
                        GlobalVariables.pipeCmdCountTemp = 0;
                    }
                }
                else
                {
                    var cmd = CommandRepository.GetCommand(cmdLine);
                    if (cmd != null)
                        cmd.Execute(cmdLine);
                }
            }
        }
    }
}
