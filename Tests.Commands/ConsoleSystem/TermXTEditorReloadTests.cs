using Core.DirFiles;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

public partial class TermXTEditorSyntaxTests
{
    [Theory]
    [InlineData("e!", false)]
    [InlineData("e!", true)]
    [InlineData("reload!", false)]
    [InlineData("reload", false)]
    public void Reload_PreservesCaretAndViewportThroughoutCommandEntry(string command, bool wheelScrolled)
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        try
        {
            string[] contents = Enumerable.Repeat(new string('x', 100), 40).ToArray();
            File.WriteAllLines(path, contents);
            var editor = new TermXTEditor(path);
            SetPrivateField(editor, "_cursorLine", 12);
            SetPrivateField(editor, "_cursorCol", 67);
            SetPrivateField(editor, "_scrollTop", 32);
            if (wheelScrolled)
                InvokePrivate<bool>(editor, "ScrollViewportByWheelNotches", -6, 10, 40).Should().BeTrue();
            int expectedScroll = wheelScrolled ? 50 : 32;
            contents[12] = new string('y', 100);
            File.WriteAllLines(path, contents);
            if (command != "reload")
            {
                Lines(editor)[12] = new string('z', 100);
                InvokePrivate(editor, "MarkDirty");
            }

            foreach (char c in ":" + command)
            {
                TypeCommand(editor, c.ToString());
                InvokePrivate(editor, "AdjustScroll", 10, 40);
                GetPrivateField<int>(editor, "_scrollTop").Should().Be(expectedScroll);
                GetPrivateField<int>(editor, "_cursorLine").Should().Be(12);
                GetPrivateField<int>(editor, "_cursorCol").Should().Be(67);
            }
            CommandKey(editor, ConsoleKey.Enter);
            InvokePrivate(editor, "AdjustScroll", 10, 40);

            Lines(editor).Should().Equal(contents);
            GetPrivateField<bool>(editor, "_dirty").Should().BeFalse();
            GetPrivateField<int>(editor, "_scrollTop").Should().Be(expectedScroll);
            GetPrivateField<int>(editor, "_cursorLine").Should().Be(12);
            GetPrivateField<int>(editor, "_cursorCol").Should().Be(67);

            // Navigation commands must still bring their destination into view.
            TypeCommand(editor, ":2");
            CommandKey(editor, ConsoleKey.Enter);
            InvokePrivate(editor, "AdjustScroll", 10, 40);
            GetPrivateField<int>(editor, "_cursorLine").Should().Be(1);
            GetPrivateField<int>(editor, "_scrollTop").Should().Be(3);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(40, 12, 1, 26)]
    [InlineData(5, 4, 1, 0)]
    [InlineData(0, 0, 0, 0)]
    public void Reload_ClampsCaretAndViewportWhenDiskContentsShrink(
        int remainingLines, int expectedLine, int expectedColumn, int expectedScroll)
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        try
        {
            File.WriteAllLines(path, Enumerable.Repeat(new string('x', 100), 40));
            var editor = new TermXTEditor(path);
            SetPrivateField(editor, "_cursorLine", 12);
            SetPrivateField(editor, "_cursorCol", 67);
            SetPrivateField(editor, "_scrollTop", 26);
            File.WriteAllLines(path, Enumerable.Repeat("y", remainingLines));

            TypeCommand(editor, ":e!");
            CommandKey(editor, ConsoleKey.Enter);
            InvokePrivate(editor, "AdjustScroll", 10, 40);

            GetPrivateField<int>(editor, "_cursorLine").Should().Be(expectedLine);
            GetPrivateField<int>(editor, "_cursorCol").Should().Be(expectedColumn);
            GetPrivateField<int>(editor, "_scrollTop").Should().Be(expectedScroll);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Reload_FailureKeepsDocumentCaretAndViewport()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        try
        {
            string[] contents = Enumerable.Repeat("original document", 40).ToArray();
            File.WriteAllLines(path, contents);
            var editor = new TermXTEditor(path);
            SetPrivateField(editor, "_cursorLine", 12);
            SetPrivateField(editor, "_cursorCol", 5);
            SetPrivateField(editor, "_scrollTop", 10);
            InvokePrivate<bool>(editor, "ScrollViewportByWheelNotches", -4, 10, 40).Should().BeTrue();
            using var lockedFile = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            TypeCommand(editor, ":e!");
            CommandKey(editor, ConsoleKey.Enter);
            InvokePrivate(editor, "AdjustScroll", 10, 40);

            GetPrivateField<string>(editor, "_status").Should().Be("Reload failed");
            Lines(editor).Should().Equal(contents);
            GetPrivateField<int>(editor, "_cursorLine").Should().Be(12);
            GetPrivateField<int>(editor, "_cursorCol").Should().Be(5);
            GetPrivateField<int>(editor, "_scrollTop").Should().Be(22);
        }
        finally { File.Delete(path); }
    }
}
