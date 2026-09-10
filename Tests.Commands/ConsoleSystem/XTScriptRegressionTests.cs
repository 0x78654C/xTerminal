using System.Reflection;
using Commands;
using Core;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

public partial class XTScriptTests
{
    [Theory]
    [InlineData("true||false", true)]
    [InlineData("true&&false", false)]
    [InlineData("3>=2&&3<5", true)]
    [InlineData("false || true && false", false)]
    [InlineData("not false && false", false)]
    [InlineData("not true || true", true)]
    [InlineData("(false || true) && true", true)]
    [InlineData("not(false || (true && false))", true)]
    [InlineData("\"a||b\" == \"a||b\"", true)]
    [InlineData("\"x == y && z\" contains \"==\"", true)]
    [InlineData("\"(text)\" == \"(text)\"", true)]
    [InlineData("1\t>=\t1", true)]
    public void Conditions_ParseOperatorsAndQuotedText(string condition, bool expected)
    {
        RunScript(CreateScript($"if {condition}", "print yes", "else", "print no", "end"))
            .Should().Be(expected ? "yes" : "no");
    }

    [Theory]
    [InlineData("false||true")]
    [InlineData("a == b && c")]
    [InlineData("(unfinished")]
    [InlineData("text \" with quotes")]
    [InlineData("first\nsecond")]
    public void Conditions_TreatInterpolatedValuesAsData(string value)
    {
        string file = Path.Combine(_tempDir, "condition.txt");
        File.WriteAllText(file, value);
        RunScript(CreateScript($"read value = {file}", "if {value} == {value}", "print equal", "end"))
            .Should().Be("equal");
    }

    [Fact]
    public void Conditions_InterpolateOnlyOnce()
    {
        RunScript(CreateScript("set text = {later}", "set later = expanded",
            "if {text} == {later}", "print wrong", "else", "print literal", "end"))
            .Should().Be("literal");
    }

    [Theory]
    [InlineData("(true")]
    [InlineData("true)")]
    [InlineData("\"unclosed")]
    [InlineData("true&&")]
    [InlineData("not")]
    public void Conditions_MalformedExpressionCanBeCaught(string condition)
    {
        RunScript(CreateScript("try", $"if {condition}", "print wrong", "end",
            "catch", "print caught", "end")).Should().Be("caught");
        GlobalVariables.isErrorCommand.Should().BeFalse();
    }

