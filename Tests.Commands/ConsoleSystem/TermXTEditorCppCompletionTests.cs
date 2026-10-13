using System.Collections;
using System.Reflection;
using System.Text.Json;
using Core.DirFiles;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

public partial class TermXTEditorSyntaxTests
{
    [Theory]
    [InlineData(TermXTEditorSyntax.C, "struct Customer { int value; };\nvoid f() { Con$$ }")]
    [InlineData(TermXTEditorSyntax.Cpp, "class Customer {};\nvoid f() { Con$$ }")]
    [InlineData(TermXTEditorSyntax.Cpp, "namespace app {}\nvoid f() { Con$$ }")]
    [InlineData(TermXTEditorSyntax.Cpp, "using namespace std;\nvoid f() { Con$$ }")]
    public void CppCompletion_ExplicitSyntaxNeverUsesCSharpHeuristics(TermXTEditorSyntax syntax, string marked)
    {
        var editor = CppEditorAtMarker(marked, syntax);
        InvokePrivate<bool>(editor, "IsCSharpCompletionContext").Should().BeFalse();
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', ConsoleKey.Spacebar, false, false, true));
        ActiveCSharpCompletions(editor).Should().NotContain(item => item.Label == "Console" || item.Label == "Convert");
        GetPrivateField<string>(editor, "_status").Should().NotContain("C#");
    }

