using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Core.DirFiles;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

public partial class TermXTEditorSyntaxTests
{
    [Theory]
    [InlineData(-32802)]
    [InlineData(-32801)]
    [InlineData(-32800)]
    public async Task RustProtocol_ServerCancellationRetriesWithoutDisconnecting(int code)
    {
        var editor = RustDiagnosticEditor("fn main() {}$$");
        using var client = InstallRustDiagnosticClient(editor);
        using var transport = InstallRustTestTransport(client);
        Task diagnostics = StartRustDiagnosticRequest(editor, client);
        JsonElement request = await transport.Request("textDocument/diagnostic");
        var requests = GetPrivateField<ConcurrentDictionary<int, TaskCompletionSource<JsonElement>>>(client, "_requests");
        Type exceptionType = typeof(TermXTEditor).GetNestedType("RustAnalyzerResponseException", BindingFlags.NonPublic)!;
        var exception = (Exception)Activator.CreateInstance(exceptionType, code, "server cancelled the request")!;
        requests[request.GetProperty("id").GetInt32()].SetException(exception);
        ReplyToRustRequest(client, await transport.Request("textDocument/diagnostic"), "{\"items\":[]}");
        await diagnostics.WaitAsync(TimeSpan.FromSeconds(2));
        GetPrivateField<Exception?>(client, "_failure").Should().BeNull();
        GetPrivateField<object>(editor, "_rustAnalyzer").Should().BeSameAs(client);
    }

