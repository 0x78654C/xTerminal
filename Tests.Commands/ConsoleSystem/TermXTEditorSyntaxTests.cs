using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
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
    public void CSharpDiagnostics_ReportWarnings()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            File.WriteAllText(path, "public class C { void M() { int x = 1; } }");

            var editor = new TermXTEditor(path, TermXTEditorSyntax.CSharp);
            List<DiagnosticInfo> diagnostics = Diagnostics(editor);

            diagnostics.Should().Contain(diagnostic =>
                diagnostic.LineNumber == 1 &&
                diagnostic.Code == "CS0219" &&
                diagnostic.Severity == "Warning");
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
    public void CSharpDiagnostics_TopLevelLocalFunction_DoesNotReportDllOutputError()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            File.WriteAllLines(path, new[]
            {
                "using System;",
                "using System.Diagnostics;",
                "using System.IO;",
                string.Empty,
                "void Data()",
                "{",
                "    var fileInfo = new FileInfo(@\"c:\\users\\mrx\\downloads\\test\\picture.jpg\");",
                "    Console.WriteLine(fileInfo.Length);",
                "}"
            });

            var editor = new TermXTEditor(path, TermXTEditorSyntax.CSharp);
            List<DiagnosticInfo> diagnostics = Diagnostics(editor);

            diagnostics.Should().NotContain(diagnostic =>
                diagnostic.LineNumber == 5 &&
                diagnostic.Code == "CS8805");
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
                completion.Kind == "method" &&
                completion.Detail.Contains("WriteLine(") &&
                completion.Detail.Contains(")"));
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
                completion.Kind == "method" &&
                completion.Detail.Contains("Append(") &&
                completion.Detail.Contains(")"));
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
    public void CSharpCompletion_UsingDirectiveDot_SuggestsNamespaceMembers()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            var editor = CSharpEditorAtMarker(path, "using System.$$");

            List<CompletionInfo> completions = CSharpCompletions(editor);

            completions.Should().Contain(completion =>
                completion.Label == "Text" &&
                completion.Kind == "namespace" &&
                completion.Detail == "System.Text");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CSharpCompletion_HandleKeyUsingDirectiveDot_AutoOpensNamespaceSuggestions()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            var editor = CSharpEditorAtMarker(path, "using System$$");
            SetPrivateEnumField(editor, "_mode", "Insert");

            InvokePrivate(
                editor,
                "HandleKey",
                new ConsoleKeyInfo('.', ConsoleKey.OemPeriod, shift: false, alt: false, control: false));

            GetPrivateField<bool>(editor, "_completionActive").Should().BeTrue();
            List<CompletionInfo> completions = ActiveCSharpCompletions(editor);
            completions.Should().Contain(completion =>
                completion.Label == "Text" &&
                completion.Kind == "namespace" &&
                completion.Detail == "System.Text");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CSharpCompletion_TermXtSyntaxWithIncompleteUsingDirective_AutoOpensNamespaceSuggestions()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xt");

        try
        {
            File.WriteAllText(path, "using System");
            var editor = new TermXTEditor(path, TermXTEditorSyntax.TermXt);
            SetPrivateEnumField(editor, "_mode", "Insert");
            SetPrivateField(editor, "_cursorLine", 0);
            SetPrivateField(editor, "_cursorCol", "using System".Length);

            InvokePrivate(
                editor,
                "HandleKey",
                new ConsoleKeyInfo('.', ConsoleKey.OemPeriod, shift: false, alt: false, control: false));

            GetPrivateField<bool>(editor, "_completionActive").Should().BeTrue();
            List<CompletionInfo> completions = ActiveCSharpCompletions(editor);
            completions.Should().Contain(completion =>
                completion.Label == "Text" &&
                completion.Kind == "namespace" &&
                completion.Detail == "System.Text");
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
    public void CSharpCompletion_IncompleteMethodConsoleDot_AutoOpensMemberSuggestions()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            var editor = CSharpEditorAtMarker(
                path,
                "using System;\npublic class C\n{\n    public void M()\n    {\n        Console$$");
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
    public void CSharpCompletion_TermXtSyntaxWithCSharpUsing_AutoOpensMemberSuggestions()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xt");

        try
        {
            File.WriteAllText(path, "using System;\nConsole");
            var editor = new TermXTEditor(path, TermXTEditorSyntax.TermXt);
            SetPrivateEnumField(editor, "_mode", "Insert");
            SetPrivateField(editor, "_cursorLine", 1);
            SetPrivateField(editor, "_cursorCol", "Console".Length);

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

    [Theory]
    [InlineData("using System;\npublic class C { void M() { var s = \"Co$$\"; } }")]
    [InlineData("using System;\npublic class C { void M() { var s = $\"Co$$ {value}\"; } }")]
    [InlineData("using System;\npublic class C { void M() { var s = @\"Co$$\"; } }")]
    [InlineData("using System;\npublic class C { void M() { var s = $@\"Co$$\"; } }")]
    [InlineData("using System;\npublic class C { void M() { var c = 'C$$'; } }")]
    [InlineData("using System;\npublic class C { void M() { // Co$$\n} }")]
    [InlineData("using System;\npublic class C { void M() { /* Co$$ */ } }")]
    [InlineData("using System;\npublic class C { void M() { var s = \"\"\"Co$$\"\"\"; } }")]
    [InlineData("using System;\npublic class C { void M() { var s = $\"\"\"Co$$ {value}\"\"\"; } }")]
    [InlineData("using System;\npublic class C { void M() { var s = \"\"\"\nCo$$\n\"\"\"; } }")]
    [InlineData("#if CO$$\n#endif\nusing System;\npublic class C { }")]
    [InlineData("using System;\npublic class C { void M() { 12$$ } }")]
    [InlineData("using System;\npublic class C { void M() { 1.$$ } }")]
    public void CSharpCompletion_SuppressedInNonExpressionContexts(string markedText)
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            var editor = CSharpEditorAtMarker(path, markedText);

            List<CompletionInfo> completions = CSharpCompletions(editor);

            completions.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Theory]
    [InlineData("using System;\npublic class C { void M() { var da$$ } }", "t")]
    [InlineData("using System;\npublic class C { void M() { int cou$$ } }", "n")]
    [InlineData("using System;\npublic class C { void M() { string na$$ } }", "m")]
    [InlineData("using System.Collections.Generic;\npublic class C { void M() { List<string> ite$$ } }", "m")]
    [InlineData("public class MyClass { }\npublic class C { void M() { MyClass insta$$ } }", "n")]
    [InlineData("using System;\npublic class C { void M() { using var strea$$ } }", "m")]
    [InlineData("using System;\npublic class C { void M() { const int ma$$ } }", "x")]
    [InlineData("using System;\npublic class C { void M() { int first, seco$$ } }", "n")]
    [InlineData("using System;\npublic class C { void M() { int first = 1, seco$$ = 2; } }", "n")]
    public void CSharpCompletion_TypingDeclarationName_DoesNotAutoOpen(string markedText, string typedText)
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            var editor = CSharpEditorAtMarker(path, markedText);

            InvokePrivate(editor, "InsertText", typedText);
            InvokePrivate(editor, "RefreshCompletionAfterText", typedText);

            GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
            CSharpCompletions(editor).Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Theory]
    [InlineData("using System;\npublic class C { void M() { var data = Co$$ } }", "n")]
    [InlineData("using System;\npublic class C { object M() { return Co$$ } }", "n")]
    [InlineData("using System;\npublic class C { void M() { Console.WriteLine(Co$$); } }", "n")]
    public void CSharpCompletion_TypingExpressionName_AutoOpens(string markedText, string typedText)
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            var editor = CSharpEditorAtMarker(path, markedText);

            InvokePrivate(editor, "InsertText", typedText);
            InvokePrivate(editor, "RefreshCompletionAfterText", typedText);

            GetPrivateField<bool>(editor, "_completionActive").Should().BeTrue();
            ActiveCSharpCompletions(editor).Should().Contain(completion => completion.Label == "Console");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void TokenizeTermXt_HighlightsFunctionDeclarationsAndCalls()
    {
        const string declaration = "func greet";
        List<TokenInfo> declarationTokens = TokenizeTermXt(declaration);

        TokenForText(declarationTokens, declaration, "func").Color.Should().Be(Color("CFlow"));
        TokenForText(declarationTokens, declaration, "greet").Color.Should().Be(Color("CFunction"));

        const string call = "call greet eval";
        List<TokenInfo> callTokens = TokenizeTermXt(call);

        TokenForText(callTokens, call, "call").Color.Should().Be(Color("CKeyword"));
        TokenForText(callTokens, call, "greet").Color.Should().Be(Color("CFunction"));
        TokenForText(callTokens, call, "eval").Color.Should().Be(Color("CFunction"));
    }

    [Fact]
    public void TokenizeCSharp_HighlightsMethodNames()
    {
        const string line = "public class C { void Render() { Console.WriteLine(Render()); } }";
        List<TokenInfo> tokens = TokenizeCSharp(line, false);

        tokens.Where(token => token.Text(line) == "Render")
            .Should()
            .HaveCount(2)
            .And.OnlyContain(token => token.Color == Color("CSharpFunction"));
        TokenForText(tokens, line, "WriteLine").Color.Should().Be(Color("CSharpFunction"));
        TokenForText(tokens, line, "Console").Color.Should().Be(Color("CSharpBcl"));
    }

    [Fact]
    public void TokenizeCStyle_HighlightsFunctionNames()
    {
        const string line = @"int sum(int value) { return printf(""%d"", sum(value)); }";
        List<TokenInfo> tokens = TokenizeCStyle(line, cpp: false);

        tokens.Where(token => token.Text(line) == "sum")
            .Should()
            .HaveCount(2)
            .And.OnlyContain(token => token.Color == Color("CSourceFunction"));
        TokenForText(tokens, line, "printf").Color.Should().Be(Color("CSourceFunction"));
    }

    [Fact]
    public void TokenizeRust_HighlightsCommonRustTokens()
    {
        const string line = @"pub fn main<'a>() { println!(r#""hi""#); let x: i32 = 42; } // comment";
        List<TokenInfo> tokens = TokenizeRust(line, 0);

        TokenForText(tokens, line, "pub").Color.Should().Be(Color("CRustModifier"));
        TokenForText(tokens, line, "fn").Color.Should().Be(Color("CRustDeclaration"));
        TokenForText(tokens, line, "main").Color.Should().Be(Color("CRustFunction"));
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
        TokenForText(tokens, line, "main").Color.Should().Be(Color("CJavaScriptFunction"));
        TokenForText(tokens, line, "console").Color.Should().Be(Color("CJavaScriptBuiltin"));
        TokenForText(tokens, line, "log").Color.Should().Be(Color("CJavaScriptFunction"));
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
        TokenForText(tokens, line, "main").Color.Should().Be(Color("CPythonFunction"));
        TokenForText(tokens, line, "print").Color.Should().Be(Color("CPythonFunction"));
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
    public void TryGetCopyTextForClipboard_WithSelection_ReturnsSelectedSourceOnly()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xt");

        try
        {
            File.WriteAllLines(path, new[] { "alpha", "beta" });
            var editor = new TermXTEditor(path, TermXTEditorSyntax.TermXt);
            SetPrivateField(editor, "_hasSelectionAnchor", true);
            SetPrivateField(editor, "_selectionAnchorLine", 0);
            SetPrivateField(editor, "_selectionAnchorCol", 0);
            SetPrivateField(editor, "_cursorLine", 1);
            SetPrivateField(editor, "_cursorCol", "beta".Length);

            (bool success, string text, string copiedItem) = CopyTextForClipboard(editor);

            success.Should().BeTrue();
            copiedItem.Should().Be("selection");
            text.Should().Be("alpha" + Environment.NewLine + "beta");
            text.Should().NotStartWith("1");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void TryGetCopyTextForClipboard_WithoutSelection_ReturnsCurrentSourceLine()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xt");

        try
        {
            File.WriteAllLines(path, new[] { "one", "two", "three" });
            var editor = new TermXTEditor(path, TermXTEditorSyntax.TermXt);
            SetPrivateField(editor, "_cursorLine", 1);
            SetPrivateField(editor, "_cursorCol", 1);

            (bool success, string text, string copiedItem) = CopyTextForClipboard(editor);

            success.Should().BeTrue();
            copiedItem.Should().Be("line");
            text.Should().Be("two" + Environment.NewLine);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void DisableNativeConsoleSelectionMode_ClearsQuickEditAndKeepsExtendedFlags()
    {
        const int enableProcessedInput = 0x0001;
        const int enableWindowInput = 0x0008;
        const int enableMouseInput = 0x0010;
        const int enableQuickEditMode = 0x0040;
        const int enableExtendedFlags = 0x0080;

        int mode = InvokePrivateStatic<int>(
            "DisableNativeConsoleSelectionMode",
            enableProcessedInput | enableQuickEditMode);

        (mode & enableProcessedInput).Should().Be(0);
        (mode & enableQuickEditMode).Should().Be(0);
        (mode & enableWindowInput).Should().Be(enableWindowInput);
        (mode & enableMouseInput).Should().Be(enableMouseInput);
        (mode & enableExtendedFlags).Should().Be(enableExtendedFlags);
    }

    [Fact]
    public void MoveDown_ThroughShortLine_RestoresPreferredColumnOnLongerLine()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xt");

        try
        {
            File.WriteAllLines(path, new[] { "abcdef", "xy", "abcdef" });
            var editor = new TermXTEditor(path, TermXTEditorSyntax.TermXt);
            SetPrivateField(editor, "_cursorLine", 0);
            SetPrivateField(editor, "_cursorCol", 4);

            InvokePrivate(editor, "MoveDown");

            GetPrivateField<int>(editor, "_cursorLine").Should().Be(1);
            GetPrivateField<int>(editor, "_cursorCol").Should().Be(1);

            InvokePrivate(editor, "MoveDown");

            GetPrivateField<int>(editor, "_cursorLine").Should().Be(2);
            GetPrivateField<int>(editor, "_cursorCol").Should().Be(4);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void MoveUp_InInsertModeThroughShortLine_RestoresPreferredColumnOnLongerLine()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xt");

        try
        {
            File.WriteAllLines(path, new[] { "abcdef", "xy", "abcdef" });
            var editor = new TermXTEditor(path, TermXTEditorSyntax.TermXt);
            SetPrivateEnumField(editor, "_mode", "Insert");
            SetPrivateField(editor, "_cursorLine", 2);
            SetPrivateField(editor, "_cursorCol", 5);

            InvokePrivate(editor, "MoveUp");

            GetPrivateField<int>(editor, "_cursorLine").Should().Be(1);
            GetPrivateField<int>(editor, "_cursorCol").Should().Be(2);

            InvokePrivate(editor, "MoveUp");

            GetPrivateField<int>(editor, "_cursorLine").Should().Be(0);
            GetPrivateField<int>(editor, "_cursorCol").Should().Be(5);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void MoveLeft_AfterVerticalMove_ResetsPreferredColumn()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xt");

        try
        {
            File.WriteAllLines(path, new[] { "abcdef", "xy", "abcdef" });
            var editor = new TermXTEditor(path, TermXTEditorSyntax.TermXt);
            SetPrivateEnumField(editor, "_mode", "Insert");
            SetPrivateField(editor, "_cursorLine", 0);
            SetPrivateField(editor, "_cursorCol", 4);

            InvokePrivate(editor, "MoveDown");
            InvokePrivate(editor, "MoveLeft");
            InvokePrivate(editor, "MoveDown");

            GetPrivateField<int>(editor, "_cursorLine").Should().Be(2);
            GetPrivateField<int>(editor, "_cursorCol").Should().Be(1);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void TryGetMouseTextPosition_InLineNumberGutter_MapsToSourceColumnZero()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xt");

        try
        {
            File.WriteAllLines(path, new[] { "alpha", "beta" });
            var editor = new TermXTEditor(path, TermXTEditorSyntax.TermXt);

            (bool success, int line, int col) = MouseTextPosition(editor, x: 0, y: 2);

            success.Should().BeTrue();
            line.Should().Be(0);
            col.Should().Be(0);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void TryGetMouseTextPosition_InTextArea_MapsToSourceColumn()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xt");

        try
        {
            File.WriteAllLines(path, new[] { "alpha", "beta" });
            var editor = new TermXTEditor(path, TermXTEditorSyntax.TermXt);

            (bool success, int line, int col) = MouseTextPosition(editor, x: 7, y: 3);

            success.Should().BeTrue();
            line.Should().Be(1);
            col.Should().Be(2);
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
    public void HandleKey_CtrlD_DuplicatesCurrentLine()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xt");

        try
        {
            File.WriteAllLines(path, new[] { "one", "two", "three" });
            var editor = new TermXTEditor(path, TermXTEditorSyntax.TermXt);
            SetPrivateField(editor, "_cursorLine", 1);
            SetPrivateField(editor, "_cursorCol", 2);

            InvokePrivate(
                editor,
                "HandleKey",
                new ConsoleKeyInfo('\u0004', ConsoleKey.D, shift: false, alt: false, control: true));

            Lines(editor).Should().Equal("one", "two", "two", "three");
            GetPrivateField<int>(editor, "_cursorLine").Should().Be(2);
            GetPrivateField<int>(editor, "_cursorCol").Should().Be(2);
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
            GetPrivateField<bool>(editor, "_bottomStatusWarning").Should().BeTrue();
            GetPrivateField<bool>(editor, "_bottomStatusError").Should().BeFalse();
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

    [Fact]
    public void Clip_RemovesTerminalControlCharacters()
    {
        string clipped = InvokePrivateStatic<string>(
            "Clip",
            "safe\u001b[31m\u0007red\ttext",
            100);

        clipped.Should().Be("safe[31mred text");
        clipped.Any(char.IsControl).Should().BeFalse();
    }

    [Fact]
    public void TryReadEditorLines_RejectsOversizedFileBeforeReading()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xt");
        long maxBytes = Constant<long>("MaxEditorFileBytes");

        try
        {
            using (FileStream stream = File.Create(path))
                stream.SetLength(maxBytes + 1);

            object[] arguments = { path, Array.Empty<string>(), string.Empty };

            bool success = InvokePrivateStatic<bool>("TryReadEditorLines", arguments);

            success.Should().BeFalse();
            ((string)arguments[2]).Should().Contain("too large");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void TryValidatePasteText_RejectsOversizedPaste()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xt");

        try
        {
            File.WriteAllText(path, "line");
            var editor = new TermXTEditor(path, TermXTEditorSyntax.TermXt);
            int maxPasteCharacters = Constant<int>("MaxPasteCharacters");
            string oversizedPaste = new string('x', maxPasteCharacters + 1);
            object[] arguments = { oversizedPaste, string.Empty };

            bool success = InvokePrivate<bool>(editor, "TryValidatePasteText", arguments);

            success.Should().BeFalse();
            ((string)arguments[1]).Should().Contain("too large");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ConsoleInputInteropStructs_MatchWindowsRecordSizes()
    {
        Marshal.SizeOf(NestedType("InputRecord")).Should().Be(20);
        Marshal.SizeOf(NestedType("KeyEventRecord")).Should().Be(16);
        Marshal.SizeOf(NestedType("MouseEventRecord")).Should().Be(16);
        Marshal.SizeOf(NestedType("WindowBufferSizeRecord")).Should().Be(4);
        Marshal.SizeOf(NestedType("Coord")).Should().Be(4);
    }

    private static List<TokenInfo> TokenizeTermXt(string line)
    {
        MethodInfo method = typeof(TermXTEditor).GetMethod(
            "TokenizeTermXt",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (IEnumerable)method.Invoke(null, new object[] { line })!;
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

    private static List<TokenInfo> TokenizeCSharp(string line, bool startsInBlockComment)
    {
        MethodInfo method = typeof(TermXTEditor).GetMethod(
            "TokenizeCSharp",
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

    private static List<TokenInfo> TokenizeCStyle(string line, bool cpp)
    {
        MethodInfo method = typeof(TermXTEditor).GetMethod(
            "TokenizeCStyle",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (IEnumerable)method.Invoke(null, new object[] { line, cpp })!;
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

    private static (bool success, string text, string copiedItem) CopyTextForClipboard(TermXTEditor editor)
    {
        MethodInfo method = typeof(TermXTEditor).GetMethod(
            "TryGetCopyTextForClipboard",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        object[] arguments = { string.Empty, string.Empty };
        bool success = (bool)method.Invoke(editor, arguments)!;
        return (success, (string)arguments[0], (string)arguments[1]);
    }

    private static (bool success, int line, int col) MouseTextPosition(TermXTEditor editor, int x, int y)
    {
        MethodInfo method = typeof(TermXTEditor).GetMethod(
            "TryGetMouseTextPosition",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        object?[] arguments = { x, y, null };
        bool success = (bool)method.Invoke(editor, arguments)!;
        object position = arguments[2]!;
        Type positionType = position.GetType();
        return (
            success,
            (int)positionType.GetProperty("Line")!.GetValue(position)!,
            (int)positionType.GetProperty("Col")!.GetValue(position)!);
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
                (string)diagnosticType.GetProperty("Description")!.GetValue(diagnostic)!,
                diagnosticType.GetProperty("Severity")!.GetValue(diagnostic)!.ToString()!));
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

    private static T InvokePrivateStatic<T>(string methodName, params object[] arguments)
    {
        MethodInfo method = typeof(TermXTEditor).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Static)!;

        return (T)method.Invoke(null, arguments)!;
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
        return Constant<int>(name);
    }

    private static T Constant<T>(string name)
    {
        FieldInfo field = typeof(TermXTEditor).GetField(
            name,
            BindingFlags.NonPublic | BindingFlags.Static)!;

        return (T)field.GetRawConstantValue()!;
    }

    private static Type NestedType(string name)
    {
        return typeof(TermXTEditor).GetNestedType(
            name,
            BindingFlags.NonPublic)!;
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
        public DiagnosticInfo(int lineNumber, string code, string description, string severity)
        {
            LineNumber = lineNumber;
            Code = code;
            Description = description;
            Severity = severity;
        }

        public int LineNumber { get; }
        public string Code { get; }
        public string Description { get; }
        public string Severity { get; }
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
