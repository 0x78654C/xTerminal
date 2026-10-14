using System.Text;
using Core.DirFiles;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

public partial class TermXTEditorSyntaxTests
{
    [Theory]
    [InlineData("pr$$", "print", "keyword", "print <text>")]
    [InlineData("  CaP$$", "capture", "keyword", "capture <name>")]
    [InlineData("set value = su$$", "substr", "function", "<start> <length>")]
    [InlineData("SET\tvalue=RE$$", "replace", "function", "<old> <new>")]
    [InlineData("if {name} co$$", "contains", "operator", "contains")]
    [InlineData("while no$$", "not", "operator", "negate")]
    [InlineData("each row in li$$", "lines:", "source", "<variable>")]
    [InlineData("each row in lines:$$", "CWD", "variable", "working directory")]
    [InlineData("print {$$", "argc", "variable", "argument count")]
    [InlineData("print \"Hello {us$$\"", "USER", "variable", "account")]
    [InlineData("print {27$$", "27", "variable", "argument 27")]
    [InlineData("# a class in a comment\nprint \"a class in text\"\npr$$", "print", "keyword", "print <text>")]
    public void TermXtCompletion_AutomaticSuggestionsMatchContext(
        string text, string label, string kind, string detail)
    {
        var editor = TermXtEditorAtMarker(text);
        InvokePrivate(editor, "RefreshCompletionAfterEdit");

        GetPrivateField<bool>(editor, "_completionActive").Should().BeTrue();
        ActiveCSharpCompletions(editor).Should().Contain(item =>
            item.Label == label && item.Kind == kind && item.Detail.Contains(detail));
    }

    [Theory]
    [InlineData("p$$")]
    [InlineData("# print {$$")]
    [InlineData("   # pr$$")]
    [InlineData("print pr$$")]
    [InlineData("print \"su$$\"")]
    [InlineData("set pr$$")]
    [InlineData("func pr$$")]
    [InlineData("each pr$$")]
    [InlineData("set value = upper su$$")]
    [InlineData("set value = \"su$$\"")]
    [InlineData("run pr$$")]
    [InlineData("call greet pr$$\nfunc greet\nend")]
    [InlineData("print {not_a_declared_variable$$")]
    [InlineData("if \"co$$\"")]
    [InlineData("if {name} and$$")]
    public void TermXtCompletion_DoesNotSuggestInUnrelatedText(string text)
    {
        var editor = TermXtEditorAtMarker(text);
        InvokePrivate(editor, "RefreshCompletionAfterEdit");

        GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
    }

