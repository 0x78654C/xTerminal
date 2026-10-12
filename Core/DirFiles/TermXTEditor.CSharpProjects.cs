using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Core.DirFiles
{
    public sealed partial class TermXTEditor
    {
        private readonly CSharpProjectWorkspace _csharpWorkspace = new CSharpProjectWorkspace();
        private string _csharpWorkspacePath;

        private string ResolveEditorOpenPath(string path, bool openAsText = false)
        {
            if (openAsText)
            {
                _lastExplorerDirectory = Path.GetDirectoryName(path);
                return path;
            }
            if (!CSharpProjectWorkspace.IsWorkspacePath(path)) return path;
            // Resolve before changing editor state, so a malformed solution leaves the buffer intact.
            string sourcePath = CSharpProjectWorkspace.GetEntryFile(path);
            _csharpWorkspacePath = path;
            _lastExplorerDirectory = Path.GetDirectoryName(path);
            _csharpWorkspace.Invalidate();
            return sourcePath;
        }

        private CSharpProjectWorkspace.Snapshot GetCSharpProjectSnapshot()
        {
            return _csharpWorkspace.GetSnapshot(_path, _csharpWorkspacePath, CancellationToken.None);
        }

        private CSharpParseOptions GetCSharpParseOptions(SourceCodeKind sourceKind)
        {
            return sourceKind == SourceCodeKind.Regular
                ? GetCSharpProjectSnapshot()?.ParseOptions ?? CreateCSharpParseOptions(sourceKind)
                : CreateCSharpParseOptions(sourceKind);
        }

        private void AddCSharpProjectCompletions(string prefix, List<CompletionItem> items, HashSet<string> labels)
        {
            try
            {
                var snapshot = GetCSharpProjectSnapshot();
                if (snapshot == null) return;
                string text = BuildDocumentText();
                SyntaxTree tree = CSharpSyntaxTree.ParseText(text, snapshot.ParseOptions, _path);
                SemanticModel model = snapshot.WithActiveTree(tree, text).GetSemanticModel(tree);
                foreach (ISymbol symbol in model.LookupSymbols(GetDocumentPosition(_cursorLine, _cursorCol)))
                    AddCSharpSymbolCompletion(items, labels, symbol, prefix, "project", 5, memberAccess: false, staticContext: false);
            }
            catch (Exception ex)
            {
                Status("C# project IntelliSense unavailable: " + ex.Message, error: true);
            }
        }

        private bool TryExecuteProjectCommand(string command)
        {
            if (command.Equals("project edit", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string path = CSharpProjectWorkspace.IsWorkspacePath(_path) ? _path : GetCSharpProjectSnapshot()?.ProjectPath;
                    if (!CSharpProjectWorkspace.IsWorkspacePath(_path) && path != null && File.Exists(_csharpWorkspacePath) &&
                        CSharpProjectWorkspace.ReadProjects(_csharpWorkspacePath).Exists(project => string.Equals(project, path, StringComparison.OrdinalIgnoreCase)))
                        path = _csharpWorkspacePath;
                    if (string.IsNullOrEmpty(path)) Status("No current C# project or solution to edit");
                    else OpenEditorFile(path, openAsText: true);
                }
                catch (Exception ex) { Status("Open failed: " + ex.Message, error: true); }
                _mode = Mode.Normal;
                return true;
            }
            if (command.Equals("project", StringComparison.OrdinalIgnoreCase) || command.Equals("project reload", StringComparison.OrdinalIgnoreCase))
            {
                _csharpWorkspace.Invalidate();
                DismissCompletion();
                InvalidateDiagnosticsCache(delayCSharpSemanticDiagnostics: false);
                try
                {
                    var snapshot = GetCSharpProjectSnapshot();
                    Status(snapshot == null ? "Standalone file (no owning C# project)" : "Project: " + Path.GetFileName(snapshot.ProjectPath));
                    if (snapshot != null) BottomStatus(snapshot.ProjectPath);
                }
                catch (Exception ex) { Status("Project load failed: " + ex.Message, error: true); }
                _mode = Mode.Normal;
                return true;
            }
            if (!command.StartsWith("e ", StringComparison.OrdinalIgnoreCase) && !command.StartsWith("open ", StringComparison.OrdinalIgnoreCase)) return false;
            string argument = command.Substring(command.IndexOf(' ') + 1).Trim();
            bool openAsText = argument.Equals("-raw", StringComparison.OrdinalIgnoreCase) ||
                argument.StartsWith("-raw ", StringComparison.OrdinalIgnoreCase);
            if (openAsText) argument = argument.Substring(4).TrimStart();
            argument = argument.Trim('"');
            if (openAsText && argument.Length == 0)
            {
                Status("Use :e -raw <file> to edit a file as text", error: true);
                _mode = Mode.Normal;
                return true;
            }
            if (argument.Length == 0) return false;
            try { OpenEditorFile(Path.GetFullPath(Path.Combine(GetExplorerStartDirectory(), argument)), openAsText); }
            catch (Exception ex) { Status("Open failed: " + ex.Message, error: true); }
            _mode = Mode.Normal;
            return true;
        }
    }
}
