using System.Collections;
using System.Reflection;
using System.Text.Json;
using Core.DirFiles;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

public partial class TermXTEditorSyntaxTests
{
    [Fact]
    public void RustCargoCompletion_AllServerMethodsCanBeSelectedAfterDot()
    {
        var editor = RustEditorAtMarker("vault_name.$$(len-1);");
        var names = Enumerable.Range(0, 100).Select(index => "method" + index).Append("truncate");
        string json = JsonSerializer.Serialize(names.Select((name, index) => new
        {
            label = name, kind = 2, sortText = index.ToString("D3"),
            textEdit = new { newText = name, range = new { start = new { line = 0, character = 11 }, end = new { line = 0, character = 11 } } }
        }));
        InstallRustProjectResult(editor, json, 11);
        InvokePrivate<bool>(editor, "CheckRustCompletionOnIdle").Should().BeTrue();
        ActiveCSharpCompletions(editor).Should().HaveCount(101).And.Contain(item => item.Label == "truncate");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, false));
        var frame = GetPrivateField<System.Text.StringBuilder>(editor, "_frame");
        frame.Clear();
        InvokePrivate(editor, "RenderCompletionPopup", 2, 5, 20, 100, 105);
        frame.ToString().Should().Contain("truncate");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false));
        Lines(editor)[0].Should().Be("vault_name.truncate(len-1);");
    }

    [RustAnalyzerIntegrationFact]
    public async Task RustCompletion_VaultExampleSuggestsTruncateAfterDotAndPrefix()
    {
        string root = Path.Combine(Path.GetTempPath(), "xte-rust-vault-" + Guid.NewGuid());
        Directory.CreateDirectory(root);
        string path = Path.Combine(root, "main.rs");
        File.WriteAllText(path, "fn main() {}\n");
        var editor = new TermXTEditor(path);
        try
        {
            SetRustProjectSource(editor, RustVaultCompletionExample);
            InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('.', ConsoleKey.OemPeriod, false, false, false));
            await AssertRustSemanticSuggestion(editor, "truncate");
            foreach (char c in "trunc")
                InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo(c, ConsoleKey.NoName, false, false, false));
            await AssertRustSemanticSuggestion(editor, "truncate");
            InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false));
            Lines(editor).Should().Contain("    vault_name.truncate");
            File.ReadAllText(path).Should().Be("fn main() {}\n");
        }
        finally
        {
            InvokePrivate(editor, "StopRustAnalyzer");
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RustCargoCompletion_UsesFilterTextAndServerReplacementRange()
    {
        var editor = RustEditorAtMarker("value.sp$$elling()");
        const string json = """
            {"items":[{"label":"speak(&self)","filterText":"speak","kind":2,"detail":"fn speak(&self) -> &str",
              "textEdit":{"newText":"speak","range":{"start":{"line":0,"character":6},"end":{"line":0,"character":14}}}}]}
            """;
        InstallRustProjectResult(editor, json, 6);
        InvokePrivate<bool>(editor, "CheckRustCompletionOnIdle").Should().BeTrue();
        ActiveCSharpCompletions(editor).Should().ContainSingle().Which.Label.Should().Be("speak(&self)");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false));
        Lines(editor)[0].Should().Be("value.speak()");
        InvokePrivate(editor, "Undo");
        Lines(editor)[0].Should().Be("value.spelling()");
    }

    [Theory]
    [InlineData("edit")]
    [InlineData("cursor")]
    [InlineData("syntax")]
    [InlineData("escape")]
    [InlineData("path")]
    public void RustCargoCompletion_DiscardsStaleResults(string change)
    {
        var editor = RustEditorAtMarker("value.sp$$");
        InstallRustProjectResult(editor, "[{\"label\":\"speak\",\"kind\":2}]", 6);
        switch (change)
        {
            case "edit": InvalidateRustTestDocument(editor); break;
            case "cursor": SetPrivateField(editor, "_cursorCol", 0); break;
            case "syntax": InvokePrivate(editor, "ExecuteEditorCommand", "syntax py"); break;
            case "escape": InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false)); break;
            case "path": SetPrivateField(editor, "_path", "different.rs"); break;
        }
        InvokePrivate<bool>(editor, "CheckRustCompletionOnIdle").Should().BeFalse();
        GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
    }

    [Fact]
    public void RustCargoCompletion_RejectsUnsupportedEditsAndSnippets()
    {
        const string json = """
            [{"label":"snippet","insertTextFormat":2,"insertText":"call($0)"},
             {"label":"import","additionalTextEdits":[{}]},
             {"label":"multiline","insertText":"first\nsecond"},
             {"label":"otherLine","textEdit":{"newText":"bad","range":{"start":{"line":1,"character":0},"end":{"line":1,"character":0}}}},
             {"label":"outOfRange","textEdit":{"newText":"bad","range":{"start":{"line":0,"character":0},"end":{"line":0,"character":99}}}},
             {"label":"valid","textEdit":{"newText":"valid","insert":{"start":{"line":0,"character":0},"end":{"line":0,"character":2}},"replace":{"start":{"line":0,"character":0},"end":{"line":0,"character":4}}}}]
            """;
        var items = ParseRustProjectItems(json, "vali", 0, 2).Cast<object>().ToList();
        items.Should().ContainSingle();
        ToCompletionInfo(items[0]).Label.Should().Be("valid");
    }

    [Fact]
    public void RustCargoCompletion_EditCancelsPendingRequestWithoutWaiting()
    {
        var editor = RustEditorAtMarker("value.sp$$");
        Type resultType = typeof(TermXTEditor).GetNestedType("RustProjectCompletionResult", BindingFlags.NonPublic)!;
        object completion = Activator.CreateInstance(typeof(TaskCompletionSource<>).MakeGenericType(resultType))!;
        var pending = (Task)completion.GetType().GetProperty("Task")!.GetValue(completion)!;
        var cancellation = new CancellationTokenSource();
        SetPrivateField(editor, "_rustProjectCompletionTask", pending);
        SetPrivateField(editor, "_rustProjectCompletionCancellation", cancellation);
        try
        {
            InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false));
            cancellation.IsCancellationRequested.Should().BeTrue();
            pending.IsCompleted.Should().BeFalse();
            GetPrivateField<Task?>(editor, "_rustProjectCompletionTask").Should().BeNull();
        }
        finally { completion.GetType().GetMethod("SetCanceled", Type.EmptyTypes)!.Invoke(completion, null); }
    }

    [Fact]
    public void RustCargoCompletion_ServerFailurePreservesBuiltInSuggestions()
    {
        var editor = RustEditorAtMarker("pri$$");
        InvokePrivate(editor, "RefreshCompletionAfterEdit");
        InstallRustProjectResult(editor, "[]", 0);
        var task = GetPrivateField<Task>(editor, "_rustProjectCompletionTask");
        object result = task.GetType().GetProperty("Result")!.GetValue(task)!;
        result.GetType().GetField("Error")!.SetValue(result, "Cargo was not found");
        result.GetType().GetField("Manual")!.SetValue(result, true);
        InvokePrivate<bool>(editor, "CheckRustCompletionOnIdle").Should().BeTrue();
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == "println!");
        GetPrivateField<string>(editor, "_bottomStatus").Should().Contain("Cargo was not found");
    }

    [Fact]
    public void RustCargoCompletion_FindsManifestFromNestedSourceDirectory()
    {
        string root = Path.Combine(Path.GetTempPath(), "xte-cargo-root-" + Guid.NewGuid());
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "Cargo.toml"), "[workspace]\nmembers = []\n");
            InvokePrivateStatic<string>("FindRustCargoRoot", Path.Combine(root, "src", "nested", "main.rs")).Should().Be(root);
            InvokePrivateStatic<string?>("FindRustCargoRoot", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "main.rs")).Should().BeNull();
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [RustAnalyzerIntegrationFact]
    public async Task RustCargoCompletion_ResolvesRenamedWorkspaceDependencyAndUnsavedImports()
    {
        string root = Path.Combine(Path.GetTempPath(), "xte-cargo-completion-" + Guid.NewGuid());
        string app = Path.Combine(root, "app");
        string helper = Path.Combine(root, "helper");
        Directory.CreateDirectory(Path.Combine(app, "src"));
        Directory.CreateDirectory(Path.Combine(helper, "src"));
        File.WriteAllText(Path.Combine(root, "Cargo.toml"), "[workspace]\nmembers = [\"app\", \"helper\"]\nresolver = \"2\"\n");
        File.WriteAllText(Path.Combine(app, "Cargo.toml"), "[package]\nname = \"xte-app\"\nversion = \"0.1.0\"\nedition = \"2021\"\n[dependencies]\nhelper_alias = { package = \"xte-helper\", path = \"../helper\" }\n");
        File.WriteAllText(Path.Combine(helper, "Cargo.toml"), "[package]\nname = \"xte-helper\"\nversion = \"0.1.0\"\nedition = \"2021\"\n");
        File.WriteAllText(Path.Combine(helper, "src", "lib.rs"), """
            pub struct Widget { pub title: String }
            impl Widget {
                pub fn new() -> Self { Self { title: String::new() } }
                pub fn speak(&self) -> &str { &self.title }
            }
            pub trait Signal { fn emit(&self) -> bool; }
            impl Signal for Widget { fn emit(&self) -> bool { true } }
            pub mod prelude { pub use crate::{Widget, Signal}; }
            """);
        string path = Path.Combine(app, "src", "main.rs");
        File.WriteAllText(path, "fn main() {}\n");
        var editor = new TermXTEditor(path);
        try
        {
            // Exercise actual '.' keystrokes with an empty prefix, including call results
            // and chains. Earlier coverage only requested a typed prefix with Ctrl+Space.
            foreach ((string text, string expected) in new[]
            {
                ("use helper_alias::Widget;\nfn main() { let value = Widget::new(); value$$ }", "speak"),
                ("use helper_alias::Widget;\nfn main() { Widget::new()$$ }", "speak"),
                ("use helper_alias::Widget;\nfn main() { Widget::new().speak()$$ }", "trim"),
                ("use helper_alias::prelude::*;\nfn main() { Widget::new()$$ }", "emit")
            })
            {
                SetRustProjectSource(editor, text);
                InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('.', ConsoleKey.OemPeriod, false, false, false));
                await AssertRustSemanticSuggestion(editor, expected);
            }
            foreach ((string text, string expected) in new[]
            {
                ("use helper_alias::Widget;\nfn main() { let value = Widget::new(); let label = \"🤖\"; value.sp$$ }", "speak"),
                ("use helper_alias::prelude::*;\nfn main() { let value = Widget::new(); value.em$$ }", "emit"),
                ("use helper_alias::Widget as Thing;\nfn main() { Thing::ne$$ }", "new"),
                ("use helper_alias::prelude::Wi$$;\nfn main() {}", "Widget"),
                ("use helper_al$$;\nfn main() {}", "helper_alias")
            })
            {
                SetRustProjectSource(editor, text);
                InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', ConsoleKey.Spacebar, false, false, true));
                Task? pending = GetPrivateField<Task?>(editor, "_rustProjectCompletionTask");
                pending.Should().NotBeNull("integration requires an installed Rust analyzer and toolchain");
                await pending!.WaitAsync(TimeSpan.FromSeconds(120));
                InvokePrivate<bool>(editor, "CheckRustCompletionOnIdle").Should().BeTrue();
                string status = GetPrivateField<string>(editor, "_bottomStatus");
                status.Should().Contain("Cargo IntelliSense", status);
                var items = (IEnumerable)typeof(TermXTEditor).GetField("_completionItems", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(editor)!;
                items.Cast<object>().Should().Contain(item =>
                    (string)item.GetType().GetProperty("FilterText")!.GetValue(item)! == expected &&
                    (int)item.GetType().GetProperty("RustEditStart")!.GetValue(item)! >= 0);
            }
            File.ReadAllText(path).Should().Be("fn main() {}\n", "completion must use the unsaved buffer without saving it");
            InvokePrivate(editor, "ExecuteEditorCommand", "syntax py");
            GetPrivateField<object?>(editor, "_rustAnalyzer").Should().BeNull();
        }
        finally
        {
            InvokePrivate(editor, "StopRustAnalyzer");
            // Cargo/server handles can take a moment to close after process termination.
            for (int attempt = 0; ; attempt++)
            {
                try { Directory.Delete(root, recursive: true); break; }
                catch (IOException) when (attempt < 10) { await Task.Delay(100); }
                catch (UnauthorizedAccessException) when (attempt < 10) { await Task.Delay(100); }
            }
        }
    }

    [RustAnalyzerIntegrationFact]
    public async Task RustCompletion_StandaloneFileSupportsDotOnAllExpressionForms()
    {
        string root = Path.Combine(Path.GetTempPath(), "xte-rust-standalone-" + Guid.NewGuid());
        Directory.CreateDirectory(root);
        string path = Path.Combine(root, "main.rs");
        File.WriteAllText(path, "fn main() {}\n");
        var editor = new TermXTEditor(path);
        try
        {
            foreach ((string expression, string expected) in new[]
            {
                ("text", "push_str"), ("items", "push"), ("number", "abs"), ("user", "name"),
                ("make_user()", "name"), ("String::new()", "push_str"), ("\"hello\".to_string()", "push_str"),
                ("user.name", "trim"), ("items[0]", "push_str"), ("Some(text).unwrap()", "push_str"),
                ("text.trim()", "chars"), ("items.iter()", "map"), ("(user)", "name"),
                ("std::io::stdin()", "read_line")
            })
            {
                SetRustProjectSource(editor, """
                    struct User { name: String }
                    fn make_user() -> User { User { name: String::new() } }
                    fn main() {
                        let text = String::new();
                        let items: Vec<String> = Vec::new();
                        let number: i32 = 5;
                        let user = make_user();
                    """ + "\n    " + expression + "$$\n}");
                InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('.', ConsoleKey.OemPeriod, false, false, false));
                await AssertRustSemanticSuggestion(editor, expected);
            }
            File.ReadAllText(path).Should().Be("fn main() {}\n");
        }
        finally
        {
            InvokePrivate(editor, "StopRustAnalyzer");
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task AssertRustSemanticSuggestion(TermXTEditor editor, string expected)
    {
        Task? task = GetPrivateField<Task?>(editor, "_rustProjectCompletionTask");
        task.Should().NotBeNull("the dot keystroke should schedule semantic completion");
        await task!.WaitAsync(TimeSpan.FromSeconds(120));
        bool updated = InvokePrivate<bool>(editor, "CheckRustCompletionOnIdle");
        string status = GetPrivateField<string>(editor, "_bottomStatus");
        updated.Should().BeTrue(status);
        var items = (IEnumerable)typeof(TermXTEditor).GetField("_completionItems", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(editor)!;
        items.Cast<object>().Should().Contain(item =>
            (string)item.GetType().GetProperty("FilterText")!.GetValue(item)! == expected &&
            (int)item.GetType().GetProperty("RustEditStart")!.GetValue(item)! >= 0, status);
        var frame = GetPrivateField<System.Text.StringBuilder>(editor, "_frame");
        frame.Clear();
        InvokePrivate(editor, "RenderCompletionPopup", 2, 5, 20, 100, 105);
        frame.Length.Should().BeGreaterThan(0, "suggestions must be visible in the editor");
    }

    private static void SetRustProjectSource(TermXTEditor editor, string marked)
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
        InvalidateRustTestDocument(editor);
    }

    private static void InvalidateRustTestDocument(TermXTEditor editor)
    {
        typeof(TermXTEditor).GetMethod("InvalidateDocumentCaches", BindingFlags.NonPublic | BindingFlags.Instance,
            null, Type.EmptyTypes, null)!.Invoke(editor, null);
    }

    private static IList ParseRustProjectItems(string json, string line, int lineNumber, int column)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return InvokePrivateStatic<IList>("ParseRustAnalyzerCompletions", document.RootElement, line, lineNumber, column);
    }

    private static void InstallRustProjectResult(TermXTEditor editor, string json, int start)
    {
        Type resultType = typeof(TermXTEditor).GetNestedType("RustProjectCompletionResult", BindingFlags.NonPublic)!;
        object result = Activator.CreateInstance(resultType, nonPublic: true)!;
        int line = GetPrivateField<int>(editor, "_cursorLine"), column = GetPrivateField<int>(editor, "_cursorCol");
        foreach ((string name, object value) in new (string, object)[]
        {
            ("Path", GetPrivateField<string>(editor, "_path")), ("Line", line), ("Column", column), ("StartColumn", start),
            ("Version", GetPrivateField<int>(editor, "_rustDocumentVersion")), ("Items", ParseRustProjectItems(json, Lines(editor)[line], line, column))
        }) resultType.GetField(name)!.SetValue(result, value);
        object task = typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType).Invoke(null, new[] { result })!;
        SetPrivateField(editor, "_rustProjectCompletionTask", task);
        SetPrivateField(editor, "_rustProjectCompletionCancellation", new CancellationTokenSource());
    }

    private sealed class RustAnalyzerIntegrationFactAttribute : FactAttribute
    {
        public RustAnalyzerIntegrationFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("XTE_RUST_ANALYZER_TESTS") != "1")
                Skip = "Set XTE_RUST_ANALYZER_TESTS=1 with rust-analyzer and Rust installed to run semantic completion integration tests.";
        }
    }
}
