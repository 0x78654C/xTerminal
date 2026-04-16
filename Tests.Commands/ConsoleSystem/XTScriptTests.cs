using System.Runtime.Versioning;
using Commands.TerminalCommands.ConsoleSystem;
using Core;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

[SupportedOSPlatform("Windows")]
public class XTScriptTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _currentDirFile;
    private readonly XTScript _sut;
    private readonly string _savedCurrentDir;

    public XTScriptTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "xt_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);

        _currentDirFile = Path.Combine(_tempDir, "cDir.t");
        File.WriteAllText(_currentDirFile, _tempDir);

        _savedCurrentDir = GlobalVariables.currentDirectory;
        GlobalVariables.currentDirectory = _currentDirFile;
        GlobalVariables.isErrorCommand = false;

        _sut = new XTScript();
    }

    public void Dispose()
    {
        GlobalVariables.currentDirectory = _savedCurrentDir;
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private string CreateScript(params string[] lines)
    {
        string path = Path.Combine(_tempDir, $"test_{Guid.NewGuid():N}.xt");
        File.WriteAllLines(path, lines);
        return path;
    }

    private string RunScript(string scriptPath, string extraArgs = "")
    {
        var originalOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            string args = string.IsNullOrEmpty(extraArgs)
                ? $"xt {scriptPath}"
                : $"xt {scriptPath} {extraArgs}";
            _sut.Execute(args);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        return sw.ToString().TrimEnd();
    }

    private string[] RunScriptLines(string scriptPath, string extraArgs = "")
    {
        return RunScript(scriptPath, extraArgs)
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private string RunCheck(string scriptPath)
    {
        var originalOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            _sut.Execute($"xt -check {scriptPath}");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        return sw.ToString();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SET & PRINT
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Print_SimpleLiteral()
    {
        var path = CreateScript(@"print ""hello world""");
        RunScript(path).Should().Be("hello world");
    }

    [Fact]
    public void Set_And_Print_Variable()
    {
        var path = CreateScript(
            "set name = Alice",
            @"print ""Hello {name}""");
        RunScript(path).Should().Be("Hello Alice");
    }

    [Fact]
    public void Set_QuotedValue()
    {
        var path = CreateScript(
            @"set msg = ""hello world""",
            @"print ""{msg}""");
        RunScript(path).Should().Be("hello world");
    }

    [Fact]
    public void Set_OverwriteVariable()
    {
        var path = CreateScript(
            "set x = first",
            "set x = second",
            @"print ""{x}""");
        RunScript(path).Should().Be("second");
    }

    [Fact]
    public void Print_EscapeTab()
    {
        var path = CreateScript(@"print ""a\tb""");
        RunScript(path).Should().Be("a\tb");
    }

    [Fact]
    public void Print_EscapeNewline()
    {
        var path = CreateScript(@"print ""line1\nline2""");
        RunScriptLines(path).Should().BeEquivalentTo(
            new[] { "line1", "line2" }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void Print_EscapeBackslash()
    {
        var path = CreateScript(@"print ""a\\b""");
        RunScript(path).Should().Be(@"a\b");
    }

    [Fact]
    public void UndefinedVariable_StaysLiteral()
    {
        var path = CreateScript(@"print ""{undefined_var}""");
        RunScript(path).Should().Be("{undefined_var}");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // EVAL (MATH)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Eval_Addition()
    {
        var path = CreateScript("set x = eval 2 + 3", @"print ""{x}""");
        RunScript(path).Should().Be("5");
    }

    [Fact]
    public void Eval_Multiplication()
    {
        var path = CreateScript("set x = eval 4 * 5", @"print ""{x}""");
        RunScript(path).Should().Be("20");
    }

    [Fact]
    public void Eval_Parentheses()
    {
        var path = CreateScript("set x = eval (10 + 5) * 2", @"print ""{x}""");
        RunScript(path).Should().Be("30");
    }

    [Fact]
    public void Eval_WithVariable()
    {
        var path = CreateScript(
            "set a = 7",
            "set b = eval {a} * 3",
            @"print ""{b}""");
        RunScript(path).Should().Be("21");
    }

    [Fact]
    public void Eval_Modulo()
    {
        var path = CreateScript("set x = eval 10 % 3", @"print ""{x}""");
        RunScript(path).Should().Be("1");
    }

    [Fact]
    public void Eval_Division()
    {
        var path = CreateScript("set x = eval 10 / 4", @"print ""{x}""");
        RunScript(path).Should().Be("2.5");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // STRING OPERATIONS
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void StringOp_Upper()
    {
        var path = CreateScript("set x = upper hello", @"print ""{x}""");
        RunScript(path).Should().Be("HELLO");
    }

    [Fact]
    public void StringOp_Lower()
    {
        var path = CreateScript("set x = lower HELLO", @"print ""{x}""");
        RunScript(path).Should().Be("hello");
    }

    [Fact]
    public void StringOp_Len()
    {
        var path = CreateScript("set x = len hello", @"print ""{x}""");
        RunScript(path).Should().Be("5");
    }

    [Fact]
    public void StringOp_Trim()
    {
        var path = CreateScript(
            @"set x = trim ""  hello  """,
            @"print ""{x}""");
        RunScript(path).Should().Be("hello");
    }

    [Fact]
    public void StringOp_Substr()
    {
        var path = CreateScript("set x = substr hello 1 3", @"print ""{x}""");
        RunScript(path).Should().Be("ell");
    }

    [Fact]
    public void StringOp_Substr_ClampedLength()
    {
        var path = CreateScript("set x = substr hi 0 100", @"print ""{x}""");
        RunScript(path).Should().Be("hi");
    }

    [Fact]
    public void StringOp_Replace()
    {
        var path = CreateScript(
            @"set x = replace ""hello world"" ""world"" ""earth""",
            @"print ""{x}""");
        RunScript(path).Should().Be("hello earth");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CONDITIONALS  (if / elif / else / end)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void If_Equal_True()
    {
        var path = CreateScript(
            "set x = 5",
            "if {x} == 5",
            @"  print ""yes""",
            "end");
        RunScript(path).Should().Be("yes");
    }

    [Fact]
    public void If_Equal_False_NoOutput()
    {
        var path = CreateScript(
            "set x = 5",
            "if {x} == 10",
            @"  print ""yes""",
            "end");
        RunScript(path).Should().BeEmpty();
    }

    [Fact]
    public void If_NotEqual()
    {
        var path = CreateScript(
            "set x = 5",
            "if {x} != 10",
            @"  print ""different""",
            "end");
        RunScript(path).Should().Be("different");
    }

    [Theory]
    [InlineData("10", "5", ">", true)]
    [InlineData("3", "5", ">", false)]
    [InlineData("3", "5", "<", true)]
    [InlineData("10", "5", "<", false)]
    [InlineData("5", "5", ">=", true)]
    [InlineData("4", "5", ">=", false)]
    [InlineData("5", "5", "<=", true)]
    [InlineData("6", "5", "<=", false)]
    public void If_NumericComparison(string left, string right, string op, bool expected)
    {
        var path = CreateScript(
            $"if {left} {op} {right}",
            @"  print ""yes""",
            "end");
        if (expected)
            RunScript(path).Should().Be("yes");
        else
            RunScript(path).Should().BeEmpty();
    }

    [Fact]
    public void If_Contains()
    {
        var path = CreateScript(
            @"set msg = ""hello world""",
            "if {msg} contains world",
            @"  print ""found""",
            "end");
        RunScript(path).Should().Be("found");
    }

    [Fact]
    public void If_StartsWith()
    {
        var path = CreateScript(
            @"set msg = ""hello world""",
            "if {msg} startswith hello",
            @"  print ""yes""",
            "end");
        RunScript(path).Should().Be("yes");
    }

    [Fact]
    public void If_EndsWith()
    {
        var path = CreateScript(
            @"set msg = ""hello world""",
            "if {msg} endswith world",
            @"  print ""yes""",
            "end");
        RunScript(path).Should().Be("yes");
    }

    [Fact]
    public void If_Elif_Else_SelectsCorrectBranch()
    {
        var path = CreateScript(
            "set x = 2",
            "if {x} == 1",
            @"  print ""one""",
            "elif {x} == 2",
            @"  print ""two""",
            "else",
            @"  print ""other""",
            "end");
        RunScript(path).Should().Be("two");
    }

    [Fact]
    public void If_Else_Fallthrough()
    {
        var path = CreateScript(
            "set x = 99",
            "if {x} == 1",
            @"  print ""one""",
            "else",
            @"  print ""other""",
            "end");
        RunScript(path).Should().Be("other");
    }

    [Fact]
    public void If_MultipleElif()
    {
        var path = CreateScript(
            "set x = 4",
            "if {x} == 1",
            @"  print ""one""",
            "elif {x} == 2",
            @"  print ""two""",
            "elif {x} == 3",
            @"  print ""three""",
            "elif {x} == 4",
            @"  print ""four""",
            "end");
        RunScript(path).Should().Be("four");
    }

    [Fact]
    public void If_And_BothTrue()
    {
        var path = CreateScript(
            "set x = 5",
            "set y = 10",
            "if {x} == 5 && {y} == 10",
            @"  print ""both""",
            "end");
        RunScript(path).Should().Be("both");
    }

    [Fact]
    public void If_And_OneFalse()
    {
        var path = CreateScript(
            "set x = 5",
            "set y = 10",
            "if {x} == 5 && {y} == 99",
            @"  print ""both""",
            "end");
        RunScript(path).Should().BeEmpty();
    }

    [Fact]
    public void If_Or_OneTrue()
    {
        var path = CreateScript(
            "set x = 5",
            "if {x} == 1 || {x} == 5",
            @"  print ""match""",
            "end");
        RunScript(path).Should().Be("match");
    }

    [Fact]
    public void If_Or_BothFalse()
    {
        var path = CreateScript(
            "set x = 5",
            "if {x} == 1 || {x} == 2",
            @"  print ""match""",
            "end");
        RunScript(path).Should().BeEmpty();
    }

    [Fact]
    public void If_Not_Negates()
    {
        var path = CreateScript(
            "set x = 5",
            "if not {x} == 10",
            @"  print ""not ten""",
            "end");
        RunScript(path).Should().Be("not ten");
    }

    [Fact]
    public void If_Not_NegatesTrue()
    {
        var path = CreateScript(
            "set x = 5",
            "if not {x} == 5",
            @"  print ""should not appear""",
            "end");
        RunScript(path).Should().BeEmpty();
    }

    [Fact]
    public void If_StringComparison_GreaterThan()
    {
        var path = CreateScript(
            "if b > a",
            @"  print ""yes""",
            "end");
        RunScript(path).Should().Be("yes");
    }

    [Fact]
    public void If_StringComparison_LessThan()
    {
        var path = CreateScript(
            "if a < b",
            @"  print ""yes""",
            "end");
        RunScript(path).Should().Be("yes");
    }

    [Fact]
    public void If_Truthy_NonEmpty()
    {
        var path = CreateScript(
            "set x = hello",
            "if {x}",
            @"  print ""truthy""",
            "end");
        RunScript(path).Should().Be("truthy");
    }

    [Fact]
    public void If_Falsy_Zero()
    {
        var path = CreateScript(
            "set x = 0",
            "if {x}",
            @"  print ""truthy""",
            "end");
        RunScript(path).Should().BeEmpty();
    }

    [Fact]
    public void If_Falsy_False()
    {
        var path = CreateScript(
            "set x = false",
            "if {x}",
            @"  print ""truthy""",
            "end");
        RunScript(path).Should().BeEmpty();
    }

    [Fact]
    public void If_Nested()
    {
        var path = CreateScript(
            "set x = 1",
            "set y = 2",
            "if {x} == 1",
            "  if {y} == 2",
            @"    print ""nested""",
            "  end",
            "end");
        RunScript(path).Should().Be("nested");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // LOOPS  (loop / while / each)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Loop_NTimes()
    {
        var path = CreateScript(
            "loop 3",
            @"  print ""{i}""",
            "end");
        RunScriptLines(path).Should().BeEquivalentTo(
            new[] { "1", "2", "3" }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void Loop_Zero_ReportsError()
    {
        var path = CreateScript(
            "loop 0",
            @"  print ""inside""",
            "end",
            @"print ""after""");
        RunScript(path).Should().Contain("'loop' expects a positive number");
    }

    [Fact]
    public void Loop_WithBreak()
    {
        var path = CreateScript(
            "loop 10",
            "  if {i} == 3",
            "    break",
            "  end",
            @"  print ""{i}""",
            "end");
        RunScriptLines(path).Should().BeEquivalentTo(
            new[] { "1", "2" }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void Loop_WithContinue()
    {
        var path = CreateScript(
            "loop 5",
            "  if {i} == 3",
            "    continue",
            "  end",
            @"  print ""{i}""",
            "end");
        RunScriptLines(path).Should().BeEquivalentTo(
            new[] { "1", "2", "4", "5" }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void Loop_Nested()
    {
        var path = CreateScript(
            "loop 2",
            "  set outer = {i}",
            "  loop 2",
            @"    print ""{outer}.{i}""",
            "  end",
            "end");
        RunScriptLines(path).Should().BeEquivalentTo(
            new[] { "1.1", "1.2", "2.1", "2.2" }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void While_Countdown()
    {
        var path = CreateScript(
            "set x = 3",
            "while {x} > 0",
            @"  print ""{x}""",
            "  set x = eval {x} - 1",
            "end");
        RunScriptLines(path).Should().BeEquivalentTo(
            new[] { "3", "2", "1" }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void While_FalseInitially_NoExecution()
    {
        var path = CreateScript(
            "set x = 0",
            "while {x} > 10",
            @"  print ""inside""",
            "end",
            @"print ""after""");
        RunScript(path).Should().Be("after");
    }

    [Fact]
    public void While_WithBreak()
    {
        var path = CreateScript(
            "set x = 0",
            "while true",
            "  set x = eval {x} + 1",
            "  if {x} == 3",
            "    break",
            "  end",
            @"  print ""{x}""",
            "end");
        RunScriptLines(path).Should().BeEquivalentTo(
            new[] { "1", "2" }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void Each_CommaSeparated()
    {
        var path = CreateScript(
            "each item in apple,banana,cherry",
            @"  print ""{item}""",
            "end");
        RunScriptLines(path).Should().BeEquivalentTo(
            new[] { "apple", "banana", "cherry" }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void Each_IterationCounter()
    {
        var path = CreateScript(
            "each item in a,b,c",
            @"  print ""{i}:{item}""",
            "end");
        RunScriptLines(path).Should().BeEquivalentTo(
            new[] { "1:a", "2:b", "3:c" }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void Each_NumericRange_Ascending()
    {
        var path = CreateScript(
            "each n in 1..5",
            @"  print ""{n}""",
            "end");
        RunScriptLines(path).Should().BeEquivalentTo(
            new[] { "1", "2", "3", "4", "5" }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void Each_NumericRange_Descending()
    {
        var path = CreateScript(
            "each n in 5..1",
            @"  print ""{n}""",
            "end");
        RunScriptLines(path).Should().BeEquivalentTo(
            new[] { "5", "4", "3", "2", "1" }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void Each_WithBreak()
    {
        var path = CreateScript(
            "each item in a,b,c,d,e",
            "  if {item} == c",
            "    break",
            "  end",
            @"  print ""{item}""",
            "end");
        RunScriptLines(path).Should().BeEquivalentTo(
            new[] { "a", "b" }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void Each_WithContinue()
    {
        var path = CreateScript(
            "each item in a,b,c,d",
            "  if {item} == b",
            "    continue",
            "  end",
            @"  print ""{item}""",
            "end");
        RunScriptLines(path).Should().BeEquivalentTo(
            new[] { "a", "c", "d" }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void Each_Range_WithVariable()
    {
        var path = CreateScript(
            "set max = 3",
            "each n in 1..{max}",
            @"  print ""{n}""",
            "end");
        RunScriptLines(path).Should().BeEquivalentTo(
            new[] { "1", "2", "3" }, o => o.WithStrictOrdering());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FUNCTIONS  (func / call / return)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Func_SimpleCall()
    {
        var path = CreateScript(
            "func greet",
            @"  print ""hello""",
            "end",
            "",
            "call greet");
        RunScript(path).Should().Be("hello");
    }

    [Fact]
    public void Func_WithPositionalArgs()
    {
        var path = CreateScript(
            "func greet",
            @"  print ""hi {1}""",
            "end",
            "",
            "call greet world");
        RunScript(path).Should().Be("hi world");
    }

    [Fact]
    public void Func_WithQuotedArgs()
    {
        var path = CreateScript(
            "func greet",
            @"  print ""{1}""",
            "end",
            "",
            @"call greet ""hello world""");
        RunScript(path).Should().Be("hello world");
    }

    [Fact]
    public void Func_Return_SetsResult()
    {
        var path = CreateScript(
            "func add",
            "  set sum = eval {1} + {2}",
            "  return {sum}",
            "end",
            "",
            "call add 3 4",
            @"print ""{result}""");
        RunScript(path).Should().Be("7");
    }

    [Fact]
    public void Func_Return_StopsExecution()
    {
        var path = CreateScript(
            "func test",
            @"  print ""before""",
            "  return done",
            @"  print ""after""",
            "end",
            "",
            "call test");
        RunScript(path).Should().Be("before");
    }

    [Fact]
    public void Func_CallingAnotherFunc()
    {
        var path = CreateScript(
            "func double",
            "  set r = eval {1} * 2",
            "  return {r}",
            "end",
            "",
            "func quadruple",
            "  call double {1}",
            "  call double {result}",
            "  return {result}",
            "end",
            "",
            "call quadruple 5",
            @"print ""{result}""");
        RunScript(path).Should().Be("20");
    }

    [Fact]
    public void Func_ArgsDoNotLeak()
    {
        var path = CreateScript(
            "func show_two",
            @"  print ""{1}:{2}""",
            "end",
            "",
            "func show_one",
            @"  print ""{1}:{2}""",
            "end",
            "",
            "call show_two a b",
            "call show_one x");
        var lines = RunScriptLines(path);
        lines[0].Should().Be("a:b");
        lines[1].Should().Be("x:{2}");  // {2} should be undefined (literal)
    }

    [Fact]
    public void Func_NotDefined_PrintsError()
    {
        var path = CreateScript("call nonexistent");
        var output = RunScript(path);
        output.Should().Contain("not found");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TRY / CATCH / ERROR HANDLING
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Try_NoError_RunsTryBlock()
    {
        var path = CreateScript(
            "try",
            @"  print ""ok""",
            "catch",
            @"  print ""error""",
            "end");
        RunScript(path).Should().Be("ok");
    }

    [Fact]
    public void Error_Variable_DefaultFalse()
    {
        var path = CreateScript(@"print ""{error}""");
        RunScript(path).Should().Be("false");
    }

    [Fact]
    public void ErrorMessage_Variable_DefaultEmpty()
    {
        var path = CreateScript(@"print ""{error_message}""");
        RunScript(path).Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BUILT-IN VARIABLES
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void BuiltIn_DATE_Format()
    {
        var path = CreateScript(@"print ""{DATE}""");
        RunScript(path).Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}$");
    }

    [Fact]
    public void BuiltIn_TIME_Format()
    {
        var path = CreateScript(@"print ""{TIME}""");
        RunScript(path).Should().MatchRegex(@"^\d{2}:\d{2}:\d{2}$");
    }

    [Fact]
    public void BuiltIn_USER()
    {
        var path = CreateScript(@"print ""{USER}""");
        RunScript(path).Should().Be(Environment.UserName);
    }

    [Fact]
    public void BuiltIn_PC()
    {
        var path = CreateScript(@"print ""{PC}""");
        RunScript(path).Should().Be(Environment.MachineName);
    }

    [Fact]
    public void BuiltIn_CWD()
    {
        var path = CreateScript(@"print ""{CWD}""");
        RunScript(path).Should().Be(_tempDir);
    }

    [Fact]
    public void BuiltIn_Variable_CaseInsensitive()
    {
        var path = CreateScript(
            "set Name = test",
            @"print ""{name}""");
        RunScript(path).Should().Be("test");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SCRIPT PARAMETERS  (-p)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parameters_Positional()
    {
        var path = CreateScript(@"print ""{1} {2}""");
        RunScript(path, "-p hello world").Should().Be("hello world");
    }

    [Fact]
    public void Parameters_QuotedArg()
    {
        var path = CreateScript(@"print ""{1}""");
        RunScript(path, @"-p ""hello world""").Should().Be("hello world");
    }

    [Fact]
    public void Parameters_UsedInCondition()
    {
        var path = CreateScript(
            "if {1} == prod",
            @"  print ""production""",
            "else",
            @"  print ""other""",
            "end");
        RunScript(path, "-p prod").Should().Be("production");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SYNTAX CHECKER  (-check)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Check_ValidScript_ShowsSuccess()
    {
        var path = CreateScript(
            "set x = 5",
            "if {x} == 5",
            @"  print ""yes""",
            "end");
        RunCheck(path).Should().Contain("✓");
    }

    [Fact]
    public void Check_UnclosedIf()
    {
        var path = CreateScript(
            "if true",
            @"  print ""yes""");
        var output = RunCheck(path);
        output.Should().Contain("never closed");
    }

    [Fact]
    public void Check_UnclosedLoop()
    {
        var path = CreateScript(
            "loop 5",
            @"  print ""{i}""");
        var output = RunCheck(path);
        output.Should().Contain("never closed");
    }

    [Fact]
    public void Check_OrphanEnd()
    {
        var path = CreateScript("end");
        var output = RunCheck(path);
        output.Should().Contain("without matching");
    }

    [Fact]
    public void Check_ElseWithoutIf()
    {
        var path = CreateScript("else");
        var output = RunCheck(path);
        output.Should().Contain("without matching");
    }

    [Fact]
    public void Check_CatchWithoutTry()
    {
        var path = CreateScript("catch");
        var output = RunCheck(path);
        output.Should().Contain("without matching");
    }

    [Fact]
    public void Check_BreakOutsideLoop()
    {
        var path = CreateScript(
            "if true",
            "  break",
            "end");
        var output = RunCheck(path);
        output.Should().Contain("outside of a loop");
    }

    [Fact]
    public void Check_ContinueOutsideLoop()
    {
        var path = CreateScript(
            "if true",
            "  continue",
            "end");
        var output = RunCheck(path);
        output.Should().Contain("outside of a loop");
    }

    [Fact]
    public void Check_ReturnOutsideFunc()
    {
        var path = CreateScript("return 5");
        var output = RunCheck(path);
        output.Should().Contain("outside of a function");
    }

    [Fact]
    public void Check_SetMissingEquals()
    {
        var path = CreateScript("set x");
        var output = RunCheck(path);
        output.Should().Contain("missing '='");
    }

    [Fact]
    public void Check_InputMissingEquals()
    {
        var path = CreateScript("input x");
        var output = RunCheck(path);
        output.Should().Contain("missing '='");
    }

    [Fact]
    public void Check_NestedBlocksValid()
    {
        var path = CreateScript(
            "func test",
            "  loop 3",
            "    if {i} == 1",
            @"      print ""yes""",
            "    end",
            "  end",
            "end");
        RunCheck(path).Should().Contain("✓");
    }

    [Fact]
    public void AutoCheck_BlocksInvalidScript()
    {
        var path = CreateScript(
            "if true",
            @"  print ""inside""");
        RunScript(path);
        GlobalVariables.isErrorCommand.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TEMPLATE  (-new)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void New_CreatesTemplateFile()
    {
        string newScript = Path.Combine(_tempDir, "template.xt");
        var originalOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            _sut.Execute($"xt -new {newScript}");
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        File.Exists(newScript).Should().BeTrue();
        string content = File.ReadAllText(newScript);
        content.Should().Contain("xTermXT Script template");
        content.Should().Contain("print");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MISCELLANEOUS
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Exit_StopsExecution()
    {
        var path = CreateScript(
            @"print ""before""",
            "exit",
            @"print ""after""");
        RunScript(path).Should().Be("before");
    }

    [Fact]
    public void Comments_AreIgnored()
    {
        var path = CreateScript(
            "# this is a comment",
            @"print ""hello""",
            "# another comment");
        RunScript(path).Should().Be("hello");
    }

    [Fact]
    public void EmptyScript_NoOutput()
    {
        var path = CreateScript("");
        RunScript(path).Should().BeEmpty();
    }

    [Fact]
    public void BlankLinesOnly_NoOutput()
    {
        var path = CreateScript("", "", "");
        RunScript(path).Should().BeEmpty();
    }

    [Fact]
    public void Wait_SmallMs_DoesNotCrash()
    {
        var path = CreateScript(
            "wait 1",
            @"print ""done""");
        RunScript(path).Should().Be("done");
    }

    [Fact]
    public void ScriptNotFound_SetsError()
    {
        var nonexistent = Path.Combine(_tempDir, "does_not_exist.xt");
        RunScript(nonexistent);
        GlobalVariables.isErrorCommand.Should().BeTrue();
    }

    [Fact]
    public void LoopInsideIf()
    {
        var path = CreateScript(
            "set x = 1",
            "if {x} == 1",
            "  loop 3",
            @"    print ""{i}""",
            "  end",
            "end");
        RunScriptLines(path).Should().BeEquivalentTo(
            new[] { "1", "2", "3" }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void IfInsideLoop()
    {
        var path = CreateScript(
            "loop 4",
            "  if {i} == 2",
            @"    print ""two""",
            "  elif {i} == 4",
            @"    print ""four""",
            "  end",
            "end");
        RunScriptLines(path).Should().BeEquivalentTo(
            new[] { "two", "four" }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void MultipleSetAndInterpolation()
    {
        var path = CreateScript(
            "set first = John",
            "set last = Doe",
            "set age = 30",
            @"print ""{first} {last} is {age}""");
        RunScript(path).Should().Be("John Doe is 30");
    }

    [Fact]
    public void Eval_ChainedComputation()
    {
        var path = CreateScript(
            "set a = eval 2 + 3",
            "set b = eval {a} * 4",
            "set c = eval {b} - 10",
            @"print ""{c}""");
        RunScript(path).Should().Be("10");
    }

    [Fact]
    public void Func_WithLoop()
    {
        var path = CreateScript(
            "func countdown",
            "  set n = {1}",
            "  while {n} > 0",
            @"    print ""{n}""",
            "    set n = eval {n} - 1",
            "  end",
            "end",
            "",
            "call countdown 3");
        RunScriptLines(path).Should().BeEquivalentTo(
            new[] { "3", "2", "1" }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void Each_Range_SingleValue()
    {
        var path = CreateScript(
            "each n in 5..5",
            @"  print ""{n}""",
            "end");
        RunScript(path).Should().Be("5");
    }
}