    [Fact]
    public void Keywords_AcceptTabsAndInvariantCasing()
    {
        var savedCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");
            RunScript(CreateScript("FUNC\tshow", "IF\ttrue", "EACH\tx\tIN\t1..1", "PRINT\t{1}", "END", "END", "END",
                "CALL\tshow\t\"hello world\"" )).Should().Be("hello world");
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = savedCulture; }
    }

    [Fact]
    public void Parameters_QuotedMarkerAndEmptyArgumentsArePreserved()
    {
        string path = Path.Combine(_tempDir, "script -p name.xt");
        File.WriteAllLines(path, new[] { "print {argc}|{1}|{2}|{3}" });
        RunScript('"' + path + '"', "-p \"one -p two\"\t\"\"\tlast")
            .Should().Be("3|one -p two||last");
        RunScript('"' + path + '"', "-p").Should().Be("0|{1}|{2}|{3}");
    }

    [Fact]
    public void Functions_RestoreAllArgumentsAndArgc()
    {
        string args = string.Join(' ', Enumerable.Range(1, 25).Select(n => $"arg{n}"));
        RunScriptLines(CreateScript("func show", "print {argc}:{1}:{25}:{50}", "end",
            "set 50 = sparse", "call show local", "print {argc}:{1}:{25}:{50}"), "-p " + args)
            .Should().Equal("1:local:{25}:{50}", "25:arg1:arg25:sparse");
    }

    [Fact]
    public void Functions_RemoveNewArgumentsAndResetResult()
    {
        RunScriptLines(CreateScript("func value", "return previous", "end", "func empty", "end",
            "call value first second", "print {result}", "call empty new",
            "print {argc}:{1}:{2}:{result}")).Should().Equal("previous", "0:{1}:{2}:");
    }

    [Fact]
    public void Functions_PreserveWhitespaceAndQuotesInVariableArguments()
    {
        string data = Path.Combine(_tempDir, "argument.txt");
        File.WriteAllText(data, "a \"quoted\" value");
        RunScript(CreateScript("func show", "print {argc}:{1}:{2}", "end", $"read text = {data}",
            "call show {text} \"\"" )).Should().Be("2:a \"quoted\" value:");
    }

    [Fact]
    public void Functions_RestoreCallerArgumentsAfterException()
    {
        RunScript(CreateScript("func fail", "write \"missing folder/out.txt\" text", "end",
            "try", "call fail local extra", "catch", "print {argc}:{1}:{2}", "end"), "-p original")
            .Should().Be("1:original:{2}");
        GlobalVariables.isErrorCommand.Should().BeFalse();
    }

    [Theory]
    [InlineData("read data = missing.txt")]
    [InlineData("set n = eval 1 / 0")]
    [InlineData("set n = eval invalid")]
    [InlineData("call missing")]
    [InlineData("set text = substr abc 0 -1")]
    public void Try_StopsAtFirstFailureAndResumesAfterCatch(string failingLine)
    {
        string output = RunScript(CreateScript("try", failingLine, "print wrong", "catch",
            "print caught:{error}", "end", "print after:{error}"));
        output.Should().Contain("caught:true").And.EndWith("after:false").And.NotContain("wrong");
        GlobalVariables.isErrorCommand.Should().BeFalse();
    }

    [Fact]
    public void Try_NestedFailureWithoutCatchReachesOuterHandler()
    {
        string output = RunScript(CreateScript("try", "try", "read x = missing.txt", "end",
            "print wrong", "catch", "print outer", "end", "print done"));
        output.Should().EndWith("outer" + Environment.NewLine + "done").And.NotContain("wrong");
    }

    [Fact]
    public void Try_FailedCatchReachesOuterHandler()
    {
        string output = RunScript(CreateScript("try", "try", "call first_missing", "catch",
            "call second_missing", "end", "print wrong", "catch", "print outer:{error_message}", "end"));
        output.Should().Contain("outer:Function 'second_missing' not found.").And.NotContain("wrong");
    }

    [Fact]
    public void Functions_UnboundedRecursionReportsAnError()
    {
        RunScript(CreateScript("func recurse", "call recurse", "end", "try", "call recurse", "catch",
            "print caught", "end", "print {argc}"))
            .Should().Contain("limit of 128").And.EndWith("caught" + Environment.NewLine + "0");
    }

    [Fact]
    public void FileOperations_HandleQuotedPathsAndEmptyText()
    {
        RunScript(CreateScript("write \"out file.txt\" \"first\"",
            "append \"out file.txt\" \"\"", "read text = \"out file.txt\"", "print {text}"))
            .Should().Be("first");
        File.ReadAllText(Path.Combine(_tempDir, "out file.txt"))
            .Should().Be("first" + Environment.NewLine + Environment.NewLine);
    }

    [Theory]
    [InlineData("2147483647..2147483647", "2147483647")]
    [InlineData("-2147483647..-2147483648", "-2147483647,-2147483648")]
    public void Each_IntBoundaryTerminatesWithoutWrapping(string range, string expected)
    {
        RunScriptLines(CreateScript($"each n in {range}", "print {n}", "if {i} > 3", "break", "end", "end"))
            .Should().Equal(expected.Split(','));
    }

    [Theory]
    [InlineData("loop 2")]
    [InlineData("each value in 1..2")]
    [InlineData("each value in a,b")]
    [InlineData("while {inner} < 2")]
    [InlineData("each value in lines:data")]
    public void NestedLoops_RestoreOuterIteration(string innerLoop)
    {
        string data = Path.Combine(_tempDir, "lines.txt");
        File.WriteAllText(data, "a\nb");
        RunScriptLines(CreateScript($"read data = {data}", "loop 2", "set inner = 0", innerLoop,
            "set inner = eval {inner} + 1", "end", "print outer:{i}", "end"))
            .Should().Equal("outer:1", "outer:2");
    }

    [Fact]
    public void NestedLoops_RestoreOuterIterationAfterCaughtFailure()
    {
        RunScript(CreateScript("loop 1", "try", "loop 2", "if {i} == 2", "call missing", "end", "end",
            "catch", "print outer:{i}", "end", "end"))
            .Should().EndWith("outer:1");
    }

    [Fact]
    public void Return_InsideTryAndLoopExitsOnlyTheFunction()
    {
        RunScript(CreateScript("func choose", "loop 3", "try", "if {i} == 2", "return done", "end",
            "catch", "print wrong", "end", "end", "print wrong", "end", "call choose", "print {result}"))
            .Should().Be("done");
    }

    [Fact]
    public void Conditions_ExcessiveNestingIsCatchable()
    {
        string condition = new string('(', 130) + "true" + new string(')', 130);
        RunScript(CreateScript("try", $"if {condition}", "print wrong", "end", "catch",
            "print {error_message}", "end")).Should().Be("Condition nesting is too deep.");
    }

    [Theory]
    [InlineData("if true\nelse\nelse\nend", "cannot follow 'else'")]
    [InlineData("if true\nelse\nelif true\nend", "cannot follow 'else'")]
    [InlineData("try\ncatch\ncatch\nend", "cannot follow 'catch'")]
    [InlineData("loop 1\nfunc wrong\nbreak\nend\nend", "outside of a loop")]
    [InlineData("set = value", "variable name")]
    [InlineData("func same\nend\nfunc SAME\nend", "already defined")]
    [InlineData("if\nend", "missing expression")]
    public void Check_RejectsInvalidStructureAndSetsCommandError(string script, string message)
    {
        string path = CreateScript(script.Split('\n'));
        RunCheck(path).Should().Contain(message);
        GlobalVariables.isErrorCommand.Should().BeTrue();
        RunScript(path).Should().Contain("Script not executed");
    }

    [Fact]
    public void Check_MissingFileSetsCommandError()
    {
        RunCheck(Path.Combine(_tempDir, "missing.xt")).Should().Contain("not found");
        GlobalVariables.isErrorCommand.Should().BeTrue();
    }

    [Fact]
    public void Substr_ClampsLengthWithoutIntegerOverflow()
    {
        RunScript(CreateScript("set value = substr abc 1 2147483647", "print {value}"))
            .Should().Be("bc");
    }

    [Fact]
    public void Check_AcceptsAllDocumentedExamples()
    {
        string[] examples = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "TermXT_Examples"), "*.xt");
        examples.Should().HaveCountGreaterThanOrEqualTo(6);
        foreach (string example in examples)
        {
            RunCheck('"' + example + '"').Should().Contain("is valid", example);
            GlobalVariables.isErrorCommand.Should().BeFalse(example);
        }
    }

    [Theory]
    [InlineData("", "reader")]
    [InlineData("-p \"Ada Lovelace\"", "Ada Lovelace")]
    public void DocumentedLanguageDemo_RunsAndWritesExpectedFile(string args, string name)
    {
        string example = Path.Combine(AppContext.BaseDirectory, "TermXT_Examples", "language_features.xt");
        string output = RunScript('"' + example + '"', args);
        output.Should().StartWith("Hello " + name).And.Contain("Selected: 2").And.Contain("Selected: 3")
            .And.Contain("Recovered:").And.NotContain("This line is skipped.").And.EndWith("Done. Error state: false");
        File.ReadAllText(Path.Combine(_tempDir, "TermXT demo output.txt"))
            .Should().Be($"Hello {name}{Environment.NewLine}Selected: 2, 3{Environment.NewLine}");
        GlobalVariables.isErrorCommand.Should().BeFalse();
    }

    [Theory]
    [InlineData("countdown.xt", "", "10|Countdown")]
    [InlineData("countdown.xt", "-p 3 \"Coffee break\"", "3|Coffee break")]
    [InlineData("portcheck.xt", "", "8.8.8.8|53,80,443")]
    [InlineData("portcheck.xt", "-p localhost 22,80", "localhost|22,80")]
    [InlineData("netaudit.xt", "", "8.8.8.8|53,80,443")]
    [InlineData("netaudit.xt", "-p localhost 22,80", "localhost|22,80")]
    public void DocumentedExamples_UseSuppliedArgumentsOrDefaults(string file, string args, string expected)
    {
        string[] lines = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "TermXT_Examples", file));
        // Execute only the parameter section; the rest intentionally waits or accesses the network.
        int start = Array.FindIndex(lines, line => line == "if {argc} >= 1");
        int second = Array.FindIndex(lines, start + 1, line => line == "if {argc} >= 2");
        int end = Array.FindIndex(lines, second + 1, line => line == "end");
        start.Should().BeGreaterThanOrEqualTo(0);
        end.Should().BeGreaterThan(start);
        string print = file == "countdown.xt" ? "print {seconds}|{label}" :
            file == "portcheck.xt" ? "print {host}|{ports}" : "print {scan_host}|{scan_ports}";
        RunScript(CreateScript(lines[start..(end + 1)].Append(print).ToArray()), args).Should().Be(expected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Pipeline_FailureSkipsLaterStagesAndRestoresConsoleState(bool throws)
    {
        int laterCalls = 0;
        WithCommands(new TestCommand("xt_test_fail", _ =>
            {
                if (throws) throw new InvalidOperationException("test failure");
                GlobalVariables.isErrorCommand = true;
            }), new TestCommand("xt_test_later", _ => laterCalls++), () =>
            {
                RunScript(CreateScript("try", "capture output = xt_test_fail | xt_test_later",
                    "print wrong", "catch", "print caught", "end", "print after"))
                    .Should().Contain("caught").And.EndWith("after").And.NotContain("wrong");
            });
        laterCalls.Should().Be(0);
        GlobalVariables.isPipeCommand.Should().BeFalse();
        GlobalVariables.pipeCmdCount.Should().Be(0);
        GlobalVariables.pipeCmdCountTemp.Should().Be(0);
    }

    [Fact]
    public void Pipeline_QuotedPipeRemainsInCommandArgument()
    {
        string received = "";
        WithCommands(new TestCommand("xt_test_echo", command => { received = command; Console.Write("captured"); }),
            new TestCommand("xt_test_unused", _ => { }), () =>
            {
                RunScript(CreateScript("capture value = xt_test_echo \"a|b\"", "print {value}"))
                    .Should().Be("captured");
            });
        received.Should().Be("xt_test_echo \"a|b\"");
    }

    private void WithCommands(ITerminalCommand first, ITerminalCommand second, Action action)
    {
        var commands = (Dictionary<string, ITerminalCommand>)typeof(CommandRepository)
            .GetField("s_terminalCommands", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        string savedAliasFile = GlobalVariables.aliasFile;
        GlobalVariables.aliasFile = Path.Combine(_tempDir, "aliases.json");
        File.WriteAllText(GlobalVariables.aliasFile, "[]");
        commands.Add(first.Name, first);
        commands.Add(second.Name, second);
        try { action(); }
        finally
        {
            commands.Remove(first.Name);
            commands.Remove(second.Name);
            GlobalVariables.aliasFile = savedAliasFile;
        }
    }

    private sealed class TestCommand(string name, Action<string> execute) : ITerminalCommand
    {
        public string Name => name;
        public void Execute(string args) => execute(args);
    }
}
