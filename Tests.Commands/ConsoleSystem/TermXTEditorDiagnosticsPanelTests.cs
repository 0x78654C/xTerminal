using System.Reflection;
using System.Text;
using System.Text.Json;
using Core.DirFiles;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

public partial class TermXTEditorSyntaxTests
{
    [Theory]
    [InlineData(0, 0, 3)]
    [InlineData(1, 1, 4)]
    public void DiagnosticsPanel_EnterJumpsToSelectedWarningOrError(int downCount, int line, int column)
    {
        var editor = RustDiagnosticEditor("fn main() {\n    missing();\n}$$");
        using var client = InstallRustDiagnosticClient(editor);
        PublishRustDiagnostics(client, editor, RustDiagnosticFixture);
        PressDetailsKey(editor, ConsoleKey.F2);
        string[] before = Lines(editor).ToArray();
        for (int i = 0; i < downCount; i++) PressDetailsKey(editor, ConsoleKey.DownArrow);
        GetPrivateField<int>(editor, "_messageDetailsSelectedIndex").Should().Be(downCount);
        PressDetailsKey(editor, ConsoleKey.Enter);
        GetPrivateField<bool>(editor, "_messageDetailsActive").Should().BeFalse();
        GetPrivateField<int>(editor, "_cursorLine").Should().Be(line);
        GetPrivateField<int>(editor, "_cursorCol").Should().Be(column);
        GetPrivateField<int>(editor, "_lastWidth").Should().Be(-1, "closing the panel must repaint the source");
        Lines(editor).Should().Equal(before, "Enter navigates without inserting text");
    }

    [Fact]
    public void DiagnosticsPanel_EndSelectsAndRevealsLastEntryAfterResize()
    {
        var editor = RustDiagnosticEditor(string.Join('\n', Enumerable.Repeat("    missing();", 80)) + "$$");
        using var client = InstallRustDiagnosticClient(editor);
        PublishRustDiagnostics(client, editor, ManyPanelDiagnostics(80));
        PressDetailsKey(editor, ConsoleKey.F2);
        PressDetailsKey(editor, ConsoleKey.End);
        GetPrivateField<int>(editor, "_messageDetailsSelectedIndex").Should().Be(79);
        GetPrivateField<int>(editor, "_messageDetailsScrollOffset").Should().BeGreaterThan(0);

        var frame = GetPrivateField<StringBuilder>(editor, "_frame");
        frame.Clear();
        InvokePrivate(editor, "RenderMessageDetails", 68, 12);
        frame.ToString().Should().Contain("> [WARNING]").And.Contain("L80:C5").And.Contain("Enter go");
        PressDetailsKey(editor, ConsoleKey.Enter);
        GetPrivateField<int>(editor, "_cursorLine").Should().Be(79);
        GetPrivateField<int>(editor, "_cursorCol").Should().Be(4);
    }

    [Fact]
    public void DiagnosticsPanel_ArrowsSelectEntriesInsteadOfWrappedDescriptionRows()
    {
        var editor = RustDiagnosticEditor("fn main() {\n    missing();\n}$$");
        using var client = InstallRustDiagnosticClient(editor);
        string items = ManyPanelDiagnostics(2, new string('x', 200) + "\n[ERROR] this is part of the description");
        PublishRustDiagnostics(client, editor, items);
        PressDetailsKey(editor, ConsoleKey.F2);
        PressDetailsKey(editor, ConsoleKey.DownArrow);
        GetPrivateField<int>(editor, "_messageDetailsSelectedIndex").Should().Be(1);
        PressDetailsKey(editor, ConsoleKey.UpArrow);
        GetPrivateField<int>(editor, "_messageDetailsSelectedIndex").Should().Be(0);
        PressDetailsKey(editor, ConsoleKey.DownArrow);
        PressDetailsKey(editor, ConsoleKey.Enter);
        GetPrivateField<int>(editor, "_cursorLine").Should().Be(1);
    }

