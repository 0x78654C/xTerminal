using System.Text;
using Core.DirFiles;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

public partial class TermXTEditorSyntaxTests
{
    [Theory]
    [InlineData("if (true\nend", "XT1001")]
    [InlineData("if true &&\nend", "XT1001")]
    [InlineData("if not\nend", "XT1001")]
    [InlineData("while > 2\nend", "XT1001")]
    [InlineData("if \"unfinished\nend", "XT1001")]
    [InlineData("each row values\nend", "XT1002")]
    [InlineData("each row in 1..999999999999\nend", "XT1002")]
    [InlineData("call missing", "XT1003")]
    [InlineData("call \"unfinished", "XT1003")]
    [InlineData("loop 0\nend", "XT1004")]
    [InlineData("loop 2147483648\nend", "XT1004")]
    [InlineData("set value = eval 2 +", "XT1005")]
    [InlineData("set value = eval 1 / 0", "XT1005")]
    [InlineData("set value = substr abc 0 -1", "XT1005")]
    [InlineData("set value = substr abc first 2", "XT1005")]
    [InlineData("set value = replace abc \"\" x", "XT1005")]
    [InlineData("write output.txt", "XT1006")]
    [InlineData("append \"unfinished text", "XT1006")]
    public void TermXtDiagnostics_ReportInvalidConstantExpressionsAndArguments(string script, string code)
    {
        Diagnostics(TermXtEditorAtMarker(script + "$$")).Should().Contain(d =>
            d.LineNumber == 1 && d.Code == code && d.Severity == "Error");
    }

    [Theory]
    [InlineData("print {missing}", "XT2001")]
    [InlineData("each row in lines:missing\nend", "XT2001")]
    [InlineData("set CWD = ignored", "XT2002")]
    [InlineData("set date = ignored", "XT2002")]
    [InlineData("try ignored\ncatch\nend", "XT2003")]
    [InlineData("wait -1", "XT2004")]
    [InlineData("wait forever", "XT2004")]
    [InlineData("func greet\nend\nall greet", "XT2005")]
    public void TermXtDiagnostics_ReportAdvisoryIssuesAsWarnings(string script, string code)
    {
        Diagnostics(TermXtEditorAtMarker(script + "$$")).Should().ContainSingle().Which.Should().Match<DiagnosticInfo>(d =>
            d.Code == code && d.Severity == "Warning");
    }

    [Fact]
    public void TermXtDiagnostics_AcceptRuntimeVariablesFunctionsLiteralsAndRegisteredCommands()
    {
        var editor = TermXtEditorAtMarker("""
            # {not_a_variable}
            print "{DATE} {time} {CWD} {USER} {PC} {argc} {27} {i} {result} {error} {error_message}"
            print {FORWARD}
            set forward = value
            input count = "Count?"
            loop {count}
              print {i}
            end
            set value = eval {count} + 1
            set literal = eval
            set text = substr "hello world" -2 999
            set text = replace "hello world" "world" ""
            if not(false) && ("a || b" contains "||")
              call "greet user" "Ada Lovelace"
            end
            func greet user
              print {1}
            end
            call {forward}
            each row in lines:{forward}
              print {row}
            end
            write "path with spaces.txt" ""
            ping localhost
            $$
            """, new[] { "ping" });
        Diagnostics(editor).Should().BeEmpty();
    }

    [Fact]
    public void TermXtDiagnostics_DisplayBothSeveritiesAndNavigateToExactLocations()
    {
        var editor = TermXtEditorAtMarker("set name = Ada\n  print \"{nmae}\"\n  call missing$$");
        var frame = GetPrivateField<StringBuilder>(editor, "_frame");
        InvokePrivate(editor, "RenderHeader", 160);
        frame.ToString().Should().Contain("[E:1] [W:1]");
        GetPrivateField<HashSet<int>>(editor, "_diagnosticLineIndexes").Should().BeEquivalentTo(new[] { 1, 2 });
        Diagnostics(editor).Select(d => d.Severity).Should().Equal("Warning", "Error");

        PressDetailsKey(editor, ConsoleKey.F2);
        frame.Clear();
        InvokePrivate(editor, "RenderMessageDetails", 120, 20);
        frame.ToString().Should().Contain("[WARNING]").And.Contain("[ERROR]")
            .And.Contain("XT2001").And.Contain("XT1003");
        PressDetailsKey(editor, ConsoleKey.Enter);
        GetPrivateField<int>(editor, "_cursorLine").Should().Be(1);
        GetPrivateField<int>(editor, "_cursorCol").Should().Be(Lines(editor)[1].IndexOf('{'));
        InvokePrivate(editor, "ExecuteEditorCommand", "next-error");
        GetPrivateField<int>(editor, "_cursorLine").Should().Be(2);
        GetPrivateField<int>(editor, "_cursorCol").Should().Be(Lines(editor)[2].IndexOf("missing", StringComparison.Ordinal));
        InvokePrivate(editor, "ExecuteEditorCommand", "next-warning");
        GetPrivateField<int>(editor, "_cursorLine").Should().Be(1);

        PressDetailsKey(editor, ConsoleKey.F2);
        PressDetailsKey(editor, ConsoleKey.DownArrow);
        PressDetailsKey(editor, ConsoleKey.Enter);
        GetPrivateField<int>(editor, "_cursorLine").Should().Be(2);
        GetPrivateField<int>(editor, "_cursorCol").Should().Be(7);
    }

    [Fact]
    public void TermXtDiagnostics_RefreshUnsavedEditsAndClearTheOpenPanel()
    {
        var editor = TermXtEditorAtMarker("print {missing}$$");
        PressDetailsKey(editor, ConsoleKey.F2);
        GetPrivateField<string>(editor, "_messageDetailsText").Should().Contain("XT2001");
        Lines(editor)[0] = "print \"fixed\"";
        InvalidateScrollingDocument(editor);
        InvokePrivate(editor, "RenderHeader", 120);
        Diagnostics(editor).Should().BeEmpty();
        InvokePrivate<string>(editor, "BuildHeaderDiagnosticBadge").Should().BeEmpty();
        GetPrivateField<HashSet<int>>(editor, "_diagnosticLineIndexes").Should().BeEmpty();
        GetPrivateField<string>(editor, "_messageDetailsText").Should().Be("No errors or warnings");
        PressDetailsKey(editor, ConsoleKey.Enter);
        GetPrivateField<int>(editor, "_messageDetailsSelectedIndex").Should().Be(-1);
        GetPrivateField<bool>(editor, "_messageDetailsActive").Should().BeTrue();
    }
}
