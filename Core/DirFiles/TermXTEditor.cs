using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace Core.DirFiles
{
    public enum TermXTEditorSyntax
    {
        TermXt,
        CSharp
    }

    [SupportedOSPlatform("windows")]
    public sealed class TermXTEditor
    {
        private const string CSI = "\x1b[";
        private const string AltScreen = "\x1b[?1049h";
        private const string NormalScreen = "\x1b[?1049l";
        private const string HideCursor = "\x1b[?25l";
        private const string ShowCursor = "\x1b[?25h";
        private const string Reset = "\x1b[0m";
        private const string ClearEol = "\x1b[K";
        private const string ClearScreen = "\x1b[2J";

        private const int CTitle = 45;
        private const int CTitleDim = 250;
        private const int CStatusFg = 232;
        private const int CStatusBg = 45;
        private const int CStatusInsertFg = 231;
        private const int CStatusInsertBg = 34;
        private const int CStatusCommandFg = 232;
        private const int CStatusCommandBg = 214;
        private const int CStatusSearchFg = 231;
        private const int CStatusSearchBg = 99;
        private const int CNormal = 252;
        private const int CDim = 244;
        private const int CMuted = 238;
        private const int CLineNo = 240;
        private const int CCurrentLineNo = 45;
        private const int CKeyword = 81;
        private const int CFlow = 111;
        private const int CFunction = 214;
        private const int CString = 150;
        private const int CVariable = 219;
        private const int CNumber = 209;
        private const int COperator = 220;
        private const int CComment = 108;
        private const int CError = 203;
        private const int CSearch = 227;
        private const int CPreprocessor = 183;

        private static readonly HashSet<string> s_flowKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "if", "elif", "else", "end", "loop", "while", "each", "func", "try", "catch",
            "break", "continue", "return", "exit"
        };

        private static readonly HashSet<string> s_commandKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "set", "print", "run", "capture", "input", "read", "write", "append", "wait", "call"
        };

        private static readonly HashSet<string> s_functionKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "eval", "upper", "lower", "len", "substr", "replace", "trim", "lines"
        };

        private static readonly HashSet<string> s_operatorWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "in", "not", "contains", "startswith", "endswith", "and", "or"
        };

        private static readonly HashSet<string> s_csharpFlowKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "if", "else", "switch", "case", "default", "for", "foreach", "while", "do",
            "break", "continue", "return", "goto", "try", "catch", "finally", "throw",
            "yield", "when"
        };

        private static readonly HashSet<string> s_csharpKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "abstract", "as", "async", "await", "base", "checked", "class", "const",
            "delegate", "enum", "event", "explicit", "extern", "file", "fixed", "global",
            "implicit", "in", "interface", "internal", "is", "lock", "namespace", "new",
            "operator", "out", "override", "params", "partial", "private", "protected",
            "public", "readonly", "record", "ref", "required", "sealed", "sizeof",
            "stackalloc", "static", "struct", "this", "typeof", "unchecked", "unsafe",
            "using", "virtual", "volatile", "where", "with"
        };

        private static readonly HashSet<string> s_csharpTypeKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "bool", "byte", "char", "decimal", "double", "dynamic", "float", "int",
            "long", "nint", "nuint", "object", "sbyte", "short", "string", "uint",
            "ulong", "ushort", "var", "void"
        };

        private static readonly HashSet<string> s_csharpLiteralKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "default", "false", "null", "true"
        };

        private readonly string _path;
        private readonly List<string> _lines = new List<string>();
        private readonly Stack<Snapshot> _undo = new Stack<Snapshot>();
        private readonly Stack<Snapshot> _redo = new Stack<Snapshot>();
        private readonly StringBuilder _frame = new StringBuilder(1 << 16);
        private string[] _savedLines = Array.Empty<string>();

        private Mode _mode = Mode.Normal;
        private bool _running = true;
        private bool _dirty;
        private bool _insertUndoStarted;
        private int _cursorLine;
        private int _cursorCol;
        private int _scrollTop;
        private int _scrollLeft;
        private int _lastWidth = -1;
        private int _lastHeight = -1;
        private bool _pendingDelete;
        private string _commandText = string.Empty;
        private string _searchText = string.Empty;
        private string _lastSearch = string.Empty;
        private string _status = "NORMAL";
        private DateTime _statusUntil = DateTime.MinValue;
        private string _bottomStatus = string.Empty;
        private DateTime _bottomStatusUntil = DateTime.MinValue;
        private bool _bottomStatusError;
        private string _lineClipboard = string.Empty;
        private bool _hasLineClipboard;
        private TermXTEditorSyntax _syntax;

        public TermXTEditor(string path)
            : this(path, DetectSyntaxFromPath(path))
        {
        }

        public TermXTEditor(string path, TermXTEditorSyntax syntax)
        {
            _path = Path.GetFullPath(path);
            _syntax = syntax;
            LoadFile();
        }

        public static TermXTEditorSyntax DetectSyntaxFromPath(string path)
        {
            string extension = Path.GetExtension(path);
            if (string.Equals(extension, ".cs", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".csx", StringComparison.OrdinalIgnoreCase))
            {
                return TermXTEditorSyntax.CSharp;
            }

            return TermXTEditorSyntax.TermXt;
        }

        public static bool TryParseSyntax(string value, out TermXTEditorSyntax syntax)
        {
            syntax = TermXTEditorSyntax.TermXt;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            switch (value.Trim().ToLowerInvariant())
            {
                case "xt":
                case "termxt":
                case "xtermxt":
                case "script":
                    syntax = TermXTEditorSyntax.TermXt;
                    return true;
                case "cs":
                case "c#":
                case "csharp":
                case "c-sharp":
                    syntax = TermXTEditorSyntax.CSharp;
                    return true;
                default:
                    return false;
            }
        }

        public static string SyntaxDisplayName(TermXTEditorSyntax syntax)
        {
            return syntax == TermXTEditorSyntax.CSharp ? "C#" : "TermXT";
        }

        public void Run()
        {
            bool oldTreatControlCAsInput = Console.TreatControlCAsInput;
            bool oldCursorVisible = Console.CursorVisible;

            Console.OutputEncoding = Encoding.UTF8;
            Console.TreatControlCAsInput = true;
            Console.Write(AltScreen + HideCursor + ClearScreen);

            try
            {
                while (_running)
                {
                    Render();

                    if (WaitForKey(out ConsoleKeyInfo key))
                    {
                        Console.Write(HideCursor);
                        HandleKey(key);
                    }
                }
            }
            finally
            {
                try
                {
                    Console.Write(Reset + ShowCursor + NormalScreen);
                    Console.CursorVisible = oldCursorVisible;
                    Console.TreatControlCAsInput = oldTreatControlCAsInput;
                }
                catch
                {
                }
            }
        }

        private bool WaitForKey(out ConsoleKeyInfo key)
        {
            key = default;

            while (_running)
            {
                if (Console.KeyAvailable)
                {
                    key = Console.ReadKey(intercept: true);
                    return true;
                }

                (int width, int height) = WindowSize();
                if (width != _lastWidth || height != _lastHeight)
                    return false;

                if (ClearExpiredMessages())
                    return false;

                Thread.Sleep(30);
            }

            return false;
        }

        private bool ClearExpiredMessages()
        {
            bool redraw = false;
            DateTime now = DateTime.UtcNow;

            if (_statusUntil != DateTime.MinValue && now > _statusUntil)
            {
                _statusUntil = DateTime.MinValue;
                redraw = true;
            }

            if (_bottomStatusUntil != DateTime.MinValue && now > _bottomStatusUntil)
            {
                _bottomStatus = string.Empty;
                _bottomStatusUntil = DateTime.MinValue;
                _bottomStatusError = false;
                redraw = true;
            }

            return redraw;
        }

        private void LoadFile()
        {
            _lines.Clear();

            if (File.Exists(_path))
                _lines.AddRange(File.ReadAllLines(_path, Encoding.UTF8));

            if (_lines.Count == 0)
                _lines.Add(string.Empty);

            _cursorLine = 0;
            _cursorCol = 0;
            _scrollTop = 0;
            _scrollLeft = 0;
            _savedLines = _lines.ToArray();
            _dirty = false;
            _insertUndoStarted = false;
            _undo.Clear();
            _redo.Clear();
        }

        private void Render()
        {
            (int width, int height) = WindowSize();

            if (width != _lastWidth || height != _lastHeight)
            {
                _lastWidth = width;
                _lastHeight = height;
                Console.Write(HideCursor + ClearScreen);
            }
            else
            {
                Console.Write(HideCursor);
            }

            if (width < 44 || height < 10)
            {
                Console.Write(At(0, 0) + F(CError) + "Terminal too small. Resize to at least 44 x 10." + Reset + ClearEol);
                return;
            }

            int headerRows = 2;
            int footerRows = 2;
            int textTop = headerRows;
            int textRows = Math.Max(1, height - headerRows - footerRows);
            int statusRow = height - 2;
            int commandRow = height - 1;
            int numberWidth = Math.Max(4, _lines.Count.ToString().Length + 2);
            int textLeft = numberWidth + 1;
            int textWidth = Math.Max(1, width - textLeft);

            ClampCursor();
            AdjustScroll(textRows, textWidth);

            _frame.Clear();
            RenderHeader(width);

            for (int row = 0; row < textRows; row++)
                RenderEditorRow(_scrollTop + row, textTop + row, numberWidth, textLeft, textWidth, width);

            RenderStatus(statusRow, width);
            RenderCommandLine(commandRow, width);

            Console.Write(_frame.ToString());
            PlaceCursor(textTop, textLeft, textRows, textWidth);
        }

        private void RenderHeader(int width)
        {
            string name = Path.GetFileName(_path);
            if (string.IsNullOrWhiteSpace(name))
                name = _path;

            string dirty = _dirty ? " [+]" : "";
            string left = " TermXT Editor ";
            string middle = " " + name + " [" + SyntaxDisplayName(_syntax) + "]" + dirty;
            string right = " " + (_cursorLine + 1) + ":" + (_cursorCol + 1) + " ";

            _frame.Append(At(0, 0))
                .Append(B(233)).Append(F(CTitle)).Append(Bold()).Append(Clip(left, width)).Append(Reset);

            int used = VisibleLength(left);
            int middleWidth = Math.Max(0, width - used - VisibleLength(right));
            _frame.Append(B(233)).Append(F(CTitleDim)).Append(Clip(middle, middleWidth).PadRight(middleWidth));
            _frame.Append(F(CTitle)).Append(Bold()).Append(Clip(right, Math.Max(0, width - used - middleWidth))).Append(Reset).Append(ClearEol);

            string help;
            switch (_mode)
            {
                case Mode.Insert:
                    help = " INSERT  Esc normal | Ctrl+Z undo | Ctrl+Y redo | Enter newline | Tab indent | arrows/Home/End move";
                    break;
                case Mode.Command:
                    help = " COMMAND  w save | q quit | 42 or goto 42 go to line | syntax xt|cs | Esc cancel";
                    break;
                case Mode.Search:
                    help = " SEARCH  Type text then Enter | Backspace edit | Esc cancel";
                    break;
                default:
                    help = " NORMAL  i or Insert edit | h/j/k/l move | dd delete | x char | z/Ctrl+Z undo | Ctrl+Y redo | / search | : command";
                    break;
            }

            _frame.Append(At(0, 1)).Append(F(CMuted)).Append(Clip(help, width)).Append(Reset).Append(ClearEol);
        }

        private void RenderEditorRow(int visualRow, int y, int numberWidth, int textLeft, int textWidth, int width)
        {
            _frame.Append(At(0, y));

            if (!TryGetVisualRow(visualRow, textWidth, out VisualRow rowInfo))
            {
                _frame.Append(F(CMuted)).Append("~".PadLeft(numberWidth)).Append(Reset).Append(' ').Append(ClearEol);
                return;
            }

            bool current = rowInfo.LineIndex == _cursorLine;
            string lineNo = rowInfo.WrapIndex == 0
                ? (rowInfo.LineIndex + 1).ToString().PadLeft(numberWidth - 1) + " "
                : "+".PadLeft(numberWidth - 1) + " ";
            _frame.Append(F(current ? CCurrentLineNo : CLineNo)).Append(lineNo).Append(Reset).Append(' ');

            string line = _lines[rowInfo.LineIndex];
            string rendered = BuildHighlightedLine(line, rowInfo.StartColumn, textWidth);
            _frame.Append(rendered);

            int used = numberWidth + 1 + Math.Min(textWidth, Math.Max(0, line.Length - rowInfo.StartColumn));
            if (used < width)
                _frame.Append(ClearEol);
        }

        private void RenderStatus(int row, int width)
        {
            string mode = _mode.ToString().ToUpperInvariant();
            string message = DateTime.UtcNow <= _statusUntil ? _status : DefaultStatus();
            string text = " " + mode.PadRight(7) + " " + message;
            GetStatusColors(out int fg, out int bg);

            _frame.Append(At(0, row))
                .Append(B(bg)).Append(F(fg)).Append(Clip(text, width).PadRight(width))
                .Append(Reset);
        }

        private void GetStatusColors(out int fg, out int bg)
        {
            switch (_mode)
            {
                case Mode.Insert:
                    fg = CStatusInsertFg;
                    bg = CStatusInsertBg;
                    break;
                case Mode.Command:
                    fg = CStatusCommandFg;
                    bg = CStatusCommandBg;
                    break;
                case Mode.Search:
                    fg = CStatusSearchFg;
                    bg = CStatusSearchBg;
                    break;
                default:
                    fg = CStatusFg;
                    bg = CStatusBg;
                    break;
            }
        }

        private void RenderCommandLine(int row, int width)
        {
            _frame.Append(At(0, row));

            if (_mode == Mode.Command)
            {
                _frame.Append(F(CSearch)).Append(":").Append(Clip(_commandText, width - 1)).Append(Reset).Append(ClearEol);
                return;
            }

            if (_mode == Mode.Search)
            {
                _frame.Append(F(CSearch)).Append("/").Append(Clip(_searchText, width - 1)).Append(Reset).Append(ClearEol);
                return;
            }

            if (DateTime.UtcNow <= _bottomStatusUntil && !string.IsNullOrWhiteSpace(_bottomStatus))
            {
                int bg = _bottomStatusError ? CError : CStatusBg;
                int fg = _bottomStatusError ? 231 : CStatusFg;
                string message = " " + _bottomStatus;
                _frame.Append(B(bg)).Append(F(fg)).Append(Clip(message, width).PadRight(width)).Append(Reset);
                return;
            }

            string tail = " " + _path;
            _frame.Append(F(CDim)).Append(Clip(tail, width)).Append(Reset).Append(ClearEol);
        }

        private void PlaceCursor(int textTop, int textLeft, int textRows, int textWidth)
        {
            int x;
            int y;

            if (_mode == Mode.Command)
            {
                y = Math.Max(0, Console.WindowHeight - 1);
                x = Math.Min(Console.WindowWidth - 1, _commandText.Length + 1);
            }
            else if (_mode == Mode.Search)
            {
                y = Math.Max(0, Console.WindowHeight - 1);
                x = Math.Min(Console.WindowWidth - 1, _searchText.Length + 1);
            }
            else
            {
                int cursorVisualRow = GetCursorVisualRow(textWidth);
                int visualLine = cursorVisualRow - _scrollTop;
                int wrapIndex = GetCursorWrapIndex(textWidth);
                int visualCol = _cursorCol - (wrapIndex * textWidth);
                visualCol = Math.Max(0, Math.Min(textWidth - 1, visualCol));

                if (visualLine < 0 || visualLine >= textRows || visualCol < 0 || visualCol >= textWidth)
                {
                    Console.Write(HideCursor);
                    return;
                }

                y = textTop + visualLine;
                x = textLeft + visualCol;
                x = Math.Min(Console.WindowWidth - 1, Math.Max(0, x));
            }

            Console.Write(At(x, y) + ShowCursor);
        }

        private void HandleKey(ConsoleKeyInfo key)
        {
            if ((key.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control)
            {
                if (key.Key == ConsoleKey.Z)
                {
                    Undo();
                    return;
                }

                if (key.Key == ConsoleKey.Y)
                {
                    Redo();
                    return;
                }
            }

            switch (_mode)
            {
                case Mode.Insert:
                    HandleInsertKey(key);
                    break;
                case Mode.Command:
                    HandleCommandKey(key);
                    break;
                case Mode.Search:
                    HandleSearchKey(key);
                    break;
                default:
                    HandleNormalKey(key);
                    break;
            }
        }

        private void HandleNormalKey(ConsoleKeyInfo key)
        {
            if (_pendingDelete && key.KeyChar != 'd')
                _pendingDelete = false;

            switch (key.Key)
            {
                case ConsoleKey.LeftArrow:
                    MoveLeft();
                    break;
                case ConsoleKey.RightArrow:
                    MoveRight();
                    break;
                case ConsoleKey.UpArrow:
                    MoveUp();
                    break;
                case ConsoleKey.DownArrow:
                    MoveDown();
                    break;
                case ConsoleKey.PageUp:
                    MoveVertical(-PageSize());
                    break;
                case ConsoleKey.PageDown:
                    MoveVertical(PageSize());
                    break;
                case ConsoleKey.Home:
                    _cursorCol = 0;
                    break;
                case ConsoleKey.End:
                    _cursorCol = CurrentLine().Length;
                    break;
                case ConsoleKey.Insert:
                    EnterInsertMode();
                    break;
                case ConsoleKey.Escape:
                    _pendingDelete = false;
                    Status("NORMAL");
                    break;
                default:
                    HandleNormalChar(key);
                    break;
            }
        }

        private void HandleNormalChar(ConsoleKeyInfo key)
        {
            switch (key.KeyChar)
            {
                case 'h':
                    MoveLeft();
                    break;
                case 'j':
                    MoveDown();
                    break;
                case 'k':
                    MoveUp();
                    break;
                case 'l':
                    MoveRight();
                    break;
                case '0':
                    _cursorCol = 0;
                    break;
                case '$':
                    _cursorCol = CurrentLine().Length;
                    break;
                case 'i':
                    EnterInsertMode();
                    break;
                case 'x':
                    DeleteCharUnderCursor();
                    break;
                case 'd':
                    if (_pendingDelete)
                    {
                        DeleteCurrentLine();
                        _pendingDelete = false;
                    }
                    else
                    {
                        _pendingDelete = true;
                        Status("d");
                    }
                    break;
                case 'p':
                    PasteLineBelow();
                    break;
                case 'u':
                    Undo();
                    break;
                case ':':
                    _mode = Mode.Command;
                    _commandText = string.Empty;
                    break;
                case '/':
                    _mode = Mode.Search;
                    _searchText = string.Empty;
                    break;
                case 'n':
                    FindNext(_lastSearch, startAfterCursor: true);
                    break;
            }
        }

        private void EnterInsertMode()
        {
            _mode = Mode.Insert;
            _pendingDelete = false;
            _insertUndoStarted = false;
            Status("INSERT");
        }

        private void ExitInsertMode()
        {
            _mode = Mode.Normal;
            _insertUndoStarted = false;
            ClampCursor();
            Status("NORMAL");
        }

        private void HandleInsertKey(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    ExitInsertMode();
                    break;
                case ConsoleKey.LeftArrow:
                    MoveLeft();
                    _insertUndoStarted = false;
                    break;
                case ConsoleKey.RightArrow:
                    MoveRight();
                    _insertUndoStarted = false;
                    break;
                case ConsoleKey.UpArrow:
                    MoveUp();
                    _insertUndoStarted = false;
                    break;
                case ConsoleKey.DownArrow:
                    MoveDown();
                    _insertUndoStarted = false;
                    break;
                case ConsoleKey.Home:
                    _cursorCol = 0;
                    _insertUndoStarted = false;
                    break;
                case ConsoleKey.End:
                    _cursorCol = CurrentLine().Length;
                    _insertUndoStarted = false;
                    break;
                case ConsoleKey.Enter:
                    InsertNewLine();
                    break;
                case ConsoleKey.Backspace:
                    Backspace();
                    break;
                case ConsoleKey.Delete:
                    DeleteForward();
                    break;
                case ConsoleKey.Tab:
                    InsertText("    ");
                    break;
                default:
                    if (TryGetInputText(key, out string insertText))
                        InsertText(insertText);
                    break;
            }
        }

        private void HandleCommandKey(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    _mode = Mode.Normal;
                    _commandText = string.Empty;
                    Status("NORMAL");
                    break;
                case ConsoleKey.Enter:
                    ExecuteEditorCommand(_commandText.Trim());
                    _commandText = string.Empty;
                    if (_running && _mode == Mode.Command)
                        _mode = Mode.Normal;
                    break;
                case ConsoleKey.Backspace:
                    if (_commandText.Length > 0)
                        _commandText = _commandText.Substring(0, _commandText.Length - 1);
                    break;
                default:
                    if (TryGetInputText(key, out string commandText))
                        _commandText += commandText;
                    break;
            }
        }

        private void HandleSearchKey(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    _mode = Mode.Normal;
                    _searchText = string.Empty;
                    Status("Search cancelled");
                    break;
                case ConsoleKey.Enter:
                    _lastSearch = _searchText;
                    FindNext(_searchText, startAfterCursor: false);
                    _searchText = string.Empty;
                    _mode = Mode.Normal;
                    break;
                case ConsoleKey.Backspace:
                    if (_searchText.Length > 0)
                        _searchText = _searchText.Substring(0, _searchText.Length - 1);
                    break;
                default:
                    if (TryGetInputText(key, out string searchText))
                        _searchText += searchText;
                    break;
            }
        }

        private void ExecuteEditorCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                _mode = Mode.Normal;
                return;
            }

            if (TryExecuteGoToLineCommand(command))
                return;

            if (TryExecuteSyntaxCommand(command))
                return;

            switch (command.ToLowerInvariant())
            {
                case "w":
                case "write":
                    Save();
                    _mode = Mode.Normal;
                    break;
                case "q":
                case "quit":
                    if (_dirty)
                    {
                        Status("Unsaved changes. Use :q! to quit or :wq to save.");
                        _mode = Mode.Normal;
                    }
                    else
                    {
                        _running = false;
                    }
                    break;
                case "q!":
                case "quit!":
                    _running = false;
                    break;
                case "wq":
                case "x":
                    Save();
                    _running = false;
                    break;
                case "e!":
                    LoadFile();
                    Status("Reloaded");
                    _mode = Mode.Normal;
                    break;
                default:
                    Status("Unknown command: " + command);
                    _mode = Mode.Normal;
                    break;
            }
        }

        private bool TryExecuteGoToLineCommand(string command)
        {
            if (!TryGetGoToLineValue(command, out string value))
                return false;

            if (!int.TryParse(value, out int lineNumber))
            {
                Status("Invalid line number: " + value, error: true);
                _mode = Mode.Normal;
                return true;
            }

            GoToLine(lineNumber);
            return true;
        }

        private static bool TryGetGoToLineValue(string command, out string value)
        {
            value = string.Empty;
            string trimmed = command.Trim();

            if (IsUnsignedInteger(trimmed))
            {
                value = trimmed;
                return true;
            }

            foreach (string prefix in new[] { "goto ", "go ", "line ", "ln " })
            {
                if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    value = trimmed.Substring(prefix.Length).Trim();
                    return true;
                }
            }

            return false;
        }

        private static bool IsUnsignedInteger(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsDigit(value[i]))
                    return false;
            }

            return true;
        }

        private void GoToLine(int lineNumber)
        {
            if (lineNumber < 1 || lineNumber > _lines.Count)
            {
                Status("Line must be between 1 and " + _lines.Count, error: true);
                _mode = Mode.Normal;
                return;
            }

            _mode = Mode.Normal;
            _pendingDelete = false;
            _insertUndoStarted = false;
            _cursorLine = lineNumber - 1;
            _cursorCol = 0;
            ClampCursor();
            Status("Line " + lineNumber);
        }

        private bool TryExecuteSyntaxCommand(string command)
        {
            const string prefix = "syntax";

            if (!command.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            if (command.Length > prefix.Length && !char.IsWhiteSpace(command[prefix.Length]))
                return false;

            string value = command.Length > prefix.Length ? command.Substring(prefix.Length).Trim() : string.Empty;
            if (!TryParseSyntax(value, out TermXTEditorSyntax syntax))
            {
                Status("Unknown syntax. Use :syntax xt or :syntax cs.", error: true);
                _mode = Mode.Normal;
                return true;
            }

            _syntax = syntax;
            Status("Syntax: " + SyntaxDisplayName(_syntax));
            _mode = Mode.Normal;
            return true;
        }

        private void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllLines(_path, _lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                _savedLines = _lines.ToArray();
                _dirty = false;
                _insertUndoStarted = false;
                Status("Saved current data");
            }
            catch (Exception ex)
            {
                Status("Write failed: " + ex.Message, error: true);
                BottomStatus("Save failed: " + ex.Message, error: true);
            }
        }

        private void InsertText(string text)
        {
            PushInsertUndo();
            string line = CurrentLine();
            _lines[_cursorLine] = line.Insert(_cursorCol, text);
            _cursorCol += text.Length;
            MarkDirty();
        }

        private void InsertNewLine()
        {
            PushInsertUndo();
            string line = CurrentLine();
            string left = line.Substring(0, _cursorCol);
            string right = line.Substring(_cursorCol);
            _lines[_cursorLine] = left;
            _lines.Insert(_cursorLine + 1, right);
            _cursorLine++;
            _cursorCol = 0;
            MarkDirty();
        }

        private void Backspace()
        {
            if (_cursorCol > 0)
            {
                PushInsertUndo();
                string line = CurrentLine();
                _lines[_cursorLine] = line.Remove(_cursorCol - 1, 1);
                _cursorCol--;
                MarkDirty();
                return;
            }

            if (_cursorLine > 0)
            {
                PushInsertUndo();
                int previousLength = _lines[_cursorLine - 1].Length;
                _lines[_cursorLine - 1] += _lines[_cursorLine];
                _lines.RemoveAt(_cursorLine);
                _cursorLine--;
                _cursorCol = previousLength;
                MarkDirty();
            }
        }

        private void DeleteForward()
        {
            string line = CurrentLine();

            if (_cursorCol < line.Length)
            {
                PushInsertUndo();
                _lines[_cursorLine] = line.Remove(_cursorCol, 1);
                MarkDirty();
                return;
            }

            if (_cursorLine < _lines.Count - 1)
            {
                PushInsertUndo();
                _lines[_cursorLine] += _lines[_cursorLine + 1];
                _lines.RemoveAt(_cursorLine + 1);
                MarkDirty();
            }
        }

        private void DeleteCharUnderCursor()
        {
            string line = CurrentLine();
            if (line.Length == 0 || _cursorCol >= line.Length)
                return;

            PushUndo();
            _lines[_cursorLine] = line.Remove(_cursorCol, 1);
            if (_cursorCol >= _lines[_cursorLine].Length)
                _cursorCol = Math.Max(0, _lines[_cursorLine].Length - 1);
            MarkDirty();
        }

        private void DeleteCurrentLine()
        {
            PushUndo();
            _lineClipboard = _lines[_cursorLine];
            _hasLineClipboard = true;
            _lines.RemoveAt(_cursorLine);

            if (_lines.Count == 0)
                _lines.Add(string.Empty);

            _cursorLine = Math.Min(_cursorLine, _lines.Count - 1);
            _cursorCol = 0;
            MarkDirty();
        }

        private void PasteLineBelow()
        {
            if (!_hasLineClipboard)
                return;

            PushUndo();
            _lines.Insert(_cursorLine + 1, _lineClipboard);
            _cursorLine++;
            _cursorCol = 0;
            MarkDirty();
        }

        private void Undo()
        {
            if (_undo.Count == 0)
            {
                Status("Already oldest change");
                return;
            }

            _redo.Push(TakeSnapshot());
            RestoreSnapshot(_undo.Pop());
            _insertUndoStarted = false;
            Status("Undo");
        }

        private void Redo()
        {
            if (_redo.Count == 0)
            {
                Status("Already newest change");
                return;
            }

            _undo.Push(TakeSnapshot());
            RestoreSnapshot(_redo.Pop());
            _insertUndoStarted = false;
            Status("Redo");
        }

        private void PushInsertUndo()
        {
            if (_insertUndoStarted)
                return;

            PushUndo();
            _insertUndoStarted = true;
        }

        private void PushUndo()
        {
            _undo.Push(TakeSnapshot());
            _redo.Clear();

            while (_undo.Count > 100)
            {
                var snapshots = _undo.ToArray();
                _undo.Clear();
                for (int i = snapshots.Length - 2; i >= 0; i--)
                    _undo.Push(snapshots[i]);
            }
        }

        private Snapshot TakeSnapshot()
        {
            return new Snapshot
            {
                Lines = _lines.ToArray(),
                CursorLine = _cursorLine,
                CursorCol = _cursorCol,
                ScrollTop = _scrollTop,
                ScrollLeft = _scrollLeft
            };
        }

        private void RestoreSnapshot(Snapshot snapshot)
        {
            _lines.Clear();
            _lines.AddRange(snapshot.Lines);
            if (_lines.Count == 0)
                _lines.Add(string.Empty);

            _cursorLine = snapshot.CursorLine;
            _cursorCol = snapshot.CursorCol;
            _scrollTop = snapshot.ScrollTop;
            _scrollLeft = snapshot.ScrollLeft;
            _dirty = !LinesEqual(_lines, _savedLines);
            _pendingDelete = false;
            ClampCursor();
        }

        private void FindNext(string text, bool startAfterCursor)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                Status("No search text");
                return;
            }

            int startLine = _cursorLine;
            int startCol = startAfterCursor ? _cursorCol + 1 : _cursorCol;

            for (int pass = 0; pass < 2; pass++)
            {
                int lineFrom = pass == 0 ? startLine : 0;
                int lineTo = pass == 0 ? _lines.Count : startLine + 1;

                for (int line = lineFrom; line < lineTo; line++)
                {
                    int colFrom = line == startLine && pass == 0 ? startCol : 0;
                    if (colFrom > _lines[line].Length)
                        colFrom = 0;

                    int idx = _lines[line].IndexOf(text, colFrom, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        _cursorLine = line;
                        _cursorCol = idx;
                        Status("Found: " + text);
                        return;
                    }
                }
            }

            Status("No match: " + text);
        }

        private void MoveLeft()
        {
            if (_cursorCol > 0)
            {
                _cursorCol--;
                return;
            }

            if (_cursorLine > 0)
            {
                _cursorLine--;
                _cursorCol = CurrentLine().Length;
            }
        }

        private void MoveRight()
        {
            int max = CurrentLine().Length;
            if (_cursorCol < max)
            {
                _cursorCol++;
                return;
            }

            if (_cursorLine < _lines.Count - 1)
            {
                _cursorLine++;
                _cursorCol = 0;
            }
        }

        private void MoveUp()
        {
            MoveVertical(-1);
        }

        private void MoveDown()
        {
            MoveVertical(1);
        }

        private void MoveVertical(int delta)
        {
            int desiredCol = _cursorCol;
            _cursorLine = Math.Max(0, Math.Min(_lines.Count - 1, _cursorLine + delta));
            _cursorCol = Math.Min(desiredCol, CurrentLine().Length);
            if (_mode == Mode.Normal && CurrentLine().Length > 0)
                _cursorCol = Math.Min(_cursorCol, CurrentLine().Length - 1);
        }

        private void ClampCursor()
        {
            if (_lines.Count == 0)
                _lines.Add(string.Empty);

            _cursorLine = Math.Max(0, Math.Min(_lines.Count - 1, _cursorLine));
            _cursorCol = Math.Max(0, Math.Min(CurrentLine().Length, _cursorCol));
        }

        private void AdjustScroll(int textRows, int textWidth)
        {
            int cursorVisualRow = GetCursorVisualRow(textWidth);
            int totalVisualRows = GetTotalVisualRows(textWidth);

            if (cursorVisualRow < _scrollTop)
                _scrollTop = cursorVisualRow;
            else if (cursorVisualRow >= _scrollTop + textRows)
                _scrollTop = cursorVisualRow - textRows + 1;

            _scrollTop = Math.Max(0, Math.Min(Math.Max(0, totalVisualRows - textRows), _scrollTop));
            _scrollLeft = 0;
        }

        private int GetCursorVisualRow(int textWidth)
        {
            int row = 0;
            for (int i = 0; i < _cursorLine; i++)
                row += GetWrapCount(_lines[i], textWidth);

            return row + GetCursorWrapIndex(textWidth);
        }

        private int GetCursorWrapIndex(int textWidth)
        {
            if (textWidth <= 0)
                return 0;

            int wrapCount = GetWrapCount(CurrentLine(), textWidth);
            int wrapIndex = _cursorCol / textWidth;
            return Math.Max(0, Math.Min(wrapCount - 1, wrapIndex));
        }

        private int GetTotalVisualRows(int textWidth)
        {
            int total = 0;
            foreach (string line in _lines)
                total += GetWrapCount(line, textWidth);

            return Math.Max(1, total);
        }

        private static int GetWrapCount(string line, int textWidth)
        {
            if (textWidth <= 0)
                return 1;

            int length = line == null ? 0 : line.Length;
            return Math.Max(1, (length + textWidth - 1) / textWidth);
        }

        private bool TryGetVisualRow(int visualRow, int textWidth, out VisualRow rowInfo)
        {
            int remaining = visualRow;

            for (int lineIndex = 0; lineIndex < _lines.Count; lineIndex++)
            {
                int wraps = GetWrapCount(_lines[lineIndex], textWidth);
                if (remaining < wraps)
                {
                    rowInfo = new VisualRow
                    {
                        LineIndex = lineIndex,
                        WrapIndex = remaining,
                        StartColumn = remaining * Math.Max(1, textWidth)
                    };
                    return true;
                }

                remaining -= wraps;
            }

            rowInfo = default;
            return false;
        }

        private int PageSize()
        {
            try
            {
                return Math.Max(1, Console.WindowHeight - 6);
            }
            catch
            {
                return 20;
            }
        }

        private string CurrentLine()
        {
            return _lines[_cursorLine];
        }

        private void MarkDirty()
        {
            _dirty = !LinesEqual(_lines, _savedLines);
            _pendingDelete = false;
        }

        private void Status(string message, bool error = false)
        {
            _status = message;
            _statusUntil = DateTime.UtcNow.AddMilliseconds(error ? 4000 : 2500);
        }

        private void BottomStatus(string message, bool error = false)
        {
            _bottomStatus = message;
            _bottomStatusError = error;
            _bottomStatusUntil = DateTime.UtcNow.AddMilliseconds(error ? 4000 : 2200);
        }

        private static bool TryGetInputText(ConsoleKeyInfo key, out string text)
        {
            text = string.Empty;

            if (key.KeyChar != '\0' && !char.IsControl(key.KeyChar))
            {
                text = key.KeyChar.ToString();
                return true;
            }

            bool shift = (key.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift;
            int keyCode = (int)key.Key;

            if (keyCode >= (int)ConsoleKey.A && keyCode <= (int)ConsoleKey.Z)
            {
                char c = (char)('a' + (keyCode - (int)ConsoleKey.A));
                text = (shift ? char.ToUpperInvariant(c) : c).ToString();
                return true;
            }

            if (keyCode >= (int)ConsoleKey.D0 && keyCode <= (int)ConsoleKey.D9)
            {
                int digit = keyCode - (int)ConsoleKey.D0;
                const string shiftedDigits = ")!@#$%^&*(";
                text = shift ? shiftedDigits[digit].ToString() : digit.ToString();
                return true;
            }

            if (keyCode >= (int)ConsoleKey.NumPad0 && keyCode <= (int)ConsoleKey.NumPad9)
            {
                text = (keyCode - (int)ConsoleKey.NumPad0).ToString();
                return true;
            }

            switch (key.Key)
            {
                case ConsoleKey.Spacebar:
                    text = " ";
                    return true;
                case ConsoleKey.Decimal:
                    text = ".";
                    return true;
                case ConsoleKey.Add:
                    text = "+";
                    return true;
                case ConsoleKey.Subtract:
                    text = "-";
                    return true;
                case ConsoleKey.Multiply:
                    text = "*";
                    return true;
                case ConsoleKey.Divide:
                    text = "/";
                    return true;
                case ConsoleKey.Oem1:
                    text = shift ? ":" : ";";
                    return true;
                case ConsoleKey.OemPlus:
                    text = shift ? "+" : "=";
                    return true;
                case ConsoleKey.OemComma:
                    text = shift ? "<" : ",";
                    return true;
                case ConsoleKey.OemMinus:
                    text = shift ? "_" : "-";
                    return true;
                case ConsoleKey.OemPeriod:
                    text = shift ? ">" : ".";
                    return true;
                case ConsoleKey.Oem2:
                    text = shift ? "?" : "/";
                    return true;
                case ConsoleKey.Oem3:
                    text = shift ? "~" : "`";
                    return true;
                case ConsoleKey.Oem4:
                    text = shift ? "{" : "[";
                    return true;
                case ConsoleKey.Oem5:
                    text = shift ? "|" : "\\";
                    return true;
                case ConsoleKey.Oem6:
                    text = shift ? "}" : "]";
                    return true;
                case ConsoleKey.Oem7:
                    text = shift ? "\"" : "'";
                    return true;
                default:
                    return false;
            }
        }

        private static bool LinesEqual(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (left.Count != right.Count)
                return false;

            for (int i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private string DefaultStatus()
        {
            if (_pendingDelete)
                return "d";

            if (_dirty)
                return "modified";

            return "ready";
        }

        private string BuildHighlightedLine(string line, int start, int width)
        {
            if (width <= 0)
                return string.Empty;

            var tokens = Tokenize(line);
            var sb = new StringBuilder(width + 128);
            int end = start + width;
            int visible = 0;

            foreach (var token in tokens)
            {
                int tokenEnd = token.Start + token.Length;
                if (tokenEnd <= start)
                    continue;

                if (token.Start >= end)
                    break;

                int clipStart = Math.Max(start, token.Start);
                int clipEnd = Math.Min(end, tokenEnd);
                string text = line.Substring(clipStart, clipEnd - clipStart);

                sb.Append(F(token.Color)).Append(EscapeText(text));
                visible += text.Length;
            }

            sb.Append(Reset);

            if (visible < width)
                sb.Append(new string(' ', width - visible));

            return sb.ToString();
        }

        private List<Token> Tokenize(string line)
        {
            return _syntax == TermXTEditorSyntax.CSharp
                ? TokenizeCSharp(line)
                : TokenizeTermXt(line);
        }

        private static List<Token> TokenizeTermXt(string line)
        {
            var tokens = new List<Token>();
            int i = 0;

            while (i < line.Length)
            {
                char c = line[i];

                if (c == '#')
                {
                    tokens.Add(new Token(i, line.Length - i, CComment));
                    break;
                }

                if (c == '"')
                {
                    int start = i++;
                    while (i < line.Length)
                    {
                        if (line[i] == '"' && line[i - 1] != '\\')
                        {
                            i++;
                            break;
                        }
                        i++;
                    }
                    tokens.Add(new Token(start, i - start, CString));
                    continue;
                }

                if (c == '{')
                {
                    int start = i++;
                    while (i < line.Length && line[i] != '}')
                        i++;
                    if (i < line.Length)
                        i++;
                    tokens.Add(new Token(start, i - start, CVariable));
                    continue;
                }

                if (char.IsDigit(c))
                {
                    int start = i++;
                    while (i < line.Length && (char.IsDigit(line[i]) || line[i] == '.'))
                        i++;
                    tokens.Add(new Token(start, i - start, CNumber));
                    continue;
                }

                if (IsWordStart(c))
                {
                    int start = i++;
                    while (i < line.Length && IsTermXtWordPart(line[i]))
                        i++;

                    string word = line.Substring(start, i - start);
                    tokens.Add(new Token(start, i - start, TermXtWordColor(word)));
                    continue;
                }

                if ("=+-*/%<>!|&.,:".IndexOf(c) >= 0)
                {
                    tokens.Add(new Token(i, 1, COperator));
                    i++;
                    continue;
                }

                tokens.Add(new Token(i, 1, CNormal));
                i++;
            }

            if (tokens.Count == 0)
                tokens.Add(new Token(0, 0, CNormal));

            return tokens;
        }

        private static List<Token> TokenizeCSharp(string line)
        {
            var tokens = new List<Token>();
            int i = 0;

            while (i < line.Length)
            {
                char c = line[i];

                if (c == '#' && IsOnlyWhitespaceBefore(line, i))
                {
                    tokens.Add(new Token(i, line.Length - i, CPreprocessor));
                    break;
                }

                if (c == '/' && i + 1 < line.Length && line[i + 1] == '/')
                {
                    tokens.Add(new Token(i, line.Length - i, CComment));
                    break;
                }

                if (c == '/' && i + 1 < line.Length && line[i + 1] == '*')
                {
                    int start = i;
                    i += 2;
                    while (i + 1 < line.Length && !(line[i] == '*' && line[i + 1] == '/'))
                        i++;

                    i = i + 1 < line.Length ? i + 2 : line.Length;
                    tokens.Add(new Token(start, i - start, CComment));
                    continue;
                }

                if (TryReadCSharpString(line, i, out int stringLength))
                {
                    tokens.Add(new Token(i, stringLength, CString));
                    i += stringLength;
                    continue;
                }

                if (c == '\'')
                {
                    int start = i++;
                    while (i < line.Length)
                    {
                        if (line[i] == '\\')
                        {
                            i = Math.Min(line.Length, i + 2);
                            continue;
                        }

                        if (line[i] == '\'')
                        {
                            i++;
                            break;
                        }

                        i++;
                    }

                    tokens.Add(new Token(start, i - start, CString));
                    continue;
                }

                if (char.IsDigit(c))
                {
                    int start = i++;

                    if (c == '0' && i < line.Length && (line[i] == 'x' || line[i] == 'X' || line[i] == 'b' || line[i] == 'B'))
                        i++;

                    while (i < line.Length && IsCSharpNumberPart(line[i]))
                        i++;

                    tokens.Add(new Token(start, i - start, CNumber));
                    continue;
                }

                if (IsCSharpWordStart(c) || (c == '@' && i + 1 < line.Length && IsCSharpWordStart(line[i + 1])))
                {
                    int start = i;
                    bool escapedIdentifier = false;

                    if (line[i] == '@')
                    {
                        escapedIdentifier = true;
                        i++;
                    }

                    int wordStart = i++;
                    while (i < line.Length && IsCSharpWordPart(line[i]))
                        i++;

                    string word = line.Substring(wordStart, i - wordStart);
                    tokens.Add(new Token(start, i - start, CSharpWordColor(word, escapedIdentifier)));
                    continue;
                }

                if ("{}[]()=+-*/%<>!|&^~?:;.,\\".IndexOf(c) >= 0)
                {
                    tokens.Add(new Token(i, 1, COperator));
                    i++;
                    continue;
                }

                tokens.Add(new Token(i, 1, CNormal));
                i++;
            }

            if (tokens.Count == 0)
                tokens.Add(new Token(0, 0, CNormal));

            return tokens;
        }

        private static int TermXtWordColor(string word)
        {
            if (s_flowKeywords.Contains(word))
                return CFlow;

            if (s_commandKeywords.Contains(word))
                return CKeyword;

            if (s_functionKeywords.Contains(word))
                return CFunction;

            if (s_operatorWords.Contains(word))
                return COperator;

            return CNormal;
        }

        private static int CSharpWordColor(string word, bool escapedIdentifier)
        {
            if (escapedIdentifier)
                return CNormal;

            if (s_csharpFlowKeywords.Contains(word))
                return CFlow;

            if (s_csharpKeywords.Contains(word))
                return CKeyword;

            if (s_csharpTypeKeywords.Contains(word))
                return CFunction;

            if (s_csharpLiteralKeywords.Contains(word))
                return CNumber;

            return CNormal;
        }

        private static bool IsWordStart(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        private static bool IsTermXtWordPart(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_' || c == '-';
        }

        private static bool IsCSharpWordStart(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        private static bool IsCSharpWordPart(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        private static bool IsCSharpNumberPart(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_' || c == '.';
        }

        private static bool IsOnlyWhitespaceBefore(string line, int index)
        {
            for (int i = 0; i < index; i++)
            {
                if (!char.IsWhiteSpace(line[i]))
                    return false;
            }

            return true;
        }

        private static bool TryReadCSharpString(string line, int index, out int length)
        {
            length = 0;
            int i = index;
            bool verbatim = false;

            while (i < line.Length && (line[i] == '@' || line[i] == '$'))
            {
                if (line[i] == '@')
                    verbatim = true;

                i++;
            }

            if (i >= line.Length || line[i] != '"')
                return false;

            int quoteCount = CountConsecutive(line, i, '"');
            if (quoteCount >= 3)
            {
                i += quoteCount;
                while (i < line.Length)
                {
                    if (CountConsecutive(line, i, '"') >= quoteCount)
                    {
                        i += quoteCount;
                        break;
                    }

                    i++;
                }

                length = i - index;
                return true;
            }

            i++;
            while (i < line.Length)
            {
                if (verbatim && line[i] == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    i += 2;
                    continue;
                }

                if (!verbatim && line[i] == '\\')
                {
                    i = Math.Min(line.Length, i + 2);
                    continue;
                }

                if (line[i] == '"')
                {
                    i++;
                    break;
                }

                i++;
            }

            length = i - index;
            return true;
        }

        private static int CountConsecutive(string line, int index, char value)
        {
            int count = 0;
            while (index + count < line.Length && line[index + count] == value)
                count++;

            return count;
        }

        private static string EscapeText(string text)
        {
            return text.Replace("\x1b", string.Empty).Replace("\t", " ");
        }

        private static (int width, int height) WindowSize()
        {
            try
            {
                return (Math.Max(1, Console.WindowWidth), Math.Max(1, Console.WindowHeight));
            }
            catch
            {
                return (100, 30);
            }
        }

        private static string Clip(string text, int width)
        {
            if (width <= 0)
                return string.Empty;

            if (text == null)
                return string.Empty;

            if (text.Length <= width)
                return text;

            return width == 1 ? text.Substring(0, 1) : text.Substring(0, width - 1) + ">";
        }

        private static int VisibleLength(string text)
        {
            return text == null ? 0 : text.Length;
        }

        private static string At(int col, int row)
        {
            return CSI + (row + 1) + ";" + (col + 1) + "H";
        }

        private static string F(int color)
        {
            return CSI + "38;5;" + color + "m";
        }

        private static string B(int color)
        {
            return CSI + "48;5;" + color + "m";
        }

        private static string Bold()
        {
            return CSI + "1m";
        }

        private enum Mode
        {
            Normal,
            Insert,
            Command,
            Search
        }

        private sealed class Snapshot
        {
            public string[] Lines { get; set; }
            public int CursorLine { get; set; }
            public int CursorCol { get; set; }
            public int ScrollTop { get; set; }
            public int ScrollLeft { get; set; }
        }

        private struct VisualRow
        {
            public int LineIndex { get; set; }
            public int WrapIndex { get; set; }
            public int StartColumn { get; set; }
        }

        private readonly struct Token
        {
            public Token(int start, int length, int color)
            {
                Start = start;
                Length = length;
                Color = color;
            }

            public int Start { get; }
            public int Length { get; }
            public int Color { get; }
        }
    }
}
