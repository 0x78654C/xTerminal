using System.Text;
using Core.DirFiles;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

public partial class TermXTEditorSyntaxTests
{
    [Fact]
    public void SearchPrompt_ArrowsInsertAndDeleteAtTheCaret()
    {
        var editor = OpenTestSearch("ac");
        SearchKey(editor, ConsoleKey.LeftArrow);
        TypeSearch(editor, "b");
        SearchState(editor).Should().Be(("abc", 2));
        SearchKey(editor, ConsoleKey.Home);
        SearchKey(editor, ConsoleKey.RightArrow);
        SearchKey(editor, ConsoleKey.Delete);
        SearchState(editor).Should().Be(("ac", 1));
        SearchKey(editor, ConsoleKey.End);
        SearchKey(editor, ConsoleKey.Backspace);
        SearchState(editor).Should().Be(("a", 1));
        Lines(editor).Should().Equal("original document");
    }

    [Fact]
    public void SearchPrompt_SubmitsEditedQueryAndEmptyEnterRepeatsIt()
    {
        var editor = OpenTestSearch("targt");
        Lines(editor).Clear();
        Lines(editor).AddRange(new[] { "first line", "target", "another target" });
        SearchKey(editor, ConsoleKey.LeftArrow);
        TypeSearch(editor, "e");
        SearchKey(editor, ConsoleKey.Enter);
        GetPrivateField<string>(editor, "_lastSearch").Should().Be("target");
        GetPrivateField<int>(editor, "_cursorLine").Should().Be(1);
        SearchState(editor).Should().Be(("", 0));
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('/', ConsoleKey.Oem2, false, false, false));
        SearchKey(editor, ConsoleKey.Enter);
        GetPrivateField<int>(editor, "_cursorLine").Should().Be(2);
        GetPrivateField<int>(editor, "_cursorCol").Should().Be(8);
    }

    [Fact]
    public void SearchPrompt_LongQueryScrollsWithCaretAndRevealsStartOnHome()
    {
        var editor = OpenTestSearch("abcdefghijklmnop");
        VisibleSearch(editor, 7).Should().Be(("klmnop", 6));
        var frame = GetPrivateField<StringBuilder>(editor, "_frame");
        frame.Clear();
        InvokePrivate(editor, "RenderCommandLine", 5, 8);
        frame.ToString().Should().Contain("/klmnop");
        SearchKey(editor, ConsoleKey.Home);
        VisibleSearch(editor, 7).Should().Be(("abcdefg", 0));
        SearchKey(editor, ConsoleKey.RightArrow);
        VisibleSearch(editor, 7).Should().Be(("abcdefg", 1));
        SearchKey(editor, ConsoleKey.End);
        VisibleSearch(editor, 4).Should().Be(("nop", 3));
        VisibleSearch(editor, 30).Should().Be(("abcdefghijklmnop", 16));
    }

    [Fact]
    public void SearchPrompt_PasteInsertsAtCaretAndLimitLeavesTextAndCaretUnchanged()
    {
        var editor = OpenTestSearch("ac");
        SearchKey(editor, ConsoleKey.LeftArrow);
        InvokePrivate(editor, "InsertSearchText", "b paste ");
        SearchState(editor).Should().Be(("ab paste c", 9));
        InvokePrivate(editor, "InsertSearchText", new string('x', 4096));
        SearchState(editor).Should().Be(("ab paste c", 9));
        GetPrivateField<string>(editor, "_status").Should().Contain("limit");
    }

    [Fact]
    public void SearchPrompt_ControlNavigationStaysInQueryAndDoesNotEditDocument()
    {
        var editor = OpenTestSearch("one two three");
        SetPrivateField(editor, "_cursorCol", 5);
        SearchKey(editor, ConsoleKey.LeftArrow, control: true);
        SearchState(editor).Should().Be(("one two three", 8));
        SearchKey(editor, ConsoleKey.LeftArrow, control: true);
        SearchState(editor).Should().Be(("one two three", 4));
        SearchKey(editor, ConsoleKey.RightArrow, control: true);
        SearchState(editor).Should().Be(("one two three", 8));
        SearchKey(editor, ConsoleKey.Home, control: true);
        SearchState(editor).Should().Be(("one two three", 0));
        SearchKey(editor, ConsoleKey.End, control: true);
        SearchState(editor).Should().Be(("one two three", 13));
        SearchKey(editor, ConsoleKey.D, control: true);
        SearchKey(editor, ConsoleKey.Z, control: true);
        SearchKey(editor, ConsoleKey.A, control: true);
        SearchState(editor).Should().Be(("one two three", 13));
        GetPrivateField<int>(editor, "_cursorCol").Should().Be(5);
        Lines(editor).Should().Equal("original document");
    }

    [Fact]
    public void SearchPrompt_EscapeAndReopenResetCaretAndScroll()
    {
        var editor = OpenTestSearch("long query");
        VisibleSearch(editor, 4);
        SearchKey(editor, ConsoleKey.Escape);
        GetPrivateField<object>(editor, "_mode").ToString().Should().Be("Normal");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('/', ConsoleKey.Oem2, false, false, false));
        SearchState(editor).Should().Be(("", 0));
        VisibleSearch(editor, 4).Should().Be(("", 0));
        SearchKey(editor, ConsoleKey.Backspace);
        SearchKey(editor, ConsoleKey.Delete);
        SearchKey(editor, ConsoleKey.LeftArrow);
        SearchKey(editor, ConsoleKey.RightArrow);
        SearchState(editor).Should().Be(("", 0));
        TypeSearch(editor, "new");
        SearchState(editor).Should().Be(("new", 3));
    }

    [Fact]
    public void SearchPrompt_UnicodeNavigationAndDeletionKeepSurrogatePairsIntact()
    {
        var editor = OpenTestSearch("a😀b");
        SearchKey(editor, ConsoleKey.LeftArrow);
        SearchState(editor).Should().Be(("a😀b", 3));
        SearchKey(editor, ConsoleKey.LeftArrow);
        SearchState(editor).Should().Be(("a😀b", 1));
        SearchKey(editor, ConsoleKey.Delete);
        SearchState(editor).Should().Be(("ab", 1));
        InvokePrivate(editor, "InsertSearchText", "😀");
        SearchKey(editor, ConsoleKey.Backspace);
        SearchState(editor).Should().Be(("ab", 1));
    }

    private static TermXTEditor OpenTestSearch(string query)
    {
        var editor = new TermXTEditor(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".xt"));
        Lines(editor).Clear();
        Lines(editor).Add("original document");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('/', ConsoleKey.Oem2, false, false, false));
        TypeSearch(editor, query);
        return editor;
    }

    private static void TypeSearch(TermXTEditor editor, string text)
    {
        foreach (char c in text)
            InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo(c, ConsoleKey.NoName, false, false, false));
    }

    private static void SearchKey(TermXTEditor editor, ConsoleKey key, bool control = false)
    {
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', key, false, false, control));
    }

    private static (string Text, int Cursor) SearchState(TermXTEditor editor)
    {
        object input = GetPrivateField<object>(editor, "_searchInput");
        return ((string)input.GetType().GetProperty("Text")!.GetValue(input)!,
            (int)input.GetType().GetProperty("Cursor")!.GetValue(input)!);
    }

    private static (string Text, int Cursor) VisibleSearch(TermXTEditor editor, int width)
    {
        object input = GetPrivateField<object>(editor, "_searchInput");
        object[] args = { width, 0 };
        string text = (string)input.GetType().GetMethod("VisibleText")!.Invoke(input, args)!;
        return (text, (int)args[1]);
    }
}
