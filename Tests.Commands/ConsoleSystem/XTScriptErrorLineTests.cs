using Core;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

public partial class XTScriptTests
{
    [Theory]
    [InlineData("write \"missing folder/out.txt\" text", "out.txt")]
    [InlineData("append \"missing folder/out.txt\" text", "out.txt")]
    [InlineData("set text = replace abc \"\" x", "")]
    [InlineData("call \"unclosed", "Unclosed double quote")]
    [InlineData("each n in 2147483648..2147483649\nend", "")]
    public void RuntimeError_ReportsOriginalLineIncludingCommentsAndBlankLines(string statement, string message)
    {
        string output = RunScript(CreateScript(new[] { "# comment", "", "print before" }
            .Concat(statement.Split('\n')).Append("print wrong").ToArray()));

        output.Should().Contain("[xt line 4]").And.NotContain("wrong");
        if (message.Length > 0) output.Should().Contain(message);
        output.Split("[xt line").Should().HaveCount(2);
        GlobalVariables.isErrorCommand.Should().BeTrue();
    }

    [Theory]
    [InlineData("if (true", 3)]
    [InlineData("while (true", 3)]
    [InlineData("if false\nprint wrong\nelif (true", 5)]
    public void ConditionError_ReportsTheConditionLine(string condition, int expectedLine)
    {
        string output = RunScript(CreateScript(new[] { "# comment", "" }
            .Concat(condition.Split('\n')).Concat(new[] { "print wrong", "end" }).ToArray()));

        output.Should().Contain($"[xt line {expectedLine}] Unclosed '(' in condition.")
            .And.NotContain("wrong");
        GlobalVariables.isErrorCommand.Should().BeTrue();
    }

    [Fact]
    public void WhileConditionError_AfterAnIterationReportsTheWhileLine()
    {
        string output = RunScript(CreateScript("set again = true", "while {again} || not",
            "set again = false", "end", "print wrong"));

        output.Should().Contain("[xt line 2] 'not' requires a condition.").And.NotContain("wrong");
        GlobalVariables.isErrorCommand.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FunctionError_PreservesInnerLineThroughNestedCallsAndLoops(bool caught)
    {
        string[] script =
        {
            "func inner", "loop 1", "", "write \"missing folder/out.txt\" text", "end", "end",
            "func outer", "call inner", "end"
        };
        string[] invocation = caught
            ? new[] { "try", "call outer", "print wrong", "catch", "print caught:{error_message}", "end" }
            : new[] { "call outer", "print wrong" };

        string output = RunScript(CreateScript(script.Concat(invocation).ToArray()));

        output.Should().Contain("[xt line 4]").And.Contain("out.txt").And.NotContain("wrong");
        output.Split("[xt line").Should().HaveCount(2);
        if (caught) output.Should().StartWith("caught:[xt line 4]");
        GlobalVariables.isErrorCommand.Should().Be(!caught);
    }

    [Theory]
    [InlineData("write \"missing folder/out.txt\" text")]
    [InlineData("call missing")]
    public void NestedTryWithoutCatch_PreservesOriginalErrorLine(string statement)
    {
        string output = RunScript(CreateScript("try", "try", "# comment", statement, "end",
            "print wrong", "catch", "print caught:{error_message}", "end", "print after:{error}"));

        output.Should().Contain("caught:[xt line 4]").And.NotContain("[xt line 1]")
            .And.NotContain("[xt line 2]").And.NotContain("wrong").And.EndWith("after:false");
        GlobalVariables.isErrorCommand.Should().BeFalse();
    }

    [Theory]
    [InlineData("write \"missing folder/out.txt\" text")]
    [InlineData("call missing")]
    public void TryWithoutCatch_PrintsErrorOnceAndKeepsErrorState(string statement)
    {
        string output = RunScript(CreateScript("try", "", statement, "print wrong", "end", "print after:{error}"));

        output.Should().Contain("[xt line 3]").And.NotContain("wrong").And.EndWith("after:true");
        output.Split("[xt line").Should().HaveCount(2);
        GlobalVariables.isErrorCommand.Should().BeTrue();
    }

    [Fact]
    public void CatchFailure_ReportsTheNewFailureLine()
    {
        string output = RunScript(CreateScript("try", "try", "write \"missing folder/first.txt\" text",
            "catch", "append \"missing folder/second.txt\" text", "end", "catch",
            "print caught:{error_message}", "end"));

        output.Should().StartWith("caught:[xt line 5]").And.Contain("second.txt").And.NotContain("first.txt");
        GlobalVariables.isErrorCommand.Should().BeFalse();
    }

    [Theory]
    [InlineData("call missing", "Function 'missing' not found.")]
    [InlineData("set n = eval 1 / 0", "Math error:")]
    [InlineData("read text = missing.txt", "File not found:")]
    public void ReportedError_IncludesLineInErrorMessage(string statement, string message)
    {
        string output = RunScript(CreateScript("# comment", "", statement, "print saved:{error_message}"));

        output.Should().Contain($"[xt line 3] {message}").And.Contain($"saved:[xt line 3] {message}");
        GlobalVariables.isErrorCommand.Should().BeTrue();
    }

    [Theory]
    [InlineData("run xt_test_fail", false)]
    [InlineData("xt_test_fail", false)]
    [InlineData("capture value = xt_test_fail", false)]
    [InlineData("capture value = xt_test_fail | xt_test_later", false)]
    [InlineData("run xt_test_fail", true)]
    [InlineData("xt_test_fail", true)]
    [InlineData("capture value = xt_test_fail", true)]
    [InlineData("capture value = xt_test_fail | xt_test_later", true)]
    public void CommandFailure_ReportsInvokingScriptLine(string statement, bool throws)
    {
        WithCommands(new TestCommand("xt_test_fail", _ =>
        {
            if (throws) throw new InvalidOperationException("test failure");
            GlobalVariables.isErrorCommand = true;
        }), new TestCommand("xt_test_later", _ => Console.WriteLine("wrong")), () =>
        {
            string output = RunScript(CreateScript("# comment", "", statement));
            output.Should().Contain(throws ? "[xt line 3] test failure" : "[xt line 3] Command failed:")
                .And.NotContain("wrong");
            output.Split("[xt line").Should().HaveCount(2);
            GlobalVariables.isErrorCommand.Should().BeTrue();
        });
    }

    [Fact]
    public void SyntaxError_ReportsSameLineForCheckAndExecution()
    {
        string script = CreateScript("# comment", "", "set missing_assignment");
        RunCheck(script).Should().Contain("Line 3:");
        RunScript(script).Should().Contain("Line 3:").And.Contain("Script not executed");
        GlobalVariables.isErrorCommand.Should().BeTrue();
    }
}
