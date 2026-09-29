using Core;
using Core.DirFiles;
using FluentAssertions;
using System.Collections;
using System.Reflection;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

// Navigation updates the shell's shared current-directory file.
[Collection("TermXT interpreter")]
public class FileExplorerNavigationTests : IDisposable
{
    private readonly string _originalDirectoryFile = GlobalVariables.currentDirectory;
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), "fxp-navigation-" + Guid.NewGuid().ToString("N"));
    private readonly string _directoryFile;

    public FileExplorerNavigationTests()
    {
        Directory.CreateDirectory(_testRoot);
        _directoryFile = Path.Combine(_testRoot, "current-directory.txt");
        GlobalVariables.currentDirectory = _directoryFile;
    }

    [Theory]
    [InlineData(@"C:\work", @"C:\work\")]
    [InlineData(@"C:\work\", @"C:\work\")]
    [InlineData(@"C:\work\\", @"C:\work\")]
    [InlineData("C:/work/", @"C:\work\")]
    [InlineData(@"C:\", @"C:\")]
    [InlineData(@"\\server\share\folder\\", @"\\server\share\folder\")]
    [InlineData(@"\\server\share\", @"\\server\share\")]
    public void SavingDirectory_AlwaysKeepsOneTrailingSeparator(string path, string expected)
    {
        var explorer = new FileExplorer(path);
        Invoke(explorer, "SetCurrentDirectory", path);
        File.ReadAllText(_directoryFile).Should().Be(expected);

        // Repeated exits/back navigation must not accumulate separators.
        Invoke(explorer, "SetCurrentDirectory", File.ReadAllText(_directoryFile));
        File.ReadAllText(_directoryFile).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("\\")]
    [InlineData("\\\\")]
    [InlineData("/")]
    public void PageUp_GoesDirectlyToParentRegardlessOfTrailingSeparator(string suffix)
    {
        string current = Directory.CreateDirectory(Path.Combine(_testRoot, "current")).FullName;
        File.WriteAllText(_directoryFile, current + suffix);
        var explorer = new FileExplorer(File.ReadAllText(_directoryFile));

        Press(explorer, ConsoleKey.PageUp);

        CurrentDirectory(explorer).Should().Be(_testRoot);
        File.ReadAllText(_directoryFile).Should().Be(_testRoot + "\\");
        History(explorer).Count.Should().Be(1);
    }

    [Theory]
    [InlineData("\\")]
    [InlineData("\\\\")]
    public void EnterBackThenReopen_PageUpMovesToParentInOnePress(string suffix)
    {
        string current = Directory.CreateDirectory(Path.Combine(_testRoot, "current")).FullName;
        string child = Directory.CreateDirectory(Path.Combine(current, "child")).FullName;
        File.WriteAllText(_directoryFile, current + suffix);
        var explorer = new FileExplorer(File.ReadAllText(_directoryFile));
        Invoke(explorer, "EnsureItemsLoaded");

        Press(explorer, ConsoleKey.Enter);
        CurrentDirectory(explorer).Should().Be(child);
        Press(explorer, ConsoleKey.Backspace);
        CurrentDirectory(explorer).Should().Be(current);
        File.ReadAllText(_directoryFile).Should().Be(current + "\\");

        var reopened = new FileExplorer(File.ReadAllText(_directoryFile));
        Press(reopened, ConsoleKey.PageUp);
        CurrentDirectory(reopened).Should().Be(_testRoot);
        File.ReadAllText(_directoryFile).Should().Be(_testRoot + "\\");
    }

    [Fact]
    public void PageUp_AtDriveRootDoesNotNavigateOrAddHistory()
    {
        string root = Path.GetPathRoot(_testRoot)!;
        File.WriteAllText(_directoryFile, root);
        var explorer = new FileExplorer(root);

        Press(explorer, ConsoleKey.PageUp);

        CurrentDirectory(explorer).Should().Be(root);
        File.ReadAllText(_directoryFile).Should().Be(root);
        History(explorer).Count.Should().Be(0);
    }

    private static void Press(FileExplorer explorer, ConsoleKey key) =>
        Invoke(explorer, "HandleMainKey", new ConsoleKeyInfo('\0', key, false, false, false));

    private static void Invoke(FileExplorer explorer, string method, params object[] args) =>
        typeof(FileExplorer).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(explorer, args);

    private static string CurrentDirectory(FileExplorer explorer) =>
        (string)typeof(FileExplorer).GetField("_currentRoot", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(explorer)!;

    private static ICollection History(FileExplorer explorer) =>
        (ICollection)typeof(FileExplorer).GetField("_historyBack", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(explorer)!;

    public void Dispose()
    {
        GlobalVariables.currentDirectory = _originalDirectoryFile;
        Directory.Delete(_testRoot, recursive: true);
    }
}
