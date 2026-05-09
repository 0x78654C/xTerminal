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
        private const int CSharpType = 68;
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

        private string _path;
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
        private string _lastExplorerDirectory = string.Empty;
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
                    help = " COMMAND  e explorer | w save | q quit | 42 or goto 42 go to line | syntax xt|cs|c|cpp | Esc cancel";
                    break;
                case Mode.Search:
                    help = " SEARCH  Type text then Enter | empty Enter next | Backspace edit | Esc cancel";
                    break;
                default:
                    help = " NORMAL  e explorer | Ctrl+Home/End first/last | Shift+arrows select | Ctrl+C copy | Ctrl+V paste | i edit | dd delete | / search | : command";
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
                case 'e':
                    OpenExplorer();
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
                case "e":
                case "explorer":
                    OpenExplorer();
                    _mode = Mode.Normal;
                    break;
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

        private void OpenExplorer()
        {
            _mode = Mode.Normal;
            _pendingDelete = false;
            _insertUndoStarted = false;
            ClearSelection();

            var explorer = new EditorFileExplorer(GetExplorerStartDirectory());
            string selectedFile = explorer.Run();
            RememberExplorerDirectory(explorer.CurrentDirectory);
            ForceFullRedraw();

            if (string.IsNullOrWhiteSpace(selectedFile))
            {
                Status("Explorer closed");
                return;
            }

            OpenExplorerFile(selectedFile);
        }

        private string GetExplorerStartDirectory()
        {
            if (IsUsableDirectory(_lastExplorerDirectory))
                return Path.GetFullPath(_lastExplorerDirectory);

            string currentDirectory = ReadXTerminalCurrentDirectory();
            if (IsUsableDirectory(currentDirectory))
                return Path.GetFullPath(currentDirectory);

            string editorDirectory = Path.GetDirectoryName(_path);
            if (IsUsableDirectory(editorDirectory))
                return Path.GetFullPath(editorDirectory);

            return Environment.CurrentDirectory;
        }

        private void RememberExplorerDirectory(string path)
        {
            if (!IsUsableDirectory(path))
                return;

            _lastExplorerDirectory = Path.GetFullPath(path);
        }

        private void OpenExplorerFile(string path)
        {
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(path);
            }
            catch (Exception ex)
            {
                Status("Open failed: " + ex.Message, error: true);
                return;
            }

            if (!File.Exists(fullPath))
            {
                Status("File does not exist: " + fullPath, error: true);
                return;
            }

            if (_dirty && !ConfirmDiscardChanges(fullPath))
            {
                Status("Open cancelled");
                BottomStatus("Unsaved changes kept");
                return;
            }

            try
            {
                _path = fullPath;
                _syntax = DetectSyntaxFromPath(_path);
                LoadFile();
                Status("Opened " + Path.GetFileName(_path));
                BottomStatus(_path);
            }
            catch (Exception ex)
            {
                Status("Open failed: " + ex.Message, error: true);
                BottomStatus("Open failed: " + ex.Message, error: true);
            }
        }

        private bool ConfirmDiscardChanges(string path)
        {
            (int width, int height) = WindowSize();
            string fileName = Path.GetFileName(path);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = path;

            string prompt = " Unsaved changes. Open " + fileName + " and discard current edits? y/N ";
            int row = Math.Max(0, height - 2);

            Console.Write(HideCursor + At(0, row) + B(CError) + F(231) + Clip(prompt, width).PadRight(width) + Reset);

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Y)
                    return true;

                if (key.Key == ConsoleKey.N || key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Enter)
                    return false;
            }
        }

        private void ForceFullRedraw()
        {
            _lastWidth = -1;
            _lastHeight = -1;
            Console.Write(HideCursor + ClearScreen);
        }

        private static string ReadXTerminalCurrentDirectory()
        {
            try
            {
                if (File.Exists(GlobalVariables.currentDirectory))
                    return File.ReadAllText(GlobalVariables.currentDirectory).Trim();
            }
            catch
            {
            }

            return string.Empty;
        }

        private static bool IsUsableDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                return Directory.Exists(path);
            }
            catch
            {
                return false;
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

        private sealed class EditorFileExplorer
        {
            private const long PreviewByteLimit = 64 * 1024;

            private readonly StringBuilder _explorerFrame = new StringBuilder(1 << 15);
            private readonly List<ExplorerItem> _items = new List<ExplorerItem>();
            private readonly List<SearchItem> _searchResults = new List<SearchItem>();
            private readonly Stack<ExplorerLocation> _back = new Stack<ExplorerLocation>();
            private readonly Stack<ExplorerLocation> _forward = new Stack<ExplorerLocation>();

            private string _currentDirectory;
            private int _selectedIndex;
            private int _scrollOffset;
            private bool _searchMode;
            private int _searchSelectedIndex;
            private int _searchScrollOffset;
            private int _lastWidth = -1;
            private int _lastHeight = -1;
            private string _message = string.Empty;
            private DateTime _messageUntil = DateTime.MinValue;
            private int _cachedDirCount;
            private int _cachedFileCount;
            private string _cachedDriveRoot = string.Empty;
            private string _cachedFreeText = string.Empty;

            public EditorFileExplorer(string startDirectory)
            {
                _currentDirectory = NormalizeDirectory(startDirectory);
                LoadItems();
            }

            public string CurrentDirectory
            {
                get { return _currentDirectory; }
            }

            public string Run()
            {
                Console.Write(HideCursor + ClearScreen);

                while (true)
                {
                    if (_searchMode)
                        RenderSearch();
                    else
                        Render();

                    ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                    bool close;
                    string selectedFile = _searchMode
                        ? HandleSearchKey(key, out close)
                        : HandleKey(key, out close);

                    if (close)
                        return null;

                    if (!string.IsNullOrWhiteSpace(selectedFile))
                        return selectedFile;
                }
            }

            private string HandleKey(ConsoleKeyInfo key, out bool close)
            {
                close = false;

                switch (key.Key)
                {
                    case ConsoleKey.Oem3:
                    case ConsoleKey.Escape:
                        close = true;
                        return null;
                    case ConsoleKey.UpArrow:
                        MoveSelection(-1);
                        return null;
                    case ConsoleKey.DownArrow:
                        MoveSelection(1);
                        return null;
                    case ConsoleKey.PageUp:
                        GoParent();
                        return null;
                    case ConsoleKey.PageDown:
                        MoveSelection(PageSize());
                        return null;
                    case ConsoleKey.Home:
                        MoveToStart();
                        return null;
                    case ConsoleKey.End:
                        MoveToEnd();
                        return null;
                    case ConsoleKey.Enter:
                        return OpenSelected();
                    case ConsoleKey.Backspace:
                    case ConsoleKey.LeftArrow:
                        GoBack();
                        return null;
                    case ConsoleKey.RightArrow:
                        GoForward();
                        return null;
                    case ConsoleKey.Oem2:
                        DoSearch();
                        return null;
                    case ConsoleKey.Tab:
                        SwitchDrive();
                        return null;
                    case ConsoleKey.Delete:
                        DeleteSelectedItem();
                        return null;
                }

                char c = char.ToLowerInvariant(key.KeyChar);
                switch (c)
                {
                    //case 'q':
                    //    close = true;
                    //    return null;
                    //case 'j':
                    //    MoveSelection(1);
                    //    return null;
                    //case 'k':
                    //    MoveSelection(-1);
                    //    return null;
                    //case 'g':
                    //    MoveToStart();
                    //    return null;
                    //case 'h':
                    //case '[':
                    //    GoBack();
                    //    return null;
                    //case 'l':
                    //    return OpenSelected();
                    //case ']':
                    //    GoForward();
                    //    return null;
                    //case '/':
                    //    DoSearch();
                    //    return null;
                    //case '-':
                    //case 'p':
                    //case 'u':
                    //    GoParent();
                    //    return null;
                    //case 'r':
                    //    Refresh();
                    //    return null;
                    default:
                        if (char.IsLetterOrDigit(c))
                            JumpToItem(c);
                        return null;
                }
            }

            private void Render()
            {
                (int width, int height) = WindowSize();
                width = Math.Max(width, 60);
                height = Math.Max(height, 20);

                if (width != _lastWidth || height != _lastHeight)
                {
                    Console.Write(HideCursor + ClearScreen);
                    _lastWidth = width;
                    _lastHeight = height;
                }

                if (width < 60 || height < 20)
                {
                    Console.Write(At(0, 0) + F(CError) + "Terminal too small. Resize to at least 60 x 20." + Reset + ClearEol);
                    return;
                }

                ClampSelection();

                int headerRows = 4;
                int contentTop = headerRows;
                int contentRows = Math.Max(4, height - headerRows - 1);
                int footerRow = contentTop + contentRows;
                int listWidth = Math.Max(20, width / 2 - 1);
                int separatorColumn = listWidth;
                int detailsLeft = separatorColumn + 1;
                int detailsWidth = Math.Max(1, width - detailsLeft);

                AdjustScroll(contentRows);

                _explorerFrame.Clear();
                RenderHeader(width);
                RenderBody(contentTop, contentRows, listWidth, separatorColumn, detailsLeft, detailsWidth);
                RenderFooter(footerRow, width);

                Console.Write(_explorerFrame.ToString());
            }

            private void RenderHeader(int width)
            {
                string counter = _items.Count > 0 ? "[" + (_selectedIndex + 1) + "/" + _items.Count + "]" : "[0/0]";
                string title = " \u25c8 xFile Explorer";
                int pad = Math.Max(0, width - title.Length - counter.Length - 1);

                _explorerFrame.Append(At(0, 0)).Append(F(CTitle)).Append(Clip(title + new string(' ', pad) + counter + " ", width)).Append(Reset);
                _explorerFrame.Append(At(0, 1)).Append(F(CMuted)).Append(new string('\u2550', width)).Append(Reset);
                _explorerFrame.Append(At(0, 2)).Append(F(COperator)).Append(Clip(" \u25b6 " + _currentDirectory, width)).Append(Reset).Append(ClearEol);
                _explorerFrame.Append(At(0, 3)).Append(F(CMuted))
                    .Append(Clip(" \u2191\u2193:move  \u21b5:open  \u232b:back  \u2192:forward  PgUp:up  Del:del  /:search  Tab:drives  `:quit", width))
                    .Append(Reset).Append(ClearEol);
            }

            private void RenderBody(int top, int rows, int listWidth, int separatorColumn, int detailsLeft, int detailsWidth)
            {
                List<DetailLine> details = BuildDetails(rows);

                for (int row = 0; row < rows; row++)
                {
                    int itemIndex = _scrollOffset + row;
                    _explorerFrame.Append(At(0, top + row));
                    RenderListCell(itemIndex, listWidth);
                    _explorerFrame.Append(Reset).Append(F(CMuted)).Append("\u2551").Append(Reset);

                    DetailLine detail = row < details.Count ? details[row] : new DetailLine(string.Empty, CNormal);
                    _explorerFrame.Append(F(detail.Color))
                        .Append(Clip(detail.Text, detailsWidth).PadRight(detailsWidth))
                        .Append(Reset);
                }
            }

            private void RenderListCell(int itemIndex, int width)
            {
                bool selected = itemIndex == _selectedIndex;
                string text = "~";
                int color = CMuted;

                if (itemIndex >= 0 && itemIndex < _items.Count)
                {
                    ExplorerItem item = _items[itemIndex];
                    text = (item.IsDirectory ? "\u25b6 " : "\u00b7 ") + item.Name;
                    if (!item.IsDirectory)
                        text += "  " + FormatSize(item.SizeBytes);

                    color = item.IsDirectory ? CTitle : FileColor(item.Path);
                }

                if (selected)
                    _explorerFrame.Append(F(45)).Append(B(24)).Append("\u258c").Append(B(23)).Append(Bold()).Append(F(253))
                        .Append(Clip(text, Math.Max(0, width - 1)).PadRight(Math.Max(0, width - 1))).Append(Reset);
                else
                    _explorerFrame.Append(F(color)).Append(Clip(" " + text, width).PadRight(width)).Append(Reset);
            }

            private void RenderFooter(int row, int width)
            {
                string text = DateTime.UtcNow <= _messageUntil && !string.IsNullOrWhiteSpace(_message)
                    ? _message
                    : StatusText();

                _explorerFrame.Append(At(0, row))
                    .Append(B(CStatusBg)).Append(F(CStatusFg))
                    .Append(Clip(" " + text, width).PadRight(width))
                    .Append(Reset);
            }

            private List<DetailLine> BuildDetails(int maxRows)
            {
                var lines = new List<DetailLine>();

                if (_items.Count == 0 || _selectedIndex < 0 || _selectedIndex >= _items.Count)
                {
                    lines.Add(new DetailLine("  (empty)", CDim));
                    return lines;
                }

                ExplorerItem item = _items[_selectedIndex];
                lines.Add(new DetailLine("\u2500 Details " + new string('\u2500', 64), CMuted));

                if (item.IsDirectory)
                {
                    AddFolderDetails(lines, item.Path, maxRows);
                }
                else
                {
                    AddFileDetails(lines, item.Path, item.SizeBytes, maxRows);
                }

                return lines;
            }

            private static void AddFileDetails(List<DetailLine> lines, string path, long sizeBytes, int maxRows)
            {
                try
                {
                    var info = new FileInfo(path);
                    AddInfoLine(lines, "Type", "File", FileColor(path), maxRows);
                    AddInfoLine(lines, "Name", info.Name, CNormal, maxRows);
                    AddInfoLine(lines, "Ext", string.IsNullOrEmpty(info.Extension) ? "(none)" : info.Extension, FileColor(path), maxRows);
                    AddInfoLine(lines, "Size", FormatSize(info.Length), CNormal, maxRows);
                    AddInfoLine(lines, "Modified", info.LastWriteTime.ToString("yyyy-MM-dd HH:mm"), CNormal, maxRows);
                    AddInfoLine(lines, "Created", info.CreationTime.ToString("yyyy-MM-dd HH:mm"), CNormal, maxRows);
                    AddSeparator(lines, maxRows);
                    AddLine(lines, "  " + info.FullName, CDim, maxRows);
                    AddSeparator(lines, maxRows);
                    AddLine(lines, "  Preview (first " + FormatSize(PreviewByteLimit) + " max):", CMuted, maxRows);
                    AddFilePreview(lines, path, info.Length, maxRows);
                }
                catch (Exception ex)
                {
                    AddLine(lines, "  (unable to read: " + ex.Message + ")", CDim, maxRows);
                }
            }

            private static void AddFolderDetails(List<DetailLine> lines, string path, int maxRows)
            {
                string[] directories = Array.Empty<string>();
                string[] files = Array.Empty<string>();

                try
                {
                    directories = Directory.GetDirectories(path);
                    files = Directory.GetFiles(path);
                }
                catch
                {
                }

                try
                {
                    var info = new DirectoryInfo(path);
                    AddInfoLine(lines, "Type", "Folder", CTitle, maxRows);
                    AddInfoLine(lines, "Subs", directories.Length.ToString(), CNormal, maxRows);
                    AddInfoLine(lines, "Files", files.Length.ToString(), CNormal, maxRows);
                    AddInfoLine(lines, "Modified", info.LastWriteTime.ToString("yyyy-MM-dd HH:mm"), CNormal, maxRows);
                    AddSeparator(lines, maxRows);
                    AddLine(lines, "  Contents:", CMuted, maxRows);

                    foreach (string directory in directories)
                    {
                        if (lines.Count >= maxRows)
                            return;

                        string name = DisplayName(directory);
                        AddLine(lines, "  \u25b6 " + name, CTitle, maxRows);
                    }

                    foreach (string file in files)
                    {
                        if (lines.Count >= maxRows)
                            return;

                        AddLine(lines, "  \u00b7 " + Path.GetFileName(file), FileColor(file), maxRows);
                    }
                }
                catch (Exception ex)
                {
                    AddLine(lines, "  (unable to read: " + ex.Message + ")", CDim, maxRows);
                }
            }

            private static void AddFilePreview(List<DetailLine> lines, string path, long totalBytes, int maxRows)
            {
                if (lines.Count >= maxRows)
                    return;

                try
                {
                    if (totalBytes == 0)
                    {
                        AddLine(lines, "  (empty)", CDim, maxRows);
                        return;
                    }

                    int bytesToRead = (int)Math.Min(totalBytes, PreviewByteLimit);
                    byte[] buffer = new byte[bytesToRead];
                    int read = 0;

                    using (var stream = File.OpenRead(path))
                    {
                        while (read < bytesToRead)
                        {
                            int chunk = stream.Read(buffer, read, bytesToRead - read);
                            if (chunk <= 0)
                                break;

                            read += chunk;
                        }
                    }

                    if (read == 0)
                    {
                        AddLine(lines, "  (empty)", CDim, maxRows);
                        return;
                    }

                    if (LooksBinary(buffer, read))
                    {
                        AddLine(lines, "  (binary preview skipped)", CDim, maxRows);
                        return;
                    }

                    string text = Encoding.UTF8.GetString(buffer, 0, read)
                        .Replace("\r\n", "\n")
                        .Replace('\r', '\n')
                        .Replace('\0', ' ');
                    string[] previewLines = text.Split('\n');

                    for (int i = 0; i < previewLines.Length && lines.Count < maxRows; i++)
                        AddLine(lines, "  " + previewLines[i].Replace("\t", "    "), CNormal, maxRows);

                    if (totalBytes > read && lines.Count < maxRows)
                        AddLine(lines, "  ... preview truncated at " + FormatSize(read) + " of " + FormatSize(totalBytes), CDim, maxRows);
                }
                catch (Exception ex)
                {
                    AddLine(lines, "  Preview unavailable: " + ex.Message, CDim, maxRows);
                }
            }

            private static bool LooksBinary(byte[] buffer, int length)
            {
                int controls = 0;
                for (int i = 0; i < length; i++)
                {
                    byte value = buffer[i];
                    if (value == 0)
                        return true;

                    bool allowed = value == 9 || value == 10 || value == 12 || value == 13;
                    if (value < 32 && !allowed)
                        controls++;
                }

                return controls > Math.Max(4, length / 10);
            }

            private static void AddInfoLine(List<DetailLine> lines, string label, string value, int valueColor, int maxRows)
            {
                AddLine(lines, "  " + label.PadRight(8) + "\u2502 " + (value ?? string.Empty), valueColor, maxRows);
            }

            private static void AddSeparator(List<DetailLine> lines, int maxRows)
            {
                AddLine(lines, "  " + new string('\u2500', 72), CMuted, maxRows);
            }

            private static void AddLine(List<DetailLine> lines, string text, int color, int maxRows)
            {
                if (lines.Count < maxRows)
                    lines.Add(new DetailLine(text, color));
            }

            private string OpenSelected()
            {
                if (_items.Count == 0 || _selectedIndex < 0 || _selectedIndex >= _items.Count)
                {
                    Message("Directory is empty");
                    return null;
                }

                ExplorerItem item = _items[_selectedIndex];
                if (item.IsDirectory)
                {
                    NavigateTo(item.Path);
                    return null;
                }

                return item.Path;
            }

            private void NavigateTo(string directory)
            {
                if (!IsUsableDirectory(directory))
                {
                    Message("Cannot open directory");
                    return;
                }

                _back.Push(CaptureLocation());
                _forward.Clear();
                _currentDirectory = NormalizeDirectory(directory);
                _selectedIndex = 0;
                _scrollOffset = 0;
                LoadItems();
            }

            private void GoBack()
            {
                if (_back.Count == 0)
                {
                    Message("No previous directory");
                    return;
                }

                ExplorerLocation current = CaptureLocation();
                ExplorerLocation previous = _back.Pop();
                _forward.Push(current);
                ApplyLocation(previous);
            }

            private void GoForward()
            {
                if (_forward.Count == 0)
                {
                    Message("No forward directory");
                    return;
                }

                ExplorerLocation current = CaptureLocation();
                ExplorerLocation next = _forward.Pop();
                _back.Push(current);
                ApplyLocation(next);
            }

            private void GoParent()
            {
                DirectoryInfo parent;
                string currentDirectory = NormalizeDirectory(_currentDirectory);
                try
                {
                    parent = Directory.GetParent(currentDirectory);
                }
                catch
                {
                    parent = null;
                }

                if (parent == null)
                {
                    Message("Already at root");
                    return;
                }

                string previousDirectory = currentDirectory;
                _back.Push(CaptureLocation());
                _forward.Clear();
                _currentDirectory = NormalizeDirectory(parent.FullName);
                _selectedIndex = 0;
                _scrollOffset = 0;
                LoadItems();
                SelectPath(previousDirectory);
            }

            private void ApplyLocation(ExplorerLocation location)
            {
                _currentDirectory = NormalizeDirectory(location.Path);
                _selectedIndex = Math.Max(0, location.SelectedIndex);
                _scrollOffset = Math.Max(0, location.ScrollOffset);
                LoadItems();
                ClampSelection();
            }

            private ExplorerLocation CaptureLocation()
            {
                return new ExplorerLocation
                {
                    Path = _currentDirectory,
                    SelectedIndex = _selectedIndex,
                    ScrollOffset = _scrollOffset
                };
            }

            private void Refresh()
            {
                LoadItems();
                ClampSelection();
                ForceExplorerRedraw();
                Message("Refreshed");
            }

            private void DeleteSelectedItem()
            {
                if (_items.Count == 0 || _selectedIndex < 0 || _selectedIndex >= _items.Count)
                    return;

                ExplorerItem item = _items[_selectedIndex];
                if (!ConfirmDelete(item.Path, item.IsDirectory))
                {
                    ForceExplorerRedraw();
                    Message("Cancelled");
                    return;
                }

                try
                {
                    if (item.IsDirectory)
                        Directory.Delete(item.Path, recursive: true);
                    else
                        File.Delete(item.Path);

                    if (_selectedIndex >= _items.Count - 1)
                        _selectedIndex = Math.Max(0, _selectedIndex - 1);

                    LoadItems();
                    ClampSelection();
                    ForceExplorerRedraw();
                    Message("Deleted");
                }
                catch (Exception ex)
                {
                    ForceExplorerRedraw();
                    Message("Delete failed: " + ex.Message);
                }
            }

            private void DeleteSearchSelectedItem()
            {
                if (_searchResults.Count == 0 || _searchSelectedIndex < 0 || _searchSelectedIndex >= _searchResults.Count)
                    return;

                SearchItem item = _searchResults[_searchSelectedIndex];
                if (!ConfirmDelete(item.Path, item.IsDirectory))
                {
                    ForceExplorerRedraw();
                    Message("Cancelled");
                    return;
                }

                try
                {
                    if (item.IsDirectory)
                        Directory.Delete(item.Path, recursive: true);
                    else
                        File.Delete(item.Path);

                    _searchResults.RemoveAt(_searchSelectedIndex);
                    if (_searchSelectedIndex >= _searchResults.Count)
                        _searchSelectedIndex = Math.Max(0, _searchSelectedIndex - 1);

                    LoadItems();
                    ForceExplorerRedraw();
                    Message("Deleted");
                }
                catch (Exception ex)
                {
                    ForceExplorerRedraw();
                    Message("Delete failed: " + ex.Message);
                }
            }

            private static bool ConfirmDelete(string path, bool isDirectory)
            {
                (int width, int height) = WindowSize();
                Console.Write(HideCursor + ClearScreen);
                WriteRawLine(0, 0, isDirectory ? " \u25c8 Delete Folder" : " \u25c8 Delete File", width, CError);
                WriteRawLine(0, 1, new string('\u2550', width), width, CMuted);
                WriteRawLine(0, 2, "  Path: " + path, width, CNormal);
                if (isDirectory)
                    WriteRawLine(0, 3, "  WARNING: This will delete the folder and ALL its contents.", width, CError);

                int row = isDirectory ? 5 : 4;
                row = Math.Min(row, Math.Max(0, height - 1));
                Console.Write(At(0, row) + F(CNormal) + "  Are you sure? (y/N): " + Reset);
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                return key.KeyChar == 'y' || key.KeyChar == 'Y';
            }

            private void SwitchDrive()
            {
                DriveInfo[] drives;
                try
                {
                    var ready = new List<DriveInfo>();
                    foreach (DriveInfo drive in DriveInfo.GetDrives())
                    {
                        if (drive.IsReady)
                            ready.Add(drive);
                    }

                    drives = ready.ToArray();
                }
                catch
                {
                    Message("Unable to read drives");
                    return;
                }

                if (drives.Length == 0)
                {
                    Message("No ready drives");
                    return;
                }

                int selected = 0;
                while (true)
                {
                    (int width, int height) = WindowSize();
                    width = Math.Max(width, 60);
                    height = Math.Max(height, 20);

                    _explorerFrame.Clear();
                    _explorerFrame.Append(HideCursor).Append(ClearScreen);
                    _explorerFrame.Append(At(0, 0)).Append(F(CTitle)).Append(Clip(" \u25c8 Select Drive", width)).Append(Reset);
                    _explorerFrame.Append(At(0, 1)).Append(F(CMuted)).Append(new string('\u2550', width)).Append(Reset);
                    _explorerFrame.Append(At(0, 2)).Append(F(CMuted)).Append(Clip(" \u2191\u2193:move  \u21b5:select  Esc:cancel", width)).Append(Reset);
                    _explorerFrame.Append(At(0, 3)).Append(F(CMuted)).Append(new string('\u2500', width)).Append(Reset);

                    for (int i = 0; i < drives.Length && i + 4 < height; i++)
                    {
                        DriveInfo drive = drives[i];
                        string line = "  " + drive.Name + "  (" + drive.DriveType + ")  " +
                            FormatSize(drive.AvailableFreeSpace) + " free of " + FormatSize(drive.TotalSize);
                        _explorerFrame.Append(At(0, 4 + i));
                        AppendPaddedSelection(_explorerFrame, line, width, i == selected, CNormal);
                    }

                    Console.Write(_explorerFrame.ToString());
                    ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                    switch (key.Key)
                    {
                        case ConsoleKey.UpArrow:
                            selected = Math.Max(0, selected - 1);
                            break;
                        case ConsoleKey.DownArrow:
                            selected = Math.Min(drives.Length - 1, selected + 1);
                            break;
                        case ConsoleKey.Enter:
                            NavigateTo(drives[selected].RootDirectory.FullName);
                            _scrollOffset = 0;
                            ForceExplorerRedraw();
                            return;
                        case ConsoleKey.Escape:
                            ForceExplorerRedraw();
                            return;
                    }
                }
            }

            private void DoSearch()
            {
                _searchResults.Clear();
                _searchSelectedIndex = 0;
                _searchScrollOffset = 0;

                (int width, int height) = WindowSize();
                width = Math.Max(width, 60);

                Console.Write(HideCursor + ClearScreen);
                WriteRawLine(0, 0, " \u25c8 Search", width, CTitle);
                WriteRawLine(0, 1, new string('\u2550', width), width, CMuted);
                WriteRawLine(0, 2, "  Base folder: " + _currentDirectory, width, COperator);
                WriteRawLine(0, 3, new string('\u2500', width), width, CMuted);
                Console.Write(At(0, 4) + F(COperator) + "  Search term: " + Reset + F(CNormal));
                Console.Write(ShowCursor);

                string term = ReadSearchTerm(15, 4, Math.Max(1, width - 15));
                Console.Write(HideCursor);

                if (string.IsNullOrWhiteSpace(term))
                {
                    ForceExplorerRedraw();
                    return;
                }

                term = term.Trim();
                WriteRawLine(0, 5, "  Searching...", width, CMuted);

                var pending = new Stack<string>();
                pending.Push(_currentDirectory);

                while (pending.Count > 0)
                {
                    string directory = pending.Pop();
                    try
                    {
                        foreach (string childDirectory in Directory.GetDirectories(directory))
                        {
                            string name = Path.GetFileName(childDirectory);
                            if (!string.IsNullOrEmpty(name) &&
                                name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                _searchResults.Add(new SearchItem { Path = childDirectory, IsDirectory = true });
                            }

                            pending.Push(childDirectory);
                        }

                        foreach (string file in Directory.GetFiles(directory))
                        {
                            string name = Path.GetFileName(file);
                            if (!string.IsNullOrEmpty(name) &&
                                name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                _searchResults.Add(new SearchItem { Path = file, IsDirectory = false });
                            }
                        }
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (PathTooLongException) { }
                    catch (IOException) { }
                }

                _searchMode = true;
                ForceExplorerRedraw();
            }

            private string HandleSearchKey(ConsoleKeyInfo key, out bool close)
            {
                close = false;

                switch (key.Key)
                {
                    case ConsoleKey.Q:
                    case ConsoleKey.Escape:
                        _searchMode = false;
                        ForceExplorerRedraw();
                        return null;
                    case ConsoleKey.UpArrow:
                        if (_searchResults.Count > 0)
                            _searchSelectedIndex = ClampValue(_searchSelectedIndex - 1, 0, _searchResults.Count - 1);
                        return null;
                    case ConsoleKey.DownArrow:
                        if (_searchResults.Count > 0)
                            _searchSelectedIndex = ClampValue(_searchSelectedIndex + 1, 0, _searchResults.Count - 1);
                        return null;
                    case ConsoleKey.Home:
                        if (_searchResults.Count > 0)
                            _searchSelectedIndex = 0;
                        return null;
                    case ConsoleKey.End:
                        if (_searchResults.Count > 0)
                            _searchSelectedIndex = _searchResults.Count - 1;
                        return null;
                    case ConsoleKey.Enter:
                        return OpenSearchSelected();
                    case ConsoleKey.Backspace:
                        GoBack();
                        _searchMode = false;
                        ForceExplorerRedraw();
                        return null;
                    case ConsoleKey.U:
                        GoParent();
                        _searchMode = false;
                        ForceExplorerRedraw();
                        return null;
                    case ConsoleKey.Delete:
                        DeleteSearchSelectedItem();
                        return null;
                    default:
                        if (char.IsLetterOrDigit(key.KeyChar))
                            JumpToSearchItem(char.ToLowerInvariant(key.KeyChar));
                        return null;
                }
            }

            private string OpenSearchSelected()
            {
                if (_searchResults.Count == 0 || _searchSelectedIndex < 0 || _searchSelectedIndex >= _searchResults.Count)
                    return null;

                SearchItem item = _searchResults[_searchSelectedIndex];
                if (item.IsDirectory)
                {
                    NavigateTo(item.Path);
                    _selectedIndex = 0;
                    _scrollOffset = 0;
                    _searchMode = false;
                    ForceExplorerRedraw();
                    return null;
                }

                return item.Path;
            }

            private void RenderSearch()
            {
                (int width, int height) = WindowSize();
                width = Math.Max(width, 60);
                height = Math.Max(height, 20);

                int headerRows = 4;
                int contentTop = headerRows;
                int contentRows = Math.Max(4, height - headerRows - 1);
                int footerRow = contentTop + contentRows;

                if (_searchResults.Count > 0)
                    _searchSelectedIndex = ClampValue(_searchSelectedIndex, 0, _searchResults.Count - 1);
                else
                    _searchSelectedIndex = 0;

                int maxOffset = Math.Max(0, _searchResults.Count - contentRows);
                if (_searchSelectedIndex < _searchScrollOffset)
                    _searchScrollOffset = _searchSelectedIndex;
                else if (_searchSelectedIndex >= _searchScrollOffset + contentRows)
                    _searchScrollOffset = _searchSelectedIndex - contentRows + 1;
                _searchScrollOffset = ClampValue(_searchScrollOffset, 0, maxOffset);

                string counter = _searchResults.Count > 0 ? "[" + (_searchSelectedIndex + 1) + "/" + _searchResults.Count + "]" : "[0/0]";
                string title = " \u25c8 Search Results";
                int pad = Math.Max(0, width - title.Length - counter.Length - 1);

                _explorerFrame.Clear();
                _explorerFrame.Append(HideCursor).Append(ClearScreen);
                _explorerFrame.Append(At(0, 0)).Append(F(CTitle)).Append(Clip(title + new string(' ', pad) + counter + " ", width)).Append(Reset);
                _explorerFrame.Append(At(0, 1)).Append(F(CMuted)).Append(new string('\u2550', width)).Append(Reset);
                _explorerFrame.Append(At(0, 2)).Append(F(COperator)).Append(Clip("  Base: " + _currentDirectory, width)).Append(Reset);
                _explorerFrame.Append(At(0, 3)).Append(F(CMuted)).Append(Clip(" \u2191\u2193:move  \u21b5:open  Del:del  Esc/Q:exit  \u232b:back  U:up", width)).Append(Reset);

                for (int row = 0; row < contentRows; row++)
                {
                    int index = _searchScrollOffset + row;
                    string text = string.Empty;
                    int color = CNormal;

                    if (index >= 0 && index < _searchResults.Count)
                    {
                        SearchItem item = _searchResults[index];
                        text = (item.IsDirectory ? "\u25b6 " : "\u00b7 ") + item.Path;
                        color = item.IsDirectory ? CTitle : FileColor(item.Path);
                    }

                    _explorerFrame.Append(At(0, contentTop + row));
                    AppendPaddedSelection(_explorerFrame, text, width, index == _searchSelectedIndex, color);
                }

                int directoryCount = 0;
                for (int i = 0; i < _searchResults.Count; i++)
                {
                    if (_searchResults[i].IsDirectory)
                        directoryCount++;
                }

                int fileCount = _searchResults.Count - directoryCount;
                string status = "  " + directoryCount + " folder" + (directoryCount == 1 ? "" : "s") +
                    " \u00b7 " + fileCount + " file" + (fileCount == 1 ? "" : "s") + " matched";
                _explorerFrame.Append(At(0, footerRow)).Append(B(CStatusBg)).Append(F(CStatusFg))
                    .Append(Clip(status, width).PadRight(width)).Append(Reset);

                Console.Write(_explorerFrame.ToString());
            }

            private void JumpToSearchItem(char firstCharacter)
            {
                if (_searchResults.Count == 0)
                    return;

                int start = _searchSelectedIndex + 1;
                if (start >= _searchResults.Count)
                    start = 0;

                for (int i = start; i < _searchResults.Count; i++)
                {
                    string name = Path.GetFileName(_searchResults[i].Path);
                    if (StartsWith(name, firstCharacter))
                    {
                        _searchSelectedIndex = i;
                        return;
                    }
                }

                for (int i = 0; i <= _searchSelectedIndex; i++)
                {
                    string name = Path.GetFileName(_searchResults[i].Path);
                    if (StartsWith(name, firstCharacter))
                    {
                        _searchSelectedIndex = i;
                        return;
                    }
                }
            }

            private static string ReadSearchTerm(int left, int top, int width)
            {
                var term = new StringBuilder();
                int inputWidth = Math.Max(1, width);

                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                    switch (key.Key)
                    {
                        case ConsoleKey.Enter:
                            return term.ToString();
                        case ConsoleKey.Escape:
                            return null;
                        case ConsoleKey.Backspace:
                            if (term.Length == 0)
                                return null;

                            term.Remove(term.Length - 1, 1);
                            RenderSearchTermInput(term.ToString(), left, top, inputWidth);
                            break;
                        default:
                            if (!char.IsControl(key.KeyChar))
                            {
                                term.Append(key.KeyChar);
                                RenderSearchTermInput(term.ToString(), left, top, inputWidth);
                            }
                            break;
                    }
                }
            }

            private static void RenderSearchTermInput(string term, int left, int top, int width)
            {
                string visible = term.Length > width ? term.Substring(term.Length - width) : term;
                Console.Write(At(left, top) + Clip(visible, width).PadRight(width));
                int cursorOffset = Math.Min(visible.Length, Math.Max(0, width - 1));
                Console.Write(At(left + cursorOffset, top));
            }

            private void ForceExplorerRedraw()
            {
                _lastWidth = -1;
                _lastHeight = -1;
                Console.Write(HideCursor + ClearScreen);
            }

            private string StatusText()
            {
                if (!string.Equals(_cachedDriveRoot, _currentDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    _cachedDriveRoot = _currentDirectory;
                    _cachedFreeText = string.Empty;

                    try
                    {
                        string root = Path.GetPathRoot(_currentDirectory);
                        if (!string.IsNullOrWhiteSpace(root))
                        {
                            var drive = new DriveInfo(root);
                            if (drive.IsReady)
                                _cachedFreeText = "  \u2502  Free: " + FormatSize(drive.AvailableFreeSpace);
                        }
                    }
                    catch
                    {
                    }
                }

                return "  " + _cachedDirCount + " folder" + (_cachedDirCount == 1 ? "" : "s") +
                    " \u00b7 " + _cachedFileCount + " file" + (_cachedFileCount == 1 ? "" : "s") +
                    _cachedFreeText;
            }

            private void LoadItems()
            {
                _items.Clear();
                _cachedDirCount = 0;
                _cachedFileCount = 0;
                _cachedDriveRoot = string.Empty;
                _cachedFreeText = string.Empty;

                try
                {
                    var directories = new List<string>(Directory.GetDirectories(_currentDirectory));
                    directories.Sort(StringComparer.OrdinalIgnoreCase);
                    foreach (string directory in directories)
                    {
                        _items.Add(new ExplorerItem
                        {
                            Path = directory,
                            Name = DisplayName(directory),
                            IsDirectory = true
                        });
                        _cachedDirCount++;
                    }
                }
                catch (Exception ex)
                {
                    Message("Directory read failed: " + ex.Message);
                }

                try
                {
                    var files = new List<string>(Directory.GetFiles(_currentDirectory));
                    files.Sort(StringComparer.OrdinalIgnoreCase);
                    foreach (string file in files)
                    {
                        long size = 0;
                        try
                        {
                            size = new FileInfo(file).Length;
                        }
                        catch
                        {
                        }

                        _items.Add(new ExplorerItem
                        {
                            Path = file,
                            Name = Path.GetFileName(file),
                            IsDirectory = false,
                            SizeBytes = size
                        });
                        _cachedFileCount++;
                    }
                }
                catch (Exception ex)
                {
                    Message("File read failed: " + ex.Message);
                }
            }

            private void MoveSelection(int delta)
            {
                if (_items.Count == 0)
                    return;

                _selectedIndex = ClampValue(_selectedIndex + delta, 0, _items.Count - 1);
            }

            private void MoveToStart()
            {
                if (_items.Count > 0)
                    _selectedIndex = 0;
            }

            private void MoveToEnd()
            {
                if (_items.Count > 0)
                    _selectedIndex = _items.Count - 1;
            }

            private void JumpToItem(char firstCharacter)
            {
                if (_items.Count == 0)
                    return;

                int start = _selectedIndex + 1;
                for (int i = start; i < _items.Count; i++)
                {
                    if (StartsWith(_items[i].Name, firstCharacter))
                    {
                        _selectedIndex = i;
                        return;
                    }
                }

                for (int i = 0; i <= _selectedIndex; i++)
                {
                    if (StartsWith(_items[i].Name, firstCharacter))
                    {
                        _selectedIndex = i;
                        return;
                    }
                }
            }

            private void SelectPath(string path)
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    if (string.Equals(_items[i].Path, path, StringComparison.OrdinalIgnoreCase))
                    {
                        _selectedIndex = i;
                        return;
                    }
                }
            }

            private void ClampSelection()
            {
                if (_items.Count == 0)
                {
                    _selectedIndex = 0;
                    _scrollOffset = 0;
                    return;
                }

                _selectedIndex = ClampValue(_selectedIndex, 0, _items.Count - 1);
            }

            private void AdjustScroll(int rows)
            {
                int maxScroll = Math.Max(0, _items.Count - rows);
                if (_selectedIndex < _scrollOffset)
                    _scrollOffset = _selectedIndex;
                else if (_selectedIndex >= _scrollOffset + rows)
                    _scrollOffset = _selectedIndex - rows + 1;

                _scrollOffset = ClampValue(_scrollOffset, 0, maxScroll);
            }

            private int PageSize()
            {
                (int width, int height) = WindowSize();
                return Math.Max(1, height - 6);
            }

            private void Message(string message)
            {
                _message = message;
                _messageUntil = DateTime.UtcNow.AddMilliseconds(2200);
            }

            private static bool StartsWith(string value, char firstCharacter)
            {
                return !string.IsNullOrEmpty(value) &&
                    char.ToLowerInvariant(value[0]) == firstCharacter;
            }

            private static int ClampValue(int value, int min, int max)
            {
                if (value < min)
                    return min;

                if (value > max)
                    return max;

                return value;
            }

            private static string NormalizeDirectory(string path)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        string fileDirectory = Path.GetDirectoryName(path);
                        if (IsUsableDirectory(fileDirectory))
                            return TrimTrailingDirectorySeparator(Path.GetFullPath(fileDirectory));
                    }

                    if (IsUsableDirectory(path))
                        return TrimTrailingDirectorySeparator(Path.GetFullPath(path));
                }
                catch
                {
                }

                return TrimTrailingDirectorySeparator(Environment.CurrentDirectory);
            }

            private static string TrimTrailingDirectorySeparator(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                    return path;

                string fullPath = Path.GetFullPath(path);
                string root = Path.GetPathRoot(fullPath);
                if (string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase))
                    return fullPath;

                return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            private static string DisplayName(string path)
            {
                string trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string name = Path.GetFileName(trimmed);
                return string.IsNullOrEmpty(name) ? path : name;
            }

            private static string FormatSize(long bytes)
            {
                string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
                double size = bytes;
                int suffix = 0;

                while (size >= 1024 && suffix < suffixes.Length - 1)
                {
                    size /= 1024;
                    suffix++;
                }

                return suffix == 0
                    ? bytes + " " + suffixes[suffix]
                    : size.ToString("0.##") + " " + suffixes[suffix];
            }

            private static int FileColor(string path)
            {
                string extension = Path.GetExtension(path).ToLowerInvariant();

                if (extension == ".exe" || extension == ".msi" || extension == ".bat" || extension == ".cmd" || extension == ".ps1" || extension == ".sh")
                    return CError;

                if (extension == ".txt" || extension == ".md" || extension == ".log" || extension == ".ini" || extension == ".cfg" || extension == ".conf" || extension == ".csv")
                    return COperator;

                if (extension == ".cs" || extension == ".csx")
                    return CSharpType;

                if (extension == ".py" || extension == ".js" || extension == ".ts" || extension == ".cpp" || extension == ".c" ||
                    extension == ".h" || extension == ".java" || extension == ".go" || extension == ".rs" || extension == ".rb" || extension == ".php")
                    return CSourceFlow;

                if (extension == ".zip" || extension == ".rar" || extension == ".7z" || extension == ".tar" || extension == ".gz" || extension == ".bz2")
                    return CSearch;

                if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".gif" || extension == ".bmp" ||
                    extension == ".svg" || extension == ".ico" || extension == ".webp")
                    return CVariable;

                if (extension == ".mp3" || extension == ".mp4" || extension == ".avi" || extension == ".mkv" || extension == ".mov" ||
                    extension == ".wav" || extension == ".flac")
                    return CPreprocessor;

                if (extension == ".pdf" || extension == ".doc" || extension == ".docx" || extension == ".xls" || extension == ".xlsx" ||
                    extension == ".ppt" || extension == ".pptx")
                    return COperator;

                if (extension == ".dll" || extension == ".sys" || extension == ".lib" || extension == ".pdb")
                    return CError;

                return CNormal;
            }

            private static void AppendPaddedSelection(StringBuilder sb, string text, int width, bool selected, int normalColor)
            {
                text = text ?? string.Empty;
                if (selected)
                {
                    string body = Clip(text, Math.Max(0, width - 1)).PadRight(Math.Max(0, width - 1));
                    sb.Append(F(45)).Append(B(24)).Append("\u258c")
                        .Append(B(23)).Append(Bold()).Append(F(253)).Append(body).Append(Reset);
                    return;
                }

                sb.Append(F(normalColor)).Append(Clip(text, width).PadRight(width)).Append(Reset);
            }

            private static void WriteRawLine(int left, int top, string text, int width, int color)
            {
                Console.Write(At(left, top) + F(color) + Clip(text ?? string.Empty, width).PadRight(width) + Reset);
            }

            private readonly struct DetailLine
            {
                public DetailLine(string text, int color)
                {
                    Text = text;
                    Color = color;
                }

                public string Text { get; }
                public int Color { get; }
            }

            private sealed class ExplorerItem
            {
                public string Path { get; set; }
                public string Name { get; set; }
                public bool IsDirectory { get; set; }
                public long SizeBytes { get; set; }
            }

            private sealed class SearchItem
            {
                public string Path { get; set; }
                public bool IsDirectory { get; set; }
            }

            private sealed class ExplorerLocation
            {
                public string Path { get; set; }
                public int SelectedIndex { get; set; }
                public int ScrollOffset { get; set; }
            }
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
