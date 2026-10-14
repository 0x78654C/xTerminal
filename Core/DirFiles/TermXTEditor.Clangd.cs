using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Core.DirFiles
{
    public sealed partial class TermXTEditor
    {
        private static string FindClangdExecutable()
        {
            string configured = Environment.GetEnvironmentVariable("XTE_CLANGD");
            if (!string.IsNullOrWhiteSpace(configured)) return configured.Trim('"');
            var directories = new List<string>((Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator));
            foreach (string programFiles in new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) })
            {
                if (string.IsNullOrEmpty(programFiles)) continue;
                directories.Add(Path.Combine(programFiles, "LLVM", "bin"));
                foreach (string version in CppToolDirectories(Path.Combine(programFiles, "Microsoft Visual Studio")))
                    foreach (string edition in CppToolDirectories(version))
                    {
                        directories.Add(Path.Combine(edition, "VC", "Tools", "Llvm", "x64", "bin"));
                        directories.Add(Path.Combine(edition, "VC", "Tools", "Llvm", "bin"));
                    }
            }
            foreach (string directory in directories)
            {
                if (string.IsNullOrWhiteSpace(directory)) continue;
                string candidate = Path.Combine(directory.Trim('"'), "clangd.exe");
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }

        private static string[] CppToolDirectories(string path)
        {
            try { return Directory.Exists(path) ? Directory.GetDirectories(path).OrderByDescending(p => p, StringComparer.OrdinalIgnoreCase).ToArray() : Array.Empty<string>(); }
            catch (IOException) { return Array.Empty<string>(); }
            catch (UnauthorizedAccessException) { return Array.Empty<string>(); }
        }

        // One stdio server per editor, with all filesystem and protocol work off
        // the input thread. Full buffer synchronization includes unsaved edits.
        private sealed class ClangdClient : IDisposable
        {
            private readonly string _root;
            private readonly bool _cpp;
            private readonly object _stateLock = new object();
            private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
            private readonly CancellationToken _shutdown;
            private readonly SemaphoreSlim _writeGate = new SemaphoreSlim(1, 1);
            private readonly SemaphoreSlim _documentGate = new SemaphoreSlim(1, 1);
            private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement>> _requests = new ConcurrentDictionary<int, TaskCompletionSource<JsonElement>>();
            private readonly Lazy<Task> _initialization;
            private Process _process;
            private Stream _input;
            private string _documentUri, _documentText, _lastError;
            private int _nextId, _documentVersion, _editorVersion = -1;
            private bool _disposed;
            private volatile Exception _failure;

            public ClangdClient(string root, bool cpp)
            {
                _root = root;
                _cpp = cpp;
                _shutdown = _lifetime.Token;
                _initialization = new Lazy<Task>(InitializeAsync);
            }

            private async Task InitializeAsync()
            {
                string executable = FindClangdExecutable();
                if (executable == null) throw new FileNotFoundException("Install clangd or set XTE_CLANGD to enable member and project suggestions.");
                lock (_stateLock)
                {
                    _shutdown.ThrowIfCancellationRequested();
                    _process = new Process
                    {
                        StartInfo = new ProcessStartInfo(executable)
                        {
                            WorkingDirectory = _root, UseShellExecute = false, CreateNoWindow = true,
                            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
                        }
                    };
                    // Wait for semantic parsing on the worker; clangd's initial
                    // text-only fallback cannot resolve members or namespaces.
                    foreach (string argument in new[] { "--background-index=false", "--header-insertion=never", "--completion-parse=always",
                        "--header-insertion-decorators=false", "--completion-style=detailed", "--pch-storage=memory", "--log=error" })
                        _process.StartInfo.ArgumentList.Add(argument);
                    _process.Start();
                    _input = _process.StandardInput.BaseStream;
                }
                _ = ReadMessagesAsync(_process.StandardOutput.BaseStream);
                _ = DrainErrorsAsync(_process.StandardError);
                JsonElement initialized = await RequestAsync("initialize", new
                {
                    processId = Environment.ProcessId,
                    rootUri = new Uri(_root + Path.DirectorySeparatorChar).AbsoluteUri,
                    capabilities = new
                    {
                        general = new { positionEncodings = new[] { "utf-16" } },
                        textDocument = new { completion = new { completionItem = new { snippetSupport = false, insertReplaceSupport = true, labelDetailsSupport = true } } }
                    },
                    initializationOptions = new { fallbackFlags = new[] { "-x", _cpp ? "c++" : "c", _cpp ? "-std=c++20" : "-std=c17" } }
                }, _shutdown).ConfigureAwait(false);
                if (initialized.TryGetProperty("capabilities", out JsonElement capabilities) &&
                    capabilities.TryGetProperty("positionEncoding", out JsonElement encoding) && encoding.GetString() != "utf-16")
                    throw new IOException("clangd must support UTF-16 positions");
                await NotifyAsync("initialized", new { }, _shutdown).ConfigureAwait(false);
            }

            public async Task<List<CompletionItem>> CompleteAsync(string path, string source, int version, int line, int column, CancellationToken cancellation)
            {
                await _initialization.Value.WaitAsync(cancellation).ConfigureAwait(false);
                string uri = new Uri(Path.GetFullPath(path)).AbsoluteUri;
                await _documentGate.WaitAsync(cancellation).ConfigureAwait(false);
                try
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (_documentUri == uri && version < _editorVersion) throw new OperationCanceledException(cancellation);
                    // Finish the notification once started; a later request sends the
                    // next version even if this request was cancelled by a keystroke.
                    if (_documentUri != uri)
                    {
                        if (_documentUri != null) await NotifyAsync("textDocument/didClose", new { textDocument = new { uri = _documentUri } }, _shutdown).ConfigureAwait(false);
                        await NotifyAsync("textDocument/didOpen", new { textDocument = new { uri, languageId = _cpp ? "cpp" : "c", version = ++_documentVersion, text = source } }, _shutdown).ConfigureAwait(false);
                    }
                    else if (_documentText != source || _editorVersion != version)
                        await NotifyAsync("textDocument/didChange", new { textDocument = new { uri, version = ++_documentVersion }, contentChanges = new[] { new { text = source } } }, _shutdown).ConfigureAwait(false);
                    _documentUri = uri;
                    _documentText = source;
                    _editorVersion = version;
                }
                finally { _documentGate.Release(); }

                for (int attempt = 0; ; attempt++)
                {
                    try
                    {
                        JsonElement result = await RequestAsync("textDocument/completion", new { textDocument = new { uri }, position = new { line, character = column } }, cancellation).ConfigureAwait(false);
                        return ParseLanguageServerCompletions(result, source.Split('\n')[line], line, column, "C/C++ / clangd");
                    }
                    catch (ClangdResponseException ex) when (ex.Code == -32800 || ex.Code == -32801 || ex.Code == -32802)
                    {
                        if (attempt >= 2) return null;
                        await Task.Delay(100, cancellation).ConfigureAwait(false);
                    }
                }
            }

            private async Task<JsonElement> RequestAsync(string method, object parameters, CancellationToken cancellation)
            {
                int id = Interlocked.Increment(ref _nextId);
                var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
                _requests[id] = completion;
                try
                {
                    if (_failure != null) throw new IOException("clangd connection closed", _failure);
                    await SendAsync(new { jsonrpc = "2.0", id, method, @params = parameters }, cancellation).ConfigureAwait(false);
                    return await completion.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellation).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException)
                {
                    if (!_shutdown.IsCancellationRequested) await NotifyAsync("$/cancelRequest", new { id }, _shutdown).ConfigureAwait(false);
                    throw;
                }
                finally { _requests.TryRemove(id, out _); }
            }

            private Task NotifyAsync(string method, object parameters, CancellationToken cancellation)
            {
                return SendAsync(new { jsonrpc = "2.0", method, @params = parameters }, cancellation);
            }

            private async Task SendAsync(object message, CancellationToken cancellation)
            {
                byte[] payload = JsonSerializer.SerializeToUtf8Bytes(message);
                byte[] header = Encoding.ASCII.GetBytes("Content-Length: " + payload.Length + "\r\n\r\n");
                await _writeGate.WaitAsync(cancellation).ConfigureAwait(false);
                try
                {
                    // Cancellation must not leave half a protocol frame on the pipe.
                    await _input.WriteAsync(header, _shutdown).ConfigureAwait(false);
                    await _input.WriteAsync(payload, _shutdown).ConfigureAwait(false);
                    await _input.FlushAsync(_shutdown).ConfigureAwait(false);
                }
                finally { _writeGate.Release(); }
            }

            private async Task ReadMessagesAsync(Stream output)
            {
                try
                {
                    using var stream = new BufferedStream(output, 8192);
                    var one = new byte[1];
                    while (!_shutdown.IsCancellationRequested)
                    {
                        var header = new StringBuilder();
                        while (true)
                        {
                            if (await stream.ReadAsync(one, _shutdown).ConfigureAwait(false) == 0)
                                throw new IOException("clangd stopped" + (string.IsNullOrEmpty(_lastError) ? "" : ": " + _lastError));
                            header.Append((char)one[0]);
                            int n = header.Length;
                            if (n >= 4 && header[n - 4] == '\r' && header[n - 3] == '\n' && header[n - 2] == '\r' && header[n - 1] == '\n') break;
                            if (n > 8192) throw new IOException("Invalid clangd message header");
                        }
                        int length = -1;
                        foreach (string field in header.ToString().Split("\r\n"))
                            if (field.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) int.TryParse(field.Substring(15).Trim(), out length);
                        if (length < 0 || length > 16 * 1024 * 1024) throw new IOException("Invalid clangd message length");
                        byte[] payload = new byte[length];
                        await stream.ReadExactlyAsync(payload, _shutdown).ConfigureAwait(false);
                        using JsonDocument document = JsonDocument.Parse(payload);
                        JsonElement message = document.RootElement;
                        if (message.TryGetProperty("method", out JsonElement method))
                        {
                            if (message.TryGetProperty("id", out JsonElement requestId))
                                await RespondToServerAsync(requestId.Clone(), method.GetString(), message).ConfigureAwait(false);
                        }
                        else if (message.TryGetProperty("id", out JsonElement responseId) && responseId.TryGetInt32(out int id) && _requests.TryRemove(id, out var pending))
                        {
                            if (message.TryGetProperty("error", out JsonElement error))
                                pending.TrySetException(new ClangdResponseException(error.GetProperty("code").GetInt32(), error.GetProperty("message").GetString()));
                            else pending.TrySetResult(message.GetProperty("result").Clone());
                        }
                    }
                }
                catch (Exception ex)
                {
                    _failure = ex;
                    foreach (var pending in _requests.Values) pending.TrySetException(ex);
                }
            }

            private Task RespondToServerAsync(JsonElement id, string method, JsonElement message)
            {
                object result = null;
                switch (method)
                {
                    case "workspace/configuration": result = new object[message.GetProperty("params").GetProperty("items").GetArrayLength()]; break;
                    case "client/registerCapability":
                    case "window/workDoneProgress/create": break;
                    case "workspace/applyEdit": result = new { applied = false, failureReason = "xte applies completion edits only" }; break;
                    default: return SendAsync(new { jsonrpc = "2.0", id, error = new { code = -32601, message = "Method not supported" } }, _shutdown);
                }
                return SendAsync(new { jsonrpc = "2.0", id, result }, _shutdown);
            }

            private async Task DrainErrorsAsync(StreamReader reader)
            {
                try
                {
                    while (await reader.ReadLineAsync(_shutdown).ConfigureAwait(false) is string line)
                        if (!string.IsNullOrWhiteSpace(line)) _lastError = line.Length > 240 ? line.Substring(0, 240) : line;
                }
                catch (Exception) { /* The process may be closing. */ }
            }

            public void Dispose()
            {
                lock (_stateLock)
                {
                    if (_disposed) return;
                    _disposed = true;
                    _lifetime.Cancel();
                    try { if (_process != null && !_process.HasExited) _process.Kill(entireProcessTree: true); }
                    catch (InvalidOperationException) { }
                    catch (System.ComponentModel.Win32Exception) { }
                    _process?.Dispose();
                }
            }
        }

        private sealed class ClangdResponseException : Exception
        {
            public int Code { get; }
            public ClangdResponseException(int code, string message) : base(message) { Code = code; }
        }
    }
}
