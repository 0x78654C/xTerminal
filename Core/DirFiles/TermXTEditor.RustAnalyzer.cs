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
        private RustAnalyzerClient _rustAnalyzer;
        private Task<RustProjectCompletionResult> _rustProjectCompletionTask;
        private CancellationTokenSource _rustProjectCompletionCancellation;
        private DateTime _rustAnalyzerRetryUtc;
        private int _rustDocumentVersion;

        private bool QueueRustProjectCompletion(string source, int startColumn, bool manual)
        {
            CancelRustProjectCompletion();
            if (!manual && DateTime.UtcNow < _rustAnalyzerRetryUtc) return false;
            bool notify = manual || (!_completionActive && startColumn == _cursorCol);
            if (!EnsureRustAnalyzer(notify)) return false;
            string analysisName = FindRustCargoRoot(_path) == null ? "Rust" : "Cargo";

            var snapshot = new RustProjectCompletionResult
            {
                Path = _path, Line = _cursorLine, Column = _cursorCol,
                StartColumn = startColumn, Version = _rustDocumentVersion, Manual = manual, Notify = notify,
                AnalysisName = analysisName
            };
            var cancellation = new CancellationTokenSource();
            _rustProjectCompletionCancellation = cancellation;
            RustAnalyzerClient client = _rustAnalyzer;
            _rustProjectCompletionTask = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(manual ? 0 : 180, cancellation.Token).ConfigureAwait(false);
                    snapshot.Items = await client.CompleteAsync(snapshot.Path, source, snapshot.Version, snapshot.Line, snapshot.Column,
                        cancellation.Token).ConfigureAwait(false);
                    snapshot.ProjectStatus = client.ProjectStatus;
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { snapshot.Error = ex.Message; }
                return snapshot;
            });
            if (notify) BottomStatus("Loading " + snapshot.AnalysisName + " suggestions...");
            return true;
        }

        private bool EnsureRustAnalyzer(bool notify)
        {
            if (_rustAnalyzer != null) return true;
            string root = FindRustCargoRoot(_path);
            string standalone = null;
            if (root == null)
            {
                // Linked standalone files get the same semantic completion as Cargo files.
                // A new file is analyzed after its first save; do not write it implicitly.
                if (!File.Exists(_path))
                {
                    _rustDiagnosticsStatus = "Save this Rust file once to enable analysis";
                    return false;
                }
                root = System.IO.Path.GetDirectoryName(_path);
                standalone = _path;
            }
            if (_rustAnalyzer == null)
            {
                string executable = FindRustAnalyzerExecutable(root);
                if (executable == null)
                {
                    _rustAnalyzerRetryUtc = DateTime.UtcNow.AddSeconds(30);
                    _rustDiagnosticsStatus = "Rust analysis needs rust-analyzer. Run: rustup component add rust-analyzer rust-src";
                    if (notify) BottomStatus(_rustDiagnosticsStatus, warning: true);
                    return false;
                }
                _rustAnalyzer = new RustAnalyzerClient(root, executable, standalone);
            }

            return true;
        }

        private bool CheckRustCompletionOnIdle()
        {
            if (_rustProjectCompletionTask == null || !_rustProjectCompletionTask.IsCompleted) return false;
            RustProjectCompletionResult result = _rustProjectCompletionTask.GetAwaiter().GetResult();
            _rustProjectCompletionTask = null;
            _rustProjectCompletionCancellation.Dispose();
            _rustProjectCompletionCancellation = null;
            if (result.Version != _rustDocumentVersion || result.Path != _path || result.Line != _cursorLine ||
                result.Column != _cursorCol || _syntax != TermXTEditorSyntax.Rust || _mode != Mode.Insert || HasSelection()) return false;
            if (result.Error != null)
            {
                StopRustAnalyzer();
                _rustAnalyzerRetryUtc = DateTime.UtcNow.AddSeconds(30);
                if (result.Notify || result.Manual) BottomStatus((result.AnalysisName ?? "Cargo") + " completion unavailable: " + result.Error, warning: true);
                return result.Notify || result.Manual;
            }
            if (result.Items == null) return false;
            string prefix = CurrentLine().Substring(result.StartColumn, result.Column - result.StartColumn);
            // Rust types can expose hundreds of methods. The popup already scrolls;
            // keep every match so lower-ranked methods remain reachable after '.'.
            List<CompletionItem> items = FilterCompletionItems(result.Items, prefix, maxItems: int.MaxValue);
            if (items.Count == 0)
            {
                if (result.Notify || result.Manual) BottomStatus(string.IsNullOrEmpty(result.ProjectStatus)
                    ? "No " + result.AnalysisName + " suggestions at this position" : result.AnalysisName + " analysis: " + result.ProjectStatus);
                return result.Notify || result.Manual;
            }
            string selected = _completionActive ? _completionItems[_completionSelectedIndex].Label : null;
            SetCompletionSession(new CompletionSession(result.Line, result.StartColumn, false, result.Items, items), selected);
            _rustCompletionManual = result.Manual;
            if (result.Notify || result.Manual) BottomStatus(result.AnalysisName + " IntelliSense: " + items.Count + " " + Pluralize("item", items.Count));
            return true;
        }

        private void CancelRustProjectCompletion()
        {
            var cancellation = _rustProjectCompletionCancellation;
            var task = _rustProjectCompletionTask;
            _rustProjectCompletionCancellation = null;
            _rustProjectCompletionTask = null;
            if (cancellation == null) return;
            cancellation.Cancel();
            _ = task.ContinueWith(_ => cancellation.Dispose(), CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        private void StopRustAnalyzer()
        {
            CancelRustProjectCompletion();
            CancelRustDiagnostics();
            _rustAnalyzer?.Dispose();
            _rustAnalyzer = null;
            _rustAnalyzerRetryUtc = DateTime.MinValue;
            _rustDiagnosticsPending = _syntax == TermXTEditorSyntax.Rust;
        }

        private static string FindRustCargoRoot(string path)
        {
            try
            {
                for (var directory = new DirectoryInfo(System.IO.Path.GetDirectoryName(path)); directory != null; directory = directory.Parent)
                    if (File.Exists(System.IO.Path.Combine(directory.FullName, "Cargo.toml"))) return directory.FullName;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException) { }
            return null;
        }

        private static string FindRustAnalyzerExecutable(string root)
        {
            string configured = Environment.GetEnvironmentVariable("XTE_RUST_ANALYZER");
            if (!string.IsNullOrWhiteSpace(configured)) return configured.Trim('"');
            var directories = new List<string>((Environment.GetEnvironmentVariable("PATH") ?? "").Split(System.IO.Path.PathSeparator));
            string cargoHome = Environment.GetEnvironmentVariable("CARGO_HOME");
            if (string.IsNullOrEmpty(cargoHome)) cargoHome = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cargo");
            string cargoBin = System.IO.Path.Combine(cargoHome, "bin");
            directories.Add(cargoBin);
            string toolchain = FindInstalledRustToolchain(root, RustupHome(), Environment.GetEnvironmentVariable("RUSTUP_TOOLCHAIN"));
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return FindInstalledRustAnalyzer(directories, cargoBin, toolchain, new[]
            {
                System.IO.Path.Combine(profile, ".vscode", "extensions"),
                System.IO.Path.Combine(profile, ".vscode-insiders", "extensions")
            });
        }

        private static string FindInstalledRustAnalyzer(IEnumerable<string> directories, string cargoBin, string toolchain, IEnumerable<string> extensionDirectories)
        {
            string proxy = null;
            foreach (string directory in directories)
            {
                if (string.IsNullOrWhiteSpace(directory)) continue;
                string candidate = System.IO.Path.Combine(directory.Trim('"'), "rust-analyzer.exe");
                if (!File.Exists(candidate)) continue;
                // Rustup can install this proxy even when the analyzer component is
                // missing. Prefer an actual server before falling back to the proxy.
                if (string.Equals(System.IO.Path.GetFullPath(directory.Trim('"')), System.IO.Path.GetFullPath(cargoBin), StringComparison.OrdinalIgnoreCase) &&
                    File.Exists(System.IO.Path.Combine(cargoBin, "rustup.exe"))) proxy = candidate;
                else return candidate;
            }
            if (toolchain != null && File.Exists(System.IO.Path.Combine(toolchain, "rust-analyzer.exe")))
                return System.IO.Path.Combine(toolchain, "rust-analyzer.exe");
            foreach (string directory in extensionDirectories)
            {
                string bundled = FindBundledRustAnalyzer(directory);
                if (bundled != null) return bundled;
            }
            return proxy;
        }

        private sealed class RustProjectCompletionResult
        {
            public string Path;
            public int Line;
            public int Column;
            public int StartColumn;
            public int Version;
            public bool Manual;
            public bool Notify;
            public string AnalysisName;
            public List<CompletionItem> Items;
            public string Error;
            public string ProjectStatus;
        }

        // A single stdio LSP session per editor. All protocol work runs off the input
        // thread; editor snapshots and completion publication stay on the input thread.
        private sealed partial class RustAnalyzerClient : IDisposable
        {
            private readonly string _root;
            private readonly string _executable;
            private readonly string _standaloneFile;
            private readonly object _stateLock = new object();
            private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
            private readonly CancellationToken _shutdown;
            private readonly SemaphoreSlim _writeGate = new SemaphoreSlim(1, 1);
            private readonly SemaphoreSlim _completionGate = new SemaphoreSlim(1, 1);
            private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement>> _requests = new ConcurrentDictionary<int, TaskCompletionSource<JsonElement>>();
            private readonly Lazy<Task> _initialization;
            private TaskCompletionSource<bool> _ready = NewReadySignal();
            private Process _process;
            private int _nextId;
            private int _documentVersion;
            private string _documentUri;
            private string _documentText;
            private bool _disposed;
            private string _lastError;
            private volatile Exception _failure;
            private volatile string _serverHealth;
            private volatile string _projectStatus;

            public string ProjectStatus { get { return _projectStatus; } }

            public RustAnalyzerClient(string root, string executable, string standaloneFile)
            {
                _root = root;
                _executable = executable;
                _standaloneFile = standaloneFile;
                _shutdown = _lifetime.Token;
                _initialization = new Lazy<Task>(InitializeAsync);
            }

            private static TaskCompletionSource<bool> NewReadySignal()
            {
                return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            private async Task InitializeAsync()
            {
                object[] projects = _standaloneFile == null ? Array.Empty<object>() :
                    new[] { await CreateRustStandaloneProjectAsync(_root, _standaloneFile, _shutdown).ConfigureAwait(false) };
                lock (_stateLock)
                {
                    _shutdown.ThrowIfCancellationRequested();
                    _process = new Process
                    {
                        StartInfo = new ProcessStartInfo(_executable)
                        {
                            WorkingDirectory = _root, UseShellExecute = false, CreateNoWindow = true,
                            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
                        }
                    };
                    ConfigureRustAnalyzerTools(_process.StartInfo, _root);
                    _process.Start();
                }
                _ = ReadMessagesAsync(_process.StandardOutput.BaseStream);
                _ = DrainErrorsAsync(_process.StandardError);
                JsonElement initialized = await RequestAsync("initialize", new
                {
                    processId = Environment.ProcessId,
                    rootUri = new Uri(_root + System.IO.Path.DirectorySeparatorChar).AbsoluteUri,
                    workspaceFolders = WorkspaceFolders(),
                    capabilities = new
                    {
                        general = new { positionEncodings = new[] { "utf-16" } },
                        workspace = new { workspaceFolders = true },
                        textDocument = new
                        {
                            completion = new { completionItem = new { snippetSupport = false, insertReplaceSupport = true, labelDetailsSupport = true } },
                            publishDiagnostics = new { versionSupport = true }
                        },
                        experimental = new { serverStatusNotification = true }
                    },
                    initializationOptions = new
                    {
                        linkedProjects = projects,
                        checkOnSave = true,
                        files = new { watcher = "server" },
                        completion = new
                        {
                            autoimport = new { enable = false },
                            postfix = new { enable = false },
                            autoAwait = new { enable = false },
                            autoIter = new { enable = false },
                            callable = new { snippets = "none" },
                            fullFunctionSignatures = new { enable = true }
                        }
                    }
                }, _shutdown).ConfigureAwait(false);
                if (initialized.TryGetProperty("capabilities", out JsonElement capabilities) &&
                    capabilities.TryGetProperty("positionEncoding", out JsonElement encoding) && encoding.GetString() != "utf-16")
                    throw new IOException("rust-analyzer must support UTF-16 positions");
                _supportsDiagnosticRequests = capabilities.ValueKind == JsonValueKind.Object &&
                    capabilities.TryGetProperty("diagnosticProvider", out JsonElement diagnosticProvider) &&
                    diagnosticProvider.ValueKind == JsonValueKind.Object;
                await NotifyAsync("initialized", new { }, _shutdown).ConfigureAwait(false);
            }

            public async Task<List<CompletionItem>> CompleteAsync(string path, string source, int editorVersion, int line, int column, CancellationToken cancellation)
            {
                await _completionGate.WaitAsync(cancellation).ConfigureAwait(false);
                try
                {
                    string uri = await SynchronizeDocumentAsync(path, source, editorVersion, cancellation).ConfigureAwait(false);
                    Task ready;
                    lock (_stateLock) ready = _ready.Task;
                    await ready.WaitAsync(TimeSpan.FromSeconds(90), cancellation).ConfigureAwait(false);
                    for (int attempt = 0; ; attempt++)
                    {
                        try
                        {
                            JsonElement result = await RequestAsync("textDocument/completion", new { textDocument = new { uri }, position = new { line, character = column } }, cancellation).ConfigureAwait(false);
                            List<CompletionItem> items = ParseRustAnalyzerCompletions(result, source.Split('\n')[line], line, column);
                            if (items.Count == 0 && _serverHealth == "error")
                                throw new IOException(_projectStatus ?? "rust-analyzer could not load the Cargo project");
                            return items;
                        }
                        catch (RustAnalyzerResponseException ex) when ((ex.Code == -32801 || ex.Code == -32800) && attempt < 2)
                        {
                            await Task.Delay(150, cancellation).ConfigureAwait(false);
                        }
                    }
                }
                finally { _completionGate.Release(); }
            }

            private object[] WorkspaceFolders()
            {
                return new object[] { new { uri = new Uri(_root + System.IO.Path.DirectorySeparatorChar).AbsoluteUri, name = System.IO.Path.GetFileName(_root) } };
            }

            private async Task<JsonElement> RequestAsync(string method, object parameters, CancellationToken cancellation)
            {
                int id = Interlocked.Increment(ref _nextId);
                var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
                _requests[id] = completion;
                try
                {
                    if (_failure != null) throw new IOException("rust-analyzer connection closed", _failure);
                    await SendAsync(new { jsonrpc = "2.0", id, method, @params = parameters }, cancellation).ConfigureAwait(false);
                    return await completion.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellation).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException)
                {
                    if (!_shutdown.IsCancellationRequested)
                        await NotifyAsync("$/cancelRequest", new { id }, _shutdown).ConfigureAwait(false);
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
                    // Finish a frame even if a keystroke cancels the request mid-write.
                    Stream stream = _process.StandardInput.BaseStream;
                    await stream.WriteAsync(header, _shutdown).ConfigureAwait(false);
                    await stream.WriteAsync(payload, _shutdown).ConfigureAwait(false);
                    await stream.FlushAsync(_shutdown).ConfigureAwait(false);
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
                                throw new IOException("rust-analyzer stopped" + (string.IsNullOrEmpty(_lastError) ? "" : ": " + _lastError));
                            header.Append((char)one[0]);
                            int n = header.Length;
                            if (n >= 4 && header[n - 4] == '\r' && header[n - 3] == '\n' && header[n - 2] == '\r' && header[n - 1] == '\n') break;
                            if (n > 8192) throw new IOException("Invalid rust-analyzer message header");
                        }
                        int length = -1;
                        foreach (string field in header.ToString().Split("\r\n"))
                            if (field.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                                int.TryParse(field.Substring(15).Trim(), out length);
                        if (length < 0 || length > 16 * 1024 * 1024) throw new IOException("Invalid rust-analyzer message length");
                        byte[] payload = new byte[length];
                        await stream.ReadExactlyAsync(payload, _shutdown).ConfigureAwait(false);
                        using JsonDocument document = JsonDocument.Parse(payload);
                        JsonElement message = document.RootElement;
                        if (message.TryGetProperty("method", out JsonElement method))
                        {
                            if (message.TryGetProperty("id", out JsonElement requestId))
                                await RespondToServerAsync(requestId.Clone(), method.GetString(), message).ConfigureAwait(false);
                            else if (method.GetString() == "textDocument/publishDiagnostics" && message.TryGetProperty("params", out JsonElement diagnostics))
                                ReceiveDiagnostics(diagnostics);
                            else if (method.GetString() == "experimental/serverStatus" && message.TryGetProperty("params", out JsonElement status))
                            {
                                _serverHealth = RustJsonString(status, "health");
                                _projectStatus = RustJsonString(status, "message");
                                lock (_stateLock)
                                {
                                    if (status.TryGetProperty("quiescent", out JsonElement quiet) && quiet.ValueKind == JsonValueKind.True) _ready.TrySetResult(true);
                                    else if (_ready.Task.IsCompleted) _ready = NewReadySignal();
                                }
                            }
                        }
                        else if (message.TryGetProperty("id", out JsonElement responseId) && responseId.TryGetInt32(out int id) && _requests.TryRemove(id, out var pending))
                        {
                            if (message.TryGetProperty("error", out JsonElement error))
                                pending.TrySetException(new RustAnalyzerResponseException(error.GetProperty("code").GetInt32(), error.GetProperty("message").GetString()));
                            else pending.TrySetResult(message.GetProperty("result").Clone());
                        }
                    }
                }
                catch (Exception ex)
                {
                    _failure = ex;
                    foreach (var pending in _requests.Values) pending.TrySetException(ex);
                    lock (_stateLock) _ready.TrySetException(ex);
                }
            }

            private Task RespondToServerAsync(JsonElement id, string method, JsonElement message)
            {
                object result = null;
                switch (method)
                {
                    case "workspace/workspaceFolders": result = WorkspaceFolders(); break;
                    case "workspace/configuration":
                        result = new object[message.GetProperty("params").GetProperty("items").GetArrayLength()];
                        break;
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

        private sealed class RustAnalyzerResponseException : Exception
        {
            public int Code { get; }
            public RustAnalyzerResponseException(int code, string message) : base(message) { Code = code; }
        }

        private static List<CompletionItem> ParseRustAnalyzerCompletions(JsonElement result, string line, int lineNumber, int column)
        {
            var completions = new List<CompletionItem>();
            if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("items", out JsonElement list)) result = list;
            if (result.ValueKind != JsonValueKind.Array) return completions;
            foreach (JsonElement item in result.EnumerateArray().OrderBy(value => RustJsonString(value, "sortText"), StringComparer.Ordinal))
            {
                string label = RustJsonString(item, "label");
                if (string.IsNullOrWhiteSpace(label)) continue;
                // No snippet or auto-import capabilities are advertised. Do not silently
                // apply a partial completion if a server nevertheless requires them.
                if (item.TryGetProperty("insertTextFormat", out JsonElement format) && format.GetInt32() == 2) continue;
                if (item.TryGetProperty("additionalTextEdits", out JsonElement additional) && additional.GetArrayLength() > 0) continue;
                string insertion = RustJsonString(item, "insertText") ?? label;
                int start = -1, end = -1;
                if (item.TryGetProperty("textEdit", out JsonElement edit))
                {
                    if (!edit.TryGetProperty("range", out JsonElement range) && !edit.TryGetProperty("replace", out range)) continue;
                    JsonElement from = range.GetProperty("start"), to = range.GetProperty("end");
                    if (from.GetProperty("line").GetInt32() != lineNumber || to.GetProperty("line").GetInt32() != lineNumber) continue;
                    start = from.GetProperty("character").GetInt32();
                    end = to.GetProperty("character").GetInt32();
                    if (start < 0 || start > column || end < column || end > line.Length) continue;
                    insertion = RustJsonString(edit, "newText");
                }
                if (string.IsNullOrEmpty(insertion) || insertion.IndexOfAny(new[] { '\r', '\n', '\t', '\x1b' }) >= 0) continue;
                string detail = RustJsonString(item, "detail") ?? "Rust / rust-analyzer";
                if (item.TryGetProperty("labelDetails", out JsonElement labelDetails))
                    detail = (RustJsonString(labelDetails, "detail") + " " + RustJsonString(labelDetails, "description") + " " + detail).Trim();
                int kind = item.TryGetProperty("kind", out JsonElement kindValue) ? kindValue.GetInt32() : 1;
                completions.Add(new CompletionItem(label, insertion, RustLspCompletionKind(kind), detail, completions.Count)
                {
                    FilterText = RustJsonString(item, "filterText") ?? label,
                    RustEditStart = start, RustEditEnd = end
                });
            }
            return completions;
        }

        private static string RustJsonString(JsonElement value, string name)
        {
            return value.TryGetProperty(name, out JsonElement property) && property.ValueKind == JsonValueKind.String ? property.GetString() : null;
        }

        private static string RustLspCompletionKind(int kind)
        {
            switch (kind)
            {
                case 2: return "method";
                case 3: case 4: return "function";
                case 5: case 10: return "field";
                case 6: return "variable";
                case 7: case 8: case 13: case 22: case 25: return "type";
                case 9: return "module";
                case 14: return "keyword";
                case 20: return "variant";
                case 21: return "constant";
                default: return "identifier";
            }
        }
    }
}
