using System;
using System.Collections.Generic;

namespace Core.DirFiles
{
    public sealed partial class TermXTEditor
    {
        private readonly Dictionary<int, (string Text, List<Token> Tokens)> _highlightTokenCache =
            new Dictionary<int, (string, List<Token>)>();
        private EditorRowState[] _renderedEditorRows = Array.Empty<EditorRowState>();
        private int _renderedScrollTop;
        private int _renderedEditorWidth;
        private int _renderedEditorTextWidth;
        private bool _renderedEditorOverlay;

        private List<Token> GetHighlightTokens(string line, int lineIndex)
        {
            if (_highlightTokenCache.TryGetValue(lineIndex, out var cached) &&
                ReferenceEquals(cached.Text, line))
            {
                return cached.Tokens;
            }

            // Keep navigation and wrapped rows from tokenizing the same line repeatedly,
            // without retaining tokens for the entire document.
            if (_highlightTokenCache.Count >= 256)
                _highlightTokenCache.Clear();

            List<Token> tokens = Tokenize(line, lineIndex);
            _highlightTokenCache[lineIndex] = (line, tokens);
            return tokens;
        }

        private void RenderEditorRows(int textTop, int textRows, int numberWidth, int textLeft, int textWidth, int width)
        {
            bool overlay = _completionActive || _messageDetailsActive;
            int scrollDelta = _scrollTop - _renderedScrollTop;
            bool reuseRows = _renderedEditorRows.Length == textRows &&
                _renderedEditorWidth == width && _renderedEditorTextWidth == textWidth &&
                !_renderedEditorOverlay && !overlay &&
                Math.Abs(scrollDelta) < textRows;

            if (reuseRows && scrollDelta != 0)
            {
                // Scroll only the document area, preserving the header and footer.
                // Reset margins immediately; subsequent drawing uses absolute positions.
                _frame.Append(Reset).Append(CSI).Append(textTop + 1).Append(';')
                    .Append(textTop + textRows).Append('r')
                    .Append(CSI).Append(Math.Abs(scrollDelta)).Append(scrollDelta > 0 ? 'S' : 'T')
                    .Append(CSI).Append('r');
            }

            var renderedRows = new EditorRowState[textRows];
            for (int row = 0; row < textRows; row++)
            {
                EditorRowState state = GetEditorRowState(_scrollTop + row, textWidth);
                renderedRows[row] = state;
                int previousRow = row + scrollDelta;
                if (reuseRows && previousRow >= 0 && previousRow < textRows &&
                    state.Matches(_renderedEditorRows[previousRow]))
                {
                    continue;
                }

                RenderEditorRow(_scrollTop + row, textTop + row, numberWidth, textLeft, textWidth, width);
            }

            _renderedEditorRows = renderedRows;
            _renderedScrollTop = _scrollTop;
            _renderedEditorWidth = width;
            _renderedEditorTextWidth = textWidth;
            _renderedEditorOverlay = overlay;
        }

        private EditorRowState GetEditorRowState(int visualRow, int textWidth)
        {
            if (!TryGetVisualRow(visualRow, textWidth, out VisualRow rowInfo))
                return default;

            bool diagnostic = TryGetDiagnosticSeverityForLine(rowInfo.LineIndex, out EditorDiagnosticSeverity severity);
            TryGetSelectionSpanForLine(rowInfo.LineIndex, out int selectionStart, out int selectionEnd);
            return new EditorRowState
            {
                Text = _lines[rowInfo.LineIndex],
                LineIndex = rowInfo.LineIndex,
                StartColumn = rowInfo.StartColumn,
                Current = rowInfo.LineIndex == _cursorLine,
                Diagnostic = diagnostic ? (int)severity + 1 : 0,
                SelectionStart = selectionStart,
                SelectionEnd = selectionEnd
            };
        }

        private struct EditorRowState
        {
            public string Text;
            public int LineIndex;
            public int StartColumn;
            public bool Current;
            public int Diagnostic;
            public int SelectionStart;
            public int SelectionEnd;

            public bool Matches(EditorRowState other)
            {
                return ReferenceEquals(Text, other.Text) && LineIndex == other.LineIndex &&
                    StartColumn == other.StartColumn && Current == other.Current &&
                    Diagnostic == other.Diagnostic && SelectionStart == other.SelectionStart &&
                    SelectionEnd == other.SelectionEnd;
            }
        }
    }
}
