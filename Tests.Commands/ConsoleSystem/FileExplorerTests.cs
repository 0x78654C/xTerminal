using Core.DirFiles;
using Commands.TerminalCommands.DirFiles;
using System.Collections;
using System.Text;
using FluentAssertions;
using System.Reflection;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

public class FileExplorerTests
{
    [Theory]
    [InlineData(3, 12, 5, 3, 0, 6, 2)]
    [InlineData(-6, 12, 5, 6, 2, 0, 0)]
    [InlineData(20, 12, 5, 6, 2, 11, 7)]
    [InlineData(-20, 12, 5, 6, 2, 0, 0)]
    [InlineData(3, 0, 5, 0, 0, 0, 0)]
    [InlineData(3, 2, 5, 0, 0, 1, 0)]
    [InlineData(0, 12, 10, 11, 7, 11, 2)]
    [InlineData(int.MaxValue, 12, 5, 6, 2, 11, 7)]
    public void MoveSelection_ClampsAndKeepsSelectionVisible(
        int delta, int count, int rows, int selected, int offset, int expectedSelected, int expectedOffset)
    {
        object[] args = { delta, count, rows, selected, offset };
        Method("MoveListSelection", true).Invoke(null, args);
        args[3].Should().Be(expectedSelected);
        args[4].Should().Be(expectedOffset);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Wheel_AccumulatesPartialNotchesAndMovesThreeRows(bool search)
    {
        var explorer = CreateExplorer(search, 12);
        Invoke(explorer, "ScrollByWheelDelta", -60, 5).Should().Be(false);
        Invoke(explorer, "ScrollByWheelDelta", -60, 5).Should().Be(true);
        Get<int>(explorer, search ? "_searchSelectedIndex" : "_selectedIndex").Should().Be(3);
        Invoke(explorer, "ScrollByWheelDelta", -120, 5).Should().Be(true);
        Get<int>(explorer, search ? "_searchSelectedIndex" : "_selectedIndex").Should().Be(6);
        Get<int>(explorer, search ? "_searchScrollOffset" : "_scrollOffset").Should().Be(2);
        Invoke(explorer, "ScrollByWheelDelta", 240, 5).Should().Be(true);
        Get<int>(explorer, search ? "_searchSelectedIndex" : "_selectedIndex").Should().Be(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void KeyboardNavigation_PagesInBothLists(bool search)
    {
        var explorer = CreateExplorer(search, 100);
        string handler = search ? "HandleSearchKey" : "HandleMainKey";
        Invoke(explorer, handler, new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, true));
        int page = (int)Method("PageSize", true).Invoke(null, null)!;
        Get<int>(explorer, search ? "_searchSelectedIndex" : "_selectedIndex").Should().Be(page);
        Invoke(explorer, handler, new ConsoleKeyInfo('\0', ConsoleKey.PageDown, false, false, false));
        Get<int>(explorer, search ? "_searchSelectedIndex" : "_selectedIndex").Should().Be(Math.Min(99, page * 2));
        Invoke(explorer, handler, new ConsoleKeyInfo('\0', ConsoleKey.PageUp, false, false, true));
        Get<int>(explorer, search ? "_searchSelectedIndex" : "_selectedIndex").Should().Be(Math.Max(0, Math.Min(99, page * 2) - page));
    }

    [Fact]
    public void PendingDetails_DoNotReadFilesWhileScrolling()
    {
        var explorer = CreateExplorer(false, 1);
        Set(explorer, "_rendering", true);
        Set(explorer, "_frameWidth", 120);
        Set(explorer, "_frameHeight", 30);
        Set(explorer, "_detailsDue", Environment.TickCount64 + 60_000);
        Invoke(explorer, "RenderInfoPane", 60, 4, 60, 25);
        Get<object?>(explorer, "_cachedFileInfo").Should().BeNull();
        Get<StringBuilder>(explorer, "_frame").ToString().Should().Contain("Loading preview...");
    }

    [Fact]
    public void LoadItems_SortsFoldersFirstAndRefreshInvalidatesDetails()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "z-folder"));
            File.WriteAllText(Path.Combine(root, "z.txt"), "last");
            File.WriteAllText(Path.Combine(root, "A.txt"), "first");
            var explorer = new FileExplorer(root);
            Invoke(explorer, "EnsureItemsLoaded");
            var items = Get<IList>(explorer, "_items");
            items.Cast<object>().Select(i => Path.GetFileName((string)i.GetType().GetProperty("Path")!.GetValue(i)!))
                .Should().Equal("z-folder", "A.txt", "z.txt");
            items[1]!.GetType().GetProperty("SizeBytes")!.GetValue(items[1]).Should().Be(5L);
            Set(explorer, "_cachedPreview", new List<string> { "stale" });
            File.WriteAllText(Path.Combine(root, "new.txt"), "new");
            Invoke(explorer, "HandleMainKey", new ConsoleKeyInfo('\0', ConsoleKey.F5, false, false, false));
            Invoke(explorer, "EnsureItemsLoaded");
            items.Count.Should().Be(4);
            Get<object?>(explorer, "_cachedPreview").Should().BeNull();
            Get<bool>(explorer, "_detailsPending").Should().BeTrue();
        }
        finally { Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData(@"C:\work", "", @"C:\work")]
    [InlineData(@"C:\work\", "", @"C:\work\")]
    [InlineData(@"C:\", "", @"C:\")]
    [InlineData(@"C:\work", "\"folder with spaces\"", @"C:\work\folder with spaces")]
    [InlineData(@"C:\work", "..", @"C:\")]
    [InlineData(@"C:\work", @"D:\elsewhere", @"D:\elsewhere")]
    public void Command_ResolvesPathsWithoutRemovingLastCharacter(string current, string argument, string expected)
    {
        typeof(xPlorer).GetMethod("ResolveStartPath", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { current, argument }).Should().Be(expected);
    }

    [Fact]
    public void PreviewText_CannotWriteTerminalControlSequences()
    {
        Method("SanitizeText", true).Invoke(null, new object[] { "before\x1b[2J\aafter" })
            .Should().Be("before [2J after");
    }

    [Theory]
    [InlineData("fxp", "", @"C:\work")]
    [InlineData("fxp  ", "", @"C:\work")]
    [InlineData("  FXP\t", "", @"C:\work")]
    [InlineData("fxp ..", "..", @"C:\")]
    [InlineData("fxp \"folder with spaces\"", "\"folder with spaces\"", @"C:\work\folder with spaces")]
    [InlineData("fxp\tfolder", "folder", @"C:\work\folder")]
    [InlineData(@"fxp D:\elsewhere", @"D:\elsewhere", @"D:\elsewhere")]
    [InlineData("fxp fxp", "fxp", @"C:\work\fxp")]
    [InlineData("fxp fxp-backup", "fxp-backup", @"C:\work\fxp-backup")]
    public void Command_FullShellCommandLineOpensIntendedDirectory(string commandLine, string expectedArgument, string expectedPath)
    {
        var command = new xPlorer();
        string argument = (string)typeof(xPlorer).GetMethod("GetParameterText", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(command, new object[] { commandLine })!;
        argument.Should().Be(expectedArgument);
        typeof(xPlorer).GetMethod("ResolveStartPath", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { @"C:\work", argument }).Should().Be(expectedPath);
    }

    [Theory]
    [InlineData("fxp --help", "--help")]
    [InlineData("fxp -h", "-h")]
    public void Command_FullShellCommandLineRecognizesHelp(string commandLine, string expectedArgument)
    {
        typeof(xPlorer).GetMethod("GetParameterText", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(new xPlorer(), new object[] { commandLine }).Should().Be(expectedArgument);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectionWithinViewport_RepaintsOnlyChangedListRows(bool search)
    {
        var explorer = CreateExplorer(search, 40);
        Set(explorer, "_rendering", true);
        Set(explorer, "_frameWidth", 120);
        Set(explorer, "_frameHeight", 30);
        Set(explorer, "_detailsDue", Environment.TickCount64 + 60_000);
        string render = search ? "RenderSearchScreen" : "RenderMainScreen";
        Invoke(explorer, render);
        var frame = Get<StringBuilder>(explorer, "_frame");
        int fullFrameLength = frame.Length;
        frame.ToString().Should().Contain("file-2.txt");
        frame.Clear();

        Invoke(explorer, "MoveSelection", 1, 25);
        Invoke(explorer, render);
        string partial = frame.ToString();
        partial.Should().Contain("file-0.txt").And.Contain("file-1.txt").And.NotContain("file-2.txt");
        partial.Should().NotContain("\x1b[2J");
        partial.Length.Should().BeLessThan(fullFrameLength);

        frame.Clear();
        Set(explorer, "_frameWidth", 100);
        Invoke(explorer, render);
        frame.ToString().Should().Contain("\x1b[2J").And.Contain("file-2.txt");
    }

    private static FileExplorer CreateExplorer(bool search, int count)
    {
        var explorer = new FileExplorer(Path.GetTempPath());
        Set(explorer, "_searchMode", search);
        Set(explorer, "_itemsDirty", false);
        var list = Get<IList>(explorer, search ? "_searchResults" : "_items");
        Type itemType = typeof(FileExplorer).GetNestedType(search ? "SearchItem" : "Item", BindingFlags.NonPublic)!;
        for (int i = 0; i < count; i++)
        {
            object item = Activator.CreateInstance(itemType)!;
            itemType.GetProperty("Path")!.SetValue(item, Path.Combine(Path.GetTempPath(), $"file-{i}.txt"));
            list.Add(item);
        }
        return explorer;
    }

    private static MethodInfo Method(string name, bool isStatic = false) => typeof(FileExplorer).GetMethod(
        name, BindingFlags.NonPublic | (isStatic ? BindingFlags.Static : BindingFlags.Instance))!;
    private static object? Invoke(FileExplorer explorer, string name, params object[] args) => Method(name).Invoke(explorer, args);
    private static T Get<T>(FileExplorer explorer, string field) =>
        (T)typeof(FileExplorer).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(explorer)!;
    private static void Set(FileExplorer explorer, string field, object value) =>
        typeof(FileExplorer).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(explorer, value);

    [Fact]
    public void BuildFilePreview_ReturnsNormalizedTextLines()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");

        try
        {
            File.WriteAllText(path, "alpha\tbeta\r\nsecond");

            List<string> preview = BuildFilePreview(path, new FileInfo(path).Length, 5);

            preview.Should().Equal("  alpha    beta", "  second");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void BuildFilePreview_SkipsBinaryFiles()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".bin");

        try
        {
            File.WriteAllBytes(path, new byte[] { 0x41, 0x00, 0x42, 0x03 });

            List<string> preview = BuildFilePreview(path, new FileInfo(path).Length, 5);

            preview.Should().Equal("  (binary preview skipped)");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void BuildFilePreview_ReportsWhenByteLimitTruncatesFile()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");

        try
        {
            File.WriteAllText(path, new string('x', 70 * 1024));

            List<string> preview = BuildFilePreview(path, new FileInfo(path).Length, 2);

            preview.Should().HaveCount(2);
            preview[1].Should().Contain("preview truncated at 64.0 KB");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static List<string> BuildFilePreview(string path, long totalBytes, int maxLines)
    {
        MethodInfo method = typeof(FileExplorer).GetMethod(
            "BuildFilePreview",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        return (List<string>)method.Invoke(null, new object[] { path, totalBytes, maxLines })!;
    }
}
