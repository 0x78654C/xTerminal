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
            xt — TermXT Script: a simple, easy-to-use scripting language for TermXTinal.

            Runs .xt script files containing TermXTinal commands with variables,
            conditionals, loops, functions, error handling and output capture.
            Every TermXTinal command works as-is inside a script.
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
    run <command>                      : Run any TermXTinal command (supports pipes).
    capture <var> = <command>           : Run command, store stdout in variable (supports pipes).
    input <var> = ""prompt""             : Read user input into a variable.
    if <a> <op> <b> / elif / else / end : Conditional block.
       Operators: ==  !=  >  <  >=  <=  contains  startswith  endswith
       Logical:   && (and)   || (or)   — evaluated left to right
    loop <n> / end                     : Repeat block N times. {i} = iteration.
    while <condition> / end            : Repeat while condition is true.
    each <var> in <a,b,c> / end        : Iterate comma-separated values.
    each <var> in lines:<varname> / end : Iterate over lines of a variable.
    func <name> / end                  : Define a reusable function.
    call <name> [args]                 : Call function. {1},{2}.. for args.
    return <value>                     : Return value from function (stored in {result}).
    try / catch / end                  : Error handling.
    break                              : Exit current loop.
    continue                           : Skip to next loop iteration.
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

        private static readonly string s_template = @"# ── TermXT Script template ────────────────────────
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

        private static void CheckScript(string path)
        {
            if (!File.Exists(path))
            {
                FileSystem.ErrorWriteLine($"Script not found: {path}");
                return;
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
                }
            }

            while (blockStack.Count > 0)
            {
                var (type, lineNum) = blockStack.Pop();
                FileSystem.ColorConsoleText(ConsoleColor.Red, $"  Line {lineNum}: ");
                Console.WriteLine($"'{type}' block never closed with 'end'.");
                errors++;
            }

            if (errors == 0)
                FileSystem.ColorConsoleText(ConsoleColor.Green, $"  ✓ Script '{path}' is valid ({lines.Length} lines).\n");
            else
                FileSystem.ColorConsoleText(ConsoleColor.Red, $"\n  ✗ Found {errors} error(s).\n");
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

            private static readonly DataTable s_calc = new DataTable();

            public ScriptEngine(string path, string[] scriptArgs)
            {
                _lines = File.ReadAllLines(path);
                _scriptArgs = scriptArgs;

                for (int i = 0; i < _scriptArgs.Length; i++)
                    _vars[$"{i + 1}"] = _scriptArgs[i];

                _vars["DATE"]   = DateTime.Now.ToString("yyyy-MM-dd");
                _vars["TIME"]   = DateTime.Now.ToString("HH:mm:ss");
                _vars["USER"]   = GlobalVariables.accountName;
                _vars["PC"]     = GlobalVariables.computerName;
                _vars["CWD"]    = File.ReadAllText(GlobalVariables.currentDirectory).Trim();
                _vars["result"] = "";
                _vars["error"]  = "false";

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

                    // Skip empty lines and comments
                    if (string.IsNullOrEmpty(raw) || raw.StartsWith('#') || raw.StartsWith("#!/"))
                    {
                        _pc++;
                        continue;
                    }

                    string line = Interpolate(raw);
                    string keyword = line.Split(' ')[0].ToLower();

                    switch (keyword)
                    {
                        case "set":      ExecSet(line); _pc++; break;
                        case "print":    ExecPrint(line); _pc++; break;
                        case "run":      ExecRun(line); _pc++; break;
                        case "capture":  ExecCapture(line); _pc++; break;
                        case "input":    ExecInput(line); _pc++; break;
                        case "wait":     ExecWait(line); _pc++; break;
                        case "exit":     _stopRequested = true; break;
                        case "break":    _breakRequested = true; _pc++; break;
                        case "continue": _continueRequested = true; _pc++; break;
                        case "return":   ExecReturn(line); break;
                        case "if":       ExecIf(end); break;
                        case "loop":     ExecLoop(end); break;
                        case "while":    ExecWhile(end); break;
                        case "each":     ExecEach(end); break;
                        case "call":     ExecCall(line); _pc++; break;
                        case "try":      ExecTry(end); break;
                        case "func":     SkipBlock(); break; // already pre-scanned
                        default:
                            // Treat unknown lines as implicit 'run' commands
                            ExecRun("run " + line);
                            _pc++;
                            break;
                    }
                }
            }

            // ── Variable interpolation ───────────────────────────────────────

            private string Interpolate(string text)
            {
                // Replace {varName} with variable values
                return Regex.Replace(text, @"\{(\w+)\}", m =>
                {
                    string key = m.Groups[1].Value;
                    return _vars.TryGetValue(key, out string val) ? val : m.Value;
                });
            }

            // ── Keyword implementations ──────────────────────────────────────

            private void ExecSet(string line)
            {
                // set <var> = <value>
                // set <var> = eval <expr>
                // set <var> = upper/lower/len/substr/replace/trim <args>
                int eq = line.IndexOf('=');
                if (eq < 0) { PrintError(_pc + 1, "'set' missing '='"); return; }
                string varName = line[3..eq].Trim();
                string rhs = line[(eq + 1)..].Trim();

                _vars[varName] = EvalRhs(rhs);
            }

            /// <summary>
            /// Evaluates the right-hand side of a set assignment.
            /// Supports: eval, upper, lower, len, substr, replace, trim, or raw value.
            /// </summary>
            private string EvalRhs(string rhs)
            {
                // eval <math expression>
                if (rhs.StartsWith("eval ", StringComparison.OrdinalIgnoreCase))
                {
                    return EvalMath(rhs[5..].Trim());
                }

                // upper <text>
                if (rhs.StartsWith("upper ", StringComparison.OrdinalIgnoreCase))
                {
                    return rhs[6..].Trim().Trim('"').ToUpper();
                }

                // lower <text>
                if (rhs.StartsWith("lower ", StringComparison.OrdinalIgnoreCase))
                {
                    return rhs[6..].Trim().Trim('"').ToLower();
                }

                // len <text>
                if (rhs.StartsWith("len ", StringComparison.OrdinalIgnoreCase))
                {
                    return rhs[4..].Trim().Trim('"').Length.ToString();
                }

                // trim <text>
                if (rhs.StartsWith("trim ", StringComparison.OrdinalIgnoreCase))
                {
                    return rhs[5..].Trim().Trim('"').Trim();
                }

                // substr <text> <start> <length>
                if (rhs.StartsWith("substr ", StringComparison.OrdinalIgnoreCase))
                {
                    return EvalSubstr(rhs[7..].Trim());
                }

                // replace <text> <old> <new>
                if (rhs.StartsWith("replace ", StringComparison.OrdinalIgnoreCase))
                {
                    return EvalReplace(rhs[8..].Trim());
                }

                // Raw value
                return rhs.Trim('"');
            }

            private string EvalMath(string expression)
            {
                try
                {
                    // DataTable.Compute handles +, -, *, /, %, parentheses
                    object result = s_calc.Compute(expression, null);
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
                // Parse: <text> <start> <length>
                // The last two tokens are numbers, everything before is the text
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
                // Parse quoted: replace "text" "old" "new"
                // Or simple:   replace text old new (last two tokens are old and new)
                var parts = ParseQuotedTokens(args);
                if (parts.Count >= 3)
                {
                    string text = parts[0];
                    string oldVal = parts[1];
                    string newVal = parts[2];
                    return text.Replace(oldVal, newVal, StringComparison.OrdinalIgnoreCase);
                }

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
                // print "text" or print text
                string text = line[5..].Trim().Trim('"');
                Console.WriteLine(text);
            }

            private void ExecRun(string line)
            {
                // run <any TermXTinal command>
                // Supports pipe commands: run ls | cat -s exe
                string cmdLine = line[3..].Trim();
                if (string.IsNullOrEmpty(cmdLine)) return;

                ResetCommandState();

                // Check for pipe commands
                if (cmdLine.Contains('|') && !cmdLine.Contains("||"))
                {
                    var pipeStages = cmdLine.Split('|');
                    GlobalVariables.isPipeCommand = true;
                    GlobalVariables.pipeCmdCount = pipeStages.Length - 1;
                    GlobalVariables.pipeCmdCountTemp = GlobalVariables.pipeCmdCount;

                    foreach (var stage in pipeStages)
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

                _vars["error"] = GlobalVariables.isErrorCommand ? "true" : "false";
            }

            private void ExecCapture(string line)
            {
                // capture <var> = <command>
                // Supports pipe commands: capture result = ls | cat -s exe
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
                        // Check for pipe commands
                        if (cmdLine.Contains('|') && !cmdLine.Contains("||"))
                        {
                            var pipeStages = cmdLine.Split('|');
                            GlobalVariables.isPipeCommand = true;
                            GlobalVariables.pipeCmdCount = pipeStages.Length - 1;
                            GlobalVariables.pipeCmdCountTemp = GlobalVariables.pipeCmdCount;

                            foreach (var stage in pipeStages)
                            {
                                string stageTrimmed = stage.Trim();
                                var cmd = CommandRepository.GetCommand(stageTrimmed);
                                if (cmd != null)
                                    cmd.Execute(stageTrimmed);
                                GlobalVariables.pipeCmdCount--;
                            }

                            // The final pipe output is in pipeCmdOutput
                            // but also check what was written to stdout
                            GlobalVariables.isPipeCommand = false;
                        }
                        else
                        {
                            var cmd = CommandRepository.GetCommand(cmdLine);
                            if (cmd != null)
                                cmd.Execute(cmdLine);
                        }
                    }
                    finally
                    {
                        Console.SetOut(originalOut);
                    }
                    captured = sw.ToString().Trim();
                }

                // Pipe commands store their final output in pipeCmdOutput.
                // If captured stdout is empty but pipeCmdOutput has content, use that.
                if (string.IsNullOrEmpty(captured) && !string.IsNullOrEmpty(GlobalVariables.pipeCmdOutput))
                    captured = GlobalVariables.pipeCmdOutput.Trim();

                _vars[varName] = captured;
                _vars["error"] = GlobalVariables.isErrorCommand ? "true" : "false";

                // Clean up pipe state
                GlobalVariables.pipeCmdOutput = string.Empty;
                GlobalVariables.pipeCmdCount = 0;
                GlobalVariables.pipeCmdCountTemp = 0;
                GlobalVariables.isPipeCommand = false;
            }

            private void ExecInput(string line)
            {
                // input <var> = "prompt text"
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
                // wait <ms>
                string msStr = line[4..].Trim();
                if (int.TryParse(msStr, out int ms) && ms > 0)
                    System.Threading.Thread.Sleep(ms);
            }

            private void ExecReturn(string line)
            {
                // return <value>
                string val = line.Length > 6 ? line[6..].Trim().Trim('"') : "";
                _vars["result"] = val;
                _returnRequested = true;
                _pc++;
            }

            // ── If / elif / else / end ───────────────────────────────────────

            private void ExecIf(int blockEnd)
            {
                // Find the matching end, collecting elif/else positions
                var branches = new List<(int Line, string Type, string Condition)>();
                int endLine = FindMatchingEnd(_pc, blockEnd, branches);

                // Evaluate branches in order
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
                    else // "if" or "elif"
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
                return i; // fallback: past end
            }

            private bool EvalCondition(string condition)
            {
                // Support && and || logical operators (evaluated left-to-right)
                // Split by || first (lower precedence), then by &&
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

                // Find the operator keyword in the condition by scanning for known
                // operators as whole words surrounded by spaces.  This avoids the
                // problem where the left-hand side contains spaces (e.g. captured
                // ls output) and a naive Split(' ',3) picks the wrong token as the
                // operator.
                string[] operators = { "==", "!=", ">=", "<=", ">", "<", "contains", "startswith", "endswith" };

                string foundOp = null;
                int opStart = -1;
                int opEnd = -1;

                foreach (string op in operators)
                {
                    // Search for " op " (surrounded by spaces) — this ensures we
                    // don't accidentally match operator text inside a value.
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
                    // Boolean-style: non-empty/non-zero = true
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
                    "=="         => left.Equals(right, StringComparison.OrdinalIgnoreCase),
                    "!="         => !left.Equals(right, StringComparison.OrdinalIgnoreCase),
                    ">"          => isNumeric && numL > numR,
                    "<"          => isNumeric && numL < numR,
                    ">="         => isNumeric && numL >= numR,
                    "<="         => isNumeric && numL <= numR,
                    "contains"   => left.Contains(right, StringComparison.OrdinalIgnoreCase),
                    "startswith" => left.StartsWith(right, StringComparison.OrdinalIgnoreCase),
                    "endswith"   => left.EndsWith(right, StringComparison.OrdinalIgnoreCase),
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
                const int maxIterations = 100000; // safety guard

                while (!_stopRequested && iter < maxIterations)
                {
                    iter++;
                    // Re-interpolate the condition each iteration
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

                if (iter >= maxIterations)
                    PrintError(condLine + 1, $"'while' exceeded {maxIterations} iterations (infinite loop guard).");

                _pc = endLine + 1;
            }

            // ── Each <var> in <a,b,c> / end ──────────────────────────────────

            private void ExecEach(int blockEnd)
            {
                // Use the raw line for parsing the 'lines:' variable reference,
                // since Interpolate would expand multiline content and break the regex.
                string raw = _lines[_pc].Trim();

                // First check for lines:<varname> pattern BEFORE interpolation
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

                // Standard each: interpolate normally for comma-separated values
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
                        _funcs[funcName] = (i + 1, j - 1); // body range (exclusive of func/end lines)
                    }
                }
            }

            private void ExecCall(string line)
            {
                // call <name> [arg1 arg2 ...]
                string rest = line[4..].Trim();
                string[] parts = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) { PrintError(_pc + 1, "'call' missing function name."); return; }

                string funcName = parts[0];
                if (!_funcs.TryGetValue(funcName, out var range))
                {
                    PrintError(_pc + 1, $"Function '{funcName}' not found.");
                    return;
                }

                // Save and set function-local positional args
                var savedArgs = new Dictionary<string, string>();
                for (int a = 1; a < parts.Length; a++)
                {
                    string key = a.ToString();
                    if (_vars.ContainsKey(key)) savedArgs[key] = _vars[key];
                    _vars[key] = parts[a];
                }

                int savedPc = _pc;
                _returnRequested = false;
                ExecuteBlock(range.Start, range.End);
                _returnRequested = false; // consumed by caller
                _pc = savedPc;

                // Restore previous positional args
                foreach (var kv in savedArgs)
                    _vars[kv.Key] = kv.Value;
            }

            // ── Try / catch / end ────────────────────────────────────────────

            private void ExecTry(int blockEnd)
            {
                var branches = new List<(int, string, string)>();
                int endLine = FindMatchingEnd(_pc, blockEnd, branches);

                // branches[0] = try, branches[1] = catch (if present)
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
                catch { GlobalVariables.isErrorCommand = true; }

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
        }
    }
}