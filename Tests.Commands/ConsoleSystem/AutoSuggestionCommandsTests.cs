using System.Runtime.Versioning;
using System.Text;
using Core;
using Core.Abstractions;
using Core.Commands;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

[SupportedOSPlatform("Windows")]
[Collection("TermXT interpreter")]
public sealed class AutoSuggestionCommandsTests : IDisposable
{
    private readonly int _savedTabPressCount = GlobalVariables.tabPressCount;
    private readonly string _savedCurrentDirectory = GlobalVariables.currentDirectory;
    private readonly string _savedAliasFile = GlobalVariables.aliasFile;
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "xt_completion_" + Guid.NewGuid().ToString("N"));
    private readonly string _completionDirectory;

    public AutoSuggestionCommandsTests()
    {
        GlobalVariables.tabPressCount = 0;
        _completionDirectory = Path.Combine(_tempDirectory, "files");
        Directory.CreateDirectory(_completionDirectory);
        GlobalVariables.currentDirectory = Path.Combine(_tempDirectory, "current-directory.txt");
        File.WriteAllText(GlobalVariables.currentDirectory, _completionDirectory);
        GlobalVariables.aliasFile = Path.Combine(_tempDirectory, "aliases.json");
    }

    public void Dispose()
    {
        GlobalVariables.tabPressCount = _savedTabPressCount;
        GlobalVariables.currentDirectory = _savedCurrentDirectory;
        GlobalVariables.aliasFile = _savedAliasFile;
        Directory.Delete(_tempDirectory, true);
    }

    [Theory]
    [InlineData("ls -cf", "-cf", "-cf")]
    [InlineData("wget --n", "--n", "--noping")]
    [InlineData("ctx li", "li", "list")]
    [InlineData("note -a", "-a", "-add")]
    [InlineData("  hash   -sha2", "-sha2", "-sha256")]
    public void CommandSuggestion_ReturnsMatchingParameter(string input, string prefix, string expected)
    {
        AutoSuggestionCommands.CommandSuggestion(input, prefix).Should().Equal(expected);
    }

    [Theory]
    [InlineData(null, "-")]
    [InlineData("", "-")]
    [InlineData("   ", "-")]
    [InlineData("ls -", null)]
    [InlineData("ls ", "")]
    [InlineData("ls", "ls")]
    [InlineData("ls -unknown", "-unknown")]
    [InlineData("unknown -", "-")]
    [InlineData("cd -", "-")]
    [InlineData("ls l", "l")]
    public void CommandSuggestion_WithoutMatchingParameter_ReturnsEmpty(string? input, string? prefix)
    {
        AutoSuggestionCommands.CommandSuggestion(input!, prefix!).Should().BeEmpty();
    }

    [Fact]
    public void Tab_CyclesMatchingParametersAndWrapsWithoutChangingThePrompt()
    {
        var console = new TestConsole();
        var handler = new KeyHandler(console, null, null);
        Type(handler, "ls -c");

        foreach (var expected in new[] { "ls -c", "ls -cf", "ls -cd", "ls -ct", "ls -c" })
        {
            Press(handler, ConsoleKey.Tab);

            AssertInput(handler, console, expected);
        }
    }

    [Fact]
    public void Tab_PreservesEarlierArgumentsSpacingAndLiteralCharactersAcrossConsoleLines()
    {
        var console = new TestConsole();
        var handler = new KeyHandler(console, null, null);
        const string prefix = "cat -o \"a+b {folder}(1).txt\"   ";
        Type(handler, prefix + "-sm");

        Press(handler, ConsoleKey.Tab);
        Press(handler, ConsoleKey.Tab);

        AssertInput(handler, console, prefix + "-smo");
    }

    [Theory]
    [InlineData("wget --n", "wget --noping")]
    [InlineData("ctx li", "ctx list")]
    [InlineData("note -a", "note -add")]
    [InlineData("hash -sha2", "hash -sha256")]
    public void Tab_SingleMatchRemainsStableOnRepeatedTabs(string input, string expected)
    {
        var console = new TestConsole();
        var handler = new KeyHandler(console, null, null);
        Type(handler, input);

        for (int i = 0; i < 3; i++)
            Press(handler, ConsoleKey.Tab);

        AssertInput(handler, console, expected);
    }

    [Theory]
    [InlineData("ls -unknown")]
    [InlineData("unknown -")]
    [InlineData("cd -")]
    public void Tab_NoMatchLeavesInputUnchangedOnRepeatedTabs(string input)
    {
        var console = new TestConsole();
        var handler = new KeyHandler(console, null, null);
        Type(handler, input);

        for (int i = 0; i < 3; i++)
            Press(handler, ConsoleKey.Tab);

        AssertInput(handler, console, input);
    }

    [Fact]
    public void Tab_EditingThePrefixResetsTheMatches()
    {
        var console = new TestConsole();
        var handler = new KeyHandler(console, null, null);
        Type(handler, "ls -c");
        Press(handler, ConsoleKey.Tab);
        Type(handler, "f");

        Press(handler, ConsoleKey.Tab);
        Press(handler, ConsoleKey.Tab);

        AssertInput(handler, console, "ls -cf");
    }

    [Fact]
    public void Tab_NewArgumentStartsItsOwnCompletion()
    {
        var console = new TestConsole();
        var handler = new KeyHandler(console, null, null);
        Type(handler, "ls -c");
        Press(handler, ConsoleKey.Tab);
        Press(handler, ConsoleKey.Tab);
        Type(handler, " -d");

        Press(handler, ConsoleKey.Tab);

        AssertInput(handler, console, "ls -cf -d");
    }

    [Fact]
    public void Tab_NewInputHandlerStartsWithTheFirstMatch()
    {
        var firstHandler = new KeyHandler(new TestConsole(), null, null);
        Type(firstHandler, "ls -c");
        Press(firstHandler, ConsoleKey.Tab);
        Press(firstHandler, ConsoleKey.Tab);
        var console = new TestConsole();
        var nextHandler = new KeyHandler(console, null, null);
        Type(nextHandler, "ls -c");

        Press(nextHandler, ConsoleKey.Tab);

        AssertInput(nextHandler, console, "ls -c");
    }

    [Fact]
    public void Tab_CompletesParametersEvenAfterAFileCompletionTab()
    {
        var console = new TestConsole();
        var handler = new KeyHandler(console, null, null);
        Type(handler, "ls ");
        Press(handler, ConsoleKey.Tab);
        Type(handler, "-du");

        Press(handler, ConsoleKey.Tab);

        AssertInput(handler, console, "ls -dup");
    }

    [Theory]
    [InlineData("xte -raw someFi", "someFile.txt", "xte -raw someFile.txt")]
    [InlineData("xte -raw   someFi", "someFile.txt", "xte -raw   someFile.txt")]
    [InlineData("xte -syntax text -raw someFi", "someFile.txt", "xte -syntax text -raw someFile.txt")]
    [InlineData("xte -raw ", "someFile.txt", "xte -raw someFile.txt")]
    [InlineData("xte someFi", "someFile.txt", "xte someFile.txt")]
    [InlineData("xte", "someFile.txt", "xte someFile.txt")]
    [InlineData("cat -o output.txt someFi", "someFile.txt", "cat -o output.txt someFile.txt")]
    [InlineData("xte -raw \"some Fi", "some File.txt", "xte -raw \"some File.txt\"")]
    [InlineData("xte -raw some Fi", "some File.txt", "xte -raw some File.txt")]
    public void DoubleTab_CompletesFileAfterParameters(string input, string fileName, string expected)
    {
        File.WriteAllText(Path.Combine(_completionDirectory, fileName), "test");
        var console = new TestConsole();
        var handler = new KeyHandler(console, null, null);
        Type(handler, input);

        Press(handler, ConsoleKey.Tab);
        AssertInput(handler, console, input);
        Press(handler, ConsoleKey.Tab);

        AssertInput(handler, console, expected);
    }

    [Theory]
    [InlineData("xte -raw someDir", "xte -raw someDirectory\\")]
    [InlineData("ls -d someDir", "ls -d someDirectory\\")]
    [InlineData("snap save someDir", "snap save someDirectory\\")]
    public void DoubleTab_CompletesDirectoryAfterParameters(string input, string expected)
    {
        Directory.CreateDirectory(Path.Combine(_completionDirectory, "someDirectory"));
        var console = new TestConsole();
        var handler = new KeyHandler(console, null, null);
        Type(handler, input);

        Press(handler, ConsoleKey.Tab);
        Press(handler, ConsoleKey.Tab);

        AssertInput(handler, console, expected);
    }

    [Theory]
    [InlineData("someDirectory\\someFi", "someDirectory\\someFile.txt")]
    [InlineData("someDirectory/someFi", "someDirectory/someFile.txt")]
    public void DoubleTab_CompletesNestedPathAfterParameters(string path, string expected)
    {
        Directory.CreateDirectory(Path.Combine(_completionDirectory, "someDirectory"));
        File.WriteAllText(Path.Combine(_completionDirectory, "someDirectory", "someFile.txt"), "test");
        var console = new TestConsole();
        var handler = new KeyHandler(console, null, null);
        Type(handler, "xte -raw " + path);

        Press(handler, ConsoleKey.Tab);
        Press(handler, ConsoleKey.Tab);

        AssertInput(handler, console, "xte -raw " + expected);
    }

    [Fact]
    public void DoubleTab_CompletesAbsolutePathAfterParameters()
    {
        File.WriteAllText(Path.Combine(_completionDirectory, "someFile.txt"), "test");
        var console = new TestConsole();
        var handler = new KeyHandler(console, null, null);
        Type(handler, "xte -raw " + Path.Combine(_completionDirectory, "someFi"));

        Press(handler, ConsoleKey.Tab);
        Press(handler, ConsoleKey.Tab);

        AssertInput(handler, console, "xte -raw " + Path.Combine(_completionDirectory, "someFile.txt"));
    }

    [Fact]
    public void DoubleTab_CompletesFileAfterCompletingParameter()
    {
        File.WriteAllText(Path.Combine(_completionDirectory, "someFile.txt"), "test");
        var console = new TestConsole();
        var handler = new KeyHandler(console, null, null);
        Type(handler, "xte -r");
        Press(handler, ConsoleKey.Tab);
        Type(handler, " someFi");

        Press(handler, ConsoleKey.Tab);
        AssertInput(handler, console, "xte -raw someFi");
        Press(handler, ConsoleKey.Tab);

        AssertInput(handler, console, "xte -raw someFile.txt");
    }

    [Fact]
    public void DoubleTab_CompletesAliasPathAfterParameters()
    {
        File.WriteAllText(GlobalVariables.aliasFile, "[{\"CommandName\":\"myeditor\",\"Command\":\"xte %1\"}]");
        File.WriteAllText(Path.Combine(_completionDirectory, "someFile.txt"), "test");
        var console = new TestConsole();
        var handler = new KeyHandler(console, null, null);
        Type(handler, "myeditor -raw someFi");

        Press(handler, ConsoleKey.Tab);
        Press(handler, ConsoleKey.Tab);

        AssertInput(handler, console, "myeditor -raw someFile.txt");
    }

    [Fact]
    public void DoubleTab_WithoutMatchingPathLeavesInputUnchanged()
    {
        var console = new TestConsole();
        var handler = new KeyHandler(console, null, null);
        Type(handler, "xte -raw missing\\someFi");

        for (int i = 0; i < 4; i++)
            Press(handler, ConsoleKey.Tab);

        AssertInput(handler, console, "xte -raw missing\\someFi");
    }

    [Theory]
    [InlineData("xte -raw ", "")]
    [InlineData("xte -raw some", "some")]
    public void GetPathSuggestions_IncludesAllMatchingFilesAndDirectoriesAfterParameter(string input, string pathPrefix)
    {
        Directory.CreateDirectory(Path.Combine(_completionDirectory, "someDirectory"));
        File.WriteAllText(Path.Combine(_completionDirectory, "someFile.txt"), "test");
        File.WriteAllText(Path.Combine(_completionDirectory, "someOtherFile.txt"), "test");

        var suggestions = AutoSuggestionCommands.GetPathSuggestions(input, "xte", _completionDirectory,
            GlobalVariables.TypeSuggestions.All, out int pathStart);

        suggestions.Should().Equal("someDirectory\\", "someFile.txt", "someOtherFile.txt");
        input.Substring(pathStart).Should().Be(pathPrefix);
        input.Substring(0, pathStart).Should().Be("xte -raw ");
    }

    private static void Type(KeyHandler handler, string text)
    {
        foreach (char character in text)
            handler.Handle(new ConsoleKeyInfo(character, ConsoleKey.A, false, false, false));
    }

    private static void Press(KeyHandler handler, ConsoleKey key)
    {
        handler.Handle(new ConsoleKeyInfo('\0', key, false, false, false));
    }

    private static void AssertInput(KeyHandler handler, TestConsole console, string expected)
    {
        handler.Text.Should().Be(expected);
        console.Text.Should().Be((TestConsole.Prompt + expected).TrimEnd(' '));
        (console.CursorTop * console.BufferWidth + console.CursorLeft)
            .Should().Be(TestConsole.Prompt.Length + expected.Length);
    }

    private sealed class TestConsole : IConsole
    {
        public const string Prompt = "prompt> ";
        private readonly StringBuilder _buffer = new();
        public int CursorLeft { get; private set; }
        public int CursorTop { get; private set; }
        public int BufferWidth { get; private set; } = 32;
        public int BufferHeight { get; private set; } = 100;
        public string Text => _buffer.ToString().TrimEnd(' ');

        public TestConsole() => Write(Prompt);

        public void SetCursorPosition(int left, int top)
        {
            left.Should().BeInRange(0, BufferWidth - 1);
            top.Should().BeInRange(0, BufferHeight - 1);
            CursorLeft = left;
            CursorTop = top;
        }

        public void SetBufferSize(int width, int height)
        {
            BufferWidth = width;
            BufferHeight = height;
        }

        public void Write(string value)
        {
            foreach (char character in value)
            {
                int position = CursorTop * BufferWidth + CursorLeft;
                while (_buffer.Length <= position)
                    _buffer.Append(' ');
                _buffer[position] = character;
                CursorLeft++;
                if (CursorLeft == BufferWidth)
                {
                    CursorLeft = 0;
                    CursorTop++;
                }
            }
        }

        public void WriteLine(string value)
        {
            Write(value);
            CursorLeft = 0;
            CursorTop++;
        }
    }
}