    [Fact]
    public void TermXtCompletion_FunctionsIncludeForwardDeclarationsAndIgnoreComments()
    {
        var editor = TermXtEditorAtMarker("call $$\nfunc greet-user\nend\nFUNC GREET-USER\nend\n# func hidden");
        InvokePrivate(editor, "RefreshCompletionAfterText", " ");

        ActiveCSharpCompletions(editor).Select(item => item.Label).Should().Equal("greet-user");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false));
        Lines(editor)[0].Should().Be("call greet-user");
    }

    [Fact]
    public void TermXtCompletion_VariablesIncludeAllDeclarationFormsAndDeduplicateIgnoringCase()
    {
        var editor = TermXtEditorAtMarker(
            "set person = Ada\nSET PERSON = Bob\ninput answer = Prompt\nread report = file.txt\n" +
            "capture output = ls\neach row in lines:report\nend\n# set hidden = no\nprint {$$");
        InvokePrivate(editor, "RefreshCompletionAfterEdit");

        var labels = ActiveCSharpCompletions(editor).Select(item => item.Label).ToList();
        labels.Should().Contain(new[] { "person", "answer", "report", "output", "row", "argc", "error_message", "1" });
        labels.Should().NotContain("hidden");
        labels.Count(label => label.Equals("person", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
    }

    [Theory]
    [InlineData("pr$$", "print", "print")]
    [InlineData("pr$$int trailing text", "print", "print trailing text")]
    [InlineData("print {CW$$", "CWD", "print {CWD}")]
    [InlineData("print {CW$$D} suffix", "CWD", "print {CWD} suffix")]
    [InlineData("print \"{CW$$}\"", "CWD", "print \"{CWD}\"")]
    [InlineData("print {CW$$suffix", "CWD", "print {CWD}")]
    [InlineData("each row in li$$nes:report", "lines:", "each row in lines:report")]
    [InlineData("each row in lines:CW$$D", "CWD", "each row in lines:CWD")]
    public void TermXtCompletion_CommitPreservesSurroundingText(string marked, string label, string expected)
    {
        var editor = TermXtEditorAtMarker(marked);
        InvokePrivate(editor, "RefreshCompletionAfterEdit");
        int selected = ActiveCSharpCompletions(editor).FindIndex(item => item.Label == label);
        selected.Should().BeGreaterThanOrEqualTo(0);
        SetPrivateField(editor, "_completionSelectedIndex", selected);

        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        Lines(editor).Should().ContainSingle().Which.Should().Be(expected);
        GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
        GetPrivateField<bool>(editor, "_dirty").Should().BeTrue();
        InvokePrivate(editor, "Undo");
        Lines(editor)[0].Should().Be(marked.Replace("$$", ""));
        InvokePrivate(editor, "Redo");
        Lines(editor)[0].Should().Be(expected);
    }

    [Fact]
    public void TermXtCompletion_TypingFiltersAndBackspaceRestoresCandidates()
    {
        var editor = TermXtEditorAtMarker("print {$$");
        InvokePrivate(editor, "RefreshCompletionAfterText", "{");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false));
        ActiveCSharpCompletions(editor).Select(item => item.Label).Should().Equal("error", "error_message");

        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == "CWD");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('}', ConsoleKey.Oem6, true, false, false));
        GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
    }

    [Fact]
    public void TermXtCompletion_SelectionAndEscapeUseExistingPopupControls()
    {
        var editor = TermXtEditorAtMarker("print {$$");
        InvokePrivate(editor, "RefreshCompletionAfterEdit");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        GetPrivateField<int>(editor, "_completionSelectedIndex").Should().Be(1);

        var frame = GetPrivateField<StringBuilder>(editor, "_frame");
        frame.Clear();
        InvokePrivate(editor, "RenderCompletionPopup", 2, 5, 20, 100, 105);
        frame.ToString().Should().Contain("argc").And.Contain("argument count");

        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false));
        GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
        GetPrivateField<object>(editor, "_mode").ToString().Should().Be("Insert");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false));
        GetPrivateField<object>(editor, "_mode").ToString().Should().Be("Normal");
    }

    [Fact]
    public void TermXtCompletion_CtrlSpaceOpensAndKeepsShortPrefixes()
    {
        var editor = TermXtEditorAtMarker("$$");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', ConsoleKey.Spacebar, false, false, true));
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == "print");
        Lines(editor)[0].Should().BeEmpty();

        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('p', ConsoleKey.P, false, false, false));
        ActiveCSharpCompletions(editor).Select(item => item.Label).Should().Equal("print");
    }

    [Theory]
    [InlineData("  $$  ", "print")]
    [InlineData("each row $$", "in")]
    [InlineData("set value = $$", "substr")]
    public void TermXtCompletion_CtrlSpaceWorksAtEmptyContext(string text, string expected)
    {
        var editor = TermXtEditorAtMarker(text);
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', ConsoleKey.Spacebar, false, false, true));
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == expected);
    }

    [Fact]
    public void CSharpCompletion_CtrlSpaceOpensSuggestions()
    {
        var editor = TermXtEditorAtMarker("$$");
        SetPrivateField(editor, "_syntax", TermXTEditorSyntax.CSharp);
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', ConsoleKey.Spacebar, false, false, true));
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == "class");
        Lines(editor)[0].Should().BeEmpty();
    }

    [Fact]
    public void TermXtCompletion_TypingKeywordFromScratchOpensAutomatically()
    {
        var editor = TermXtEditorAtMarker("$$");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('p', ConsoleKey.P, false, false, false));
        GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('r', ConsoleKey.R, false, false, false));
        ActiveCSharpCompletions(editor).Select(item => item.Label).Should().Equal("print");
    }

    [Fact]
    public void TermXtCompletion_SwitchingSyntaxDismissesSuggestions()
    {
        var editor = TermXtEditorAtMarker("pr$$");
        InvokePrivate(editor, "RefreshCompletionAfterEdit");
        InvokePrivate(editor, "ExecuteEditorCommand", "syntax py");
        GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
    }

    [Theory]
    [InlineData("run pi$$", "ping", "run ping")]
    [InlineData("capture result = pi$$", "ping", "capture result = ping")]
    [InlineData("run ls | ca$$", "cat", "run ls | cat")]
    [InlineData("capture result = ls | ca$$", "cat", "capture result = ls | cat")]
    [InlineData("run custom-$$tool --flag", "custom-tool", "run custom-tool --flag")]
    [InlineData("run pi$$|cat", "ping", "run ping|cat")]
    [InlineData("pi$$", "ping", "ping")]
    public void TermXtCompletion_TerminalCommandsUseTheCommandAndPipelineContext(string marked, string label, string expected)
    {
        var editor = TermXtEditorAtMarker(marked, new[] { "ping", "cat", "custom-tool" });
        InvokePrivate(editor, "RefreshCompletionAfterEdit");
        var items = ActiveCSharpCompletions(editor);
        items.Should().Contain(item => item.Label == label && item.Kind == "command");
        SetPrivateField(editor, "_completionSelectedIndex", items.FindIndex(item => item.Label == label));
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false));
        Lines(editor).Should().Equal(expected);
    }

    [Theory]
    [InlineData("run ping ca$$")]
    [InlineData("run echo \"a | ca$$\"")]
    [InlineData("run echo \"|\" ca$$")]
    [InlineData("capture ca$$")]
    [InlineData("run ping || ca$$")]
    public void TermXtCompletion_CommandNamesDoNotReplaceArgumentsOrQuotedText(string marked)
    {
        var editor = TermXtEditorAtMarker(marked, new[] { "cat" });
        InvokePrivate(editor, "RefreshCompletionAfterEdit");
        GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
    }

    [Fact]
    public void TermXtCompletion_EditorFactorySuppliesRealTerminalCommands()
    {
        var factory = typeof(global::Commands.TerminalCommands.ScriptingLanguage.TermXTEditorCommand).GetMethod(
            "CreateEditorFromArguments", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var editor = (TermXTEditor)factory.Invoke(null, new object[] { Guid.NewGuid() + ".xt", Path.GetTempPath() })!;
        SetPrivateEnumField(editor, "_mode", "Insert");
        foreach (char c in "run pi")
            InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo(c, ConsoleKey.NoName, false, false, false));
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == "ping" && item.Kind == "command");
    }

    [Theory]
    [InlineData("if tr$$", "true")]
    [InlineData("while fa$$", "false")]
    [InlineData("if {argc} > 0 $$", "&&")]
    [InlineData("if {argc} > 0 $$", "||")]
    public void TermXtCompletion_ConditionsOfferRuntimeSupportedOperatorsAndLiterals(string marked, string label)
    {
        var editor = TermXtEditorAtMarker(marked);
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', ConsoleKey.Spacebar, false, false, true));
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == label);
    }

    [Fact]
    public void TermXtCompletion_FunctionNamesWithSpacesAreQuotedOnAcceptance()
    {
        var editor = TermXtEditorAtMarker("func greet user\nend\ncall gr$$");
        InvokePrivate(editor, "RefreshCompletionAfterEdit");
        ActiveCSharpCompletions(editor).Should().ContainSingle().Which.Label.Should().Be("greet user");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false));
        Lines(editor)[2].Should().Be("call \"greet user\"");
    }

    [Fact]
    public void TermXtCompletion_TermXtStatementsTakePriorityOverCSharpLookingExternalCommands()
    {
        var editor = TermXtEditorAtMarker("namespace tools\nset name = user\npr$$");
        InvokePrivate<bool>(editor, "IsCSharpCompletionContext").Should().BeFalse();
        InvokePrivate(editor, "RefreshCompletionAfterEdit");
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == "print");
    }

    private static TermXTEditor TermXtEditorAtMarker(string markedText, IEnumerable<string>? commands = null)
    {
        int marker = markedText.IndexOf("$$", StringComparison.Ordinal);
        marker.Should().BeGreaterThanOrEqualTo(0);
        var editor = new TermXTEditor(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".xt"), TermXTEditorSyntax.TermXt, false, commands);
        string text = markedText.Remove(marker, 2);
        Lines(editor).Clear();
        Lines(editor).AddRange(text.Split('\n'));
        (int line, int column) = LineColumnAtPosition(text, marker);
        SetPrivateField(editor, "_cursorLine", line);
        SetPrivateField(editor, "_cursorCol", column);
        SetPrivateEnumField(editor, "_mode", "Insert");
        return editor;
    }
}
