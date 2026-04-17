using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Core;

namespace Commands.TerminalCommands.ConsoleSystem
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

        public string Name => "xt";

        private static readonly string s_helpMessage = @"Usage of xt command:
    xt <script.xt>              : Run an TermXT Script file.
    xt <script.xt> -p <args>    : Run with parameters ({1}, {2}... in script).
    xt -h                       : Display this help message.
    xt -new <script.xt>         : Create a new empty script template.
    xt -check <script.xt>       : Validate script syntax without running.

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
       Logical:   && (and)   || (or)   not <cond>   — evaluated left to right
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

                string currentDir = File.ReadAllText(GlobalVariables.currentDirectory);
                string rest = args.Substring(Name.Length).TrimStart();

                // xt -new <file>
                if (rest.StartsWith("-new "))
                {
                    string newFile = FileSystem.SanitizePath(rest.Substring(5).Trim(), currentDir);
                    File.WriteAllText(newFile, s_template.Replace("{DATE}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    FileSystem.SuccessWriteLine($"Script template created: {newFile}");
                    return;
                }

                // xt -check <file>
                if (rest.StartsWith("-check "))
                {
                    string checkFile = FileSystem.SanitizePath(rest.Substring(7).Trim(), currentDir);
                    CheckScript(checkFile);
                    return;
                }

                // xt <file> [-p <args>]
                string[] scriptArgs = Array.Empty<string>();
                string scriptPath;

                int pIdx = rest.LastIndexOf(" -p ");
                if (pIdx >= 0)
                {
                    scriptPath = FileSystem.SanitizePath(rest[..pIdx].Trim(), currentDir);
                    string paramStr = rest[(pIdx + 4)..].Trim();
                    scriptArgs = ParseArgs(paramStr);
                }
                else
                {
                    scriptPath = FileSystem.SanitizePath(rest, currentDir);
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
            return result.ToArray();
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
            int errors = 0;
            var blockStack = new Stack<(string type, int line)>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith('#')) continue;
                string keyword = line.Split(' ')[0].ToLower();

                switch (keyword)
                {
                    case "if":
                    case "loop":
                    case "each":
                    case "func":
                    case "try":
                    case "while":
                        blockStack.Push((keyword, i + 1));
                        break;
                    case "end":
                        if (blockStack.Count == 0)
                        {
                            FileSystem.ColorConsoleText(ConsoleColor.Red, $"  Line {i + 1}: ");
                            Console.WriteLine("'end' without matching block opener.");
                            errors++;
                        }
                        else
                            blockStack.Pop();
                        break;
                    case "elif":
                    case "else":
                        if (blockStack.Count == 0 || blockStack.Peek().type != "if")
                        {
                            FileSystem.ColorConsoleText(ConsoleColor.Red, $"  Line {i + 1}: ");
                            Console.WriteLine($"'{keyword}' without matching 'if'.");
                            errors++;
                        }
                        break;
                    case "catch":
                        if (blockStack.Count == 0 || blockStack.Peek().type != "try")
                        {
                            FileSystem.ColorConsoleText(ConsoleColor.Red, $"  Line {i + 1}: ");
                            Console.WriteLine("'catch' without matching 'try'.");
                            errors++;
                        }
                        break;
                    case "set":
                    case "capture":
                    case "read":
                        if (!line.Contains('='))
                        {
                            FileSystem.ColorConsoleText(ConsoleColor.Red, $"  Line {i + 1}: ");
                            Console.WriteLine($"'{keyword}' missing '=' assignment.");
                            errors++;
                        }
                        break;
                    case "break":
                    case "continue":
                        bool inLoop = false;
                        foreach (var entry in blockStack)
                        {
                            if (entry.type is "loop" or "each" or "while") { inLoop = true; break; }
                        }
                        if (!inLoop)
                        {
                            FileSystem.ColorConsoleText(ConsoleColor.Red, $"  Line {i + 1}: ");
                            Console.WriteLine($"'{keyword}' outside of a loop.");
                            errors++;
                        }
                        break;
                    case "return":
                        bool inFunc = false;
                        foreach (var entry in blockStack)
                        {
                            if (entry.type == "func") { inFunc = true; break; }
                        }
                        if (!inFunc)
                        {
                            FileSystem.ColorConsoleText(ConsoleColor.Red, $"  Line {i + 1}: ");
                            Console.WriteLine("'return' outside of a function.");
                            errors++;
                        }
                        break;
                    case "input":
                        if (!line.Contains('='))
                        {
                            FileSystem.ColorConsoleText(ConsoleColor.Red, $"  Line {i + 1}: ");
                            Console.WriteLine("'input' missing '=' assignment.");
                            errors++;
                        }
                        break;
                }
            }

            while (blockStack.Count > 0)
            {
                var (type, lineNum) = blockStack.Pop();
                FileSystem.ColorConsoleText(ConsoleColor.Red, $"  Line {lineNum}: ");
                Console.WriteLine($"'{type}' block never closed with 'end'.");
                errors++;
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
            private readonly string[] _scriptArgs;
            private readonly Dictionary<string, string> _vars = new(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, (int Start, int End)> _funcs = new(StringComparer.OrdinalIgnoreCase);
            private int _pc;
            private bool _stopRequested;
            private bool _breakRequested;
            private bool _continueRequested;
            private bool _returnRequested;


            private readonly DataTable _calc = new DataTable();

            public ScriptEngine(string path, string[] scriptArgs)
            {
                _lines = File.ReadAllLines(path);
                _scriptArgs = scriptArgs;

                for (int i = 0; i < _scriptArgs.Length; i++)
                    _vars[$"{i + 1}"] = _scriptArgs[i];

                _vars["USER"] = GlobalVariables.accountName;
                _vars["PC"] = GlobalVariables.computerName;
                _vars["result"] = "";
                _vars["error"] = "false";
                _vars["error_message"] = "";

                PreScanFunctions();
            }

            public void Run()
            {
                ExecuteBlock(0, _lines.Length - 1);
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
                    string keyword = line.Split(' ')[0].ToLower();

                    switch (keyword)
                    {
                        case "set": ExecSet(line); _pc++; break;
                        case "print": ExecPrint(line); _pc++; break;
                        case "run": ExecRun(line); _pc++; break;
                        case "capture": ExecCapture(line); _pc++; break;
                        case "input": ExecInput(line); _pc++; break;
                        case "read": ExecRead(line); _pc++; break;
                        case "wait": ExecWait(line); _pc++; break;
                        case "write": ExecFileWrite(line, append: false); _pc++; break;
                        case "append": ExecFileWrite(line, append: true); _pc++; break;
                        case "exit": _stopRequested = true; break;
                        case "break": _breakRequested = true; _pc++; break;
                        case "continue": _continueRequested = true; _pc++; break;
                        case "return": ExecReturn(line); break;
                        case "if": ExecIf(end); break;
                        case "loop": ExecLoop(end); break;
                        case "while": ExecWhile(end); break;
                        case "each": ExecEach(end); break;
                        case "call": ExecCall(line); _pc++; break;
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
                        PrintError(_pc + 1, $"Math expression contains invalid characters: '{expression}'");
                        return "0";
                    }
                    object result = _calc.Compute(expression, null);
                    return Convert.ToDouble(result).ToString(CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {
                    PrintError(_pc + 1, $"Math error: {ex.Message} in '{expression}'");
                    return "0";
                }
            }

            private string EvalSubstr(string args)
            {
                string[] tokens = args.Split(' ');
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
                if (start < 0) start = 0;
                if (start >= text.Length) return "";
                if (start + length > text.Length) length = text.Length - start;
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

            private void ExecPrint(string line)
            {
                string text = line[5..].Trim().Trim('"');
                text = text.Replace("\\n", "\n")
                           .Replace("\\t", "\t")
                           .Replace("\\\\", "\\");
                Console.WriteLine(text);
            }

            private void ExecRun(string line)
            {
                string cmdLine = line[3..].Trim();
                if (string.IsNullOrEmpty(cmdLine)) return;

                ResetCommandState();
                ExecutePipeline(cmdLine);
                _vars["error"] = GlobalVariables.isErrorCommand ? "true" : "false";
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
                    _vars["error"] = "true";
                    _vars["error_message"] = $"File not found: {filePath}";
                    return;
                }

                _vars[varName] = File.ReadAllText(filePath);
                _vars["error"] = "false";
            }

            private void ExecFileWrite(string line, bool append)
            {
                string keyword = append ? "append" : "write";
                string rest = line[keyword.Length..].Trim();

                int sep = rest.IndexOf(' ');
                if (sep < 0)
                {
                    PrintError(_pc + 1, $"'{keyword}' expects: {keyword} <file> \"text\"");
                    return;
                }

                string filePath = rest[..sep].Trim();
                string text = rest[(sep + 1)..].Trim().Trim('"');
                text = text.Replace("\\n", "\n")
                           .Replace("\\t", "\t")
                           .Replace("\\\\", "\\");

                string currentDir = File.ReadAllText(GlobalVariables.currentDirectory).Trim();
                filePath = FileSystem.SanitizePath(filePath, currentDir);


                if (append)
                    File.AppendAllText(filePath, text + Environment.NewLine);
                else
                    File.WriteAllText(filePath, text + Environment.NewLine);
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
                    int bodyEnd = (b + 1 < branches.Count) ? branches[b + 1].Line - 1 : endLine - 1;

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
                string firstLine = Interpolate(_lines[start].Trim());
                string firstKeyword = firstLine.Split(' ')[0].ToLower();
                string firstCond = firstLine.Length > firstKeyword.Length ? firstLine[(firstKeyword.Length + 1)..].Trim() : "";
                branches.Add((start, firstKeyword, firstCond));

                int depth = 1;
                int i = start + 1;
                while (i <= blockEnd && depth > 0)
                {
                    string trimmed = _lines[i].Trim();
                    string kw = trimmed.Split(' ')[0].ToLower();

                    if (kw is "if" or "loop" or "each" or "func" or "try" or "while")
                        depth++;
                    else if (kw == "end")
                    {
                        depth--;
                        if (depth == 0) return i;
                    }
                    else if (depth == 1 && (kw == "elif" || kw == "else" || kw == "catch"))
                    {
                        string cond = trimmed.Length > kw.Length ? Interpolate(trimmed[(kw.Length + 1)..].Trim()) : "";
                        branches.Add((i, kw, cond));
                    }
                    i++;
                }
                return i;
            }

            private bool EvalCondition(string condition)
            {
                // not <condition>
                string trimmedCond = condition.Trim();
                if (trimmedCond.StartsWith("not ", StringComparison.OrdinalIgnoreCase))
                    return !EvalCondition(trimmedCond[4..].Trim());

                if (condition.Contains("||"))
                {
                    string[] orParts = Regex.Split(condition, @"\s+\|\|\s+");
                    foreach (string part in orParts)
                    {
                        if (EvalCondition(part.Trim()))
                            return true;
                    }
                    return false;
                }

                if (condition.Contains("&&"))
                {
                    string[] andParts = Regex.Split(condition, @"\s+&&\s+");
                    foreach (string part in andParts)
                    {
                        if (!EvalCondition(part.Trim()))
                            return false;
                    }
                    return true;
                }

                string[] operators = { "==", "!=", ">=", "<=", ">", "<", "contains", "startswith", "endswith" };

                string foundOp = null;
                int opStart = -1;
                int opEnd = -1;

                foreach (string op in operators)
                {
                    string pattern = $" {op} ";
                    int idx = condition.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        foundOp = op;
                        opStart = idx;
                        opEnd = idx + pattern.Length;
                        break;
                    }
                }

                if (foundOp == null)
                {
                    string val = condition.Trim().Trim('"');
                    return !string.IsNullOrEmpty(val) && val != "0" && val.ToLower() != "false";
                }

                string left = condition[..opStart].Trim().Trim('"');
                string right = condition[opEnd..].Trim().Trim('"');
                string opLower = foundOp.ToLower();

                double numL = 0;
                double numR = 0;
                bool isNumeric = double.TryParse(left, NumberStyles.Any, CultureInfo.InvariantCulture, out numL)
                              && double.TryParse(right, NumberStyles.Any, CultureInfo.InvariantCulture, out numR);

                return opLower switch
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

            // ── Loop N / end ─────────────────────────────────────────────────

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

                for (int iter = 1; iter <= count && !_stopRequested; iter++)
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

                int iter = 0;

                while (!_stopRequested)
                {
                    iter++;
                    string condRaw = _lines[condLine].Trim();
                    string condInterp = Interpolate(condRaw);
                    string condStr = condInterp.Length > 5 ? condInterp[5..].Trim() : "";

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
                var linesMatch = Regex.Match(raw, @"^each\s+(\w+)\s+in\s+lines:\{?(\w+)\}?$", RegexOptions.IgnoreCase);
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
                var match = Regex.Match(line, @"^each\s+(\w+)\s+in\s+(.+)$", RegexOptions.IgnoreCase);
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

                    int idxRange = 0;
                    for (int n = rangeStart; step > 0 ? n <= rangeEnd : n >= rangeEnd; n += step)
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
                    if (trimmed.StartsWith("func ", StringComparison.OrdinalIgnoreCase))
                    {
                        string funcName = trimmed[5..].Trim();
                        int depth = 1;
                        int j = i + 1;
                        while (j < _lines.Length && depth > 0)
                        {
                            string kw = _lines[j].Trim().Split(' ')[0].ToLower();
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
                var parts = ParseQuotedTokens(rest);
                if (parts.Count == 0) { PrintError(_pc + 1, "'call' missing function name."); return; }

                string funcName = parts[0];
                if (!_funcs.TryGetValue(funcName, out var range))
                {
                    PrintError(_pc + 1, $"Function '{funcName}' not found.");
                    return;
                }
       
                    int argCount = parts.Count - 1;
                    var savedArgs = new Dictionary<string, string>();

                    // Save and set positional args for this call.
                    for (int a = 1; a <= argCount; a++)
                    {
                        string key = a.ToString();
                        if (_vars.ContainsKey(key)) savedArgs[key] = _vars[key];
                        _vars[key] = parts[a];
                    }

                    // Remove any leftover positional args beyond what we're passing
                    // so they don't leak from a previous call.
                    for (int a = argCount + 1; a <= 20; a++)
                    {
                        string key = a.ToString();
                        if (!_vars.ContainsKey(key)) break;
                        savedArgs[key] = _vars[key];
                        _vars.Remove(key);
                    }

                    int savedPc = _pc;
                    _returnRequested = false;
                    ExecuteBlock(range.Start, range.End);
                    _returnRequested = false;
                    _pc = savedPc;

                    // Restore previous positional args.
                    foreach (var kv in savedArgs)
                        _vars[kv.Key] = kv.Value;
          
            }

            // ── Try / catch / end ────────────────────────────────────────────

            private void ExecTry(int blockEnd)
            {
                var branches = new List<(int, string, string)>();
                int endLine = FindMatchingEnd(_pc, blockEnd, branches);

                int tryBodyStart = _pc + 1;
                int tryBodyEnd = branches.Count > 1 ? branches[1].Item1 - 1 : endLine - 1;
                int catchBodyStart = branches.Count > 1 ? branches[1].Item1 + 1 : -1;
                int catchBodyEnd = endLine - 1;

                bool savedError = GlobalVariables.isErrorCommand;
                GlobalVariables.isErrorCommand = false;

                try
                {
                    ExecuteBlock(tryBodyStart, tryBodyEnd);
                }
                catch (Exception ex)
                {
                    GlobalVariables.isErrorCommand = true;
                    _vars["error_message"] = ex.Message;
                }

                if (GlobalVariables.isErrorCommand && catchBodyStart >= 0)
                {
                    GlobalVariables.isErrorCommand = false;
                    ExecuteBlock(catchBodyStart, catchBodyEnd);
                }

                if (!GlobalVariables.isErrorCommand)
                    GlobalVariables.isErrorCommand = savedError;

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
                    string kw = _lines[_pc].Trim().Split(' ')[0].ToLower();
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

            private static void PrintError(int lineNum, string msg)
            {
                FileSystem.ColorConsoleText(ConsoleColor.Red, $"  [xt line {lineNum}] ");
                Console.WriteLine(msg);
            }

            /// <summary>
            /// Splits a command line on single pipe <c>|</c> characters,
            /// leaving double-pipe <c>||</c> sequences intact.
            /// </summary>
            private static string[] SplitPipes(string cmdLine)
            {
                var segments = new List<string>();
                int segStart = 0;
                for (int i = 0; i < cmdLine.Length; i++)
                {
                    if (cmdLine[i] == '|')
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

                    foreach (var stage in stages)
                    {
                        string stageTrimmed = stage.Trim();
                        var cmd = CommandRepository.GetCommand(stageTrimmed);
                        if (cmd != null)
                            cmd.Execute(stageTrimmed);
                        GlobalVariables.pipeCmdCount--;
                    }

                    GlobalVariables.isPipeCommand = false;
                    GlobalVariables.pipeCmdOutput = string.Empty;
                    GlobalVariables.pipeCmdCount = 0;
                    GlobalVariables.pipeCmdCountTemp = 0;
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