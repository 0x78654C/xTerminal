using System.Collections;
using System.Reflection;
using System.Runtime.Versioning;
using Core.DirFiles;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

[SupportedOSPlatform("Windows")]
public class TermXTEditorSyntaxTests
{
    [Theory]
    [InlineData("main.rs")]
    [InlineData("MAIN.RS")]
    public void DetectSyntaxFromPath_RustFiles_ReturnsRust(string path)
    {
        TermXTEditor.DetectSyntaxFromPath(path).Should().Be(TermXTEditorSyntax.Rust);
    }

    [Theory]
    [InlineData("app.js")]
    [InlineData("module.MJS")]
    [InlineData("common.cjs")]
    [InlineData("component.jsx")]
    public void DetectSyntaxFromPath_JavaScriptFiles_ReturnsJavaScript(string path)
    {
        TermXTEditor.DetectSyntaxFromPath(path).Should().Be(TermXTEditorSyntax.JavaScript);
    }

    [Theory]
    [InlineData("script.py")]
    [InlineData("windowed.PYW")]
    [InlineData("types.pyi")]
    public void DetectSyntaxFromPath_PythonFiles_ReturnsPython(string path)
    {
        TermXTEditor.DetectSyntaxFromPath(path).Should().Be(TermXTEditorSyntax.Python);
    }

    [Theory]
    [InlineData("rust")]
    [InlineData("rs")]
    public void TryParseSyntax_RustAliases_ReturnsRust(string value)
    {
        TermXTEditor.TryParseSyntax(value, out TermXTEditorSyntax syntax).Should().BeTrue();
        syntax.Should().Be(TermXTEditorSyntax.Rust);
        TermXTEditor.SyntaxDisplayName(syntax).Should().Be("Rust");
    }

    [Theory]
    [InlineData("javascript")]
    [InlineData("js")]
    public void TryParseSyntax_JavaScriptAliases_ReturnsJavaScript(string value)
    {
        TermXTEditor.TryParseSyntax(value, out TermXTEditorSyntax syntax).Should().BeTrue();
        syntax.Should().Be(TermXTEditorSyntax.JavaScript);
        TermXTEditor.SyntaxDisplayName(syntax).Should().Be("JavaScript");
    }

    [Theory]
    [InlineData("python")]
    [InlineData("py")]
    public void TryParseSyntax_PythonAliases_ReturnsPython(string value)
    {
        TermXTEditor.TryParseSyntax(value, out TermXTEditorSyntax syntax).Should().BeTrue();
        syntax.Should().Be(TermXTEditorSyntax.Python);
        TermXTEditor.SyntaxDisplayName(syntax).Should().Be("Python");
    }

    [Fact]
    public void TermXtDiagnostics_ReportLineAndDescription()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xt");

