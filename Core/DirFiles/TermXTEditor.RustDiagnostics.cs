using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Core.DirFiles
{
    public sealed partial class TermXTEditor
    {
        private const int RustDiagnosticDelayMs = 300;
        private bool _rustDiagnosticsPending;
        private bool _rustDiagnosticsSavePending;
        private bool _rustDiagnosticsTaskIncludesSave;
        private DateTime _rustDiagnosticsReadyUtc;
        private Task<string> _rustDiagnosticsTask;
        private CancellationTokenSource _rustDiagnosticsCancellation;
        private string _rustDiagnosticsStatus;

        private void InvalidateRustDiagnostics(bool delay)
        {
            CancelRustDiagnostics();
            _rustDiagnosticsPending = _syntax == TermXTEditorSyntax.Rust;
            if (!_rustDiagnosticsPending) _rustDiagnosticsSavePending = false;
            _rustDiagnosticsReadyUtc = delay ? DateTime.UtcNow.AddMilliseconds(RustDiagnosticDelayMs) : DateTime.MinValue;
            _rustDiagnosticsStatus = _rustDiagnosticsPending ? "Checking Rust code..." : null;
            if (_rustDiagnosticsPending) ClearDiagnostics();
        }

        private void CancelRustDiagnostics()
        {
            var task = _rustDiagnosticsTask;
            var cancellation = _rustDiagnosticsCancellation;
            _rustDiagnosticsTask = null;
            _rustDiagnosticsCancellation = null;
            if (_rustDiagnosticsTaskIncludesSave && task != null && !task.IsCompleted)
                _rustDiagnosticsSavePending = true;
            _rustDiagnosticsTaskIncludesSave = false;
            if (cancellation == null) return;
            cancellation.Cancel();
            _ = task.ContinueWith(_ => cancellation.Dispose(), CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        private bool CheckRustDiagnosticsOnIdle()
        {
            if (_syntax != TermXTEditorSyntax.Rust) return false;
            bool changed = TryApplyRustDiagnostics();
            if (_rustAnalyzer?.TakeDiagnosticsRefresh() == true)
                _rustDiagnosticsPending = true;
            string failure = _rustAnalyzer?.FailureMessage;
            if (_rustDiagnosticsTask != null && _rustDiagnosticsTask.IsCompleted)
            {
                failure = _rustDiagnosticsTask.GetAwaiter().GetResult() ?? failure;
                _rustDiagnosticsTask = null;
                _rustDiagnosticsTaskIncludesSave = false;
                _rustDiagnosticsCancellation.Dispose();
                _rustDiagnosticsCancellation = null;
            }
            if (failure != null)
            {
                StopRustAnalyzer();
                ClearDiagnostics();
                _rustAnalyzerRetryUtc = DateTime.UtcNow.AddSeconds(30);
                _rustDiagnosticsStatus = "Rust diagnostics unavailable: " + failure;
                BottomStatus(_rustDiagnosticsStatus, warning: true);
                RefreshRustDiagnosticsDetails();
                return true;
            }
            if (!_rustDiagnosticsPending || _rustDiagnosticsTask != null ||
                DateTime.UtcNow < _rustDiagnosticsReadyUtc || DateTime.UtcNow < _rustAnalyzerRetryUtc) return changed;
            string previousStatus = _rustDiagnosticsStatus;
            if (!EnsureRustAnalyzer(notify: false))
            {
                _rustDiagnosticsReadyUtc = DateTime.UtcNow.AddSeconds(30);
                RefreshRustDiagnosticsDetails();
                return changed || previousStatus != _rustDiagnosticsStatus;
            }

            // Capture all editor state before starting the worker. Completion and
            // diagnostics share the server's serialized document synchronization.
            string path = _path, source = BuildDocumentText();
            int version = _rustDocumentVersion;
            bool saved = _rustDiagnosticsSavePending;
            RustAnalyzerClient client = _rustAnalyzer;
            var cancellation = new CancellationTokenSource();
            _rustDiagnosticsCancellation = cancellation;
            _rustDiagnosticsPending = false;
            _rustDiagnosticsSavePending = false;
            _rustDiagnosticsTaskIncludesSave = saved;
            _rustDiagnosticsTask = Task.Run(async () =>
            {
                try
                {
                    await client.UpdateDiagnosticsAsync(path, source, version, saved, cancellation.Token).ConfigureAwait(false);
                    return null;
                }
                catch (OperationCanceledException) { return null; }
                catch (Exception ex) { return ex.Message; }
            });
            return changed;
        }

        private bool TryApplyRustDiagnostics()
        {
            if (_syntax != TermXTEditorSyntax.Rust || _rustAnalyzer == null) return false;
            RustDiagnosticResult result = _rustAnalyzer.TakeDiagnostics();
            if (result == null || result.Version != _rustDocumentVersion ||
                !string.Equals(result.Path, _path, StringComparison.OrdinalIgnoreCase) || result.Source != BuildDocumentText()) return false;
            ClearDiagnostics();
            foreach (EditorDiagnostic diagnostic in result.Diagnostics)
                AddDiagnostic(diagnostic.LineIndex, diagnostic.StartColumn, diagnostic.EndColumn,
                    diagnostic.Code, diagnostic.Description, diagnostic.Severity);
            _diagnostics.Sort(CompareDiagnostics);
            _diagnosticsCacheDirty = false;
            _rustDiagnosticsStatus = null;
            RefreshRustDiagnosticsDetails();
            return true;
        }

        private void RefreshRustDiagnosticsDetails()
        {
            if (_messageDetailsActive && _messageDetailsShowsDiagnostics)
            {
                SetMessageDetailsDiagnostics(resetSelection: false);
                _messageDetailsText = _diagnostics.Count == 0
                    ? _rustDiagnosticsStatus ?? "No errors or warnings" : BuildDiagnosticsDetails();
                _messageDetailsSeverity = HasDiagnosticSeverity(EditorDiagnosticSeverity.Error, null)
                    ? EditorNotificationSeverity.Error : _diagnostics.Count > 0
                    ? EditorNotificationSeverity.Warning : EditorNotificationSeverity.Normal;
            }
        }

        private sealed class RustDiagnosticResult
        {
            public string Path;
            public string Source;
            public int Version;
            public List<EditorDiagnostic> Diagnostics;
        }

        private static List<EditorDiagnostic> ParseRustAnalyzerDiagnostics(JsonElement items, string source, bool compilerOnly)
        {
            var diagnostics = new List<EditorDiagnostic>();
            if (items.ValueKind != JsonValueKind.Array) return diagnostics;
            string[] lines = source.Split('\n');
            foreach (JsonElement item in items.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                string origin = RustJsonString(item, "source");
                if (compilerOnly && (string.IsNullOrEmpty(origin) || origin == "rust-analyzer")) continue;
                int severity = 1;
                if (item.TryGetProperty("severity", out JsonElement severityValue) &&
                    (severityValue.ValueKind != JsonValueKind.Number || !severityValue.TryGetInt32(out severity))) continue;
                if (severity != 1 && severity != 2) continue;
                string message = RustJsonString(item, "message");
                if (string.IsNullOrWhiteSpace(message) || !item.TryGetProperty("range", out JsonElement range) ||
                    range.ValueKind != JsonValueKind.Object ||
                    !TryReadRustDiagnosticPosition(range, "start", out int line, out int column) ||
                    !TryReadRustDiagnosticPosition(range, "end", out int endLine, out int endColumn) ||
                    line >= lines.Length || endLine < line || (endLine == line && endColumn < column)) continue;
                string code = string.Empty;
                if (item.TryGetProperty("code", out JsonElement codeValue))
                {
                    if (codeValue.ValueKind == JsonValueKind.String) code = codeValue.GetString();
                    else if (codeValue.ValueKind == JsonValueKind.Number) code = codeValue.GetRawText();
                }
                diagnostics.Add(new EditorDiagnostic(line, Math.Min(column, lines[line].Length),
                    endLine == line ? Math.Min(endColumn, lines[line].Length) : lines[line].Length,
                    code, message, severity == 1 ? EditorDiagnosticSeverity.Error : EditorDiagnosticSeverity.Warning));
            }
            return diagnostics;
        }

        private static bool TryReadRustDiagnosticPosition(JsonElement range, string name, out int line, out int column)
        {
            line = column = 0;
            return range.TryGetProperty(name, out JsonElement position) && position.ValueKind == JsonValueKind.Object &&
                position.TryGetProperty("line", out JsonElement lineValue) && lineValue.ValueKind == JsonValueKind.Number &&
                lineValue.TryGetInt32(out line) && line >= 0 &&
                position.TryGetProperty("character", out JsonElement columnValue) && columnValue.ValueKind == JsonValueKind.Number &&
                columnValue.TryGetInt32(out column) && column >= 0;
        }

        private sealed partial class RustAnalyzerClient
        {
            private int _documentEditorVersion;
            private RustDiagnosticResult _latestDiagnostics;
            private bool _supportsDiagnosticRequests;
            private List<EditorDiagnostic> _compilerDiagnostics = new List<EditorDiagnostic>();
            private List<EditorDiagnostic> _nativeDiagnostics = new List<EditorDiagnostic>();
            private bool _diagnosticsRefreshPending;

            public string FailureMessage { get { return _failure?.Message; } }

            public bool TakeDiagnosticsRefresh()
            {
                lock (_stateLock)
                {
                    bool pending = _diagnosticsRefreshPending;
                    _diagnosticsRefreshPending = false;
                    return pending;
                }
            }

            public RustDiagnosticResult TakeDiagnostics()
            {
                lock (_stateLock)
                {
                    var result = _latestDiagnostics;
                    _latestDiagnostics = null;
                    return result;
                }
            }

            private void ReceiveDiagnostics(JsonElement parameters)
            {
                if (parameters.ValueKind != JsonValueKind.Object) return;
                string uri = RustJsonString(parameters, "uri");
                lock (_stateLock)
                {
                    if (_documentUri == null || !Uri.TryCreate(uri, UriKind.Absolute, out Uri documentUri) || !documentUri.IsFile ||
                        !string.Equals(documentUri.LocalPath, new Uri(_documentUri).LocalPath, StringComparison.OrdinalIgnoreCase)) return;
                    if (parameters.TryGetProperty("version", out JsonElement version) && version.ValueKind != JsonValueKind.Null &&
                        (version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out int value) || value != _documentVersion)) return;
                    if (!parameters.TryGetProperty("diagnostics", out JsonElement items) || items.ValueKind != JsonValueKind.Array) return;
                    // Each notification replaces the complete set, including an empty
                    // set when an error has been fixed. Never retain a JsonDocument view.
                    _compilerDiagnostics = ParseRustAnalyzerDiagnostics(items, _documentText, compilerOnly: true);
                    if (_supportsDiagnosticRequests)
                        PublishCombinedDiagnostics();
                    else
                        PublishDiagnosticsSnapshot(ParseRustAnalyzerDiagnostics(items, _documentText, compilerOnly: false));
                }
            }

            private void PublishCombinedDiagnostics()
            {
                var diagnostics = new List<EditorDiagnostic>(_nativeDiagnostics);
                var seen = new HashSet<(int, int, int, string, string, EditorDiagnosticSeverity)>();
                foreach (EditorDiagnostic diagnostic in diagnostics)
                    seen.Add((diagnostic.LineIndex, diagnostic.StartColumn, diagnostic.EndColumn,
                        diagnostic.Code, diagnostic.Description, diagnostic.Severity));
                foreach (EditorDiagnostic diagnostic in _compilerDiagnostics)
                    if (seen.Add((diagnostic.LineIndex, diagnostic.StartColumn, diagnostic.EndColumn,
                        diagnostic.Code, diagnostic.Description, diagnostic.Severity))) diagnostics.Add(diagnostic);
                PublishDiagnosticsSnapshot(diagnostics);
            }

            private void PublishDiagnosticsSnapshot(List<EditorDiagnostic> diagnostics)
            {
                _latestDiagnostics = new RustDiagnosticResult
                {
                    Path = new Uri(_documentUri).LocalPath, Source = _documentText,
                    Version = _documentEditorVersion, Diagnostics = diagnostics
                };
            }

            public async Task UpdateDiagnosticsAsync(string path, string source, int version, bool saved, CancellationToken cancellation)
            {
                string uri = await SynchronizeDocumentAsync(path, source, version, cancellation).ConfigureAwait(false);
                if (saved) await NotifyAsync("textDocument/didSave", new { textDocument = new { uri } }, _shutdown).ConfigureAwait(false);
                if (!_supportsDiagnosticRequests) return;
                // Diagnostic requests must not hold the document gate while the
                // server works: completion and newer edits can proceed independently.
                for (int attempt = 0; ; attempt++)
                {
                    cancellation.ThrowIfCancellationRequested();
                    try
                    {
                        JsonElement report = await RequestAsync("textDocument/diagnostic",
                            new { textDocument = new { uri }, identifier = "rust-analyzer" }, cancellation).ConfigureAwait(false);
                        if (report.TryGetProperty("items", out JsonElement items))
                        {
                            var diagnostics = ParseRustAnalyzerDiagnostics(items, source, compilerOnly: false);
                            lock (_stateLock)
                            {
                                if (cancellation.IsCancellationRequested || _documentUri != uri ||
                                    _documentText != source || _documentEditorVersion != version) return;
                                _nativeDiagnostics = diagnostics;
                                PublishCombinedDiagnostics();
                            }
                        }
                        return;
                    }
                    catch (RustAnalyzerResponseException ex) when (ex.Code == -32802 || ex.Code == -32801 || ex.Code == -32800)
                    {
                        // ServerCancelled (-32802) is normal while Rust loads or
                        // edits invalidate analysis. Retry without restarting it.
                        if (attempt >= 2)
                        {
                            lock (_stateLock) _diagnosticsRefreshPending = true;
                            return;
                        }
                        await Task.Delay(150, cancellation).ConfigureAwait(false);
                    }
                }
            }

            private async Task<string> SynchronizeDocumentAsync(string path, string source, int editorVersion, CancellationToken cancellation)
            {
                await _initialization.Value.WaitAsync(cancellation).ConfigureAwait(false);
                await _documentGate.WaitAsync(cancellation).ConfigureAwait(false);
                try
                {
                    return await SendDocumentChangesAsync(path, source, editorVersion, cancellation).ConfigureAwait(false);
                }
                finally { _documentGate.Release(); }
            }

            private async Task<string> SendDocumentChangesAsync(string path, string source, int editorVersion, CancellationToken cancellation)
            {
                cancellation.ThrowIfCancellationRequested();
                if (_failure != null) throw new IOException("rust-analyzer connection closed", _failure);
                string uri = new Uri(System.IO.Path.GetFullPath(path)).AbsoluteUri;
                string previousUri;
                bool changed;
                int version;
                lock (_stateLock)
                {
                    previousUri = _documentUri;
                    if (previousUri == uri && editorVersion < _documentEditorVersion)
                        throw new OperationCanceledException("A newer Rust document is already synchronized", cancellation);
                    changed = previousUri != uri || _documentText != source || _documentEditorVersion != editorVersion;
                    if (changed)
                    {
                        _documentVersion++;
                        _latestDiagnostics = null;
                        // Compiler diagnostics describe the saved project. The server
                        // may omit an unchanged set after the next check, so retain it
                        // until a replacement push arrives (including an empty set).
                        if (previousUri != uri) _compilerDiagnostics.Clear();
                        _nativeDiagnostics.Clear();
                    }
                    version = _documentVersion;
                    _documentUri = uri;
                    _documentText = source;
                    _documentEditorVersion = editorVersion;
                }
                // Once the snapshot is committed, finish sending it even if another
                // keystroke cancels the caller. The next worker sends the next version.
                if (previousUri != uri)
                {
                    if (previousUri != null) await NotifyAsync("textDocument/didClose", new { textDocument = new { uri = previousUri } }, _shutdown).ConfigureAwait(false);
                    await NotifyAsync("textDocument/didOpen", new { textDocument = new { uri, languageId = "rust", version, text = source } }, _shutdown).ConfigureAwait(false);
                }
                else if (changed)
                    await NotifyAsync("textDocument/didChange", new { textDocument = new { uri, version }, contentChanges = new[] { new { text = source } } }, _shutdown).ConfigureAwait(false);
                return uri;
            }
        }
    }
}
