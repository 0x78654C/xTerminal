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
        CSharp,
        C,
        Cpp
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
        private const int CSelectionFg = 232;
        private const int CSelectionBg = 153;
        private const int CSourceFlow = 39;
        private const int CSourceKeyword = 75;
        private const int CSourceType = 179;
        private const int CSourceStd = 117;
        private const int CSourceDirective = 208;
        private const int CSourceInclude = 159;
        private const int CSourceString = 186;
        private const int CSourceNumber = 203;
        private const int CSourceOperator = 250;
        private const int CSourceComment = 101;
        private const int CppSourceFlow = 75;
        private const int CppSourceKeyword = 141;
        private const int CppSourceType = 111;
        private const int CppSourceStd = 219;
        private const int CppSourceDirective = 105;
        private const int CppSourceInclude = 183;
        private const int CppSourceString = 150;
        private const int CppSourceNumber = 214;
        private const int CppSourceOperator = 222;
        private const int CppSourceComment = 103;
        private const int CSharpFlow = 39;
        private const int CSharpKeyword = 81;
        private const int CSharpType = 51;
        private const int CSharpDeclaration = 214;
        private const int CSharpModifier = 117;
        private const int CSharpBcl = 159;
        private const int CSharpDirective = 183;
        private const int CSharpAttribute = 213;
        private const int CSharpString = 150;
        private const int CSharpNumber = 209;
        private const int CSharpOperator = 220;
        private const int CSharpComment = 108;

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

        private static readonly HashSet<string> s_csharpDeclarationKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "class", "delegate", "enum", "event", "interface", "namespace", "record",
            "struct"
        };

        private static readonly HashSet<string> s_csharpModifierKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "abstract", "async", "const", "extern", "file", "fixed", "internal",
            "new", "override", "params", "partial", "private", "protected", "public",
            "readonly", "ref", "required", "sealed", "static", "unsafe", "virtual",
            "volatile"
        };

        private static readonly HashSet<string> s_csharpContextualKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "add", "alias", "ascending", "by", "descending", "dynamic", "equals",
            "from", "get", "global", "group", "init", "into", "join", "let", "nameof",
            "notnull", "on", "orderby", "remove", "select", "set", "unmanaged",
            "value", "var", "where", "with", "yield"
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

        private static readonly HashSet<string> s_csharpBclIdentifiers = new HashSet<string>(StringComparer.Ordinal)
        {
            "Action", "ArgumentException", "Array", "Attribute", "CancellationToken",
            "Console", "DateTime", "Dictionary", "Directory", "Enumerable", "Exception",
            "File", "Func", "Guid", "HashSet", "IEnumerable", "IDisposable", "IList",
            "InvalidOperationException", "KeyValuePair", "List", "Math", "Nullable",
            "Object", "Path", "Random", "ReadOnlySpan", "Regex", "Span", "String",
            "StringBuilder", "Task", "Thread", "TimeSpan", "Tuple", "Uri"
        };

        private static readonly HashSet<string> s_csharpPreprocessorDirectives = new HashSet<string>(StringComparer.Ordinal)
        {
            "define", "elif", "else", "endif", "endregion", "error", "if", "line",
            "nullable", "pragma", "region", "undef", "warning"
        };

        private static readonly HashSet<string> s_cFlowKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "if", "else", "switch", "case", "default", "for", "while", "do",
            "break", "continue", "return", "goto"
        };

        private static readonly HashSet<string> s_cKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "auto", "const", "enum", "extern", "inline", "register", "restrict", "sizeof",
            "static", "struct", "typedef", "union", "volatile", "_Alignas",
            "_Alignof", "_Atomic", "_Generic", "_Noreturn", "_Static_assert",
            "_Thread_local"
        };

        private static readonly HashSet<string> s_cTypeKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "void", "char", "short", "int", "long", "float", "double", "signed",
            "unsigned", "_Bool", "_Complex", "_Imaginary"
        };

        private static readonly HashSet<string> s_cLiteralKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "NULL", "false", "nullptr", "true"
        };

        private static readonly HashSet<string> s_cOperatorWords = new HashSet<string>(StringComparer.Ordinal)
        {
            "and", "and_eq", "bitand", "bitor", "compl", "not", "not_eq", "or",
            "or_eq", "xor", "xor_eq"
        };

        private static readonly HashSet<string> s_cStdIdentifiers = new HashSet<string>(StringComparer.Ordinal)
        {
            "abort", "abs", "atexit", "atof", "atoi", "atol", "bsearch", "calloc",
            "clock", "errno", "exit", "fclose", "feof", "ferror", "fflush", "fgetc",
            "fgets", "fopen", "fprintf", "fputc", "fputs", "fread", "free", "fscanf",
            "fseek", "ftell", "fwrite", "getchar", "gets", "malloc", "memchr",
            "memcmp", "memcpy", "memmove", "memset", "perror", "printf", "putchar",
            "puts", "qsort", "rand", "realloc", "remove", "rename", "scanf", "size_t",
            "snprintf", "sprintf", "srand", "sscanf", "stderr", "stdin", "stdout",
            "strcat", "strchr", "strcmp", "strcpy", "strlen", "strncmp", "strncpy",
            "strstr", "time"
        };

        private static readonly HashSet<string> s_cPreprocessorDirectives = new HashSet<string>(StringComparer.Ordinal)
        {
            "define", "elif", "else", "endif", "error", "if", "ifdef", "ifndef",
            "include", "include_next", "line", "pragma", "undef", "warning"
        };

        private static readonly HashSet<string> s_cppFlowKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "if", "else", "switch", "case", "default", "for", "while", "do",
            "break", "continue", "return", "goto", "try", "catch", "throw",
            "co_await", "co_return", "co_yield"
        };

        private static readonly HashSet<string> s_cppKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "alignas", "alignof", "asm", "class", "concept", "const", "consteval",
            "constexpr", "constinit", "decltype", "delete", "explicit", "export",
            "extern", "friend", "inline", "mutable", "namespace", "new", "noexcept",
            "operator", "private", "protected", "public", "requires", "sizeof",
            "static", "static_assert", "struct", "template", "this", "thread_local",
            "typedef", "typeid", "typename", "union", "using", "virtual", "volatile",
            "enum", "final", "override"
        };

        private static readonly HashSet<string> s_cppTypeKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "auto", "bool", "char", "char8_t", "char16_t", "char32_t", "double",
            "float", "int", "long", "short", "signed", "unsigned", "void", "wchar_t"
        };

        private static readonly HashSet<string> s_cppLiteralKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "false", "nullptr", "NULL", "true"
        };

        private static readonly HashSet<string> s_cppOperatorWords = new HashSet<string>(StringComparer.Ordinal)
        {
            "and", "and_eq", "bitand", "bitor", "compl", "not", "not_eq", "or",
            "or_eq", "xor", "xor_eq"
        };

        private static readonly HashSet<string> s_cppStdIdentifiers = new HashSet<string>(StringComparer.Ordinal)
        {
            "array", "begin", "cerr", "cin", "cout", "deque", "endl", "exception",
            "find", "forward", "function", "get", "make_pair", "make_shared",
            "make_unique", "map", "move", "optional", "pair", "queue", "set",
            "shared_ptr", "sort", "span", "stack", "std", "string", "string_view",
            "tuple", "unique_ptr", "unordered_map", "unordered_set", "variant",
            "vector", "weak_ptr"
        };

        private static readonly HashSet<string> s_cppPreprocessorDirectives = new HashSet<string>(StringComparer.Ordinal)
        {
            "define", "elif", "else", "endif", "error", "if", "ifdef", "ifndef",
            "import", "include", "include_next", "line", "pragma", "undef", "warning"
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
        private bool _hasSelectionAnchor;
        private int _selectionAnchorLine;
        private int _selectionAnchorCol;
        private int _wrapCacheTextWidth = -1;
        private int[] _wrapPrefixRows = Array.Empty<int>();
        private bool _wrapCacheDirty = true;
        private bool[] _csharpBlockCommentLineStarts = Array.Empty<bool>();
        private bool _csharpBlockCommentCacheDirty = true;

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

            if (string.Equals(extension, ".c", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".h", StringComparison.OrdinalIgnoreCase))
            {
                return TermXTEditorSyntax.C;
            }

            if (string.Equals(extension, ".cpp", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".cxx", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".cc", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".hpp", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".hxx", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".hh", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".ipp", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".ixx", StringComparison.OrdinalIgnoreCase))
            {
                return TermXTEditorSyntax.Cpp;
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
                case "c":
                case "c89":
                case "c99":
                case "c11":
                case "c17":
                case "c23":
                    syntax = TermXTEditorSyntax.C;
                    return true;
                case "cpp":
                case "c++":
                case "cplusplus":
                case "cxx":
                case "cc":
                    syntax = TermXTEditorSyntax.Cpp;
                    return true;
                default:
                    return false;
            }
        }

        public static string SyntaxDisplayName(TermXTEditorSyntax syntax)
        {
            switch (syntax)
            {
                case TermXTEditorSyntax.CSharp:
                    return "C#";
                case TermXTEditorSyntax.C:
                    return "C";
                case TermXTEditorSyntax.Cpp:
                    return "C++";
                default:
                    return "TermXT";
            }
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
            ClearSelection();
            InvalidateDocumentCaches();
        }

        private void InvalidateDocumentCaches()
        {
            _wrapCacheDirty = true;
            _csharpBlockCommentCacheDirty = true;
        }

        private void InvalidateSyntaxStateCache()
        {
            _csharpBlockCommentCacheDirty = true;
        }

        private void EnsureWrapCache(int textWidth)
        {
            int width = Math.Max(1, textWidth);
            if (!_wrapCacheDirty &&
                _wrapCacheTextWidth == width &&
                _wrapPrefixRows.Length == _lines.Count + 1)
            {
                return;
            }

            var prefixRows = new int[_lines.Count + 1];
            for (int i = 0; i < _lines.Count; i++)
                prefixRows[i + 1] = prefixRows[i] + GetWrapCount(_lines[i], width);

            _wrapPrefixRows = prefixRows;
            _wrapCacheTextWidth = width;
            _wrapCacheDirty = false;
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
            EnsureWrapCache(textWidth);
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
                    help = " INSERT  Esc normal | Ctrl+Home/End first/last | Shift+arrows select | Ctrl+C copy | Ctrl+V paste | Ctrl+Z/Y undo/redo";
                    break;
                case Mode.Command:
                    help = " COMMAND  w save | q quit | 42 or goto 42 go to line | syntax xt|cs|c|cpp | Esc cancel";
                    break;
                case Mode.Search:
                    help = " SEARCH  Type text then Enter | empty Enter next | Backspace edit | Esc cancel";
                    break;
                default:
                    help = " NORMAL  Ctrl+Home/End first/last | Shift+arrows select | Ctrl+C copy | Ctrl+V paste | i edit | dd delete | / search | : command";
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
            string rendered = BuildHighlightedLine(line, rowInfo.LineIndex, rowInfo.StartColumn, textWidth);
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
                if (key.Key == ConsoleKey.C)
                {
                    CopySelectionToClipboard();
                    return;
                }

                if (key.Key == ConsoleKey.V)
                {
                    PasteFromClipboard();
                    return;
                }

                if (key.Key == ConsoleKey.Home)
                {
                    MoveToDocumentStart();
                    return;
                }

                if (key.Key == ConsoleKey.End)
                {
                    MoveToDocumentEnd();
                    return;
                }

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
                    MoveWithSelection(key, MoveLeft);
                    break;
                case ConsoleKey.RightArrow:
                    MoveWithSelection(key, MoveRight);
                    break;
                case ConsoleKey.UpArrow:
                    MoveWithSelection(key, MoveUp);
                    break;
                case ConsoleKey.DownArrow:
                    MoveWithSelection(key, MoveDown);
                    break;
                case ConsoleKey.PageUp:
                    MoveWithSelection(key, () => MoveVertical(-PageSize()));
                    break;
                case ConsoleKey.PageDown:
                    MoveWithSelection(key, () => MoveVertical(PageSize()));
                    break;
                case ConsoleKey.Home:
                    UpdateSelectionBeforeMove(key);
                    _cursorCol = 0;
                    ClearSelectionAfterMoveIfNeeded(key);
                    break;
                case ConsoleKey.End:
                    UpdateSelectionBeforeMove(key);
                    _cursorCol = CurrentLine().Length;
                    ClearSelectionAfterMoveIfNeeded(key);
                    break;
                case ConsoleKey.Insert:
                    EnterInsertMode();
                    break;
                case ConsoleKey.F3:
                    SearchNext();
                    break;
                case ConsoleKey.Escape:
                    _pendingDelete = false;
                    ClearSelection();
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
                    ClearSelection();
                    MoveLeft();
                    break;
                case 'j':
                    ClearSelection();
                    MoveDown();
                    break;
                case 'k':
                    ClearSelection();
                    MoveUp();
                    break;
                case 'l':
                    ClearSelection();
                    MoveRight();
                    break;
                case '0':
                    ClearSelection();
                    _cursorCol = 0;
                    break;
                case '$':
                    ClearSelection();
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
                    SearchNext();
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
                    MoveWithSelection(key, MoveLeft);
                    _insertUndoStarted = false;
                    break;
                case ConsoleKey.RightArrow:
                    MoveWithSelection(key, MoveRight);
                    _insertUndoStarted = false;
                    break;
                case ConsoleKey.UpArrow:
                    MoveWithSelection(key, MoveUp);
                    _insertUndoStarted = false;
                    break;
                case ConsoleKey.DownArrow:
                    MoveWithSelection(key, MoveDown);
                    _insertUndoStarted = false;
                    break;
                case ConsoleKey.Home:
                    UpdateSelectionBeforeMove(key);
                    _cursorCol = 0;
                    ClearSelectionAfterMoveIfNeeded(key);
                    _insertUndoStarted = false;
                    break;
                case ConsoleKey.End:
                    UpdateSelectionBeforeMove(key);
                    _cursorCol = CurrentLine().Length;
                    ClearSelectionAfterMoveIfNeeded(key);
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
                {
                    bool repeatLastSearch = string.IsNullOrWhiteSpace(_searchText);
                    string queryText = repeatLastSearch ? _lastSearch : _searchText;
                    if (!repeatLastSearch)
                        _lastSearch = _searchText;

                    FindNext(queryText, startAfterCursor: repeatLastSearch);
                    _searchText = string.Empty;
                    _mode = Mode.Normal;
                    break;
                }
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
            ClearSelection();
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
                Status("Unknown syntax. Use :syntax xt, cs, c, or cpp.", error: true);
                _mode = Mode.Normal;
                return true;
            }

            _syntax = syntax;
            InvalidateSyntaxStateCache();
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
            if (string.IsNullOrEmpty(text))
                return;

            PushInsertUndo();
            DeleteSelectionWithoutUndo();
            InsertTextWithoutUndo(text);
            MarkDirty();
        }

        private void InsertTextWithoutUndo(string text)
        {
            string normalized = NormalizeNewlines(text);
            string[] parts = normalized.Split('\n');
            string line = CurrentLine();

            if (parts.Length == 1)
            {
                _lines[_cursorLine] = line.Insert(_cursorCol, parts[0]);
                _cursorCol += parts[0].Length;
                InvalidateDocumentCaches();
                return;
            }

            string left = line.Substring(0, _cursorCol);
            string right = line.Substring(_cursorCol);
            _lines[_cursorLine] = left + parts[0];

            int insertLine = _cursorLine + 1;
            for (int i = 1; i < parts.Length - 1; i++)
            {
                _lines.Insert(insertLine, parts[i]);
                insertLine++;
            }

            string last = parts[parts.Length - 1];
            _lines.Insert(insertLine, last + right);
            _cursorLine = insertLine;
            _cursorCol = last.Length;
            InvalidateDocumentCaches();
        }

        private void InsertNewLine()
        {
            PushInsertUndo();
            DeleteSelectionWithoutUndo();
            string line = CurrentLine();
            string left = line.Substring(0, _cursorCol);
            string right = line.Substring(_cursorCol);
            _lines[_cursorLine] = left;
            _lines.Insert(_cursorLine + 1, right);
            _cursorLine++;
            _cursorCol = 0;
            InvalidateDocumentCaches();
            MarkDirty();
        }

        private void Backspace()
        {
            if (HasSelection())
            {
                PushInsertUndo();
                DeleteSelectionWithoutUndo();
                MarkDirty();
                return;
            }

            ClearSelection();

            if (_cursorCol > 0)
            {
                PushInsertUndo();
                string line = CurrentLine();
                _lines[_cursorLine] = line.Remove(_cursorCol - 1, 1);
                _cursorCol--;
                InvalidateDocumentCaches();
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
                InvalidateDocumentCaches();
                MarkDirty();
            }
        }

        private void DeleteForward()
        {
            if (HasSelection())
            {
                PushInsertUndo();
                DeleteSelectionWithoutUndo();
                MarkDirty();
                return;
            }

            ClearSelection();

            string line = CurrentLine();

            if (_cursorCol < line.Length)
            {
                PushInsertUndo();
                _lines[_cursorLine] = line.Remove(_cursorCol, 1);
                InvalidateDocumentCaches();
                MarkDirty();
                return;
            }

            if (_cursorLine < _lines.Count - 1)
            {
                PushInsertUndo();
                _lines[_cursorLine] += _lines[_cursorLine + 1];
                _lines.RemoveAt(_cursorLine + 1);
                InvalidateDocumentCaches();
                MarkDirty();
            }
        }

        private void DeleteCharUnderCursor()
        {
            if (HasSelection())
            {
                PushUndo();
                DeleteSelectionWithoutUndo();
                MarkDirty();
                return;
            }

            ClearSelection();

            string line = CurrentLine();
            if (line.Length == 0 || _cursorCol >= line.Length)
                return;

            PushUndo();
            _lines[_cursorLine] = line.Remove(_cursorCol, 1);
            if (_cursorCol >= _lines[_cursorLine].Length)
                _cursorCol = Math.Max(0, _lines[_cursorLine].Length - 1);
            InvalidateDocumentCaches();
            MarkDirty();
        }

        private void DeleteCurrentLine()
        {
            PushUndo();
            ClearSelection();
            _lineClipboard = _lines[_cursorLine];
            _hasLineClipboard = true;
            _lines.RemoveAt(_cursorLine);

            if (_lines.Count == 0)
                _lines.Add(string.Empty);

            _cursorLine = Math.Min(_cursorLine, _lines.Count - 1);
            _cursorCol = 0;
            InvalidateDocumentCaches();
            MarkDirty();
        }

        private void PasteLineBelow()
        {
            if (!_hasLineClipboard)
                return;

            PushUndo();
            ClearSelection();
            _lines.Insert(_cursorLine + 1, _lineClipboard);
            _cursorLine++;
            _cursorCol = 0;
            InvalidateDocumentCaches();
            MarkDirty();
        }

        private void CopySelectionToClipboard()
        {
            if (!TryGetSelectedText(out string text))
            {
                Status("No selection", error: true);
                return;
            }

            if (TrySetClipboardText(text, out string error))
            {
                Status("Copied selection");
                BottomStatus("Copied " + text.Length + " characters");
            }
            else
            {
                Status("Copy failed", error: true);
                BottomStatus(error, error: true);
            }
        }

        private void PasteFromClipboard()
        {
            if (!TryGetClipboardText(out string text, out string error))
            {
                Status("Paste failed", error: true);
                BottomStatus(error, error: true);
                return;
            }

            if (string.IsNullOrEmpty(text))
            {
                Status("Clipboard empty", error: true);
                return;
            }

            if (_mode == Mode.Command)
            {
                _commandText += ToSingleLine(text);
                return;
            }

            if (_mode == Mode.Search)
            {
                _searchText += ToSingleLine(text);
                return;
            }

            if (_mode == Mode.Insert)
            {
                InsertText(text);
            }
            else
            {
                PushUndo();
                DeleteSelectionWithoutUndo();
                InsertTextWithoutUndo(text);
                MarkDirty();
                _insertUndoStarted = false;
            }

            Status("Pasted");
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
            ClearSelection();
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
            ClearSelection();
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
            ClearSelection();
            InvalidateDocumentCaches();
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
                        ClearSelection();
                        _cursorLine = line;
                        _cursorCol = idx;
                        Status("Found: " + text);
                        return;
                    }
                }
            }

            Status("No match: " + text);
        }

        private void SearchNext()
        {
            FindNext(_lastSearch, startAfterCursor: true);
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

        private void MoveToDocumentStart()
        {
            ClearSelection();
            _pendingDelete = false;
            _insertUndoStarted = false;
            _cursorLine = 0;
            _cursorCol = 0;
            _scrollTop = 0;
            _scrollLeft = 0;
            ClampCursor();
            Status("First line");
        }

        private void MoveToDocumentEnd()
        {
            ClearSelection();
            _pendingDelete = false;
            _insertUndoStarted = false;
            _cursorLine = Math.Max(0, _lines.Count - 1);
            _cursorCol = CurrentLine().Length;
            _scrollLeft = 0;
            ClampCursor();
            Status("Last line");
        }

        private void MoveWithSelection(ConsoleKeyInfo key, Action move)
        {
            UpdateSelectionBeforeMove(key);
            move();
            ClearSelectionAfterMoveIfNeeded(key);
        }

        private void UpdateSelectionBeforeMove(ConsoleKeyInfo key)
        {
            if (IsShiftPressed(key))
            {
                if (!_hasSelectionAnchor)
                {
                    _selectionAnchorLine = _cursorLine;
                    _selectionAnchorCol = _cursorCol;
                    _hasSelectionAnchor = true;
                }
            }
            else
            {
                ClearSelection();
            }
        }

        private void ClearSelectionAfterMoveIfNeeded(ConsoleKeyInfo key)
        {
            if (!IsShiftPressed(key))
                ClearSelection();
        }

        private static bool IsShiftPressed(ConsoleKeyInfo key)
        {
            return (key.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift;
        }

        private void ClampCursor()
        {
            if (_lines.Count == 0)
            {
                _lines.Add(string.Empty);
                InvalidateDocumentCaches();
            }

            _cursorLine = Math.Max(0, Math.Min(_lines.Count - 1, _cursorLine));
            _cursorCol = Math.Max(0, Math.Min(CurrentLine().Length, _cursorCol));
        }

        private void AdjustScroll(int textRows, int textWidth)
        {
            EnsureWrapCache(textWidth);
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
            EnsureWrapCache(textWidth);
            int line = Math.Max(0, Math.Min(_lines.Count - 1, _cursorLine));
            int row = line < _wrapPrefixRows.Length ? _wrapPrefixRows[line] : 0;
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
            EnsureWrapCache(textWidth);
            if (_wrapPrefixRows.Length == 0)
                return 1;

            return Math.Max(1, _wrapPrefixRows[_wrapPrefixRows.Length - 1]);
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
            EnsureWrapCache(textWidth);

            if (visualRow < 0 || visualRow >= GetTotalVisualRows(textWidth))
            {
                rowInfo = default;
                return false;
            }

            int lineIndex = Array.BinarySearch(_wrapPrefixRows, visualRow);
            if (lineIndex < 0)
                lineIndex = ~lineIndex - 1;

            lineIndex = Math.Max(0, Math.Min(_lines.Count - 1, lineIndex));
            int wrapIndex = visualRow - _wrapPrefixRows[lineIndex];
            rowInfo = new VisualRow
            {
                LineIndex = lineIndex,
                WrapIndex = wrapIndex,
                StartColumn = wrapIndex * Math.Max(1, textWidth)
            };
            return true;
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

        private bool HasSelection()
        {
            return _hasSelectionAnchor &&
                ComparePositions(_selectionAnchorLine, _selectionAnchorCol, _cursorLine, _cursorCol) != 0;
        }

        private void ClearSelection()
        {
            _hasSelectionAnchor = false;
        }

        private bool TryGetSelectionRange(out TextPosition start, out TextPosition end)
        {
            start = default;
            end = default;

            if (!HasSelection())
                return false;

            var anchor = new TextPosition(_selectionAnchorLine, _selectionAnchorCol);
            var cursor = new TextPosition(_cursorLine, _cursorCol);

            if (ComparePositions(anchor.Line, anchor.Col, cursor.Line, cursor.Col) <= 0)
            {
                start = ClampPosition(anchor);
                end = ClampPosition(cursor);
            }
            else
            {
                start = ClampPosition(cursor);
                end = ClampPosition(anchor);
            }

            return ComparePositions(start.Line, start.Col, end.Line, end.Col) != 0;
        }

        private TextPosition ClampPosition(TextPosition position)
        {
            int line = Math.Max(0, Math.Min(_lines.Count - 1, position.Line));
            int col = Math.Max(0, Math.Min(_lines[line].Length, position.Col));
            return new TextPosition(line, col);
        }

        private static int ComparePositions(int leftLine, int leftCol, int rightLine, int rightCol)
        {
            if (leftLine != rightLine)
                return leftLine.CompareTo(rightLine);

            return leftCol.CompareTo(rightCol);
        }

        private bool DeleteSelectionWithoutUndo()
        {
            if (!TryGetSelectionRange(out TextPosition start, out TextPosition end))
            {
                ClearSelection();
                return false;
            }

            if (start.Line == end.Line)
            {
                _lines[start.Line] = _lines[start.Line].Remove(start.Col, end.Col - start.Col);
            }
            else
            {
                string left = _lines[start.Line].Substring(0, start.Col);
                string right = _lines[end.Line].Substring(end.Col);
                _lines[start.Line] = left + right;
                _lines.RemoveRange(start.Line + 1, end.Line - start.Line);
            }

            _cursorLine = start.Line;
            _cursorCol = start.Col;
            ClearSelection();
            _pendingDelete = false;
            InvalidateDocumentCaches();
            return true;
        }

        private bool TryGetSelectedText(out string text)
        {
            text = string.Empty;

            if (!TryGetSelectionRange(out TextPosition start, out TextPosition end))
                return false;

            if (start.Line == end.Line)
            {
                text = _lines[start.Line].Substring(start.Col, end.Col - start.Col);
                return text.Length > 0;
            }

            var sb = new StringBuilder();
            sb.Append(_lines[start.Line].Substring(start.Col));
            sb.Append(Environment.NewLine);

            for (int line = start.Line + 1; line < end.Line; line++)
            {
                sb.Append(_lines[line]);
                sb.Append(Environment.NewLine);
            }

            sb.Append(_lines[end.Line].Substring(0, end.Col));
            text = sb.ToString();
            return text.Length > 0;
        }

        private bool TryGetSelectionSpanForLine(int lineIndex, out int startCol, out int endCol)
        {
            startCol = 0;
            endCol = 0;

            if (!TryGetSelectionRange(out TextPosition start, out TextPosition end))
                return false;

            if (lineIndex < start.Line || lineIndex > end.Line)
                return false;

            int lineLength = _lines[lineIndex].Length;
            startCol = lineIndex == start.Line ? start.Col : 0;
            endCol = lineIndex == end.Line ? end.Col : lineLength;
            startCol = Math.Max(0, Math.Min(lineLength, startCol));
            endCol = Math.Max(0, Math.Min(lineLength, endCol));
            return endCol > startCol;
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

        private static bool TrySetClipboardText(string text, out string error)
        {
            error = string.Empty;

            try
            {
                RunSta(() =>
                {
                    System.Windows.Forms.Clipboard.SetText(text, System.Windows.Forms.TextDataFormat.UnicodeText);
                });
                return true;
            }
            catch (Exception ex)
            {
                error = "Clipboard write failed: " + ex.Message;
                return false;
            }
        }

        private static bool TryGetClipboardText(out string text, out string error)
        {
            text = string.Empty;
            error = string.Empty;

            try
            {
                text = RunSta(() =>
                    System.Windows.Forms.Clipboard.ContainsText()
                        ? System.Windows.Forms.Clipboard.GetText(System.Windows.Forms.TextDataFormat.UnicodeText)
                        : string.Empty);
                return true;
            }
            catch (Exception ex)
            {
                error = "Clipboard read failed: " + ex.Message;
                return false;
            }
        }

        private static void RunSta(Action action)
        {
            RunSta(() =>
            {
                action();
                return true;
            });
        }

        private static T RunSta<T>(Func<T> action)
        {
            T result = default;
            Exception error = null;

            var thread = new Thread(() =>
            {
                try
                {
                    result = action();
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (error != null)
                throw error;

            return result;
        }

        private static string NormalizeNewlines(string text)
        {
            return text.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        private static string ToSingleLine(string text)
        {
            return NormalizeNewlines(text).Replace('\n', ' ');
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

            if (HasSelection())
                return "selection";

            if (_dirty)
                return "modified";

            return "ready";
        }

        private string BuildHighlightedLine(string line, int lineIndex, int start, int width)
        {
            if (width <= 0)
                return string.Empty;

            var tokens = Tokenize(line, lineIndex);
            var sb = new StringBuilder(width + 128);
            int end = start + width;
            int visible = 0;
            TryGetSelectionSpanForLine(lineIndex, out int selectionStart, out int selectionEnd);

            foreach (var token in tokens)
            {
                int tokenEnd = token.Start + token.Length;
                if (tokenEnd <= start)
                    continue;

                if (token.Start >= end)
                    break;

                int clipStart = Math.Max(start, token.Start);
                int clipEnd = Math.Min(end, tokenEnd);
                AppendHighlightedSegment(sb, line, clipStart, clipEnd, token.Color, selectionStart, selectionEnd);

                visible += clipEnd - clipStart;
            }

            sb.Append(Reset);

            if (visible < width)
                sb.Append(new string(' ', width - visible));

            return sb.ToString();
        }

        private static void AppendHighlightedSegment(
            StringBuilder sb,
            string line,
            int start,
            int end,
            int color,
            int selectionStart,
            int selectionEnd)
        {
            int cursor = start;

            while (cursor < end)
            {
                bool selected = cursor >= selectionStart && cursor < selectionEnd;
                int next = selected
                    ? Math.Min(end, selectionEnd)
                    : Math.Min(end, selectionStart > cursor ? selectionStart : end);

                if (next <= cursor)
                    next = end;

                string text = line.Substring(cursor, next - cursor);
                if (selected)
                    sb.Append(B(CSelectionBg)).Append(F(CSelectionFg));
                else
                    sb.Append(Reset).Append(F(color));

                sb.Append(EscapeText(text));
                cursor = next;
            }
        }

        private List<Token> Tokenize(string line, int lineIndex)
        {
            switch (_syntax)
            {
                case TermXTEditorSyntax.CSharp:
                    return TokenizeCSharp(line, IsCSharpLineInBlockComment(lineIndex));
                case TermXTEditorSyntax.C:
                    return TokenizeCStyle(line, cpp: false);
                case TermXTEditorSyntax.Cpp:
                    return TokenizeCStyle(line, cpp: true);
                default:
                    return TokenizeTermXt(line);
            }
        }

        private bool IsCSharpLineInBlockComment(int lineIndex)
        {
            EnsureCSharpBlockCommentCache();
            if (lineIndex < 0 || lineIndex >= _csharpBlockCommentLineStarts.Length)
                return false;

            return _csharpBlockCommentLineStarts[lineIndex];
        }

        private void EnsureCSharpBlockCommentCache()
        {
            if (!_csharpBlockCommentCacheDirty &&
                _csharpBlockCommentLineStarts.Length == _lines.Count)
            {
                return;
            }

            var lineStarts = new bool[_lines.Count];
            bool inBlockComment = false;
            for (int i = 0; i < _lines.Count; i++)
            {
                lineStarts[i] = inBlockComment;
                inBlockComment = ScanCSharpBlockCommentState(_lines[i], inBlockComment);
            }

            _csharpBlockCommentLineStarts = lineStarts;
            _csharpBlockCommentCacheDirty = false;
        }

        private static bool ScanCSharpBlockCommentState(string line, bool inBlockComment)
        {
            int i = 0;

            while (i < line.Length)
            {
                if (inBlockComment)
                {
                    int end = line.IndexOf("*/", i, StringComparison.Ordinal);
                    if (end < 0)
                        return true;

                    i = end + 2;
                    inBlockComment = false;
                    continue;
                }

                if (line[i] == '/' && i + 1 < line.Length && line[i + 1] == '/')
                    return false;

                if (line[i] == '/' && i + 1 < line.Length && line[i + 1] == '*')
                {
                    i += 2;
                    inBlockComment = true;
                    continue;
                }

                if (TryReadCSharpString(line, i, out int stringLength))
                {
                    i += Math.Max(1, stringLength);
                    continue;
                }

                if (TryReadCSharpChar(line, i, out int charLength))
                {
                    i += Math.Max(1, charLength);
                    continue;
                }

                i++;
            }

            return inBlockComment;
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

        private static List<Token> TokenizeCSharp(string line, bool startsInBlockComment)
        {
            var tokens = new List<Token>();
            int i = 0;
            bool inBlockComment = startsInBlockComment;

            while (i < line.Length)
            {
                if (inBlockComment)
                {
                    int start = i;
                    int end = line.IndexOf("*/", i, StringComparison.Ordinal);
                    if (end < 0)
                    {
                        tokens.Add(new Token(start, line.Length - start, CSharpComment));
                        i = line.Length;
                    }
                    else
                    {
                        i = end + 2;
                        tokens.Add(new Token(start, i - start, CSharpComment));
                        inBlockComment = false;
                    }

                    continue;
                }

                char c = line[i];

                if (c == '#' && IsOnlyWhitespaceBefore(line, i))
                {
                    tokens.AddRange(TokenizeCSharpPreprocessor(line, i));
                    break;
                }

                if (c == '/' && i + 1 < line.Length && line[i + 1] == '/')
                {
                    tokens.Add(new Token(i, line.Length - i, CSharpComment));
                    break;
                }

                if (c == '/' && i + 1 < line.Length && line[i + 1] == '*')
                {
                    int start = i;
                    i += 2;
                    while (i + 1 < line.Length && !(line[i] == '*' && line[i + 1] == '/'))
                        i++;

                    if (i + 1 < line.Length)
                    {
                        i += 2;
                    }
                    else
                    {
                        i = line.Length;
                        inBlockComment = true;
                    }

                    tokens.Add(new Token(start, i - start, CSharpComment));
                    continue;
                }

                if (TryReadCSharpString(line, i, out int stringLength))
                {
                    tokens.Add(new Token(i, stringLength, CSharpString));
                    i += stringLength;
                    continue;
                }

                if (TryReadCSharpAttribute(line, i, out int attributeLength))
                {
                    tokens.Add(new Token(i, attributeLength, CSharpAttribute));
                    i += attributeLength;
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

                    tokens.Add(new Token(start, i - start, CSharpString));
                    continue;
                }

                if (char.IsDigit(c))
                {
                    int start = i++;

                    if (c == '0' && i < line.Length && (line[i] == 'x' || line[i] == 'X' || line[i] == 'b' || line[i] == 'B'))
                        i++;

                    while (i < line.Length && IsCSharpNumberPart(line[i]))
                        i++;

                    tokens.Add(new Token(start, i - start, CSharpNumber));
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
                    tokens.Add(new Token(i, 1, CSharpOperator));
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

        private static List<Token> TokenizeCSharpPreprocessor(string line, int index)
        {
            var tokens = new List<Token>();
            int i = index;

            tokens.Add(new Token(i, 1, CSharpOperator));
            i++;

            while (i < line.Length)
            {
                char c = line[i];

                if (c == '/' && i + 1 < line.Length && line[i + 1] == '/')
                {
                    tokens.Add(new Token(i, line.Length - i, CSharpComment));
                    break;
                }

                if (TryReadCSharpString(line, i, out int stringLength))
                {
                    tokens.Add(new Token(i, stringLength, CSharpString));
                    i += stringLength;
                    continue;
                }

                if (char.IsDigit(c))
                {
                    int start = i++;
                    while (i < line.Length && IsCSharpNumberPart(line[i]))
                        i++;

                    tokens.Add(new Token(start, i - start, CSharpNumber));
                    continue;
                }

                if (IsCSharpWordStart(c))
                {
                    int start = i++;
                    while (i < line.Length && IsCSharpWordPart(line[i]))
                        i++;

                    string word = line.Substring(start, i - start);
                    int color = s_csharpPreprocessorDirectives.Contains(word) || IsLikelyMacroName(word)
                        ? CSharpDirective
                        : CSharpKeyword;
                    tokens.Add(new Token(start, i - start, color));
                    continue;
                }

                if ("{}[]()=+-*/%<>!|&^~?:;.,\\".IndexOf(c) >= 0)
                {
                    tokens.Add(new Token(i, 1, CSharpOperator));
                    i++;
                    continue;
                }

                tokens.Add(new Token(i, 1, CNormal));
                i++;
            }

            return tokens;
        }

        private static List<Token> TokenizeCStyle(string line, bool cpp)
        {
            var tokens = new List<Token>();
            int i = 0;

            while (i < line.Length)
            {
                char c = line[i];

                if (c == '#' && IsOnlyWhitespaceBefore(line, i))
                {
                    tokens.AddRange(TokenizeCStylePreprocessor(line, i, cpp));
                    break;
                }

                if (c == '/' && i + 1 < line.Length && line[i + 1] == '/')
                {
                    tokens.Add(new Token(i, line.Length - i, CStyleCommentColor(cpp)));
                    break;
                }

                if (c == '/' && i + 1 < line.Length && line[i + 1] == '*')
                {
                    int start = i;
                    i += 2;
                    while (i + 1 < line.Length && !(line[i] == '*' && line[i + 1] == '/'))
                        i++;

                    i = i + 1 < line.Length ? i + 2 : line.Length;
                    tokens.Add(new Token(start, i - start, CStyleCommentColor(cpp)));
                    continue;
                }

                if (TryReadCStyleString(line, i, cpp, out int stringLength))
                {
                    tokens.Add(new Token(i, stringLength, CStyleStringColor(cpp)));
                    i += stringLength;
                    continue;
                }

                if (TryReadCStyleChar(line, i, out int charLength))
                {
                    tokens.Add(new Token(i, charLength, CStyleStringColor(cpp)));
                    i += charLength;
                    continue;
                }

                if (char.IsDigit(c))
                {
                    int start = i++;

                    if (c == '0' && i < line.Length && (line[i] == 'x' || line[i] == 'X' || line[i] == 'b' || line[i] == 'B'))
                        i++;

                    while (i < line.Length && IsCStyleNumberPart(line[i]))
                        i++;

                    tokens.Add(new Token(start, i - start, CStyleNumberColor(cpp)));
                    continue;
                }

                if (IsCStyleWordStart(c))
                {
                    int start = i++;
                    while (i < line.Length && IsCStyleWordPart(line[i]))
                        i++;

                    string word = line.Substring(start, i - start);
                    tokens.Add(new Token(start, i - start, CStyleWordColor(word, cpp)));
                    continue;
                }

                if ("{}[]()=+-*/%<>!|&^~?:;.,\\#".IndexOf(c) >= 0)
                {
                    tokens.Add(new Token(i, 1, CStyleOperatorColor(cpp)));
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

        private static List<Token> TokenizeCStylePreprocessor(string line, int index, bool cpp)
        {
            var tokens = new List<Token>();
            int i = index;
            bool expectIncludePath = false;

            tokens.Add(new Token(i, 1, CStyleOperatorColor(cpp)));
            i++;

            while (i < line.Length)
            {
                char c = line[i];

                if (c == '/' && i + 1 < line.Length && line[i + 1] == '/')
                {
                    tokens.Add(new Token(i, line.Length - i, CStyleCommentColor(cpp)));
                    break;
                }

                if (c == '/' && i + 1 < line.Length && line[i + 1] == '*')
                {
                    int start = i;
                    i += 2;
                    while (i + 1 < line.Length && !(line[i] == '*' && line[i + 1] == '/'))
                        i++;

                    i = i + 1 < line.Length ? i + 2 : line.Length;
                    tokens.Add(new Token(start, i - start, CStyleCommentColor(cpp)));
                    continue;
                }

                if (c == '<' && expectIncludePath)
                {
                    int start = i++;
                    while (i < line.Length && line[i] != '>')
                        i++;

                    if (i < line.Length)
                        i++;

                    tokens.Add(new Token(start, i - start, CStyleIncludeColor(cpp)));
                    expectIncludePath = false;
                    continue;
                }

                if (TryReadCStyleString(line, i, cpp, out int stringLength))
                {
                    tokens.Add(new Token(i, stringLength, expectIncludePath ? CStyleIncludeColor(cpp) : CStyleStringColor(cpp)));
                    i += stringLength;
                    expectIncludePath = false;
                    continue;
                }

                if (char.IsDigit(c))
                {
                    int start = i++;
                    while (i < line.Length && IsCStyleNumberPart(line[i]))
                        i++;

                    tokens.Add(new Token(start, i - start, CStyleNumberColor(cpp)));
                    continue;
                }

                if (IsCStyleWordStart(c))
                {
                    int start = i++;
                    while (i < line.Length && IsCStyleWordPart(line[i]))
                        i++;

                    string word = line.Substring(start, i - start);
                    int color = CStylePreprocessorDirectiveColor(word, cpp);
                    tokens.Add(new Token(start, i - start, color));
                    expectIncludePath = IsIncludeDirective(word);
                    continue;
                }

                if ("{}[]()=+-*/%<>!|&^~?:;.,\\#".IndexOf(c) >= 0)
                {
                    tokens.Add(new Token(i, 1, CStyleOperatorColor(cpp)));
                    i++;
                    expectIncludePath = false;
                    continue;
                }

                tokens.Add(new Token(i, 1, CNormal));
                i++;
            }

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
                return CSharpFlow;

            if (s_csharpDeclarationKeywords.Contains(word))
                return CSharpDeclaration;

            if (s_csharpModifierKeywords.Contains(word))
                return CSharpModifier;

            if (s_csharpKeywords.Contains(word))
                return CSharpKeyword;

            if (s_csharpTypeKeywords.Contains(word))
                return CSharpType;

            if (s_csharpLiteralKeywords.Contains(word))
                return CSharpNumber;

            if (s_csharpContextualKeywords.Contains(word))
                return CSharpKeyword;

            if (s_csharpBclIdentifiers.Contains(word))
                return CSharpBcl;

            if (IsLikelyMacroName(word))
                return CSharpDirective;

            return CNormal;
        }

        private static int CStyleWordColor(string word, bool cpp)
        {
            if (cpp)
            {
                if (s_cppOperatorWords.Contains(word))
                    return CppSourceOperator;

                if (s_cppFlowKeywords.Contains(word))
                    return CppSourceFlow;

                if (s_cppKeywords.Contains(word))
                    return CppSourceKeyword;

                if (s_cppTypeKeywords.Contains(word))
                    return CppSourceType;

                if (s_cppLiteralKeywords.Contains(word))
                    return CppSourceNumber;

                if (s_cppStdIdentifiers.Contains(word))
                    return CppSourceStd;

                if (IsLikelyMacroName(word))
                    return CppSourceDirective;
            }
            else
            {
                if (s_cOperatorWords.Contains(word))
                    return CSourceOperator;

                if (s_cFlowKeywords.Contains(word))
                    return CSourceFlow;

                if (s_cKeywords.Contains(word))
                    return CSourceKeyword;

                if (s_cTypeKeywords.Contains(word))
                    return CSourceType;

                if (s_cLiteralKeywords.Contains(word))
                    return CSourceNumber;

                if (s_cStdIdentifiers.Contains(word))
                    return CSourceStd;

                if (IsLikelyMacroName(word))
                    return CSourceDirective;
            }

            return CNormal;
        }

        private static int CStylePreprocessorDirectiveColor(string word, bool cpp)
        {
            if (cpp)
            {
                if (s_cppPreprocessorDirectives.Contains(word) || IsLikelyMacroName(word))
                    return CppSourceDirective;

                if (s_cppStdIdentifiers.Contains(word))
                    return CppSourceStd;

                return CppSourceKeyword;
            }

            if (s_cPreprocessorDirectives.Contains(word) || IsLikelyMacroName(word))
                return CSourceDirective;

            if (s_cStdIdentifiers.Contains(word))
                return CSourceStd;

            return CSourceKeyword;
        }

        private static int CStyleCommentColor(bool cpp)
        {
            return cpp ? CppSourceComment : CSourceComment;
        }

        private static int CStyleIncludeColor(bool cpp)
        {
            return cpp ? CppSourceInclude : CSourceInclude;
        }

        private static int CStyleStringColor(bool cpp)
        {
            return cpp ? CppSourceString : CSourceString;
        }

        private static int CStyleNumberColor(bool cpp)
        {
            return cpp ? CppSourceNumber : CSourceNumber;
        }

        private static int CStyleOperatorColor(bool cpp)
        {
            return cpp ? CppSourceOperator : CSourceOperator;
        }

        private static bool IsIncludeDirective(string word)
        {
            return string.Equals(word, "include", StringComparison.Ordinal) ||
                string.Equals(word, "include_next", StringComparison.Ordinal) ||
                string.Equals(word, "import", StringComparison.Ordinal);
        }

        private static bool IsLikelyMacroName(string word)
        {
            if (string.IsNullOrWhiteSpace(word) || word.Length < 2)
                return false;

            bool hasLetter = false;
            for (int i = 0; i < word.Length; i++)
            {
                char c = word[i];
                if (char.IsLetter(c))
                {
                    hasLetter = true;
                    if (char.IsLower(c))
                        return false;
                }
                else if (!char.IsDigit(c) && c != '_')
                {
                    return false;
                }
            }

            return hasLetter;
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

        private static bool IsCStyleWordStart(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        private static bool IsCStyleWordPart(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        private static bool IsCStyleNumberPart(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '\'';
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

        private static bool TryReadCSharpChar(string line, int index, out int length)
        {
            length = 0;

            if (index >= line.Length || line[index] != '\'')
                return false;

            int i = index + 1;
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

            length = i - index;
            return true;
        }

        private static bool TryReadCSharpAttribute(string line, int index, out int length)
        {
            length = 0;

            if (index >= line.Length || line[index] != '[' || !IsLikelyCSharpAttributeStart(line, index))
                return false;

            int depth = 0;
            int i = index;
            bool hasName = false;

            while (i < line.Length)
            {
                if (TryReadCSharpString(line, i, out int stringLength))
                {
                    i += stringLength;
                    continue;
                }

                if (line[i] == '\'')
                {
                    i++;
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
                    continue;
                }

                if (IsCSharpWordStart(line[i]))
                    hasName = true;

                if (line[i] == '[')
                    depth++;
                else if (line[i] == ']')
                {
                    depth--;
                    i++;
                    if (depth == 0)
                    {
                        length = i - index;
                        return hasName;
                    }
                    continue;
                }

                i++;
            }

            return false;
        }

        private static bool IsLikelyCSharpAttributeStart(string line, int index)
        {
            int previous = index - 1;
            while (previous >= 0 && char.IsWhiteSpace(line[previous]))
                previous--;

            if (previous >= 0 && line[previous] != '{' && line[previous] != ';' && line[previous] != ',')
                return false;

            int next = index + 1;
            while (next < line.Length && char.IsWhiteSpace(line[next]))
                next++;

            return next < line.Length && (IsCSharpWordStart(line[next]) || line[next] == '@');
        }

        private static bool TryReadCStyleString(string line, int index, bool cpp, out int length)
        {
            length = 0;
            int i = index;

            if (cpp && line[i] == 'R')
                return TryReadCppRawString(line, index, i, out length);

            if (line[i] == 'u' && i + 1 < line.Length && line[i + 1] == '8')
                i += 2;
            else if (line[i] == 'u' || line[i] == 'U' || line[i] == 'L')
                i++;

            if (i >= line.Length)
                return false;

            if (cpp && line[i] == 'R')
                return TryReadCppRawString(line, index, i, out length);

            if (line[i] != '"')
                return false;

            i++;
            while (i < line.Length)
            {
                if (line[i] == '\\')
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

        private static bool TryReadCStyleChar(string line, int index, out int length)
        {
            length = 0;
            int i = index;

            if (line[i] == 'u' && i + 1 < line.Length && line[i + 1] == '8')
                i += 2;
            else if (line[i] == 'u' || line[i] == 'U' || line[i] == 'L')
                i++;

            if (i >= line.Length || line[i] != '\'')
                return false;

            i++;
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

            length = i - index;
            return true;
        }

        private static bool TryReadCppRawString(string line, int tokenStart, int rawStart, out int length)
        {
            length = 0;
            int openQuote = rawStart + 1;
            if (openQuote >= line.Length || line[openQuote] != '"')
                return false;

            int delimiterStart = openQuote + 1;
            int openParen = line.IndexOf('(', delimiterStart);
            if (openParen < 0)
                return false;

            string delimiter = line.Substring(delimiterStart, openParen - delimiterStart);
            string terminator = ")" + delimiter + "\"";
            int end = line.IndexOf(terminator, openParen + 1, StringComparison.Ordinal);
            length = end >= 0
                ? end + terminator.Length - tokenStart
                : line.Length - tokenStart;
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

        private readonly struct TextPosition
        {
            public TextPosition(int line, int col)
            {
                Line = line;
                Col = col;
            }

            public int Line { get; }
            public int Col { get; }
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