        try
        {
            File.WriteAllLines(path, new[]
            {
                "if true",
                "  break"
            });

            var editor = new TermXTEditor(path, TermXTEditorSyntax.TermXt);
            List<DiagnosticInfo> diagnostics = Diagnostics(editor);

            diagnostics.Should().Contain(diagnostic =>
                diagnostic.LineNumber == 2 &&
                diagnostic.Code == string.Empty &&
                diagnostic.Description.Contains("outside of a loop"));

            diagnostics.Should().Contain(diagnostic =>
                diagnostic.LineNumber == 1 &&
                diagnostic.Description.Contains("never closed"));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void TermXtDiagnostics_FlagsCallKeywordTypo()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xt");

        try
        {
            File.WriteAllLines(path, new[]
            {
                "func greet",
                @"  print ""hi""",
                "end",
                "all greet"
            });

            var editor = new TermXTEditor(path, TermXTEditorSyntax.TermXt);
            List<DiagnosticInfo> diagnostics = Diagnostics(editor);

            diagnostics.Should().Contain(diagnostic =>
                diagnostic.LineNumber == 4 &&
                diagnostic.Description.Contains("Did you mean 'call'"));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CSharpDiagnostics_ReportLineCodeAndDescription()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            File.WriteAllText(path, "public class C { void M() { int x = ; } }");

            var editor = new TermXTEditor(path, TermXTEditorSyntax.CSharp);
            List<DiagnosticInfo> diagnostics = Diagnostics(editor);

            diagnostics.Should().Contain(diagnostic =>
                diagnostic.LineNumber == 1 &&
                diagnostic.Code.StartsWith("CS") &&
                !string.IsNullOrWhiteSpace(diagnostic.Description));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CSharpDiagnostics_ReportSemanticErrorsWhenUsingIsMissing()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            File.WriteAllLines(path, new[]
            {
                "// using System.Text;",
                "public class C",
                "{",
                "    void M()",
                "    {",
                "        StringBuilder sb = new StringBuilder();",
                "    }",
                "}"
            });

            var editor = new TermXTEditor(path, TermXTEditorSyntax.CSharp);
            List<DiagnosticInfo> diagnostics = Diagnostics(editor);

            diagnostics.Should().Contain(diagnostic =>
                diagnostic.LineNumber == 6 &&
                diagnostic.Code == "CS0246" &&
                diagnostic.Description.Contains("StringBuilder"));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CSharpCompletion_GlobalPrefix_SuggestsBclTypes()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            var editor = CSharpEditorAtMarker(path, "using System;\npublic class C { void M() { Con$$ } }");

            List<CompletionInfo> completions = CSharpCompletions(editor);

            completions.Should().Contain(completion =>
                completion.Label == "Console" &&
                completion.Kind == "type");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CSharpCompletion_GlobalPrefixWithoutUsing_DoesNotSuggestUnimportedBclTypes()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            var editor = CSharpEditorAtMarker(path, "public class C { void M() { Con$$ } }");

            List<CompletionInfo> completions = CSharpCompletions(editor);

            completions.Should().NotContain(completion => completion.Label == "Console");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CSharpCompletion_StaticMemberAccess_SuggestsConsoleWriteLine()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            var editor = CSharpEditorAtMarker(path, "using System;\npublic class C { void M() { Console.Wr$$ } }");

            List<CompletionInfo> completions = CSharpCompletions(editor);

            completions.Should().Contain(completion =>
                completion.Label == "WriteLine" &&
                completion.Kind == "method");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CSharpCompletion_UnimportedBclType_DoesNotSuggestStaticMembers()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            var editor = CSharpEditorAtMarker(path, "public class C { void M() { Console.Wr$$ } }");

            List<CompletionInfo> completions = CSharpCompletions(editor);

            completions.Should().NotContain(completion => completion.Label == "WriteLine");
            completions.Should().NotContain(completion => completion.Label == "Write");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CSharpCompletion_FullyQualifiedBclType_SuggestsStaticMembers()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            var editor = CSharpEditorAtMarker(path, "public class C { void M() { System.Console.Wr$$ } }");

            List<CompletionInfo> completions = CSharpCompletions(editor);

            completions.Should().Contain(completion =>
                completion.Label == "WriteLine" &&
                completion.Kind == "method");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CSharpCompletion_ImportedBclPropertyChain_SuggestsReturnedTypeMembers()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            var editor = CSharpEditorAtMarker(path, "using System;\npublic class C { void M() { DateTime.Now.To$$ } }");

            List<CompletionInfo> completions = CSharpCompletions(editor);

            completions.Should().Contain(completion =>
                completion.Label == "ToString" &&
                completion.Kind == "method");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CSharpCompletion_LocalVariableMemberAccess_UsesSemanticType()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            var editor = CSharpEditorAtMarker(
                path,
                "using System.Text;\npublic class C { void M() { var sb = new StringBuilder(); sb.Ap$$ } }");

            List<CompletionInfo> completions = CSharpCompletions(editor);

            completions.Should().Contain(completion =>
                completion.Label == "Append" &&
                completion.Kind == "method");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CSharpCompletion_TypingIdentifierPart_AutoOpensSuggestions()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            var editor = CSharpEditorAtMarker(path, "using System;\npublic class C { void M() { Co$$ } }");

            InvokePrivate(editor, "InsertText", "n");
            InvokePrivate(editor, "RefreshCompletionAfterText", "n");

            GetPrivateField<bool>(editor, "_completionActive").Should().BeTrue();
            CSharpCompletions(editor).Should().Contain(completion => completion.Label == "Console");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CSharpCompletion_TypingDot_AutoOpensMemberSuggestions()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            var editor = CSharpEditorAtMarker(path, "using System;\npublic class C { void M() { Console$$ } }");

            InvokePrivate(editor, "InsertText", ".");
            InvokePrivate(editor, "RefreshCompletionAfterText", ".");

            GetPrivateField<bool>(editor, "_completionActive").Should().BeTrue();
            List<CompletionInfo> completions = ActiveCSharpCompletions(editor);
            completions.Should().Contain(completion => completion.Label == "WriteLine");
            completions.Take(4).Should().Contain(completion => completion.Label == "WriteLine");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CSharpCompletion_TopLevelConsoleWithUsing_SuggestsStaticMembers()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            var editor = CSharpEditorAtMarker(path, "using System;\nConsole.Wr$$");

            List<CompletionInfo> completions = CSharpCompletions(editor);

            completions.Should().Contain(completion =>
                completion.Label == "WriteLine" &&
                completion.Kind == "method");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CSharpCompletion_HandleKeyDot_AutoOpensMemberSuggestions()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            var editor = CSharpEditorAtMarker(path, "using System;\nConsole$$");
            SetPrivateEnumField(editor, "_mode", "Insert");

            InvokePrivate(
                editor,
                "HandleKey",
                new ConsoleKeyInfo('.', ConsoleKey.OemPeriod, shift: false, alt: false, control: false));

            GetPrivateField<bool>(editor, "_completionActive").Should().BeTrue();
            List<CompletionInfo> completions = ActiveCSharpCompletions(editor);
            completions.Should().Contain(completion => completion.Label == "WriteLine");
            completions.Take(4).Should().Contain(completion => completion.Label == "WriteLine");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CSharpCompletion_TypingConsoleDotFromScratch_AutoOpensMemberSuggestions()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            var editor = CSharpEditorAtMarker(path, "using System;\n $$");
            SetPrivateEnumField(editor, "_mode", "Insert");

            foreach (char value in "Console.")
                InvokePrivate(
                    editor,
                    "HandleKey",
                    new ConsoleKeyInfo(value, ConsoleKeyFromChar(value), shift: false, alt: false, control: false));

            GetPrivateField<bool>(editor, "_completionActive").Should().BeTrue();
            List<CompletionInfo> completions = ActiveCSharpCompletions(editor);
            completions.Should().Contain(completion => completion.Label == "WriteLine");
            completions.Take(4).Should().Contain(completion => completion.Label == "WriteLine");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void TokenizeRust_HighlightsCommonRustTokens()
    {
        const string line = @"pub fn main<'a>() { println!(r#""hi""#); let x: i32 = 42; } // comment";
        List<TokenInfo> tokens = TokenizeRust(line, 0);

        TokenForText(tokens, line, "pub").Color.Should().Be(Color("CRustModifier"));
        TokenForText(tokens, line, "fn").Color.Should().Be(Color("CRustDeclaration"));
        TokenForText(tokens, line, "'a").Color.Should().Be(Color("CRustLifetime"));
        TokenForText(tokens, line, "println!").Color.Should().Be(Color("CRustMacro"));
        TokenForText(tokens, line, @"r#""hi""#").Color.Should().Be(Color("CRustString"));
        TokenForText(tokens, line, "let").Color.Should().Be(Color("CRustKeyword"));
        TokenForText(tokens, line, "i32").Color.Should().Be(Color("CRustType"));
        TokenForText(tokens, line, "42").Color.Should().Be(Color("CRustNumber"));
        TokenForText(tokens, line, "// comment").Color.Should().Be(Color("CRustComment"));
    }

    [Fact]
    public void TokenizeRust_ContinuesNestedBlockCommentState()
    {
        const string line = "still /* nested */ commented */ let value = true";
        List<TokenInfo> tokens = TokenizeRust(line, 1);

        tokens[0].Text(line).Should().Be("still /* nested */ commented */");
        tokens[0].Color.Should().Be(Color("CRustComment"));
        TokenForText(tokens, line, "let").Color.Should().Be(Color("CRustKeyword"));
        TokenForText(tokens, line, "true").Color.Should().Be(Color("CRustNumber"));
    }

    [Fact]
    public void TokenizeRust_LeavesRangeDotsAsOperators()
    {
        const string line = "for i in 0..10 { }";
        List<TokenInfo> tokens = TokenizeRust(line, 0);

        TokenForText(tokens, line, "0").Color.Should().Be(Color("CRustNumber"));
        TokenForText(tokens, line, "10").Color.Should().Be(Color("CRustNumber"));

        List<TokenInfo> dots = tokens.Where(token => token.Text(line) == ".").ToList();
        dots.Should().HaveCount(2);
        dots.Should().OnlyContain(token => token.Color == Color("CRustOperator"));
    }

    [Fact]
    public void TokenizeJavaScript_HighlightsCommonJavaScriptTokens()
    {
        const string line = "const rx = /[a-z]+/gi; async function main() { console.log(`hi ${name}`); return null; } // note";
        List<TokenInfo> tokens = TokenizeJavaScript(line, false);

        TokenForText(tokens, line, "const").Color.Should().Be(Color("CJavaScriptDeclaration"));
        TokenForText(tokens, line, "/[a-z]+/gi").Color.Should().Be(Color("CJavaScriptRegex"));
        TokenForText(tokens, line, "async").Color.Should().Be(Color("CJavaScriptKeyword"));
        TokenForText(tokens, line, "function").Color.Should().Be(Color("CJavaScriptDeclaration"));
        TokenForText(tokens, line, "console").Color.Should().Be(Color("CJavaScriptBuiltin"));
        TokenForText(tokens, line, "`hi ${name}`").Color.Should().Be(Color("CJavaScriptString"));
        TokenForText(tokens, line, "return").Color.Should().Be(Color("CJavaScriptFlow"));
        TokenForText(tokens, line, "null").Color.Should().Be(Color("CJavaScriptNumber"));
        TokenForText(tokens, line, "// note").Color.Should().Be(Color("CJavaScriptComment"));
    }

    [Fact]
    public void TokenizeJavaScript_ContinuesBlockCommentState()
    {
        const string line = "still commented */ const value = true";
        List<TokenInfo> tokens = TokenizeJavaScript(line, true);

        tokens[0].Text(line).Should().Be("still commented */");
        tokens[0].Color.Should().Be(Color("CJavaScriptComment"));
        TokenForText(tokens, line, "const").Color.Should().Be(Color("CJavaScriptDeclaration"));
        TokenForText(tokens, line, "true").Color.Should().Be(Color("CJavaScriptNumber"));
    }

    [Fact]
    public void TokenizePython_HighlightsCommonPythonTokens()
    {
        const string line = @"async def main(name): print(f""Hello {name}""); return None # note";
        List<TokenInfo> tokens = TokenizePython(line, 0);

        TokenForText(tokens, line, "async").Color.Should().Be(Color("CPythonKeyword"));
        TokenForText(tokens, line, "def").Color.Should().Be(Color("CPythonDeclaration"));
        TokenForText(tokens, line, "print").Color.Should().Be(Color("CPythonBuiltin"));
        TokenForText(tokens, line, @"f""Hello {name}""").Color.Should().Be(Color("CPythonString"));
        TokenForText(tokens, line, "return").Color.Should().Be(Color("CPythonFlow"));
        TokenForText(tokens, line, "None").Color.Should().Be(Color("CPythonNumber"));
        TokenForText(tokens, line, "# note").Color.Should().Be(Color("CPythonComment"));
    }

    [Fact]
    public void TokenizePython_HighlightsDecoratorsAndContinuesTripleQuotedStringState()
    {
        const string decorator = "@click.command()";
        List<TokenInfo> decoratorTokens = TokenizePython(decorator, 0);
        TokenForText(decoratorTokens, decorator, "@click.command").Color.Should().Be(Color("CPythonDecorator"));

        const string line = "still string''' value = 42";
        List<TokenInfo> tokens = TokenizePython(line, '\'');

        tokens[0].Text(line).Should().Be("still string'''");
        tokens[0].Color.Should().Be(Color("CPythonString"));
        TokenForText(tokens, line, "42").Color.Should().Be(Color("CPythonNumber"));
    }

    [Fact]
    public void InsertTextWithoutUndo_MultilinePaste_PreservesPastedIndentation()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            File.WriteAllText(path, "    start");
            var editor = new TermXTEditor(path, TermXTEditorSyntax.CSharp);
            SetPrivateField(editor, "_cursorLine", 0);
            SetPrivateField(editor, "_cursorCol", "    start".Length);

            InvokePrivate(editor, "InsertTextWithoutUndo", "\n        nested\n    done");

            Lines(editor).Should().Equal(
                "    start",
                "        nested",
                "    done");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CutSelectionOrCurrentLine_WithSelection_CutsSelectedText()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xt");

        try
        {
            File.WriteAllText(path, "alpha beta");
            var editor = new TermXTEditor(path, TermXTEditorSyntax.TermXt);
            SetPrivateField(editor, "_hasSelectionAnchor", true);
            SetPrivateField(editor, "_selectionAnchorLine", 0);
            SetPrivateField(editor, "_selectionAnchorCol", 6);
            SetPrivateField(editor, "_cursorLine", 0);
            SetPrivateField(editor, "_cursorCol", 10);

            InvokePrivate(editor, "CutSelectionOrCurrentLine", false);

            Lines(editor).Should().Equal("alpha ");
            GetPrivateField<bool>(editor, "_hasSelectionAnchor").Should().BeFalse();
            GetPrivateField<bool>(editor, "_dirty").Should().BeTrue();
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CutSelectionOrCurrentLine_WithoutSelection_CutsCurrentLine()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xt");

        try
        {
            File.WriteAllLines(path, new[] { "one", "two", "three" });
            var editor = new TermXTEditor(path, TermXTEditorSyntax.TermXt);
            SetPrivateField(editor, "_cursorLine", 1);
            SetPrivateField(editor, "_cursorCol", 1);

            InvokePrivate(editor, "CutSelectionOrCurrentLine", false);

            Lines(editor).Should().Equal("one", "three");
            GetPrivateField<bool>(editor, "_hasLineClipboard").Should().BeTrue();
            GetPrivateField<string>(editor, "_lineClipboard").Should().Be("two");
            GetPrivateField<bool>(editor, "_dirty").Should().BeTrue();
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CheckExternalFileChange_WhenDiskFileChanges_SetsPendingDiskWarning()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xt");

        try
        {
            File.WriteAllText(path, "print \"old\"");
            var editor = new TermXTEditor(path);

            WriteExternalChange(path, "print \"new\"");

            InvokePrivate<bool>(editor, "CheckExternalFileChange").Should().BeTrue();
            GetPrivateField<bool>(editor, "_externalChangePending").Should().BeTrue();
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Save_WithoutForce_WhenDiskFileChanged_DoesNotOverwriteExternalChange()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xt");

        try
        {
            File.WriteAllText(path, "print \"old\"");
            var editor = new TermXTEditor(path);
            WriteExternalChange(path, "print \"changed by other app\"");

            InvokePrivate<bool>(editor, "CheckExternalFileChange").Should().BeTrue();

            InvokePrivate<bool>(editor, "Save", false).Should().BeFalse();
            File.ReadAllText(path).Should().Be("print \"changed by other app\"");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void TryGetQueuedPasteTextFragment_PasteKeys_ReturnsExactText()
    {
        PasteFragment(new ConsoleKeyInfo('\r', ConsoleKey.Enter, shift: false, alt: false, control: false))
            .Should().Be("\n");
        PasteFragment(new ConsoleKeyInfo('\t', ConsoleKey.Tab, shift: false, alt: false, control: false))
            .Should().Be("\t");
        PasteFragment(new ConsoleKeyInfo('x', ConsoleKey.X, shift: false, alt: false, control: false))
            .Should().Be("x");
    }

    private static List<TokenInfo> TokenizeRust(string line, int blockCommentDepth)
    {
        MethodInfo method = typeof(TermXTEditor).GetMethod(
            "TokenizeRust",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (IEnumerable)method.Invoke(null, new object[] { line, blockCommentDepth })!;
        var tokens = new List<TokenInfo>();
        foreach (object token in result)
        {
            Type tokenType = token.GetType();
            tokens.Add(new TokenInfo(
                (int)tokenType.GetProperty("Start")!.GetValue(token)!,
                (int)tokenType.GetProperty("Length")!.GetValue(token)!,
                (int)tokenType.GetProperty("Color")!.GetValue(token)!));
        }

        return tokens;
    }

    private static TermXTEditor CSharpEditorAtMarker(string path, string markedText)
    {
        int marker = markedText.IndexOf("$$", StringComparison.Ordinal);
        marker.Should().BeGreaterThanOrEqualTo(0);

        string text = markedText.Remove(marker, 2);
        File.WriteAllText(path, text);

        var editor = new TermXTEditor(path, TermXTEditorSyntax.CSharp);
        (int line, int column) = LineColumnAtPosition(text, marker);
        SetPrivateField(editor, "_cursorLine", line);
        SetPrivateField(editor, "_cursorCol", column);
        return editor;
    }

    private static (int line, int column) LineColumnAtPosition(string text, int position)
    {
        int line = 0;
        int column = 0;
        int limit = Math.Min(position, text.Length);

        for (int i = 0; i < limit; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                column = 0;
            }
            else
            {
                column++;
            }
        }

        return (line, column);
    }

    private static List<CompletionInfo> CSharpCompletions(TermXTEditor editor)
    {
        MethodInfo method = typeof(TermXTEditor).GetMethod(
            "GetCSharpCompletionItems",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var result = new List<CompletionInfo>();
        foreach (object completion in (IEnumerable)method.Invoke(editor, Array.Empty<object>())!)
            result.Add(ToCompletionInfo(completion));

        return result;
    }

    private static List<CompletionInfo> ActiveCSharpCompletions(TermXTEditor editor)
    {
        FieldInfo field = typeof(TermXTEditor).GetField(
            "_completionItems",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var result = new List<CompletionInfo>();
        foreach (object completion in (IEnumerable)field.GetValue(editor)!)
            result.Add(ToCompletionInfo(completion));

        return result;
    }

    private static CompletionInfo ToCompletionInfo(object completion)
    {
        Type completionType = completion.GetType();
        return new CompletionInfo(
            (string)completionType.GetProperty("Label")!.GetValue(completion)!,
            (string)completionType.GetProperty("Kind")!.GetValue(completion)!,
            (string)completionType.GetProperty("Detail")!.GetValue(completion)!);
    }

    private static void SetPrivateEnumField(object target, string fieldName, string value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        field.SetValue(target, Enum.Parse(field.FieldType, value));
    }

    private static List<DiagnosticInfo> Diagnostics(TermXTEditor editor)
    {
        InvokePrivate(editor, "EnsureDiagnostics");

        FieldInfo field = typeof(TermXTEditor).GetField(
            "_diagnostics",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var result = new List<DiagnosticInfo>();
        foreach (object diagnostic in (IEnumerable)field.GetValue(editor)!)
        {
            Type diagnosticType = diagnostic.GetType();
            result.Add(new DiagnosticInfo(
                (int)diagnosticType.GetProperty("LineNumber")!.GetValue(diagnostic)!,
                (string)diagnosticType.GetProperty("Code")!.GetValue(diagnostic)!,
                (string)diagnosticType.GetProperty("Description")!.GetValue(diagnostic)!));
        }

        return result;
    }

    private static List<TokenInfo> TokenizeJavaScript(string line, bool startsInBlockComment)
    {
        MethodInfo method = typeof(TermXTEditor).GetMethod(
            "TokenizeJavaScript",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (IEnumerable)method.Invoke(null, new object[] { line, startsInBlockComment })!;
        var tokens = new List<TokenInfo>();
        foreach (object token in result)
        {
            Type tokenType = token.GetType();
            tokens.Add(new TokenInfo(
                (int)tokenType.GetProperty("Start")!.GetValue(token)!,
                (int)tokenType.GetProperty("Length")!.GetValue(token)!,
                (int)tokenType.GetProperty("Color")!.GetValue(token)!));
        }

        return tokens;
    }

    private static List<TokenInfo> TokenizePython(string line, int multilineStringQuote)
    {
        MethodInfo method = typeof(TermXTEditor).GetMethod(
            "TokenizePython",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (IEnumerable)method.Invoke(null, new object[] { line, multilineStringQuote })!;
        var tokens = new List<TokenInfo>();
        foreach (object token in result)
        {
            Type tokenType = token.GetType();
            tokens.Add(new TokenInfo(
                (int)tokenType.GetProperty("Start")!.GetValue(token)!,
                (int)tokenType.GetProperty("Length")!.GetValue(token)!,
                (int)tokenType.GetProperty("Color")!.GetValue(token)!));
        }

        return tokens;
    }

    private static string PasteFragment(ConsoleKeyInfo key)
    {
        MethodInfo method = typeof(TermXTEditor).GetMethod(
            "TryGetQueuedPasteTextFragment",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        object?[] arguments = { key, null };
        ((bool)method.Invoke(null, arguments)!).Should().BeTrue();
        return (string)arguments[1]!;
    }

    private static void WriteExternalChange(string path, string text)
    {
        File.WriteAllText(path, text);
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(2));
    }

    private static ConsoleKey ConsoleKeyFromChar(char value)
    {
        if (value >= 'A' && value <= 'Z')
            return (ConsoleKey)((int)ConsoleKey.A + (value - 'A'));

        if (value >= 'a' && value <= 'z')
            return (ConsoleKey)((int)ConsoleKey.A + (value - 'a'));

        if (value == '.')
            return ConsoleKey.OemPeriod;

        throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported test key.");
    }

    private static void InvokePrivate(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        method.Invoke(target, arguments);
    }

    private static T InvokePrivate<T>(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        return (T)method.Invoke(target, arguments)!;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        return (T)field.GetValue(target)!;
    }

    private static List<string> Lines(TermXTEditor editor)
    {
        FieldInfo field = typeof(TermXTEditor).GetField(
            "_lines",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        return (List<string>)field.GetValue(editor)!;
    }

    private static TokenInfo TokenForText(List<TokenInfo> tokens, string line, string text)
    {
        return tokens.Single(token => token.Text(line) == text);
    }

    private static int Color(string name)
    {
        FieldInfo field = typeof(TermXTEditor).GetField(
            name,
            BindingFlags.NonPublic | BindingFlags.Static)!;

        return (int)field.GetRawConstantValue()!;
    }

    private readonly struct TokenInfo
    {
        public TokenInfo(int start, int length, int color)
        {
            Start = start;
            Length = length;
            Color = color;
        }

        public int Start { get; }
        public int Length { get; }
        public int Color { get; }

        public string Text(string line)
        {
            return line.Substring(Start, Length);
        }
    }

    private readonly struct DiagnosticInfo
    {
        public DiagnosticInfo(int lineNumber, string code, string description)
        {
            LineNumber = lineNumber;
            Code = code;
            Description = description;
        }

        public int LineNumber { get; }
        public string Code { get; }
        public string Description { get; }
    }

    private readonly struct CompletionInfo
    {
        public CompletionInfo(string label, string kind, string detail)
        {
            Label = label;
            Kind = kind;
            Detail = detail;
        }

        public string Label { get; }
        public string Kind { get; }
        public string Detail { get; }
    }
}
