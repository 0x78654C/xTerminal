using System.Runtime.Versioning;
using System.Text;
using Core;
using Core.Abstractions;
using Core.Commands;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

[SupportedOSPlatform("Windows")]
public sealed class AutoSuggestionCommandsTests : IDisposable
{
    private readonly int _savedTabPressCount = GlobalVariables.tabPressCount;

    public AutoSuggestionCommandsTests()
    {
        GlobalVariables.tabPressCount = 0;
    }

    public void Dispose()
    {
        GlobalVariables.tabPressCount = _savedTabPressCount;
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
        console.Text.Should().Be(TestConsole.Prompt + expected);
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