    [Fact]
    public void DiagnosticsPanel_WheelScrollsAndKeepsSelectionVisible()
    {
        var editor = RustDiagnosticEditor(string.Join('\n', Enumerable.Repeat("    missing();", 80)) + "$$");
        using var client = InstallRustDiagnosticClient(editor);
        PublishRustDiagnostics(client, editor, ManyPanelDiagnostics(80));
        PressDetailsKey(editor, ConsoleKey.F2);
        Type mouseType = typeof(TermXTEditor).GetNestedType("MouseEventRecord", BindingFlags.NonPublic)!;
        object mouse = Activator.CreateInstance(mouseType)!;
        mouseType.GetField("EventFlags")!.SetValue(mouse, 4);
        mouseType.GetField("ButtonState")!.SetValue(mouse, unchecked(-120 << 16));
        InvokePrivate<bool>(editor, "HandleMessageDetailsMouse", mouse).Should().BeTrue();
        GetPrivateField<int>(editor, "_messageDetailsScrollOffset").Should().BeGreaterThan(0);
        object[] args = { InvokePrivateStatic<int>("MessageDetailsContentWidth",
            InvokePrivateStatic<(int width, int height)>("WindowSize").width), null! };
        InvokePrivate<List<string>>(editor, "BuildMessageDetailsRows", args);
        var indexes = (List<int>)args[1];
        indexes[GetPrivateField<int>(editor, "_messageDetailsScrollOffset")].Should().Be(
            GetPrivateField<int>(editor, "_messageDetailsSelectedIndex"));
        PressDetailsKey(editor, ConsoleKey.Home);
        GetPrivateField<int>(editor, "_messageDetailsSelectedIndex").Should().Be(0);
        GetPrivateField<int>(editor, "_messageDetailsScrollOffset").Should().Be(0);
    }

    [Fact]
    public void DiagnosticsPanel_AsyncRefreshPreservesSelectionAndClearedListCannotJump()
    {
        var editor = RustDiagnosticEditor("fn main() {\n    missing();\n}$$");
        using var client = InstallRustDiagnosticClient(editor);
        PublishRustDiagnostics(client, editor, RustDiagnosticFixture);
        PressDetailsKey(editor, ConsoleKey.F2);
        PressDetailsKey(editor, ConsoleKey.DownArrow);
        // Removing the earlier warning changes the selected error's list index.
        using var fixture = JsonDocument.Parse(RustDiagnosticFixture);
        PublishRustDiagnostics(client, editor, "[" + fixture.RootElement[0].GetRawText() + "]");
        InvokePrivate<bool>(editor, "TryApplyRustDiagnostics").Should().BeTrue();
        GetPrivateField<int>(editor, "_messageDetailsSelectedIndex").Should().Be(0);
        PressDetailsKey(editor, ConsoleKey.Enter);
        GetPrivateField<int>(editor, "_cursorLine").Should().Be(1);

        PressDetailsKey(editor, ConsoleKey.F2);
        PublishRustDiagnostics(client, editor, "[]");
        InvokePrivate<bool>(editor, "TryApplyRustDiagnostics").Should().BeTrue();
        PressDetailsKey(editor, ConsoleKey.Enter);
        GetPrivateField<bool>(editor, "_messageDetailsActive").Should().BeTrue();
        GetPrivateField<int>(editor, "_messageDetailsSelectedIndex").Should().Be(-1);
        GetPrivateField<int>(editor, "_cursorLine").Should().Be(1);
    }

    [Fact]
    public void DiagnosticsPanel_NativeEnterKeyIsHandledByThePanel()
    {
        object[] args = { (short)ConsoleKey.Enter, default(ConsoleKey) };
        InvokePrivateStatic<bool>("TryGetMessageDetailsKey", args).Should().BeTrue();
        args[1].Should().Be(ConsoleKey.Enter);
    }

    private static void PressDetailsKey(TermXTEditor editor, ConsoleKey key)
    {
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', key, false, false, false));
    }

    private static string ManyPanelDiagnostics(int count, string? description = null)
    {
        return JsonSerializer.Serialize(Enumerable.Range(0, count).Select(line => new
        {
            range = new { start = new { line, character = 4 }, end = new { line, character = 8 } },
            severity = line % 2 + 1, code = "test", message = description ?? "Diagnostic " + line
        }));
    }
}
