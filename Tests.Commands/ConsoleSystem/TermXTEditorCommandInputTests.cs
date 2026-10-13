using System.Text;
using Core.DirFiles;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

public partial class TermXTEditorSyntaxTests
{
    [Fact]
    public void CommandPrompt_ArrowsInsertDeleteAndBackspaceAtTheCaret()
    {
        var editor = OpenTestCommand("synax xt");
        CommandKey(editor, ConsoleKey.Home);
        for (int i = 0; i < 3; i++) CommandKey(editor, ConsoleKey.RightArrow);
        TypeCommand(editor, "t");
        CommandState(editor).Should().Be(("syntax xt", 4));
        CommandKey(editor, ConsoleKey.End);
        CommandKey(editor, ConsoleKey.LeftArrow);
        CommandKey(editor, ConsoleKey.Delete);
        CommandKey(editor, ConsoleKey.Backspace);
        TypeCommand(editor, "py");
        CommandState(editor).Should().Be(("syntax py", 9));
        CommandKey(editor, ConsoleKey.Enter);
        GetPrivateField<TermXTEditorSyntax>(editor, "_syntax").Should().Be(TermXTEditorSyntax.Python);
        GetPrivateField<object>(editor, "_mode").ToString().Should().Be("Normal");
        CommandState(editor).Should().Be(("", 0));
        Lines(editor).Should().Equal("original document");
    }

    [Fact]
    public void CommandPrompt_LongCommandScrollsWithCaretAndRendersEditedText()
    {
        var editor = OpenTestCommand("abcdefghijklmnop");
        VisibleCommand(editor, 7).Should().Be(("klmnop", 6));
        var frame = GetPrivateField<StringBuilder>(editor, "_frame");
        frame.Clear();
        InvokePrivate(editor, "RenderCommandLine", 5, 8);
        frame.ToString().Should().Contain(":klmnop");
        CommandKey(editor, ConsoleKey.Home);
        VisibleCommand(editor, 7).Should().Be(("abcdefg", 0));
        CommandKey(editor, ConsoleKey.Delete);
        frame.Clear();
        InvokePrivate(editor, "RenderCommandLine", 5, 8);
        frame.ToString().Should().Contain(":bcdefgh");
        CommandKey(editor, ConsoleKey.End);
        VisibleCommand(editor, 4).Should().Be(("nop", 3));
        VisibleCommand(editor, 30).Should().Be(("bcdefghijklmnop", 15));
    }

    [Fact]
    public void CommandPrompt_PasteInsertsAtCaretAndLimitKeepsTextAndCaretUnchanged()
    {
        var editor = OpenTestCommand("ac");
        CommandKey(editor, ConsoleKey.LeftArrow);
        InvokePrivate(editor, "InsertCommandText", "b paste ");
        CommandState(editor).Should().Be(("ab paste c", 9));
        InvokePrivate(editor, "InsertCommandText", new string('x', 4096));
        CommandState(editor).Should().Be(("ab paste c", 9));
        GetPrivateField<string>(editor, "_status").Should().Contain("limit");
    }

    [Fact]
    public void CommandPrompt_ControlKeysStayInPromptWithoutChangingDocument()
    {
        var editor = OpenTestCommand("one two three");
        SetPrivateField(editor, "_cursorCol", 5);
        CommandKey(editor, ConsoleKey.LeftArrow, control: true);
        CommandState(editor).Should().Be(("one two three", 8));
        CommandKey(editor, ConsoleKey.LeftArrow, control: true);
        CommandState(editor).Should().Be(("one two three", 4));
        CommandKey(editor, ConsoleKey.RightArrow, control: true);
        CommandState(editor).Should().Be(("one two three", 8));
        CommandKey(editor, ConsoleKey.Home, control: true);
        CommandState(editor).Should().Be(("one two three", 0));
        CommandKey(editor, ConsoleKey.End, control: true);
        CommandState(editor).Should().Be(("one two three", 13));
        foreach (ConsoleKey key in new[] { ConsoleKey.D, ConsoleKey.X, ConsoleKey.Z, ConsoleKey.Y, ConsoleKey.A })
            CommandKey(editor, key, control: true);
        CommandState(editor).Should().Be(("one two three", 13));
        GetPrivateField<int>(editor, "_cursorCol").Should().Be(5);
        Lines(editor).Should().Equal("original document");
    }

    [Fact]
    public void CommandPrompt_EscapeAndReopenResetCaretAndScrollAndHandleEmptyInput()
    {
        var editor = OpenTestCommand("long command");
        VisibleCommand(editor, 4);
        CommandKey(editor, ConsoleKey.Escape);
        TypeCommand(editor, ":");
        CommandState(editor).Should().Be(("", 0));
        VisibleCommand(editor, 4).Should().Be(("", 0));
        foreach (ConsoleKey key in new[] { ConsoleKey.Backspace, ConsoleKey.Delete, ConsoleKey.LeftArrow, ConsoleKey.RightArrow })
            CommandKey(editor, key);
        CommandState(editor).Should().Be(("", 0));
        TypeCommand(editor, "new");
        CommandState(editor).Should().Be(("new", 3));
    }

    [Fact]
    public void CommandPrompt_UnicodeNavigationAndDeletionKeepSurrogatePairsIntact()
    {
        var editor = OpenTestCommand("a😀b");
        CommandKey(editor, ConsoleKey.LeftArrow);
        CommandState(editor).Should().Be(("a😀b", 3));
        CommandKey(editor, ConsoleKey.LeftArrow);
        CommandState(editor).Should().Be(("a😀b", 1));
        CommandKey(editor, ConsoleKey.Delete);
        CommandState(editor).Should().Be(("ab", 1));
        InvokePrivate(editor, "InsertCommandText", "😀");
        CommandKey(editor, ConsoleKey.Backspace);
        CommandState(editor).Should().Be(("ab", 1));
    }

    private static TermXTEditor OpenTestCommand(string command)
    {
        var editor = new TermXTEditor(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt"));
        Lines(editor)[0] = "original document";
        TypeCommand(editor, ":" + command);
        return editor;
    }

    private static void TypeCommand(TermXTEditor editor, string text)
    {
        foreach (char c in text)
            InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo(c, ConsoleKey.NoName, false, false, false));
    }

    private static void CommandKey(TermXTEditor editor, ConsoleKey key, bool control = false)
    {
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', key, false, false, control));
    }

    private static (string Text, int Cursor) CommandState(TermXTEditor editor)
    {
        object input = GetPrivateField<object>(editor, "_commandInput");
        return ((string)input.GetType().GetProperty("Text")!.GetValue(input)!,
            (int)input.GetType().GetProperty("Cursor")!.GetValue(input)!);
    }

    private static (string Text, int Cursor) VisibleCommand(TermXTEditor editor, int width)
    {
        object input = GetPrivateField<object>(editor, "_commandInput");
        object[] args = { width, 0 };
        string text = (string)input.GetType().GetMethod("VisibleText")!.Invoke(input, args)!;
        return (text, (int)args[1]);
    }
}
