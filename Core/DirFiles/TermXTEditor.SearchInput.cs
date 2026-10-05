using System;

namespace Core.DirFiles
{
    public sealed partial class TermXTEditor
    {
        private void InsertSearchText(string text)
        {
            if (_searchInput.TryInsert(text, MaxSearchTextLength)) return;
            Status("Search text limit reached", error: true);
            BottomStatus("Search text is limited to " + MaxSearchTextLength + " characters.", error: true);
        }

        // Both the document search and file-search prompts edit at a caret and
        // scroll their text horizontally to keep that caret visible.
        private sealed class SearchTextInput
        {
            public string Text { get; private set; } = string.Empty;
            public int Cursor { get; private set; }
            private int _scrollLeft;

            public void Clear()
            {
                Text = string.Empty;
                Cursor = _scrollLeft = 0;
            }

            public bool TryInsert(string text, int maxLength)
            {
                text = EscapeText(text);
                if (text.Length > maxLength - Text.Length) return false;
                Text = Text.Insert(Cursor, text);
                Cursor += text.Length;
                return true;
            }

            public bool HandleEditingKey(ConsoleKeyInfo key)
            {
                bool control = (key.Modifiers & ConsoleModifiers.Control) != 0;
                switch (key.Key)
                {
                    case ConsoleKey.LeftArrow:
                        if (control)
                        {
                            while (Cursor > 0 && char.IsWhiteSpace(Text[Cursor - 1])) Cursor = PreviousPosition(Cursor);
                            while (Cursor > 0 && !char.IsWhiteSpace(Text[Cursor - 1])) Cursor = PreviousPosition(Cursor);
                        }
                        else Cursor = PreviousPosition(Cursor);
                        return true;
                    case ConsoleKey.RightArrow:
                        if (control)
                        {
                            while (Cursor < Text.Length && !char.IsWhiteSpace(Text[Cursor])) Cursor = NextPosition(Cursor);
                            while (Cursor < Text.Length && char.IsWhiteSpace(Text[Cursor])) Cursor = NextPosition(Cursor);
                        }
                        else Cursor = NextPosition(Cursor);
                        return true;
                    case ConsoleKey.Home: Cursor = 0; return true;
                    case ConsoleKey.End: Cursor = Text.Length; return true;
                    case ConsoleKey.Backspace:
                        int previous = PreviousPosition(Cursor);
                        Text = Text.Remove(previous, Cursor - previous);
                        Cursor = previous;
                        return true;
                    case ConsoleKey.Delete:
                        Text = Text.Remove(Cursor, NextPosition(Cursor) - Cursor);
                        return true;
                    default: return false;
                }
            }

            public string VisibleText(int width, out int cursorColumn)
            {
                width = Math.Max(1, width);
                _scrollLeft = Math.Min(_scrollLeft, Math.Max(0, Text.Length - width + 1));
                if (Cursor < _scrollLeft) _scrollLeft = Cursor;
                else if (Cursor >= _scrollLeft + width) _scrollLeft = Cursor - width + 1;
                if (_scrollLeft > 0 && _scrollLeft < Text.Length && char.IsSurrogatePair(Text, _scrollLeft - 1))
                    _scrollLeft++;
                cursorColumn = Cursor - _scrollLeft;
                int length = Math.Min(width, Text.Length - _scrollLeft);
                if (length > 0 && _scrollLeft + length < Text.Length && char.IsSurrogatePair(Text, _scrollLeft + length - 1))
                    length--;
                return Text.Substring(_scrollLeft, length);
            }

            private int PreviousPosition(int position)
            {
                if (position == 0) return 0;
                return position > 1 && char.IsSurrogatePair(Text, position - 2) ? position - 2 : position - 1;
            }

            private int NextPosition(int position)
            {
                if (position == Text.Length) return position;
                return char.IsSurrogatePair(Text, position) ? position + 2 : position + 1;
            }
        }
    }
}
