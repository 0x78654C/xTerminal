using System.Collections;
using System.Reflection;
using System.Text.Json;
using Core.DirFiles;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

public partial class TermXTEditorSyntaxTests
{
    private const string RustDiagnosticFixture = """
        [
          {"range":{"start":{"line":1,"character":4},"end":{"line":1,"character":11}},"severity":1,"code":"E0425","message":"cannot find value `missing` in this scope"},
          {"range":{"start":{"line":0,"character":3},"end":{"line":0,"character":7}},"severity":2,"code":"unused_variables","message":"unused variable"},
          {"range":{"start":{"line":0,"character":0},"end":{"line":0,"character":1}},"severity":4,"message":"hint"}
        ]
        """;

    [Fact]
    public void RustDiagnostics_PopulateMarkersCountsDetailsAndNavigation_ThenClear()
    {
        var editor = RustDiagnosticEditor("fn main() {\n    missing();\n}$$");
        using var client = InstallRustDiagnosticClient(editor);
        PublishRustDiagnostics(client, editor, RustDiagnosticFixture);
        InvokePrivate<bool>(editor, "CheckDiagnosticsOnIdle").Should().BeTrue();
        Diagnostics(editor).Select(d => d.Severity).Should().Equal("Warning", "Error");
        InvokePrivate<string>(editor, "BuildHeaderDiagnosticBadge").Should().Be(" [E:1] [W:1]");
        GetPrivateField<HashSet<int>>(editor, "_diagnosticLineIndexes").Should().BeEquivalentTo(new[] { 0, 1 });
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', ConsoleKey.F2, false, false, false));
        GetPrivateField<string>(editor, "_messageDetailsText").Should().Contain("E0425").And.Contain("unused_variables");
        GetPrivateField<bool>(editor, "_messageDetailsShowsDiagnostics").Should().BeTrue();
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false));
        InvokePrivate(editor, "ExecuteEditorCommand", "next-error");
        GetPrivateField<int>(editor, "_cursorLine").Should().Be(1);
        GetPrivateField<int>(editor, "_cursorCol").Should().Be(4);
        InvokePrivate(editor, "ExecuteEditorCommand", "next-warning");
        GetPrivateField<int>(editor, "_cursorLine").Should().Be(0);

        PublishRustDiagnostics(client, editor, "[]");
        InvokePrivate<bool>(editor, "TryApplyRustDiagnostics").Should().BeTrue();
        Diagnostics(editor).Should().BeEmpty();
        GetPrivateField<HashSet<int>>(editor, "_diagnosticLineIndexes").Should().BeEmpty();
        InvokePrivate<string>(editor, "BuildHeaderDiagnosticBadge").Should().BeEmpty();
    }

    [Theory]
    [InlineData("version")]
    [InlineData("file")]
    [InlineData("edit")]
    [InlineData("reload")]
    [InlineData("syntax")]
    public void RustDiagnostics_IgnoreStaleOrUnrelatedNotifications(string change)
    {
        var editor = RustDiagnosticEditor("fn main() {\n    missing();\n}$$");
        using var client = InstallRustDiagnosticClient(editor);
        if (change == "version") SetPrivateField(client, "_documentVersion", 2);
        if (change == "file") SetPrivateField(client, "_documentUri", new Uri(Path.Combine(Path.GetTempPath(), "other.rs")).AbsoluteUri);
        PublishRustDiagnostics(client, editor, RustDiagnosticFixture);
        if (change == "edit")
        {
            Lines(editor)[1] = "";
            InvalidateRustTestDocument(editor);
        }
        if (change == "reload") InvokePrivate(editor, "LoadFile");
        if (change == "syntax") InvokePrivate(editor, "ExecuteEditorCommand", "syntax py");
        InvokePrivate<bool>(editor, "TryApplyRustDiagnostics").Should().BeFalse();
        Diagnostics(editor).Should().BeEmpty();
    }

    [Fact]
    public void RustDiagnostics_UseUtf16ColumnsAndHandleMultilineAndMalformedRanges()
    {
        using var json = JsonDocument.Parse("""
            [
              {"range":{"start":{"line":0,"character":5},"end":{"line":1,"character":3}},"code":42,"message":"multiline"},
              {"range":{"start":{"line":1,"character":99},"end":{"line":1,"character":99}},"severity":2,"message":"at end"},
              {"range":{"start":{"line":9,"character":0},"end":{"line":9,"character":1}},"message":"outside buffer"},
              {"range":{"start":{"line":0,"character":-1},"end":{"line":0,"character":0}},"message":"negative"},
              {"range":{"start":{"line":0,"character":5},"end":{"line":0,"character":4}},"message":"reversed"},
              {"severity":3,"message":"info"}, {"range":null}, null
            ]
            """);
        var diagnostics = InvokePrivateStatic<IList>("ParseRustAnalyzerDiagnostics", json.RootElement, "// 😀 code\nend", false);
        diagnostics.Count.Should().Be(2);
        object first = diagnostics[0]!;
        first.GetType().GetProperty("StartColumn")!.GetValue(first).Should().Be(5);
        first.GetType().GetProperty("EndColumn")!.GetValue(first).Should().Be(10);
        first.GetType().GetProperty("Code")!.GetValue(first).Should().Be("42");
        object second = diagnostics[1]!;
        second.GetType().GetProperty("StartColumn")!.GetValue(second).Should().Be(3);
    }

    [Fact]
    public async Task RustDiagnostics_RenderAndF2DoNotWaitForServer_AndEditCancelsSynchronization()
    {
        var editor = RustDiagnosticEditor("fn main() {}$$");
        var completion = new TaskCompletionSource<string>();
        var cancellation = new CancellationTokenSource();
        SetPrivateField(editor, "_rustDiagnosticsTask", completion.Task);
        SetPrivateField(editor, "_rustDiagnosticsCancellation", cancellation);
        try
        {
            await Task.Run(() =>
            {
                InvokePrivate(editor, "RenderHeader", 120);
                InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', ConsoleKey.F2, false, false, false));
            }).WaitAsync(TimeSpan.FromSeconds(5));
            completion.Task.IsCompleted.Should().BeFalse();
            GetPrivateField<string>(editor, "_messageDetailsText").Should().Contain("Checking Rust code");
            InvalidateRustTestDocument(editor);
            cancellation.IsCancellationRequested.Should().BeTrue();
            GetPrivateField<Task?>(editor, "_rustDiagnosticsTask").Should().BeNull();
        }
        finally { completion.SetResult(null!); }
    }

    [Fact]
    public void RustDiagnostics_ServerFailureShowsUnavailableAndRetriesWithoutInventingCodeErrors()
    {
        var editor = RustDiagnosticEditor("fn main() {}$$");
        SetPrivateField(editor, "_rustDiagnosticsTask", Task.FromResult("server stopped"));
        SetPrivateField(editor, "_rustDiagnosticsCancellation", new CancellationTokenSource());
        SetPrivateField(editor, "_messageDetailsActive", true);
        SetPrivateField(editor, "_messageDetailsShowsDiagnostics", true);
        InvokePrivate<bool>(editor, "CheckDiagnosticsOnIdle").Should().BeTrue();
        GetPrivateField<string>(editor, "_messageDetailsText").Should().Contain("Rust diagnostics unavailable: server stopped");
        GetPrivateField<DateTime>(editor, "_rustAnalyzerRetryUtc").Should().BeAfter(DateTime.UtcNow);
        Diagnostics(editor).Should().BeEmpty();
    }

    [Theory]
    [InlineData("1")]
    [InlineData("null")]
    [InlineData("")]
    public void RustDiagnostics_AcceptOptionalServerVersion(string version)
    {
        var editor = RustDiagnosticEditor("fn main() {\n    missing();\n}$$");
        using var client = InstallRustDiagnosticClient(editor);
        PublishRustDiagnostics(client, editor, RustDiagnosticFixture, version);
        InvokePrivate<bool>(editor, "TryApplyRustDiagnostics").Should().BeTrue();
        Diagnostics(editor).Should().HaveCount(2);
    }

    [Fact]
    public void RustDiagnostics_NewStandaloneFileRequestsSaveWithoutCreatingAFile()
    {
        var editor = RustDiagnosticEditor("fn main() {}$$");
        InvokePrivate(editor, "EnsureFullDiagnostics");
        GetPrivateField<string>(editor, "_rustDiagnosticsStatus").Should().Contain("Save this Rust file once");
        File.Exists(GetPrivateField<string>(editor, "_path")).Should().BeFalse();
        GetPrivateField<object?>(editor, "_rustAnalyzer").Should().BeNull();
    }

    [RustAnalyzerIntegrationFact]
    public async Task RustDiagnostics_CargoChecksSavedCodeAndUpdatesUnsavedBuffer()
    {
        string root = Path.Combine(Path.GetTempPath(), "xte-rust-diagnostics-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(root, "src"));
        File.WriteAllText(Path.Combine(root, "Cargo.toml"), "[package]\nname = \"xte-diagnostics\"\nversion = \"0.1.0\"\nedition = \"2021\"\n");
        string path = Path.Combine(root, "src", "main.rs");
        File.WriteAllText(path, "fn main() {}\n");
        var editor = new TermXTEditor(path);
        try
        {
            SetRustProjectSource(editor, "fn main() {\n    let value = ;$$\n}");
            await WaitForRustDiagnostics(editor, diagnostics => diagnostics.Any(d => d.Severity == "Error"));
            File.ReadAllText(path).Should().Be("fn main() {}\n", "analysis must not save edits");

            SetRustProjectSource(editor, "fn main() {\n    let value = ;$$\n}\n");
            await WaitForRustDiagnostics(editor, diagnostics => diagnostics.Any(d => d.Severity == "Error"));

            SetRustProjectSource(editor, "fn main() {}$$");
            await WaitForRustDiagnostics(editor, diagnostics => diagnostics.Count == 0);

            SetRustProjectSource(editor, "fn main() {\n    let _value: i32 = \"bad\";$$\n}");
            InvokePrivate<bool>(editor, "Save", false).Should().BeTrue();
            await WaitForRustDiagnostics(editor, diagnostics => diagnostics.Any(d => d.Code == "E0308" && d.Description.Contains("mismatched types")));
            InvokePrivate<string>(editor, "BuildDiagnosticsDetails").Should().Contain("E0308").And.Contain("mismatched types");

            SetRustProjectSource(editor, "fn main() {}$$");
            InvokePrivate<bool>(editor, "Save", false).Should().BeTrue();
            await WaitForRustDiagnostics(editor, diagnostics => diagnostics.Count == 0);

            SetRustProjectSource(editor, "fn main() {\n    let unused = 1;$$\n}");
            InvokePrivate<bool>(editor, "Save", false).Should().BeTrue();
            await WaitForRustDiagnostics(editor, diagnostics => diagnostics.Any(d => d.Severity == "Warning" && d.Code == "unused_variables"));

            // Cargo may publish nothing when the same warning survives a check.
            SetRustProjectSource(editor, "fn main() {\n    let unused = 1;$$\n}\n");
            InvokePrivate<bool>(editor, "Save", false).Should().BeTrue();
            await WaitForRustDiagnostics(editor, diagnostics => diagnostics.Any(d => d.Severity == "Warning" && d.Code == "unused_variables"));

            SetRustProjectSource(editor, "fn main() {}$$");
            InvokePrivate<bool>(editor, "Save", false).Should().BeTrue();
            await WaitForRustDiagnostics(editor, diagnostics => diagnostics.Count == 0);
        }
        finally
        {
            InvokePrivate(editor, "StopRustAnalyzer");
            await DeleteRustDiagnosticFixture(root);
        }
    }

    [RustAnalyzerIntegrationFact]
    public Task RustDiagnostics_CommentedBindingIsReportedWithoutSavingInCargo()
    {
        return VerifyRustCommentedBinding(cargo: true);
    }

    [RustAnalyzerIntegrationFact]
    public Task RustDiagnostics_CommentedBindingIsReportedWithoutSavingStandalone()
    {
        return VerifyRustCommentedBinding(cargo: false);
    }

    private static async Task VerifyRustCommentedBinding(bool cargo)
    {
        string root = Path.Combine(Path.GetTempPath(), "xte-rust-diagnostics-" + Guid.NewGuid());
        string directory = cargo ? Path.Combine(root, "src") : root;
        Directory.CreateDirectory(directory);
        if (cargo)
            File.WriteAllText(Path.Combine(root, "Cargo.toml"), "[package]\nname = \"xte-arg1\"\nversion = \"0.1.0\"\nedition = \"2021\"\n");
        const string source = "const HELP_MESSAGE: &str = \"help\";\nfn main() {\n    let args: Vec<String> = std::env::args().collect();\n    let arg1 = &args[1];\n    if arg1 == \"-h\" {\n        println!(\"{}\", HELP_MESSAGE);\n    }\n}";
        string path = Path.Combine(directory, "main.rs");
        File.WriteAllText(path, source);
        var editor = new TermXTEditor(path);
        try
        {
            SetRustProjectSource(editor, source.Replace("let arg1", "// let arg1") + "$$");
            await WaitForRustDiagnostics(editor, diagnostics => diagnostics.Any(d =>
                d.Severity == "Error" && d.Code == "E0425" && d.LineNumber == 5));
            File.ReadAllText(path).Should().Be(source, "live semantic diagnostics must not save the buffer");
            InvokePrivate<string>(editor, "BuildDiagnosticsDetails").Should().Contain("E0425");

            SetRustProjectSource(editor, source + "$$");
            await WaitForRustDiagnostics(editor, diagnostics => diagnostics.All(d => d.Code != "E0425"));
            File.ReadAllText(path).Should().Be(source);
        }
        finally
        {
            InvokePrivate(editor, "StopRustAnalyzer");
            await DeleteRustDiagnosticFixture(root);
        }
    }

    [RustAnalyzerIntegrationFact]
    public async Task RustDiagnostics_StandaloneReportsUnsavedSyntaxErrors()
    {
        string root = Path.Combine(Path.GetTempPath(), "xte-rust-diagnostics-" + Guid.NewGuid());
        Directory.CreateDirectory(root);
        string path = Path.Combine(root, "main.rs");
        File.WriteAllText(path, "fn main() {}\n");
        var editor = new TermXTEditor(path);
        try
        {
            SetRustProjectSource(editor, "fn main() {\n    let value = ;$$\n}");
            await WaitForRustDiagnostics(editor, diagnostics => diagnostics.Any(d => d.Severity == "Error" && d.LineNumber == 2));
            SetRustProjectSource(editor, "fn main() {}$$");
            await WaitForRustDiagnostics(editor, diagnostics => diagnostics.Count == 0);
            File.ReadAllText(path).Should().Be("fn main() {}\n");
            InvokePrivate<bool>(editor, "Save", false).Should().BeTrue();
            await WaitForRustDiagnostics(editor, diagnostics => diagnostics.Count == 0);
        }
        finally
        {
            InvokePrivate(editor, "StopRustAnalyzer");
            await DeleteRustDiagnosticFixture(root);
        }
    }

    private static async Task DeleteRustDiagnosticFixture(string root)
    {
        // Cargo child processes may briefly retain their working directory after
        // the analyzer is stopped, especially after a save starts a new check.
        for (int attempt = 0; ; attempt++)
        {
            try { Directory.Delete(root, recursive: true); return; }
            catch (IOException) when (attempt < 20) { await Task.Delay(100); }
        }
    }

    private static async Task WaitForRustDiagnostics(TermXTEditor editor, Func<List<DiagnosticInfo>, bool> expected)
    {
        SetPrivateField(editor, "_rustDiagnosticsReadyUtc", DateTime.MinValue);
        DateTime deadline = DateTime.UtcNow.AddSeconds(90);
        while (DateTime.UtcNow < deadline)
        {
            InvokePrivate(editor, "CheckDiagnosticsOnIdle");
            if (GetPrivateField<string?>(editor, "_rustDiagnosticsStatus") == null &&
                GetPrivateField<Task?>(editor, "_rustDiagnosticsTask") == null && expected(Diagnostics(editor))) return;
            await Task.Delay(50);
        }
        Assert.Fail("Rust diagnostics timed out: " + GetPrivateField<string?>(editor, "_rustDiagnosticsStatus") + "\n" +
            InvokePrivate<string>(editor, "BuildDiagnosticsDetails"));
    }

    private static TermXTEditor RustDiagnosticEditor(string source)
    {
        var editor = new TermXTEditor(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".rs"));
        SetRustProjectSource(editor, source);
        return editor;
    }

    private static IDisposable InstallRustDiagnosticClient(TermXTEditor editor)
    {
        string path = GetPrivateField<string>(editor, "_path");
        Type type = typeof(TermXTEditor).GetNestedType("RustAnalyzerClient", BindingFlags.NonPublic)!;
        var client = (IDisposable)Activator.CreateInstance(type, Path.GetDirectoryName(path)!, "unused.exe", null!)!;
        SetPrivateField(client, "_documentUri", new Uri(path).AbsoluteUri);
        SetPrivateField(client, "_documentText", string.Join('\n', Lines(editor)));
        SetPrivateField(client, "_documentVersion", 1);
        SetPrivateField(client, "_documentEditorVersion", GetPrivateField<int>(editor, "_rustDocumentVersion"));
        SetPrivateField(editor, "_rustAnalyzer", client);
        SetPrivateField(editor, "_rustDiagnosticsPending", false);
        return client;
    }

    private static void PublishRustDiagnostics(object client, TermXTEditor editor, string items, string version = "1")
    {
        string uri = new Uri(GetPrivateField<string>(editor, "_path")).AbsoluteUri;
        using var json = JsonDocument.Parse("{\"uri\":" + JsonSerializer.Serialize(uri) +
            (version.Length == 0 ? "" : ",\"version\":" + version) + ",\"diagnostics\":" + items + "}");
        InvokePrivate(client, "ReceiveDiagnostics", json.RootElement);
    }
}
