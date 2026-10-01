using System;
using System.Collections.Generic;
using System.Globalization;

namespace Core.DirFiles
{
    public sealed partial class TermXTEditor
    {
        private readonly List<EditorDiagnostic> _messageDetailsDiagnosticItems = new List<EditorDiagnostic>();
        private int _messageDetailsSelectedIndex = -1;

        private void SetMessageDetailsDiagnostics(bool resetSelection)
        {
            EditorDiagnostic selected = !resetSelection && _messageDetailsSelectedIndex >= 0 &&
                _messageDetailsSelectedIndex < _messageDetailsDiagnosticItems.Count
                ? _messageDetailsDiagnosticItems[_messageDetailsSelectedIndex] : null;
            _messageDetailsDiagnosticItems.Clear();
            if (_messageDetailsShowsDiagnostics) _messageDetailsDiagnosticItems.AddRange(_diagnostics);
            if (_messageDetailsDiagnosticItems.Count == 0)
            {
                _messageDetailsSelectedIndex = -1;
                return;
            }
            int previousIndex = selected == null ? -1 : _messageDetailsDiagnosticItems.FindIndex(item =>
                item.LineIndex == selected.LineIndex && item.StartColumn == selected.StartColumn &&
                item.Code == selected.Code && item.Description == selected.Description && item.Severity == selected.Severity);
            _messageDetailsSelectedIndex = previousIndex >= 0 ? previousIndex : resetSelection ? 0 :
                ClampValue(_messageDetailsSelectedIndex, 0, _messageDetailsDiagnosticItems.Count - 1);
        }

        private static string FormatDiagnosticDetailsEntry(EditorDiagnostic diagnostic, int index, int count)
        {
            string total = count.ToString(CultureInfo.InvariantCulture);
            return "[" + DiagnosticSeverityName(diagnostic.Severity).ToUpperInvariant() + "] " +
                (index + 1).ToString(CultureInfo.InvariantCulture).PadLeft(total.Length) + "/" + total +
                "  " + FormatDiagnosticListLocation(diagnostic);
        }

        private List<string> BuildMessageDetailsRows(int width, out List<int> diagnosticIndexes)
        {
            diagnosticIndexes = new List<int>();
            if (!_messageDetailsShowsDiagnostics || _messageDetailsDiagnosticItems.Count == 0)
            {
                List<string> message = WrapMessageText(_messageDetailsText, width);
                foreach (string line in message) diagnosticIndexes.Add(-1);
                return message;
            }

            var rows = new List<string>();
            for (int index = 0; index < _messageDetailsDiagnosticItems.Count; index++)
            {
                // Keep the diagnostic identity on every wrapped row. Descriptions
                // can contain newlines and text that looks like another list entry.
                foreach (string line in WrapMessageText(FormatDiagnosticDetailsEntry(
                    _messageDetailsDiagnosticItems[index], index, _messageDetailsDiagnosticItems.Count), width))
                {
                    rows.Add(line);
                    diagnosticIndexes.Add(index);
                }
            }
            return rows;
        }

        private bool SelectMessageDiagnostic(int index)
        {
            if (!_messageDetailsShowsDiagnostics || _messageDetailsDiagnosticItems.Count == 0) return false;
            _messageDetailsSelectedIndex = ClampValue(index, 0, _messageDetailsDiagnosticItems.Count - 1);
            (int width, int height) = WindowSize();
            BuildMessageDetailsRows(MessageDetailsContentWidth(width), out List<int> diagnosticIndexes);
            EnsureDiagnosticDetailsSelectionVisible(diagnosticIndexes, MessageDetailsContentRows(height), revealStart: true);
            return true;
        }

        private void EnsureDiagnosticDetailsSelectionVisible(List<int> diagnosticIndexes, int visibleRows, bool revealStart)
        {
            if (_messageDetailsSelectedIndex < 0) return;
            int first = diagnosticIndexes.IndexOf(_messageDetailsSelectedIndex);
            int last = diagnosticIndexes.LastIndexOf(_messageDetailsSelectedIndex);
            if (first < 0) return;
            if (revealStart)
            {
                if (first < _messageDetailsScrollOffset || last >= _messageDetailsScrollOffset + visibleRows)
                    _messageDetailsScrollOffset = last - first + 1 > visibleRows ? first :
                        Math.Max(0, Math.Min(first, last - visibleRows + 1));
            }
            else if (last < _messageDetailsScrollOffset || first >= _messageDetailsScrollOffset + visibleRows)
                _messageDetailsScrollOffset = first;
            _messageDetailsScrollOffset = ClampValue(_messageDetailsScrollOffset, 0,
                Math.Max(0, diagnosticIndexes.Count - visibleRows));
        }

        private void SelectVisibleMessageDiagnostic(List<int> diagnosticIndexes, int visibleRows)
        {
            if (_messageDetailsSelectedIndex < 0) return;
            int first = diagnosticIndexes.IndexOf(_messageDetailsSelectedIndex);
            int last = diagnosticIndexes.LastIndexOf(_messageDetailsSelectedIndex);
            if (last < _messageDetailsScrollOffset || first >= _messageDetailsScrollOffset + visibleRows)
                _messageDetailsSelectedIndex = diagnosticIndexes[_messageDetailsScrollOffset];
        }

        private void JumpToMessageDiagnostic()
        {
            if (!_messageDetailsShowsDiagnostics || _messageDetailsSelectedIndex < 0 ||
                _messageDetailsSelectedIndex >= _messageDetailsDiagnosticItems.Count) return;
            EditorDiagnostic diagnostic = _messageDetailsDiagnosticItems[_messageDetailsSelectedIndex];
            CloseMessageDetails();
            MoveCursorToDiagnostic(diagnostic, _messageDetailsSelectedIndex + 1, _messageDetailsDiagnosticItems.Count);
        }

        private void CloseMessageDetails()
        {
            _messageDetailsActive = false;
            _lastWidth = -1;
            _lastHeight = -1;
        }
    }
}