    [Theory]
    [InlineData(TermXTEditorSyntax.C, "struct Customer {};\npri$$", "printf")]
    [InlineData(TermXTEditorSyntax.C, "#inc$$", "include")]
    [InlineData(TermXTEditorSyntax.C, "int customer_count;\ncust$$", "customer_count")]
    [InlineData(TermXTEditorSyntax.C, "ret$$", "return")]
    [InlineData(TermXTEditorSyntax.Cpp, "using namespace std;\nstd::ve$$", "vector")]
    [InlineData(TermXTEditorSyntax.Cpp, "namespace app {}\nstd::$$", "cout")]
    [InlineData(TermXTEditorSyntax.Cpp, "class Widget {};\nWid$$", "Widget")]
    [InlineData(TermXTEditorSyntax.Cpp, "cons$$", "constexpr")]
    public void CppCompletion_BasicSuggestionsWorkWithoutClangd(TermXTEditorSyntax syntax, string marked, string expected)
    {
        var editor = CppEditorAtMarker(marked, syntax);
        InvokePrivate(editor, "RefreshCompletionAfterEdit");
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == expected);
    }

    [Theory]
    [InlineData("// pri$$")]
    [InlineData("// comment \\\npri$$")]
    [InlineData("/* comment\npri$$\n*/")]
    [InlineData("const char *text = \"pri$$\";")]
    [InlineData("const char *text = \"escaped \\\" pri$$\";")]
    [InlineData("const char *text = R\"raw(first line\npri$$)raw\";")]
    [InlineData("const char *text = u8R\"(pri$$)\";")]
    [InlineData("char c = 'p$$';")]
    [InlineData("int value = 123f$$;")]
    [InlineData("unknown.$$ ")]
    [InlineData("unknown->$$ ")]
    public void CppCompletion_DoesNotOfferGlobalsInCommentsStringsOrMemberAccess(string marked)
    {
        var editor = CppEditorAtMarker(marked);
        InvokePrivate(editor, "RefreshCompletionAfterEdit");
        GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
    }

    [Theory]
    [InlineData("/* comment */ pri$$")]
    [InlineData("const char *text = R\"(hi)\"; pri$$")]
    [InlineData("int number = 1'000; pri$$")]
    public void CppCompletion_ResumesAfterCommentsStringsAndNumericSeparators(string marked)
    {
        var editor = CppEditorAtMarker(marked);
        InvokePrivate(editor, "RefreshCompletionAfterEdit");
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == "printf");
    }

    [Fact]
    public void CppCompletion_TypingAcceptingAndUndoWorkThroughNormalKeyHandling()
    {
        var editor = CppEditorAtMarker("using namespace std;\nstd:$$");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo(':', ConsoleKey.NoName, false, false, false));
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == "vector");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('v', ConsoleKey.V, false, false, false));
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false));
        var items = ActiveCSharpCompletions(editor);
        SetPrivateField(editor, "_completionSelectedIndex", items.FindIndex(item => item.Label == "vector"));
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false));
        Lines(editor)[1].Should().Be("std::vector");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', ConsoleKey.Z, false, false, true));
        Lines(editor)[1].Should().Be("std:");
    }

    [Fact]
    public void CppCompletion_ManualCompletionReplacesTheWholeWord()
    {
        var editor = CppEditorAtMarker("void f() { pri$$nt; }", TermXTEditorSyntax.C);
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', ConsoleKey.Spacebar, false, false, true));
        SetPrivateField(editor, "_completionSelectedIndex", ActiveCSharpCompletions(editor).FindIndex(item => item.Label == "printf"));
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
        Lines(editor).Should().Equal("void f() { printf; }");
    }

    [Theory]
    [InlineData("edit")]
    [InlineData("escape")]
    [InlineData("move")]
    [InlineData("syntax")]
    [InlineData("reload")]
    public void CppCompletion_LateResultsCannotReopenPopupAfterContextChanges(string change)
    {
        var editor = CppEditorAtMarker("struct Item { int value; };\nvoid f() { Item item; item.va$$; }");
        var resultType = NestedType("CppCompletionResult");
        object result = Activator.CreateInstance(resultType, nonPublic: true)!;
        foreach (var entry in new Dictionary<string, object>
        {
            ["Path"] = GetPrivateField<string>(editor, "_path"), ["Line"] = GetPrivateField<int>(editor, "_cursorLine"),
            ["Column"] = GetPrivateField<int>(editor, "_cursorCol"), ["StartColumn"] = GetPrivateField<int>(editor, "_cursorCol") - 2,
            ["Version"] = GetPrivateField<int>(editor, "_cppDocumentVersion"), ["Syntax"] = TermXTEditorSyntax.Cpp,
            ["Items"] = ParseCppItems("[{\"label\":\"value\"}]", Lines(editor)[1], 1, GetPrivateField<int>(editor, "_cursorCol"))
        }) resultType.GetField(entry.Key)!.SetValue(result, entry.Value);
        object completion = Activator.CreateInstance(typeof(TaskCompletionSource<>).MakeGenericType(resultType))!;
        Task pending = (Task)completion.GetType().GetProperty("Task")!.GetValue(completion)!;
        SetPrivateField(editor, "_cppCompletionTask", pending);
        SetPrivateField(editor, "_cppCompletionCancellation", new CancellationTokenSource());
        switch (change)
        {
            case "edit": InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('l', ConsoleKey.L, false, false, false)); break;
            case "escape": InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false)); break;
            case "move": InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false)); break;
            case "syntax": InvokePrivate(editor, "ExecuteEditorCommand", "syntax cs"); break;
            case "reload": InvokePrivate(editor, "ExecuteEditorCommand", "e!"); break;
        }
        completion.GetType().GetMethod("SetResult")!.Invoke(completion, new[] { result });
        InvokePrivate<bool>(editor, "CheckCppCompletionOnIdle").Should().BeFalse();
        GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
    }

    [ClangdIntegrationFact]
    public async Task CppCompletion_ClangdResolvesCFieldsAndStandardFunctionsWithoutSaving()
    {
        var editor = CppEditorAtMarker("#include <stdio.h>\nstruct Item { int value; };\nvoid f(struct Item *p) { p->va$$; }", TermXTEditorSyntax.C, semantic: true);
        try
        {
            await CompleteCppAsync(editor);
            ActiveCppCompletions(editor).Should().Contain(item => item.FilterText == "value" && item.Kind == "field");
            SetCppSource(editor, "#include <stdio.h>\nvoid f(void) { pri$$; }");
            await CompleteCppAsync(editor);
            ActiveCppCompletions(editor).Should().Contain(item => item.FilterText == "printf" && item.Kind == "function");
            File.Exists(GetPrivateField<string>(editor, "_path")).Should().BeFalse();
        }
        finally { InvokePrivate(editor, "StopClangd"); }
    }

    [ClangdIntegrationFact]
    public async Task CppCompletion_ClangdResolvesNamespacesTemplatesChainsAndUnsavedChanges()
    {
        var editor = CppEditorAtMarker("#include <vector>\n#include <string>\nnamespace app { struct Item { std::string name; }; }\nvoid f() { std::vector<app::Item> items; items.front().na$$; }", semantic: true);
        try
        {
            await CompleteCppAsync(editor);
            ActiveCppCompletions(editor).Should().Contain(item => item.FilterText == "name" && item.Kind == "field");
            SetCppSource(editor, "struct Item { int renamed; };\nvoid f() { Item item; item.re$$; }");
            await CompleteCppAsync(editor);
            var items = ActiveCppCompletions(editor);
            items.Should().Contain(item => item.FilterText == "renamed");
            items.Should().NotContain(item => item.FilterText == "name" || item.FilterText == "Console");
            SetPrivateField(editor, "_completionSelectedIndex", items.FindIndex(item => item.FilterText == "renamed"));
            InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false));
            Lines(editor)[1].Should().Be("void f() { Item item; item.renamed; }");
        }
        finally { InvokePrivate(editor, "StopClangd"); }
    }

    [ClangdIntegrationFact]
    public async Task CppCompletion_ClangdUsesProjectFlagsIncludePathsAndHeaderCompletion()
    {
        string directory = Path.Combine(Path.GetTempPath(), "xte-cpp-" + Guid.NewGuid().ToString("N"));
        string includes = Path.Combine(directory, "include");
        string path = Path.Combine(directory, "main.cpp");
        string header = Path.Combine(includes, "feature.h");
        string database = Path.Combine(directory, "compile_commands.json");
        TermXTEditor? editor = null;
        try
        {
            Directory.CreateDirectory(includes);
            File.WriteAllText(path, "// saved file stays unchanged");
            File.WriteAllText(header, "#ifdef ENABLE_FEATURE\nstruct Feature { int project_value; };\n#endif\n");
            File.WriteAllText(database, JsonSerializer.Serialize(new[] { new
            {
                directory, file = path,
                arguments = new[] { "clang++", "-std=c++20", "-DENABLE_FEATURE", "-I" + includes, "-c", path }
            } }));
            editor = new TermXTEditor(path);
            SetCppSource(editor, "#include <feature.h>\nvoid f() { Feature feature; feature.project_$$; }");
            await CompleteCppAsync(editor);
            ActiveCppCompletions(editor).Should().Contain(item => item.FilterText == "project_value" && item.Kind == "field");

            SetCppSource(editor, "#include <feature.$$>\n");
            await CompleteCppAsync(editor);
            ActiveCppCompletions(editor).Should().Contain(item => item.FilterText.Contains("feature.h"));
            InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false));
            Lines(editor)[0].Should().Be("#include <feature.h>");
            File.ReadAllText(path).Should().Be("// saved file stays unchanged");
        }
        finally
        {
            if (editor != null) InvokePrivate(editor, "StopClangd");
            File.Delete(path);
            File.Delete(header);
            File.Delete(database);
            if (Directory.Exists(includes)) Directory.Delete(includes);
            if (Directory.Exists(directory)) Directory.Delete(directory);
        }
    }

    [Fact]
    public async Task CppCompletion_UnavailableServerKeepsBasicSuggestionsAndReportsSetupError()
    {
        var editor = CppEditorAtMarker("pri$$", TermXTEditorSyntax.C, semantic: true);
        using var client = (IDisposable)Activator.CreateInstance(NestedType("ClangdClient"),
            new object[] { Path.GetTempPath(), false })!;
        SetPrivateField(client, "_initialization", new Lazy<Task>(() => Task.FromException(new FileNotFoundException("test clangd unavailable"))));
        SetPrivateField(editor, "_clangd", client);
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', ConsoleKey.Spacebar, false, false, true));
        await GetPrivateField<Task>(editor, "_cppCompletionTask").WaitAsync(TimeSpan.FromSeconds(5));
        InvokePrivate<bool>(editor, "CheckCppCompletionOnIdle").Should().BeTrue();
        GetPrivateField<string>(editor, "_bottomStatus").Should().Contain("test clangd unavailable");
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == "printf");
        GetPrivateField<object?>(editor, "_clangd").Should().BeNull();
    }

    private static TermXTEditor CppEditorAtMarker(string source, TermXTEditorSyntax syntax = TermXTEditorSyntax.Cpp, bool semantic = false)
    {
        var editor = new TermXTEditor(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + (syntax == TermXTEditorSyntax.C ? ".c" : ".cpp")), syntax);
        SetCppSource(editor, source);
        if (!semantic) SetPrivateField(editor, "_clangdRetryUtc", DateTime.MaxValue);
        return editor;
    }

    private static void SetCppSource(TermXTEditor editor, string marked)
    {
        marked = marked.Replace("\r\n", "\n");
        int position = marked.IndexOf("$$", StringComparison.Ordinal);
        string source = marked.Remove(position, 2);
        Lines(editor).Clear();
        Lines(editor).AddRange(source.Split('\n'));
        (int line, int column) = LineColumnAtPosition(source, position);
        SetPrivateField(editor, "_cursorLine", line);
        SetPrivateField(editor, "_cursorCol", column);
        SetPrivateEnumField(editor, "_mode", "Insert");
        InvalidateScrollingDocument(editor);
    }

    private static async Task CompleteCppAsync(TermXTEditor editor)
    {
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', ConsoleKey.Spacebar, false, false, true));
        Task pending = GetPrivateField<Task>(editor, "_cppCompletionTask");
        pending.Should().NotBeNull();
        await pending.WaitAsync(TimeSpan.FromSeconds(40));
        InvokePrivate(editor, "CheckCppCompletionOnIdle");
        GetPrivateField<string>(editor, "_bottomStatus").Should().Contain("C/C++ IntelliSense:");
    }

    private static IList ParseCppItems(string json, string line, int lineNumber, int column)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return InvokePrivateStatic<IList>("ParseLanguageServerCompletions", document.RootElement, line, lineNumber, column, "C/C++ / clangd");
    }

    private static List<(string FilterText, string Kind)> ActiveCppCompletions(TermXTEditor editor)
    {
        return GetPrivateField<IList>(editor, "_completionItems").Cast<object>().Select(item =>
            ((string)item.GetType().GetProperty("FilterText")!.GetValue(item)!,
             (string)item.GetType().GetProperty("Kind")!.GetValue(item)!)).ToList();
    }

    private sealed class ClangdIntegrationFactAttribute : FactAttribute
    {
        public ClangdIntegrationFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("XTE_CLANGD_TESTS") != "1")
                Skip = "Set XTE_CLANGD_TESTS=1 with clangd installed to run C/C++ semantic completion tests.";
        }
    }
}
