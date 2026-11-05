using System.Runtime.Versioning;
using System.Text;
using Commands;
using Commands.TerminalCommands.DirFiles;
using Commands.TerminalCommands.ScriptingLanguage;
using Core;
using Core.DirFiles;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

[SupportedOSPlatform("Windows")]
[Collection("TermXT interpreter")]
public sealed class GrepTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "xt_grep_" + Guid.NewGuid().ToString("N"));
    private readonly string _savedDirectory = GlobalVariables.currentDirectory;
    private readonly string _savedAliases = GlobalVariables.aliasFile;
    private readonly bool _savedPipe = GlobalVariables.isPipeCommand;
    private readonly string _savedOutput = GlobalVariables.pipeCmdOutput;
    private readonly int _savedCount = GlobalVariables.pipeCmdCount;
    private readonly int _savedTotal = GlobalVariables.pipeCmdCountTemp;
    private readonly bool _savedError = GlobalVariables.isErrorCommand;
    private readonly bool _savedCancel = GlobalVariables.eventCancelKey;
    private readonly bool _savedCancelable = GlobalVariables.eventKeyFlagX;

    public GrepTests()
    {
        Directory.CreateDirectory(_directory);
        GlobalVariables.currentDirectory = Path.Combine(_directory, "current-directory");
        File.WriteAllText(GlobalVariables.currentDirectory, _directory);
        GlobalVariables.aliasFile = Path.Combine(_directory, "aliases.json");
        GlobalVariables.isPipeCommand = false;
        GlobalVariables.pipeCmdCount = GlobalVariables.pipeCmdCountTemp = 0;
        GlobalVariables.pipeCmdOutput = string.Empty;
    }

    public void Dispose()
    {
        GlobalVariables.currentDirectory = _savedDirectory;
        GlobalVariables.aliasFile = _savedAliases;
        GlobalVariables.isPipeCommand = _savedPipe;
        GlobalVariables.pipeCmdOutput = _savedOutput;
        GlobalVariables.pipeCmdCount = _savedCount;
        GlobalVariables.pipeCmdCountTemp = _savedTotal;
        GlobalVariables.isErrorCommand = _savedError;
        GlobalVariables.eventCancelKey = _savedCancel;
        GlobalVariables.eventKeyFlagX = _savedCancelable;
        Directory.Delete(_directory, true);
    }

    private void Write(string name, string text, Encoding? encoding = null)
    {
        string path = Path.Combine(_directory, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text, encoding ?? new UTF8Encoding(false));
    }

    private (int Status, string Output, string Error) Search(string arguments, string? input = null, Func<bool>? cancelled = null)
    {
        using var reader = input == null ? null : new StringReader(input);
        using var output = new StringWriter();
        using var error = new StringWriter();
        int status = GrepManage.Search(GrepManage.ParseArguments(arguments), _directory, reader, output, error, cancelled);
        return (status, Normalize(output.ToString()), Normalize(error.ToString()));
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n");

    private static (string Output, string Error) Capture(Action action)
    {
        TextWriter savedOut = Console.Out, savedError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();
        Console.SetOut(output);
        Console.SetError(error);
        try { action(); }
        finally { Console.SetOut(savedOut); Console.SetError(savedError); }
        return (Normalize(output.ToString()), Normalize(error.ToString()));
    }

    [Theory]
    [InlineData("error", "error one\nERROR two\nok\n", "error one\n")]
    [InlineData("-in error", "error one\nERROR two\nok\n", "1:error one\n2:ERROR two\n")]
    [InlineData("-inv error", "error one\nERROR two\nok\n", "3:ok\n")]
    [InlineData("-F a.b", "a.b\naxb\n", "a.b\n")]
    [InlineData("-E '^a.b$'", "a.b\naxb\naxbc\n", "a.b\naxb\n")]
    [InlineData("-w cat", "cat\ncat!\nbobcat\ncat_1\n", "cat\ncat!\n")]
    [InlineData("-w @", "@\nx@y\n", "@\n")]
    [InlineData("-x 'cat|dog'", "cat\ndog\ncat dog\n", "cat\ndog\n")]
    [InlineData("-F -x '[a]'", "[a]\na\n [a]\n", "[a]\n")]
    [InlineData("-n ''", "one\n\nthree\n", "1:one\n2:\n3:three\n")]
    [InlineData("-e error -e warning", "error warning\nwarning\nok\n", "error warning\nwarning\n")]
    [InlineData("--regexp=error --regexp=warning --line-number", "error\nwarning", "1:error\n2:warning\n")]
    [InlineData("-- -error", "-error\nother\n", "-error\n")]
    [InlineData("-c error", "error error\nerror\nok\n", "2\n")]
    [InlineData("-n -m1 error", "ok\nerror one\nerror two\n", "2:error one\n")]
    [InlineData("-o '\\d+'", "id 12 and 34\n", "12\n34\n")]
    [InlineData("-no -e abc -e ab", "abc ab\n", "1:abc\n1:ab\n")]
    [InlineData("-ov error", "error\nok\n", "")]
    [InlineData("-o '^'", "ok\nerror\n", "")]
    [InlineData("-n -C1 hit", "a\nhit\nhit\nb\nc\nd\nhit\ne\n", "1-a\n2:hit\n3:hit\n4-b\n--\n6-d\n7:hit\n8-e\n")]
    [InlineData("-n -B1 hit", "a\nhit\nb\nc\nhit\n", "1-a\n2:hit\n--\n4-c\n5:hit\n")]
    [InlineData("-n -A2 -m1 hit", "hit\nhit\na\nhit\n", "1:hit\n2-hit\n3-a\n")]
    public void SearchesText(string arguments, string input, string expected)
    {
        var result = Search(arguments, input);
        result.Status.Should().Be(0);
        result.Output.Should().Be(expected);
        result.Error.Should().BeEmpty();
    }

    [Theory]
    [InlineData("missing", "text\n", "")]
    [InlineData("-c missing", "text\n", "0\n")]
    [InlineData("-m0 text", "text\n", "")]
    [InlineData("''", "", "")]
    public void NoSelectedLinesReturnsOne(string arguments, string input, string expected)
    {
        var result = Search(arguments, input);
        result.Status.Should().Be(1);
        result.Output.Should().Be(expected);
        result.Error.Should().BeEmpty();
    }

    [Fact]
    public void QuotedPathsAndRegexBackslashesArePreserved()
    {
        Write("folder with spaces/server log.txt", "id 42\nid xx\n");
        Search("-n '\\d+' 'folder with spaces\\server log.txt'").Output.Should().Be("1:id 42\n");
        var options = GrepManage.ParseArguments("-e '\\bword\\b' \"C:\\path with spaces\\\"");
        options.Patterns.Should().Equal(@"\bword\b");
        options.Files.Should().Equal("C:\\path with spaces\\");
    }

    [Fact]
    public void WildcardsAndMultipleFilesControlPrefixes()
    {
        Write("a.log", "error\n");
        Write("b.log", "ok\nerror\n");
        Search("-n error *.log").Output.Should().Be("a.log:1:error\nb.log:2:error\n");
        Search("-hn error a.log b.log").Output.Should().Be("1:error\n2:error\n");
        Search("-H error a.log").Output.Should().Be("a.log:error\n");
        Search("-cm1 error a.log b.log").Output.Should().Be("a.log:1\nb.log:1\n");
    }

    [Fact]
    public void FileListsOverrideCountsAndContext()
    {
        Write("a.log", "before\nerror\nafter\n");
        Write("b.log", "ok\n");
        Search("-lcC2 error a.log b.log").Output.Should().Be("a.log\n");
        Search("-LC2 error a.log b.log").Output.Should().Be("b.log\n");
        Search("-L missing a.log").Status.Should().Be(1);
        Search("-L error a.log").Status.Should().Be(0);
        Search("-qC2 error a.log b.log").Output.Should().BeEmpty();
    }

    [Fact]
    public void RecursiveSearchAndDefaultDirectoryWork()
    {
        Write("tree/a.txt", "needle\n");
        Write("tree/sub/b.txt", "needle\n");
        var result = Search("-rn needle tree");
        result.Status.Should().Be(0);
        result.Output.Should().Be("tree\\a.txt:1:needle\ntree\\sub\\b.txt:1:needle\n");
        Search("-rl needle").Output.Should().Contain("tree\\sub\\b.txt");
        Search("-rl needle", "ignored input\n").Output.Should().Contain("tree\\sub\\b.txt");
        Search("needle tree").Error.Should().Contain("use -r");
    }

    [Fact]
    public void PatternFilesHandleEmptyPatternsAndEmptyFiles()
    {
        Write("patterns.txt", "error\nwarning\n");
        Search("-f patterns.txt", "error\nwarning\nok\n").Output.Should().Be("error\nwarning\n");
        Write("patterns.txt", "");
        Search("-f patterns.txt", "text\n").Status.Should().Be(1);
        Search("-vf patterns.txt", "text\n").Output.Should().Be("text\n");
        Write("patterns.txt", "\n");
        Search("-f patterns.txt", "text\n\n").Output.Should().Be("text\n\n");
    }

    [Fact]
    public void ReadsBomEncodedFilesAndDoesNotLoseWhitespace()
    {
        Write("unicode.txt", "  café  \r\n", Encoding.Unicode);
        Search("café unicode.txt").Output.Should().Be("  café  \n");
        Write("unicode.txt", "  café  \r\n", Encoding.UTF32);
        Search("café unicode.txt").Output.Should().Be("  café  \n");
    }

    [Fact]
    public void ExplicitFilesIgnorePipedTextUnlessDashIsPresent()
    {
        Write("a.txt", "file hit\n");
        Search("hit a.txt", "pipe hit\n").Output.Should().Be("file hit\n");
        Search("hit a.txt -", "pipe hit\n").Output.Should().Be("a.txt:file hit\n(standard input):pipe hit\n");
    }

    [Fact]
    public void FileErrorsDoNotPreventSearchingRemainingFiles()
    {
        Write("a.txt", "hit\n");
        var result = Search("hit missing.txt a.txt");
        result.Status.Should().Be(2);
        result.Error.Should().Contain("missing.txt");
        result.Output.Should().Be("a.txt:hit\n");
        Search("-s hit missing.txt a.txt").Error.Should().BeEmpty();
        Search("-s hit missing.txt a.txt").Status.Should().Be(2);
        Search("-q hit a.txt missing.txt").Status.Should().Be(0);
        Search("-q hit a.txt missing.txt").Error.Should().BeEmpty();
        Search("hit missing*.txt").Status.Should().Be(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-z hit")]
    [InlineData("--unknown hit")]
    [InlineData("-e")]
    [InlineData("-m -1 hit")]
    [InlineData("-A nope hit")]
    [InlineData("--max-count=999999999999999999999 hit")]
    [InlineData("--ignore-case=true hit")]
    [InlineData("'unclosed")]
    public void RejectsInvalidArguments(string arguments)
    {
        Action parse = () => GrepManage.ParseArguments(arguments);
        parse.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void InvalidRegexAndMissingInputAreErrors()
    {
        var result = Search("-s '['", "line\n");
        result.Status.Should().Be(2);
        result.Error.Should().Contain("grep:");
        Search("hit").Status.Should().Be(2);
        Search("hit -").Status.Should().Be(2);
    }

    [Fact]
    public void CancellationStopsReading()
    {
        bool cancelled = false;
        using var reader = new CountingReader(() => cancelled = true);
        int status = GrepManage.Search(GrepManage.ParseArguments("hit"), _directory, reader,
            TextWriter.Null, TextWriter.Null, () => cancelled);
        status.Should().Be(130);
        reader.Reads.Should().Be(1);
    }

    [Theory]
    [InlineData("-q hit")]
    [InlineData("-m1 hit")]
    [InlineData("-l hit")]
    public void EarlyExitDoesNotReadTheWholeInput(string arguments)
    {
        using var reader = new CountingReader(() => { });
        int status = GrepManage.Search(GrepManage.ParseArguments(arguments), _directory, reader,
            TextWriter.Null, TextWriter.Null);
        status.Should().Be(0);
        reader.Reads.Should().Be(1);
    }

    private sealed class CountingReader(Action onRead) : TextReader
    {
        public int Reads { get; private set; }
        public override string? ReadLine()
        {
            if (++Reads > 1) throw new InvalidOperationException("Should have stopped after the first line.");
            onRead();
            return "hit";
        }
    }

    [Fact]
    public void CommandIsDiscoveredAndHelpIsAvailableWithoutDirectoryState()
    {
        CommandRepository.GetCommand("grep --help").Should().BeOfType<Grep>();
        GlobalVariables.masterParamCommands["grep"].Should().Contain("--help").And.Contain("-n");
        GlobalVariables.currentDirectory = Path.Combine(_directory, "missing-state");
        Capture(() => new Grep().Execute("grep --help")).Output.Should().Contain("Usage: grep");
        GlobalVariables.isErrorCommand.Should().BeFalse();
    }

    [Fact]
    public void CommandCapturesFirstAndMiddlePipelineStagesAndPrintsLastStage()
    {
        Write("a.txt", "error\nERROR debug\nok\n");
        GlobalVariables.isPipeCommand = true;
        GlobalVariables.pipeCmdCountTemp = GlobalVariables.pipeCmdCount = 2;
        GlobalVariables.pipeCmdOutput = "stale input";
        Capture(() => new Grep().Execute("grep -i error a.txt")).Output.Should().BeEmpty();
        Normalize(GlobalVariables.pipeCmdOutput).Should().Be("error\nERROR debug\n");
        GlobalVariables.pipeCmdCount = 1;
        Capture(() => new Grep().Execute("grep -v debug")).Output.Should().BeEmpty();
        Normalize(GlobalVariables.pipeCmdOutput).Should().Be("error\n");
        GlobalVariables.pipeCmdCount = 0;
        Capture(() => new Grep().Execute("grep -n error")).Output.Should().Be("1:error\n");
        GlobalVariables.isErrorCommand.Should().BeFalse();
    }

    [Fact]
    public void EmptyPipelineInputAndErrorsClearOldOutput()
    {
        GlobalVariables.isPipeCommand = true;
        GlobalVariables.pipeCmdCountTemp = 2;
        GlobalVariables.pipeCmdCount = 1;
        GlobalVariables.pipeCmdOutput = "";
        Capture(() => new Grep().Execute("grep ''")).Output.Should().BeEmpty();
        GlobalVariables.isErrorCommand.Should().BeTrue();
        GlobalVariables.pipeCmdOutput.Should().BeEmpty();
        GlobalVariables.pipeCmdOutput = "stale";
        Capture(() => new Grep().Execute("grep --bad")).Error.Should().Contain("Unknown option");
        GlobalVariables.pipeCmdOutput.Should().BeEmpty();
    }

    [Theory]
    [InlineData("grep -E 'error|warning' a.txt | grep -v debug", "grep -E 'error|warning' a.txt", "grep -v debug")]
    [InlineData("grep -F \"a||b;c\" a.txt | grep -n .", "grep -F \"a||b;c\" a.txt", "grep -n .")]
    public void PipelineSplitterPreservesQuotedMetacharacters(string command, string first, string second)
    {
        FileSystem.SplitPipeline(command).Should().Equal(first, second);
        FileSystem.SplitPipeline(first).Should().Equal(first);
    }

    [Fact]
    public void TermXtPipelineSupportsQuotedRegexAndMultipleGrepStages()
    {
        Write("a.txt", "error\nwarning\nerror debug\nok\n");
        Write("search.xt", "capture matches = grep -E 'error|warning' a.txt | grep -v debug\nprint \"{matches}\"\n");
        var result = Capture(() => new XTScript().Execute("xt " + Path.Combine(_directory, "search.xt")));
        result.Error.Should().BeEmpty();
        result.Output.Should().Contain("error\nwarning").And.NotContain("error debug");
    }
}