    [Fact]
    public async Task RustProtocol_CompletionDoesNotWaitForDiagnosticsOrServerIdle()
    {
        var editor = RustDiagnosticEditor("fn main() { missing(); }$$");
        using var client = InstallRustDiagnosticClient(editor);
        using var transport = InstallRustTestTransport(client);
        Task diagnostics = StartRustDiagnosticRequest(editor, client);
        JsonElement diagnosticRequest = await transport.Request("textDocument/diagnostic");

        Task completion = InvokeRustClient<Task>(client, "CompleteAsync", GetPrivateField<string>(editor, "_path"),
            string.Join('\n', Lines(editor)), GetPrivateField<int>(editor, "_rustDocumentVersion"), 0, 10, CancellationToken.None);
        JsonElement completionRequest = await transport.Request("textDocument/completion");
        ReplyToRustRequest(client, completionRequest, "[{\"label\":\"main\"}]");
        await completion.WaitAsync(TimeSpan.FromSeconds(2));
        diagnostics.IsCompleted.Should().BeFalse();
        ReplyToRustRequest(client, diagnosticRequest, "{\"items\":[]}");
        await diagnostics.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task RustProtocol_CompilerPushDoesNotEraseNativeErrors()
    {
        var editor = RustDiagnosticEditor("fn main() {\n    missing();\n}$$");
        using var client = InstallRustDiagnosticClient(editor);
        using var transport = InstallRustTestTransport(client);
        Task diagnostics = StartRustDiagnosticRequest(editor, client);
        ReplyToRustRequest(client, await transport.Request("textDocument/diagnostic"), "{\"items\":" + RustDiagnosticFixture + "}");
        await diagnostics;
        InvokePrivate<bool>(editor, "TryApplyRustDiagnostics").Should().BeTrue();
        Diagnostics(editor).Should().HaveCount(2);

        PublishRustDiagnostics(client, editor, "[]");
        InvokePrivate<bool>(editor, "TryApplyRustDiagnostics").Should().BeTrue();
        Diagnostics(editor).Should().HaveCount(2, "an empty compiler check must not clear native diagnostics");

        PublishRustDiagnostics(client, editor, RustDiagnosticFixture.Replace("\"message\":", "\"source\":\"rustc\",\"message\":"));
        InvokePrivate<bool>(editor, "TryApplyRustDiagnostics").Should().BeTrue();
        Diagnostics(editor).Should().HaveCount(2, "identical native and compiler messages should appear once");
    }

    [Fact]
    public async Task RustProtocol_LateDiagnosticResponseCannotOverwriteANewerDocument()
    {
        var editor = RustDiagnosticEditor("fn main() {\n    missing();\n}$$");
        using var client = InstallRustDiagnosticClient(editor);
        using var transport = InstallRustTestTransport(client);
        Task diagnostics = StartRustDiagnosticRequest(editor, client);
        JsonElement request = await transport.Request("textDocument/diagnostic");
        SetRustProjectSource(editor, "fn main() {}$$");
        await InvokePrivate<Task>(client, "SynchronizeDocumentAsync", GetPrivateField<string>(editor, "_path"),
            string.Join('\n', Lines(editor)), GetPrivateField<int>(editor, "_rustDocumentVersion"), CancellationToken.None);
        ReplyToRustRequest(client, request, "{\"items\":" + RustDiagnosticFixture + "}");
        await diagnostics;
        InvokePrivate<bool>(editor, "TryApplyRustDiagnostics").Should().BeFalse();
        Diagnostics(editor).Should().BeEmpty();
    }

    [Fact]
    public async Task RustProtocol_ServerCanRequestDiagnosticRefreshWithoutAnEdit()
    {
        var editor = RustDiagnosticEditor("fn main() {}$$");
        using var client = InstallRustDiagnosticClient(editor);
        using var transport = InstallRustTestTransport(client);
        using var request = JsonDocument.Parse("{\"id\":42,\"method\":\"workspace/diagnostic/refresh\"}");
        await InvokePrivate<Task>(client, "RespondToServerAsync", request.RootElement.GetProperty("id"),
            "workspace/diagnostic/refresh", request.RootElement);
        client.GetType().GetMethod("TakeDiagnosticsRefresh")!.Invoke(client, null).Should().Be(true);
        client.GetType().GetMethod("TakeDiagnosticsRefresh")!.Invoke(client, null).Should().Be(false);
    }

    [Fact]
    public void RustDiagnostics_AnEditDoesNotDiscardAPendingSaveNotification()
    {
        var editor = RustDiagnosticEditor("fn main() {}$$");
        SetPrivateField(editor, "_rustDiagnosticsSavePending", true);
        InvalidateRustTestDocument(editor);
        GetPrivateField<bool>(editor, "_rustDiagnosticsSavePending").Should().BeTrue();
    }

    [Fact]
    public void RustDiagnostics_CancelledSynchronizationRetriesTheSaveNotification()
    {
        var editor = RustDiagnosticEditor("fn main() {}$$");
        var pending = new TaskCompletionSource<string>();
        SetPrivateField(editor, "_rustDiagnosticsTask", pending.Task);
        SetPrivateField(editor, "_rustDiagnosticsCancellation", new CancellationTokenSource());
        SetPrivateField(editor, "_rustDiagnosticsTaskIncludesSave", true);
        try
        {
            InvalidateRustTestDocument(editor);
            GetPrivateField<bool>(editor, "_rustDiagnosticsSavePending").Should().BeTrue();
        }
        finally { pending.SetResult(null!); }
    }

    private static RustTestTransport InstallRustTestTransport(object client)
    {
        var transport = new RustTestTransport();
        SetPrivateField(client, "_input", transport);
        SetPrivateField(client, "_initialization", new Lazy<Task>(() => Task.CompletedTask));
        SetPrivateField(client, "_supportsDiagnosticRequests", true);
        return transport;
    }

    private static Task StartRustDiagnosticRequest(TermXTEditor editor, object client)
    {
        return InvokeRustClient<Task>(client, "UpdateDiagnosticsAsync", GetPrivateField<string>(editor, "_path"),
            string.Join('\n', Lines(editor)), GetPrivateField<int>(editor, "_rustDocumentVersion"), false, CancellationToken.None);
    }

    private static T InvokeRustClient<T>(object client, string method, params object[] arguments)
    {
        return (T)client.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance)!.Invoke(client, arguments)!;
    }

    private static void ReplyToRustRequest(object client, JsonElement request, string json)
    {
        var requests = GetPrivateField<ConcurrentDictionary<int, TaskCompletionSource<JsonElement>>>(client, "_requests");
        using var result = JsonDocument.Parse(json);
        requests[request.GetProperty("id").GetInt32()].SetResult(result.RootElement.Clone());
    }

    private sealed class RustTestTransport : MemoryStream
    {
        private readonly Channel<JsonElement> _messages = Channel.CreateUnbounded<JsonElement>();

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            string frame = Encoding.UTF8.GetString(ToArray());
            using var json = JsonDocument.Parse(frame.Substring(frame.IndexOf("\r\n\r\n", StringComparison.Ordinal) + 4));
            _messages.Writer.TryWrite(json.RootElement.Clone());
            SetLength(0);
            Position = 0;
            return Task.CompletedTask;
        }

        public async Task<JsonElement> Request(string method)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            while (true)
            {
                JsonElement message = await _messages.Reader.ReadAsync(timeout.Token);
                if (message.TryGetProperty("method", out JsonElement name) && name.GetString() == method)
                    return message;
            }
        }
    }
}
