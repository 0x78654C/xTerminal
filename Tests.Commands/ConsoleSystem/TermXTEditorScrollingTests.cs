using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Core.DirFiles;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

public partial class TermXTEditorSyntaxTests
{
    [Theory]
    [InlineData("EnsureCSharpBlockCommentCache")]
    [InlineData("EnsureRustBlockCommentCache")]
    [InlineData("EnsureJavaScriptBlockCommentCache")]
    [InlineData("EnsurePythonMultilineStringCache")]
    public void Scrolling_SyntaxStateCacheGrowsWithoutCopyingEveryPreviousLine(string methodName)
    {
        var editor = ScrollingEditor();
        Lines(editor).Clear();
        Lines(editor).AddRange(Enumerable.Repeat("plain text", 20_000));
        var ensure = typeof(TermXTEditor).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .CreateDelegate<Action<int>>(editor);
        ensure(0);

        int lineCount = Lines(editor).Count;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int line = 1; line < lineCount; line++)
            ensure(line);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // The old arrays allocated 200–800 MB while visiting these same 20,000 lines.
        allocated.Should().BeLessThan(2_000_000);
    }

    [Theory]
    [InlineData(TermXTEditorSyntax.CSharp, "/*", "*/")]
    [InlineData(TermXTEditorSyntax.Rust, "/* /*", "*/ */")]
    [InlineData(TermXTEditorSyntax.JavaScript, "/*", "*/")]
    [InlineData(TermXTEditorSyntax.Python, "\"\"\"", "\"\"\"")]
    public void Scrolling_HighlightingUpdatesAfterChangingEarlierMultilineDelimiter(
        TermXTEditorSyntax syntax, string open, string close)
    {
        var editor = ScrollingEditor(syntax);
        Lines(editor).Clear();
        Lines(editor).AddRange(new[] { open, "return 123", close, "return 456" });
        string highlighted = InvokePrivate<string>(editor, "BuildHighlightedLine", Lines(editor)[1], 1, 0, 40);
        InvokePrivate<string>(editor, "BuildHighlightedLine", Lines(editor)[3], 3, 0, 40);

        Lines(editor)[0] = "";
        InvalidateScrollingDocument(editor);
        string updated = InvokePrivate<string>(editor, "BuildHighlightedLine", Lines(editor)[1], 1, 0, 40);
        updated.Should().NotBe(highlighted);

        Lines(editor)[0] = open;
        InvalidateScrollingDocument(editor);
        InvokePrivate<string>(editor, "BuildHighlightedLine", Lines(editor)[1], 1, 0, 40)
            .Should().Be(highlighted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Scrolling_ArrowFramesMatchFullRedrawIncludingSelectionAndWrapping(bool select)
    {
        var editor = ScrollingEditor();
        var terminal = new ScrollTestTerminal(80, 16);
        terminal.Apply("\x1b[1;1HHEADER\x1b[16;1HFOOTER");
        for (int step = 0; step < 85; step++)
        {
            if (step > 0)
                InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0',
                    step < 45 ? ConsoleKey.DownArrow : ConsoleKey.UpArrow, select, false, false));
            InvokePrivate(editor, "AdjustScroll", 12, 75);
            string incremental = ScrollFrame(editor);
            terminal.Apply(incremental);

            var expected = new ScrollTestTerminal(80, 16);
            expected.Apply("\x1b[1;1HHEADER\x1b[16;1HFOOTER");
            expected.Apply(ScrollFrame(editor, full: true));
            terminal.Cells.Should().Equal(expected.Cells, "frame {0} must match a full repaint", step);
        }
    }

    [Fact]
    public void Scrolling_OneRowScrollWritesOnlyChangedRowsAndResetsMargins()
    {
        var editor = ScrollingEditor();
        ScrollFrame(editor);
        SetPrivateField(editor, "_scrollTop", 1);
        SetPrivateField(editor, "_cursorLine", 1);

        string incremental = ScrollFrame(editor);
        string full = ScrollFrame(editor, full: true);
        incremental.Should().Contain("\x1b[3;14r\x1b[1S\x1b[r");
        incremental.Length.Should().BeLessThan(full.Length / 3);
        ScrollFrame(editor).Should().BeEmpty("an unchanged document viewport needs no repaint");
    }

    [Theory]
    [InlineData("_completionActive")]
    [InlineData("_messageDetailsActive")]
    public void Scrolling_OverlayDismissalRestoresUnderlyingRows(string overlayField)
    {
        var editor = ScrollingEditor();
        ScrollFrame(editor);
        SetPrivateField(editor, overlayField, true);
        ScrollFrame(editor).Should().Be(ScrollFrame(editor, full: true));

        SetPrivateField(editor, overlayField, false);
        SetPrivateField(editor, "_scrollTop", 1);
        ScrollFrame(editor).Should().Be(ScrollFrame(editor, full: true));
        ScrollFrame(editor).Should().BeEmpty();
    }

    [Fact]
    public void Scrolling_ResizeAndPageJumpRepaintTheViewport()
    {
        var editor = ScrollingEditor();
        ScrollFrame(editor);
        ScrollFrame(editor, width: 96).Should().Be(ScrollFrame(editor, full: true, width: 96));
        SetPrivateField(editor, "_scrollTop", 30);
        ScrollFrame(editor).Should().Be(ScrollFrame(editor, full: true));
        SetPrivateField(editor, "_scrollTop", 60);
        ScrollFrame(editor).Should().Be(ScrollFrame(editor, full: true));
    }

    [Fact]
    public void Scrolling_EditedTextAndSyntaxChangesRepaintCachedRows()
    {
        var editor = ScrollingEditor();
        var terminal = new ScrollTestTerminal(80, 16);
        terminal.Apply(ScrollFrame(editor));
        Lines(editor)[0] = "/*";
        InvalidateScrollingDocument(editor);
        terminal.Apply(ScrollFrame(editor));
        var expected = new ScrollTestTerminal(80, 16);
        expected.Apply(ScrollFrame(editor, full: true));
        terminal.Cells.Should().Equal(expected.Cells);

        SetPrivateField(editor, "_syntax", TermXTEditorSyntax.Python);
        InvokePrivate(editor, "InvalidateSyntaxStateCache");
        terminal.Apply(ScrollFrame(editor));
        expected.Apply(ScrollFrame(editor, full: true));
        terminal.Cells.Should().Equal(expected.Cells);
    }

    [Fact]
    public void Scrolling_DiagnosticChangesRepaintCachedRows()
    {
        var editor = ScrollingEditor();
        var terminal = new ScrollTestTerminal(80, 16);
        terminal.Apply(ScrollFrame(editor));
        Type severityType = typeof(TermXTEditor).GetNestedType("EditorDiagnosticSeverity", BindingFlags.NonPublic)!;
        foreach (string severity in new[] { "Warning", "Error" })
        {
            InvokePrivate(editor, "SetDiagnosticLineSeverity", 1, Enum.Parse(severityType, severity));
            string incremental = ScrollFrame(editor);
            incremental.Should().NotBeEmpty();
            terminal.Apply(incremental);
            var expected = new ScrollTestTerminal(80, 16);
            expected.Apply(ScrollFrame(editor, full: true));
            terminal.Cells.Should().Equal(expected.Cells);
        }

        InvokePrivate(editor, "ClearDiagnostics");
        terminal.Apply(ScrollFrame(editor));
        var cleared = new ScrollTestTerminal(80, 16);
        cleared.Apply(ScrollFrame(editor, full: true));
        terminal.Cells.Should().Equal(cleared.Cells);
    }

    private static void InvalidateScrollingDocument(TermXTEditor editor)
    {
        typeof(TermXTEditor).GetMethod("InvalidateDocumentCaches", BindingFlags.Instance | BindingFlags.NonPublic,
            null, Type.EmptyTypes, null)!.Invoke(editor, null);
    }

    private static TermXTEditor ScrollingEditor(TermXTEditorSyntax syntax = TermXTEditorSyntax.CSharp)
    {
        var editor = new TermXTEditor(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".cs"), syntax);
        Lines(editor).Clear();
        Lines(editor).AddRange(Enumerable.Range(0, 100).Select(i =>
            $"    return value{i} + 123; // " + (i % 5 == 0 ? new string('x', 100) : "text")));
        return editor;
    }

    private static string ScrollFrame(TermXTEditor editor, bool full = false, int width = 80)
    {
        var frame = GetPrivateField<StringBuilder>(editor, "_frame");
        frame.Clear();
        if (full)
        {
            for (int row = 0; row < 12; row++)
                InvokePrivate(editor, "RenderEditorRow", GetPrivateField<int>(editor, "_scrollTop") + row,
                    row + 2, 4, 5, width - 5, width);
        }
        else
        {
            InvokePrivate(editor, "RenderEditorRows", 2, 12, 4, 5, width - 5, width);
        }
        return frame.ToString();
    }

    // Interpret the terminal operations emitted by the editor to verify both text and
    // colors after incremental scrolling, including cells outside the scroll region.
    private sealed class ScrollTestTerminal
    {
        private readonly int _width;
        private readonly int _height;
        private int _x, _y, _top, _bottom;
        private int _foreground = -1, _background = -1;
        private bool _bold;
        public (char Text, int Foreground, int Background, bool Bold)[] Cells { get; }

        public ScrollTestTerminal(int width, int height)
        {
            _width = width;
            _height = height;
            _bottom = height - 1;
            Cells = Enumerable.Repeat((' ', -1, -1, false), width * height).ToArray();
        }

        public void Apply(string frame)
        {
            for (int i = 0; i < frame.Length;)
            {
                if (frame[i] != '\x1b')
                {
                    Cells[_y * _width + _x] = (frame[i++], _foreground, _background, _bold);
                    _x = Math.Min(_width - 1, _x + 1);
                    continue;
                }

                Match escape = Regex.Match(frame[i..], @"^\x1b\[([0-9;]*)([A-Za-z])");
                escape.Success.Should().BeTrue();
                int[] args = escape.Groups[1].Value.Length == 0 ? Array.Empty<int>() :
                    escape.Groups[1].Value.Split(';').Select(int.Parse).ToArray();
                i += escape.Length;
                switch (escape.Groups[2].Value)
                {
                    case "H":
                        _y = args[0] - 1;
                        _x = args[1] - 1;
                        break;
                    case "m":
                        for (int n = 0; n < args.Length; n++)
                        {
                            if (args[n] == 0) { _foreground = _background = -1; _bold = false; }
                            else if (args[n] == 1) _bold = true;
                            else if (args[n] == 38) { _foreground = args[n + 2]; n += 2; }
                            else if (args[n] == 48) { _background = args[n + 2]; n += 2; }
                            else throw new InvalidOperationException("Unhandled SGR: " + args[n]);
                        }
                        break;
                    case "K":
                        for (int x = _x; x < _width; x++)
                            Cells[_y * _width + x] = (' ', _foreground, _background, _bold);
                        break;
                    case "r":
                        _top = args.Length == 0 ? 0 : args[0] - 1;
                        _bottom = args.Length == 0 ? _height - 1 : args[1] - 1;
                        _x = _y = 0;
                        break;
                    case "S":
                    case "T":
                        var old = Cells.ToArray();
                        int delta = escape.Groups[2].Value == "S" ? args[0] : -args[0];
                        for (int y = _top; y <= _bottom; y++)
                            for (int x = 0; x < _width; x++)
                                Cells[y * _width + x] = y + delta >= _top && y + delta <= _bottom
                                    ? old[(y + delta) * _width + x] : (' ', -1, -1, false);
                        break;
                    default:
                        throw new InvalidOperationException("Unhandled escape: " + escape.Value);
                }
            }
        }
    }
}
