using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace Core.Spreadsheets
{
    [SupportedOSPlatform("windows")]
    public sealed class SpreadsheetGridEditor
    {
        // ── VT / screen control ──────────────────────────────────────────────
        private const string AltScreen   = "\x1b[?1049h";
        private const string NormalScreen = "\x1b[?1049l";
        private const string ClearAll    = "\x1b[2J";
        private const string CursorHome  = "\x1b[H";
        private const string HideCursor  = "\x1b[?25l";
        private const string ShowCursor  = "\x1b[?25h";
        private const string Reset       = "\x1b[0m";
        // Synchronized-output mode: buffer the whole frame, then display atomically.
        // Terminals that don't support this safely ignore the sequences.
        private const string BeginSync   = "\x1b[?2026h";
        private const string EndSync     = "\x1b[?2026l";

        private const int StdInputHandle = -10;
        private const int EnableProcessedInput = 0x0001;
        private const int EnableWindowInput = 0x0008;
        private const int EnableMouseInput = 0x0010;
        private const int EnableExtendedFlags = 0x0080;
        private const int EnableQuickEditMode = 0x0040;
        private const short KeyEvent = 0x0001;
        private const short MouseEvent = 0x0002;
        private const short WindowBufferSizeEvent = 0x0004;
        private const int FromLeft1stButtonPressed = 0x0001;
        private const int MouseMoved = 0x0001;
        private const int MouseWheeled = 0x0004;
        private const int MouseHorizontalWheeled = 0x0008;
        private const int AltPressed = 0x0003;
        private const int ControlPressed = 0x000c;
        private const int ShiftPressed = 0x0010;
        private const int WheelDelta = 120;
        private const int RowsPerWheelNotch = 3;
        private const int ColumnsPerWheelNotch = 1;
        private const int ColumnHeaderRow = 3;
        private const int DataTopRow = 4;
        private const int FooterRows = 2;
        private const int VkShift = 0x10;
        private const int VkControl = 0x11;
        private const int VkS = 0x53;

        // Thin vertical box-drawing separator
        private const char Sep = '│';

        // ── Colour palette — xTerminal style (24-bit RGB) ───────────────────
        // All colours are derived from the xTerminal 256-colour palette used
        // across TermXTEditor and ProcessListingUI.
        //   Brand cyan  = xterm 45  → #00d7ff
        //   Accent cyan = xterm 38  → #00afd7
        //   Dark teal   = xterm 23  → #005f5f  (selection)
        //   Near-black  = xterm 232 → #080808
        //   Dark bg     = xterm 233 → #121212
        //   Dim bg      = xterm 234 → #1c1c1c
        //   Text        = xterm 252 → #d0d0d0
        //   Bright text = xterm 253 → #dadada
        //   Muted       = xterm 240 → #585858
        //   Very muted  = xterm 238 → #444444
        //   Saved green = xterm 78  → #5fd787
        //   Modified    = xterm 214 → #ffaf00

        // Title bar
        private const string BgTitle      = "\x1b[48;2;18;18;18m";   // xterm 233
        private const string FgTitleText  = "\x1b[38;2;0;215;255m";   // xterm 45 - brand cyan
        private const string FgTitleSep   = "\x1b[38;2;68;68;68m";    // xterm 238
        private const string FgSaved      = "\x1b[38;2;95;215;135m";  // xterm 78
        private const string FgModified   = "\x1b[38;2;255;175;0m";   // xterm 214

        // Info bar  (sheet name + cell ref)
        private const string BgInfo       = "\x1b[48;2;8;8;8m";       // xterm 232
        private const string FgInfoLabel  = "\x1b[38;2;0;175;215m";   // xterm 38 - accent cyan
        private const string FgInfoValue  = "\x1b[38;2;218;218;218m"; // xterm 253

        // Help/hints bar
        private const string BgHelp       = "\x1b[48;2;8;8;8m";       // xterm 232
        private const string FgHelp       = "\x1b[38;2;68;68;68m";    // xterm 238

        // Row-number header column
        private const string BgRowHdr     = "\x1b[48;2;18;18;18m";    // xterm 233
        private const string FgRowHdr     = "\x1b[38;2;88;88;88m";    // xterm 240
        private const string BgRowHdrSel  = "\x1b[48;2;0;95;95m";     // xterm 23
        private const string FgRowHdrSel  = "\x1b[38;2;0;215;255m";   // xterm 45

        // Column-letter header row
        private const string BgColHdr     = "\x1b[48;2;18;18;18m";    // xterm 233
        private const string FgColHdr     = "\x1b[38;2;88;88;88m";    // xterm 240
        private const string BgColHdrSel  = "\x1b[48;2;0;95;95m";     // xterm 23
        private const string FgColHdrSel  = "\x1b[38;2;0;215;255m";   // xterm 45

        // Cell backgrounds / foregrounds
        private const string BgCell       = "\x1b[48;2;10;10;10m";    // near-black
        private const string FgCell       = "\x1b[38;2;208;208;208m"; // xterm 252
        private const string BgRowHl      = "\x1b[48;2;20;20;20m";    // slightly lifted
        private const string FgRowHl      = "\x1b[38;2;218;218;218m"; // xterm 253
        private const string BgColHl      = "\x1b[48;2;14;22;22m";    // dark teal tint
        private const string FgColHl      = "\x1b[38;2;0;175;215m";   // xterm 38
        private const string BgSelected   = "\x1b[48;2;0;95;95m";     // xterm 23
        private const string FgSelected   = "\x1b[38;2;0;215;255m";   // xterm 45

        // Separator colour (within rows)
        private const string FgSepDim     = "\x1b[38;2;44;44;44m";    // xterm 238 dark

        // Status bar
        private const string BgStatus     = "\x1b[48;2;8;8;8m";       // xterm 232
        private const string FgStatusInfo = "\x1b[38;2;88;88;88m";    // xterm 240
        private const string FgStatusMsg  = "\x1b[38;2;255;175;0m";   // xterm 214

        // Formula / prompt bar
        private const string BgFormula    = "\x1b[48;2;0;28;28m";     // very dark teal
        private const string FgFmlaRef    = "\x1b[38;2;0;215;255m";   // xterm 45
        private const string FgFmlaVal    = "\x1b[38;2;218;218;218m"; // xterm 253

        // ── State ────────────────────────────────────────────────────────────
        private readonly SpreadsheetWorkbook _workbook;
        private readonly string _path;
        private int _selectedRow;
        private int _selectedColumn;
        private int _rowOffset;
        private int _columnOffset;
        private bool _dirty;
        private string _message;
        private int _lastWidth;
        private int _lastHeight;
        private SelectionKind _selectionKind;
        private int _selectionAnchorRow;
        private int _selectionAnchorColumn;
        private int _selectionEndRow;
        private int _selectionEndColumn;
        private bool _mouseSelecting;
        private bool _keepSelectionInView = true;
        private bool _saveAfterPrompt;
        private bool _saveShortcutDown;

        public SpreadsheetGridEditor(SpreadsheetWorkbook workbook, string path)
        {
            _workbook = workbook ?? throw new ArgumentNullException(nameof(workbook));
            _path = path ?? string.Empty;
            _message = string.Empty;
        }

        // ── Entry point ──────────────────────────────────────────────────────

        public void Run()
        {
            Console.OutputEncoding = Encoding.UTF8;
            var oldFg = Console.ForegroundColor;
            var oldBg = Console.BackgroundColor;
            bool oldTreatControlCAsInput = Console.TreatControlCAsInput;
            IntPtr inputHandle = GetStdHandle(StdInputHandle);
            int oldInputMode;
            bool restoreInputMode = TryGetConsoleInputMode(inputHandle, out oldInputMode);

            if (restoreInputMode)
                SetConsoleMode(inputHandle, DisableNativeConsoleSelectionMode(oldInputMode));

            Console.TreatControlCAsInput = true;

            Console.Write(AltScreen + HideCursor + ClearAll + CursorHome);

            try
            {
                bool running = true;
                while (running)
                {
                    Render();
                    ConsoleKeyInfo key;
                    if (WaitForInput(out key))
                        running = HandleKey(key);
                }
            }
            finally
            {
                Console.ForegroundColor = oldFg;
                Console.BackgroundColor = oldBg;
                if (restoreInputMode)
                    SetConsoleMode(inputHandle, oldInputMode);

                Console.TreatControlCAsInput = oldTreatControlCAsInput;
                Console.Write(ShowCursor + Reset + NormalScreen);
            }
        }

        // ── Input ────────────────────────────────────────────────────────────

        private bool HandleKey(ConsoleKeyInfo key)
        {
            bool ctrl  = (key.Modifiers & ConsoleModifiers.Control) != 0;
            bool shift = (key.Modifiers & ConsoleModifiers.Shift)   != 0;

            if (IsSaveKey(key))                       { Save();     return true; }
            if (IsCtrlKey(key, ConsoleKey.N, '\x0e')) { AddSheet(); return true; }
            if (IsCtrlKey(key, ConsoleKey.C, '\x03')) { CopySelectionToClipboard(); return true; }
            if (IsCtrlKey(key, ConsoleKey.Q, '\x11')) return TryQuit();
            if (ctrl && key.Key == ConsoleKey.Spacebar) { SelectColumnRange(_selectedColumn, _selectedColumn); return true; }

            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    if (HasSelection()) { ClearSelection(); break; }
                    return TryQuit();
                case ConsoleKey.Q:
                    return TryQuit();
                case ConsoleKey.S:
                    Save();
                    break;
                case ConsoleKey.UpArrow:
                    if (!TryMoveWholeSelection(-1, 0, shift))
                        MoveTo(_selectedRow - 1, _selectedColumn, shift);
                    break;
                case ConsoleKey.DownArrow:
                    if (!TryMoveWholeSelection(1, 0, shift))
                        MoveTo(_selectedRow + 1, _selectedColumn, shift);
                    break;
                case ConsoleKey.LeftArrow:
                    if (!TryMoveWholeSelection(0, -1, shift))
                        MoveTo(_selectedRow, _selectedColumn - 1, shift);
                    break;
                case ConsoleKey.RightArrow:
                    if (!TryMoveWholeSelection(0, 1, shift))
                        MoveTo(_selectedRow, _selectedColumn + 1, shift);
                    break;
                case ConsoleKey.PageUp:
                    MoveTo(_selectedRow - VisibleRowCount(), _selectedColumn, shift);
                    break;
                case ConsoleKey.PageDown:
                    MoveTo(_selectedRow + VisibleRowCount(), _selectedColumn, shift);
                    break;
                case ConsoleKey.Home:
                    MoveTo(_selectedRow, 0, shift);
                    break;
                case ConsoleKey.End:
                    MoveTo(_selectedRow, Math.Max(0, ActiveSheet.UsedColumnCount - 1), shift);
                    break;
                case ConsoleKey.Tab:
                    SwitchSheet(shift ? -1 : 1);
                    break;
                case ConsoleKey.Enter:
                case ConsoleKey.F2:
                    EditSelectedCell();
                    break;
                case ConsoleKey.Delete:
                    ClearSelectedCell();
                    break;
                case ConsoleKey.Insert:
                    if (ctrl) InsertColumn();
                    else      InsertRow();
                    break;
                case ConsoleKey.F5:
                    GoToCell();
                    break;
            }

            return true;
        }

        private SpreadsheetWorksheet ActiveSheet => _workbook.ActiveWorksheet;

        // ── Rendering ────────────────────────────────────────────────────────

        private void Render()
        {
            int width  = Math.Max(Console.WindowWidth,  60);
            int height = Math.Max(Console.WindowHeight, 18);

            bool sizeChanged = (width != _lastWidth) || (height != _lastHeight);
            _lastWidth  = width;
            _lastHeight = height;

            var sheet = ActiveSheet;
            int rowHeaderWidth = Math.Max(6,
                Math.Max(sheet.RowCount, _selectedRow + 1)
                    .ToString(CultureInfo.InvariantCulture).Length + 2);
            int cellWidth   = width >= 120 ? 16 : 12;
            int visibleRows = VisibleRowCount();
            int visibleCols = Math.Max(1, (width - rowHeaderWidth) / (cellWidth + 1));

            ClampOffsets(visibleRows, visibleCols);

            // Pre-allocate enough capacity to avoid reallocs during one frame
            var buf = new StringBuilder(width * height * 32);

            // Tell the terminal to buffer this entire frame and display atomically.
            buf.Append(BeginSync);

            if (sizeChanged)
                buf.Append(ClearAll);

            buf.Append(CursorHome);

            AppendTitleBar(buf, width);
            AppendInfoBar(buf, width);
            AppendHelpBar(buf, width);
            AppendColumnHeaders(buf, width, rowHeaderWidth, cellWidth, visibleCols);
            AppendRows(buf, width, rowHeaderWidth, cellWidth, visibleRows, visibleCols);
            AppendStatusBar(buf, width, height);
            AppendFormulaBar(buf, width, height);

            buf.Append(Reset);
            buf.Append(EndSync);
            Console.Write(buf.ToString());
        }

        private void AppendTitleBar(StringBuilder buf, int width)
        {
            string fileName  = string.IsNullOrWhiteSpace(_path) ? "untitled" : Path.GetFileName(_path);
            string dirtyClr  = _dirty ? FgModified : FgSaved;
            string dirtyText = _dirty ? "modified" : "saved";

            buf.Append(BgTitle);
            string line = "  " + FgTitleText + "xTerminal Excel"
                + "  " + FgTitleSep + "│"
                + "  " + FgTitleText + fileName
                + "  " + FgTitleSep + "│"
                + "  " + dirtyClr + dirtyText;
            AppendFilledLine(buf, line, width, BgTitle);
        }

        private void AppendInfoBar(StringBuilder buf, int width)
        {
            string sheetNum = (_workbook.ActiveSheetIndex + 1).ToString(CultureInfo.InvariantCulture)
                + "/" + _workbook.Worksheets.Count.ToString(CultureInfo.InvariantCulture);
            string cellRef  = HasSelection()
                ? SelectionLabel()
                : CellName(_selectedRow, _selectedColumn);
            string modeTag  = HasSelection()
                ? "  " + FgTitleSep + "│" + "  " + FgModified + "SELECT"
                : string.Empty;

            buf.Append(BgInfo);
            string line = "  " + FgInfoLabel + "Sheet "
                + FgInfoValue + sheetNum
                + "  " + FgInfoLabel + ActiveSheet.Name
                + "  " + FgTitleSep + "│"
                + "  " + FgInfoLabel + "Cell " + FgInfoValue + cellRef
                + modeTag;
            AppendFilledLine(buf, line, width, BgInfo);
        }

        private void AppendHelpBar(StringBuilder buf, int width)
        {
            buf.Append(BgHelp + FgHelp);
            const string hint = "  Wheel-scroll  │  Shift+wheel-columns  │  Mouse-select  │  Ctrl+C-copy  │  Column header-select column  │  Ctrl+S-save  │  Esc-quit";
            // Hint string is longer than narrow terminals — must truncate to prevent line wrapping.
            AppendFilledLine(buf, Trim(hint, width), width, BgHelp);
        }

        private void AppendColumnHeaders(StringBuilder buf, int width, int rowHeaderWidth, int cellWidth, int visibleCols)
        {
            // Corner cell (top-left blank)
            buf.Append(BgRowHdr + FgRowHdr);
            buf.Append(new string(' ', rowHeaderWidth));

            int x = rowHeaderWidth;
            for (int i = 0; i < visibleCols && x < width; i++)
            {
                int col      = _columnOffset + i;
                bool colSel  = IsColumnHeaderSelected(col);
                string label = Center(XlsxSpreadsheetFormat.GetColumnName(col), cellWidth);

                buf.Append(colSel ? BgColHdrSel : BgColHdr);
                buf.Append(FgSepDim);
                buf.Append(Sep);
                buf.Append(colSel ? FgColHdrSel : FgColHdr);
                buf.Append(label);
                x += cellWidth + 1;
            }

            // Fill remainder of column-header row
            if (x < width)
            {
                buf.Append(BgColHdr);
                buf.Append(new string(' ', width - x));
            }

            buf.Append("\r\n");
        }

        private void AppendRows(StringBuilder buf, int width, int rowHeaderWidth, int cellWidth, int visibleRows, int visibleCols)
        {
            var sheet = ActiveSheet;
            // The data area occupies rows 4 … height-3 (0-indexed).
            int maxDataRows = Math.Max(0, _lastHeight - 6);

            for (int rowSlot = 0; rowSlot < maxDataRows; rowSlot++)
            {
                int row    = _rowOffset + rowSlot;
                bool rowSel = row == _selectedRow;
                bool rowHeaderSel = IsRowHeaderSelected(row);

                string rowLabel = (row + 1).ToString(CultureInfo.InvariantCulture)
                    .PadLeft(rowHeaderWidth - 1) + " ";

                buf.Append(rowHeaderSel ? BgRowHdrSel : BgRowHdr);
                buf.Append(rowHeaderSel ? FgRowHdrSel : FgRowHdr);
                buf.Append(Trim(rowLabel, rowHeaderWidth));

                int x = rowHeaderWidth;

                if (rowSlot < visibleRows)
                {
                    // Render actual data cells
                    for (int colSlot = 0; colSlot < visibleCols && x < width; colSlot++)
                    {
                        int col    = _columnOffset + colSlot;
                        bool colSel = col == _selectedColumn;
                        bool sel    = IsCellSelected(row, col);

                        string value = NormalizeCellText(sheet.GetCell(row, col));
                        string cell  = Pad(Trim(value, cellWidth), cellWidth);

                        string sepBg, cellBg, cellFg;

                        sepBg = sel    ? BgSelected :
                                rowSel ? BgRowHl    :
                                colSel ? BgColHl    : BgCell;
                        if (sel)         { cellBg = BgSelected; cellFg = FgSelected; }
                        else if (rowSel) { cellBg = BgRowHl;    cellFg = FgRowHl;    }
                        else if (colSel) { cellBg = BgColHl;    cellFg = FgColHl;    }
                        else             { cellBg = BgCell;     cellFg = FgCell;     }

                        buf.Append(sepBg + FgSepDim + Sep);
                        buf.Append(cellBg + cellFg);
                        buf.Append(cell);
                        x += cellWidth + 1;
                    }
                }

                // Fill any remaining width (blank or empty-row fill)
                if (x < width)
                {
                    buf.Append(BgCell + FgCell);
                    buf.Append(new string(' ', width - x));
                }

                buf.Append("\r\n");
            }
        }

        private void AppendStatusBar(StringBuilder buf, int width, int height)
        {
            var sheet = ActiveSheet;
            string format = _workbook.FileKind == SpreadsheetFileKind.LegacyXls
                ? "xls/xml"
                : _workbook.FileKind.ToString().ToLowerInvariant();

            string statusText = "  " + format
                + "  │  rows " + sheet.UsedRowCount.ToString(CultureInfo.InvariantCulture)
                + "  cols "    + sheet.UsedColumnCount.ToString(CultureInfo.InvariantCulture)
                + "  │  "      + (_dirty ? "unsaved changes" : "no changes");

            if (!string.IsNullOrWhiteSpace(_message))
                statusText += "  │  " + FgStatusMsg + _message + FgStatusInfo;

            // Jump directly to the second-to-last row (1-indexed ANSI)
            buf.Append($"\x1b[{height - 1};1H");
            buf.Append(BgStatus + FgStatusInfo);
            AppendFilledLine(buf, statusText, width, BgStatus);
        }

        private void AppendFormulaBar(StringBuilder buf, int width, int height)
        {
            string cellRef   = CellName(_selectedRow, _selectedColumn);
            string cellValue = NormalizeCellText(ActiveSheet.GetCell(_selectedRow, _selectedColumn));

            buf.Append($"\x1b[{height};1H");
            buf.Append(BgFormula);
            string line = "  " + FgFmlaRef + cellRef + FgFmlaVal + " = " + cellValue;
            AppendFilledLine(buf, line, width, BgFormula);
        }

        // ── Navigation & editing ─────────────────────────────────────────────

        private void MoveTo(int row, int column, bool extendSelection = false)
        {
            int nextRow = Math.Max(0, row);
            int nextColumn = Math.Max(0, column);
            _keepSelectionInView = true;

            if (extendSelection)
            {
                if (_selectionKind != SelectionKind.CellRange)
                {
                    _selectionKind = SelectionKind.CellRange;
                    _selectionAnchorRow = _selectedRow;
                    _selectionAnchorColumn = _selectedColumn;
                }

                _selectionEndRow = nextRow;
                _selectionEndColumn = nextColumn;
            }
            else
            {
                ClearSelectionCore();
            }

            _selectedRow    = nextRow;
            _selectedColumn = nextColumn;
            _message        = string.Empty;
        }

        private void ClampOffsets(int visibleRows, int visibleColumns)
        {
            if (_keepSelectionInView)
            {
                if (_selectedRow < _rowOffset)
                    _rowOffset = _selectedRow;
                else if (_selectedRow >= _rowOffset + visibleRows)
                    _rowOffset = _selectedRow - visibleRows + 1;

                if (_selectedColumn < _columnOffset)
                    _columnOffset = _selectedColumn;
                else if (_selectedColumn >= _columnOffset + visibleColumns)
                    _columnOffset = _selectedColumn - visibleColumns + 1;
            }

            _rowOffset    = Math.Max(0, _rowOffset);
            _columnOffset = Math.Max(0, _columnOffset);
        }

        private int VisibleRowCount() => Math.Max(1, Console.WindowHeight - 6);

        private void EditSelectedCell()
        {
            string current = ActiveSheet.GetCell(_selectedRow, _selectedColumn);
            _saveAfterPrompt = false;
            if (!ReadPrompt(" " + CellName(_selectedRow, _selectedColumn) + " > ", current, out var value, saveOnCtrlS: true))
            {
                _message = "edit canceled";
                return;
            }

            ActiveSheet.SetCell(_selectedRow, _selectedColumn, value);
            _dirty   = true;
            if (_saveAfterPrompt)
            {
                _saveAfterPrompt = false;
                Save();
            }
            else
            {
                _message = "cell updated";
            }
        }

        private void ClearSelectedCell()
        {
            ActiveSheet.SetCell(_selectedRow, _selectedColumn, string.Empty);
            _dirty   = true;
            _message = CellName(_selectedRow, _selectedColumn) + " cleared";
        }

        private void InsertRow()
        {
            ActiveSheet.InsertRow(_selectedRow + 1);
            _selectedRow++;
            _dirty   = true;
            _message = "row inserted";
        }

        private void InsertColumn()
        {
            ActiveSheet.InsertColumn(_selectedColumn + 1);
            _selectedColumn++;
            _dirty   = true;
            _message = "column inserted";
        }

        private void SwitchSheet(int direction)
        {
            if (_workbook.Worksheets.Count == 0)
                return;

            int next = _workbook.ActiveSheetIndex + direction;
            if (next < 0) next = _workbook.Worksheets.Count - 1;
            if (next >= _workbook.Worksheets.Count) next = 0;

            _workbook.ActiveSheetIndex = next;
            ClearSelectionCore();
            _selectedRow    = 0;
            _selectedColumn = 0;
            _rowOffset      = 0;
            _columnOffset   = 0;
            _keepSelectionInView = true;
            _message        = "sheet changed";
        }

        private void AddSheet()
        {
            string defaultName = "Sheet" + (_workbook.Worksheets.Count + 1).ToString(CultureInfo.InvariantCulture);
            if (!ReadPrompt(" New sheet name > ", defaultName, out var name))
            {
                _message = "new sheet canceled";
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
                name = defaultName;

            _workbook.Worksheets.Add(new SpreadsheetWorksheet(name));
            _workbook.ActiveSheetIndex = _workbook.Worksheets.Count - 1;
            ClearSelectionCore();
            _selectedRow    = 0;
            _selectedColumn = 0;
            _rowOffset      = 0;
            _columnOffset   = 0;
            _keepSelectionInView = true;
            _dirty   = true;
            _message = "sheet added";
        }

        private void GoToCell()
        {
            if (!ReadPrompt(" Go to cell > ", CellName(_selectedRow, _selectedColumn), out var reference))
            {
                _message = "goto canceled";
                return;
            }

            if (XlsxSpreadsheetFormat.TryParseReference(reference, out var row, out var column))
            {
                MoveTo(row, column);
                _message = "moved to " + CellName(row, column);
            }
            else
            {
                _message = "invalid cell reference";
            }
        }

        private void Save()
        {
            try
            {
                SpreadsheetFile.Save(_workbook, _path);
                _dirty   = false;
                _message = _workbook.FileKind == SpreadsheetFileKind.LegacyXls
                    ? "saved as Excel XML .xls"
                    : "saved";
            }
            catch (Exception ex)
            {
                _message = "save failed: " + ex.Message;
            }
        }

        private bool TryQuit()
        {
            if (!_dirty)
                return false;

            int h = _lastHeight > 0 ? _lastHeight : Console.WindowHeight;
            int w = _lastWidth  > 0 ? _lastWidth  : Console.WindowWidth;

            var buf = new StringBuilder();
            buf.Append($"\x1b[{h};1H");
            buf.Append("\x1b[48;2;110;20;20m\x1b[38;2;255;235;235m");
            buf.Append(Pad("  Save changes before quit?   Y = yes   N = no   Esc = cancel  ", w));
            buf.Append(Reset);
            Console.Write(buf.ToString());

            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Y)    { Save(); return !_dirty; }
                if (key.Key == ConsoleKey.N)    return false;
                if (key.Key == ConsoleKey.Escape) { _message = "quit canceled"; return true; }
            }
        }

        // ── Prompt / inline editor ───────────────────────────────────────────

        private bool ReadPrompt(string prompt, string initial, out string value, bool saveOnCtrlS = false)
        {
            value = initial ?? string.Empty;
            var text   = new StringBuilder(value);
            int cursor = text.Length;

            Console.Write(ShowCursor);
            try
            {
                while (true)
                {
                    DrawPrompt(prompt, text.ToString(), cursor);
                    var key = ReadPromptKey(saveOnCtrlS);
                    if (saveOnCtrlS && IsSaveKey(key))
                    {
                        value = text.ToString();
                        _saveAfterPrompt = true;
                        return true;
                    }

                    switch (key.Key)
                    {
                        case ConsoleKey.Enter:
                            value = text.ToString();
                            return true;
                        case ConsoleKey.Escape:
                            return false;
                        case ConsoleKey.LeftArrow:
                            cursor = Math.Max(0, cursor - 1);
                            break;
                        case ConsoleKey.RightArrow:
                            cursor = Math.Min(text.Length, cursor + 1);
                            break;
                        case ConsoleKey.Home:
                            cursor = 0;
                            break;
                        case ConsoleKey.End:
                            cursor = text.Length;
                            break;
                        case ConsoleKey.Backspace:
                            if (cursor > 0) { text.Remove(cursor - 1, 1); cursor--; }
                            break;
                        case ConsoleKey.Delete:
                            if (cursor < text.Length) text.Remove(cursor, 1);
                            break;
                        default:
                            if (!char.IsControl(key.KeyChar))
                            {
                                text.Insert(cursor, key.KeyChar);
                                cursor++;
                            }
                            break;
                    }
                }
            }
            finally
            {
                Console.Write(HideCursor);
            }
        }

        private ConsoleKeyInfo ReadPromptKey(bool saveOnCtrlS)
        {
            while (true)
            {
                ConsoleKeyInfo key;
                if (saveOnCtrlS && TryGetAsyncSaveKey(out key))
                    return key;

                if (IsConsoleKeyAvailable())
                    return Console.ReadKey(intercept: true);

                Thread.Sleep(30);
            }
        }

        private void DrawPrompt(string prompt, string text, int cursor)
        {
            int width     = Math.Max(_lastWidth  > 0 ? _lastWidth  : Console.WindowWidth,  20);
            int rowAnsi   = Math.Max(_lastHeight > 0 ? _lastHeight : Console.WindowHeight, 1);
            int available = Math.Max(1, width - prompt.Length - 1);

            int displayStart = 0;
            if (text.Length > available)
            {
                displayStart = Math.Max(0, cursor - available + 1);
                displayStart = Math.Min(displayStart, Math.Max(0, text.Length - available));
            }

            string display = text.Substring(displayStart, Math.Min(available, text.Length - displayStart));

            var buf = new StringBuilder();
            buf.Append($"\x1b[{rowAnsi};1H");
            buf.Append(BgFormula + FgFmlaRef);
            buf.Append(Pad(prompt, prompt.Length));
            buf.Append(FgFmlaVal);
            buf.Append(Pad(display, width - prompt.Length));
            buf.Append(Reset);
            Console.Write(buf.ToString());

            int cursorX = Math.Min(width - 1, prompt.Length + cursor - displayStart);
            Console.SetCursorPosition(Math.Max(0, cursorX), rowAnsi - 1);
        }

        // ── Selection / clipboard / mouse input ─────────────────────────────

        private bool WaitForInput(out ConsoleKeyInfo key)
        {
            key = default(ConsoleKeyInfo);

            while (true)
            {
                if (TryGetAsyncSaveKey(out key))
                    return true;

                if (TryProcessPendingConsoleInput(out key))
                    return true;

                if (IsConsoleKeyAvailable())
                {
                    key = Console.ReadKey(intercept: true);
                    return true;
                }

                Thread.Sleep(30);
            }
        }

        private bool TryGetAsyncSaveKey(out ConsoleKeyInfo key)
        {
            key = default(ConsoleKeyInfo);
            bool pressed = IsAsyncKeyDown(VkControl) && IsAsyncKeyDown(VkS);
            if (!pressed)
            {
                _saveShortcutDown = false;
                return false;
            }

            if (_saveShortcutDown)
                return false;

            _saveShortcutDown = true;
            key = new ConsoleKeyInfo('\x13', ConsoleKey.S, IsAsyncKeyDown(VkShift), false, true);
            return true;
        }

        private bool TryProcessPendingConsoleInput(out ConsoleKeyInfo key)
        {
            key = default(ConsoleKeyInfo);

            IntPtr inputHandle = GetStdHandle(StdInputHandle);
            InputRecord record;
            if (!TryPeekConsoleInput(inputHandle, out record))
                return false;

            if (record.EventType == MouseEvent)
            {
                if (!TryReadConsoleInputRecord(inputHandle, out record))
                    return false;

                return HandleMouseInput(record.MouseEvent);
            }

            if (record.EventType == KeyEvent)
            {
                if (!record.KeyEvent.KeyDown)
                {
                    TryReadConsoleInputRecord(inputHandle, out record);
                    return false;
                }

                if (TryGetRawControlKey(record.KeyEvent, out key))
                {
                    TryReadConsoleInputRecord(inputHandle, out record);
                    return true;
                }

                return false;
            }

            if (record.EventType == WindowBufferSizeEvent)
            {
                TryReadConsoleInputRecord(inputHandle, out record);
                return true;
            }

            return false;
        }

        private static bool TryGetRawControlKey(KeyEventRecord record, out ConsoleKeyInfo key)
        {
            key = default(ConsoleKeyInfo);
            bool ctrl = (record.ControlKeyState & ControlPressed) != 0;

            ConsoleKey consoleKey;
            char keyChar;
            if (IsRawSaveKey(record, ctrl, out consoleKey, out keyChar)
                || IsRawControlKey(record, ctrl, ConsoleKey.N, '\x0e', out consoleKey, out keyChar)
                || IsRawControlKey(record, ctrl, ConsoleKey.C, '\x03', out consoleKey, out keyChar)
                || IsRawControlKey(record, ctrl, ConsoleKey.Q, '\x11', out consoleKey, out keyChar))
            {
                key = new ConsoleKeyInfo(
                    keyChar,
                    consoleKey,
                    (record.ControlKeyState & ShiftPressed) != 0,
                    (record.ControlKeyState & AltPressed) != 0,
                    true);
                return true;
            }

            return false;
        }

        private static bool IsRawSaveKey(KeyEventRecord record, bool ctrl, out ConsoleKey key, out char keyChar)
        {
            key = ConsoleKey.S;
            keyChar = '\x13';
            bool shift = (record.ControlKeyState & ShiftPressed) != 0;
            return record.UnicodeChar == '\x13'
                   || (ctrl && record.VirtualKeyCode == (short)ConsoleKey.S)
                   || (ctrl && shift && (record.UnicodeChar == 'S' || record.UnicodeChar == 's'));
        }

        private static bool IsRawControlKey(KeyEventRecord record, bool ctrl, ConsoleKey expectedKey, char expectedChar, out ConsoleKey key, out char keyChar)
        {
            key = expectedKey;
            keyChar = expectedChar;
            return record.UnicodeChar == expectedChar || (ctrl && record.VirtualKeyCode == (short)expectedKey);
        }

        private bool HandleMouseInput(MouseEventRecord mouse)
        {
            if ((mouse.EventFlags & (MouseWheeled | MouseHorizontalWheeled)) != 0)
                return ScrollViewportFromWheel(mouse);

            bool leftDown = (mouse.ButtonState & FromLeft1stButtonPressed) != 0;
            bool moved = (mouse.EventFlags & MouseMoved) != 0;
            bool leftReleased = !leftDown && _mouseSelecting;

            if (!leftDown && !leftReleased)
                return false;

            GridPosition position;
            if (!TryGetMouseGridPosition(mouse.MousePosition.X, mouse.MousePosition.Y, out position))
            {
                if (leftReleased)
                    _mouseSelecting = false;

                return false;
            }

            if (leftDown && !_mouseSelecting)
            {
                _mouseSelecting = true;
                StartMouseSelection(position);
                return true;
            }

            if (_mouseSelecting && (leftDown || leftReleased))
            {
                ExtendMouseSelection(position);

                if (leftReleased)
                    _mouseSelecting = false;

                return moved || leftReleased || leftDown;
            }

            return false;
        }

        private bool TryGetMouseGridPosition(int x, int y, out GridPosition position)
        {
            position = default(GridPosition);

            int width = Math.Max(_lastWidth > 0 ? _lastWidth : Console.WindowWidth, 60);
            int height = Math.Max(_lastHeight > 0 ? _lastHeight : Console.WindowHeight, 18);
            int rowHeaderWidth = GetRowHeaderWidth(ActiveSheet);
            int cellWidth = GetCellWidth(width);
            int visibleCols = GetVisibleColumnCount(width, rowHeaderWidth, cellWidth);

            if (y == ColumnHeaderRow)
            {
                if (x < rowHeaderWidth)
                    return false;

                int colSlot = (x - rowHeaderWidth) / (cellWidth + 1);
                if (colSlot < 0 || colSlot >= visibleCols)
                    return false;

                position = new GridPosition(GridHitKind.ColumnHeader, 0, _columnOffset + colSlot);
                return true;
            }

            int dataY = y + 1;
            int dataRows = Math.Max(0, height - DataTopRow - FooterRows);
            if (dataY < DataTopRow || dataY >= DataTopRow + dataRows)
                return false;

            int row = _rowOffset + (dataY - DataTopRow);
            if (x < rowHeaderWidth)
            {
                position = new GridPosition(GridHitKind.RowHeader, row, 0);
                return true;
            }

            int cellSlot = (x - rowHeaderWidth) / (cellWidth + 1);
            if (cellSlot < 0 || cellSlot >= visibleCols)
                return false;

            position = new GridPosition(GridHitKind.Cell, row, _columnOffset + cellSlot);
            return true;
        }

        private void StartMouseSelection(GridPosition position)
        {
            _keepSelectionInView = true;
            switch (position.Kind)
            {
                case GridHitKind.ColumnHeader:
                    SelectColumnRange(position.Column, position.Column);
                    _selectedColumn = position.Column;
                    break;
                case GridHitKind.RowHeader:
                    SelectRowRange(position.Row, position.Row);
                    _selectedRow = position.Row;
                    break;
                default:
                    SelectCellRange(position.Row, position.Column, position.Row, position.Column);
                    _selectedRow = position.Row;
                    _selectedColumn = position.Column;
                    break;
            }

            _message = string.Empty;
        }

        private void ExtendMouseSelection(GridPosition position)
        {
            switch (_selectionKind)
            {
                case SelectionKind.ColumnRange:
                    if (position.Kind == GridHitKind.ColumnHeader || position.Kind == GridHitKind.Cell)
                    {
                        _selectionEndColumn = position.Column;
                        _selectedColumn = position.Column;
                    }
                    break;
                case SelectionKind.RowRange:
                    if (position.Kind == GridHitKind.RowHeader || position.Kind == GridHitKind.Cell)
                    {
                        _selectionEndRow = position.Row;
                        _selectedRow = position.Row;
                    }
                    break;
                case SelectionKind.CellRange:
                    if (position.Kind == GridHitKind.Cell)
                    {
                        _selectionEndRow = position.Row;
                        _selectionEndColumn = position.Column;
                        _selectedRow = position.Row;
                        _selectedColumn = position.Column;
                    }
                    break;
            }

            _message = string.Empty;
        }

        private bool TryMoveWholeSelection(int rowDelta, int columnDelta, bool extendSelection)
        {
            if (_selectionKind == SelectionKind.ColumnRange && columnDelta != 0 && rowDelta == 0)
            {
                _keepSelectionInView = true;
                int nextColumn = Math.Max(0, _selectedColumn + columnDelta);
                if (extendSelection)
                    _selectionEndColumn = nextColumn;
                else
                    SelectColumnRange(nextColumn, nextColumn);

                _selectedColumn = nextColumn;
                _message = string.Empty;
                return true;
            }

            if (_selectionKind == SelectionKind.RowRange && rowDelta != 0 && columnDelta == 0)
            {
                _keepSelectionInView = true;
                int nextRow = Math.Max(0, _selectedRow + rowDelta);
                if (extendSelection)
                    _selectionEndRow = nextRow;
                else
                    SelectRowRange(nextRow, nextRow);

                _selectedRow = nextRow;
                _message = string.Empty;
                return true;
            }

            return false;
        }

        private bool ScrollViewportFromWheel(MouseEventRecord mouse)
        {
            int notches = GetWheelNotches(mouse.ButtonState);
            if (notches == 0)
                return false;

            bool horizontal = (mouse.EventFlags & MouseHorizontalWheeled) != 0
                              || (mouse.ControlKeyState & ShiftPressed) != 0;
            int oldRowOffset = _rowOffset;
            int oldColumnOffset = _columnOffset;

            if (horizontal)
            {
                int direction = (mouse.EventFlags & MouseHorizontalWheeled) != 0 ? notches : -notches;
                _columnOffset = Math.Max(0, _columnOffset + (direction * ColumnsPerWheelNotch));
            }
            else
            {
                _rowOffset = Math.Max(0, _rowOffset - (notches * RowsPerWheelNotch));
            }

            _keepSelectionInView = false;
            _message = string.Empty;
            return oldRowOffset != _rowOffset || oldColumnOffset != _columnOffset;
        }

        private void SelectCellRange(int startRow, int startColumn, int endRow, int endColumn)
        {
            _selectionKind = SelectionKind.CellRange;
            _keepSelectionInView = true;
            _selectionAnchorRow = Math.Max(0, startRow);
            _selectionAnchorColumn = Math.Max(0, startColumn);
            _selectionEndRow = Math.Max(0, endRow);
            _selectionEndColumn = Math.Max(0, endColumn);
        }

        private void SelectRowRange(int startRow, int endRow)
        {
            _selectionKind = SelectionKind.RowRange;
            _keepSelectionInView = true;
            _selectionAnchorRow = Math.Max(0, startRow);
            _selectionEndRow = Math.Max(0, endRow);
            _selectionAnchorColumn = 0;
            _selectionEndColumn = Math.Max(0, ActiveSheet.UsedColumnCount - 1);
        }

        private void SelectColumnRange(int startColumn, int endColumn)
        {
            _selectionKind = SelectionKind.ColumnRange;
            _keepSelectionInView = true;
            _selectionAnchorColumn = Math.Max(0, startColumn);
            _selectionEndColumn = Math.Max(0, endColumn);
            _selectionAnchorRow = 0;
            _selectionEndRow = Math.Max(0, ActiveSheet.UsedRowCount - 1);
        }

        private void ClearSelection()
        {
            ClearSelectionCore();
            _message = "selection cleared";
        }

        private void ClearSelectionCore()
        {
            _selectionKind = SelectionKind.None;
            _mouseSelecting = false;
            _keepSelectionInView = true;
            _selectionAnchorRow = 0;
            _selectionAnchorColumn = 0;
            _selectionEndRow = 0;
            _selectionEndColumn = 0;
        }

        private bool HasSelection()
        {
            return _selectionKind != SelectionKind.None;
        }

        private bool IsCellSelected(int row, int column)
        {
            switch (_selectionKind)
            {
                case SelectionKind.CellRange:
                    return IsWithin(row, SelectionStartRow(), SelectionEndRow())
                           && IsWithin(column, SelectionStartColumn(), SelectionEndColumn());
                case SelectionKind.RowRange:
                    return IsWithin(row, SelectionStartRow(), SelectionEndRow());
                case SelectionKind.ColumnRange:
                    return IsWithin(column, SelectionStartColumn(), SelectionEndColumn());
                default:
                    return row == _selectedRow && column == _selectedColumn;
            }
        }

        private bool IsRowHeaderSelected(int row)
        {
            return _selectionKind == SelectionKind.RowRange
                   && IsWithin(row, SelectionStartRow(), SelectionEndRow());
        }

        private bool IsColumnHeaderSelected(int column)
        {
            if (_selectionKind == SelectionKind.None)
                return column == _selectedColumn;

            if (_selectionKind == SelectionKind.RowRange)
                return false;

            return IsWithin(column, SelectionStartColumn(), SelectionEndColumn());
        }

        private void CopySelectionToClipboard()
        {
            string text;
            string copiedItem;
            if (!TryGetCopyTextForClipboard(out text, out copiedItem))
            {
                _message = "nothing to copy";
                return;
            }

            string error;
            if (TrySetClipboardText(text, out error))
            {
                _message = "copied " + copiedItem;
            }
            else
            {
                _message = error;
            }
        }

        private bool TryGetCopyTextForClipboard(out string text, out string copiedItem)
        {
            text = string.Empty;
            copiedItem = string.Empty;

            int startRow;
            int endRow;
            int startColumn;
            int endColumn;

            switch (_selectionKind)
            {
                case SelectionKind.RowRange:
                    startRow = SelectionStartRow();
                    endRow = SelectionEndRow();
                    startColumn = 0;
                    endColumn = Math.Max(0, ActiveSheet.UsedColumnCount - 1);
                    copiedItem = startRow == endRow ? "row" : "rows";
                    break;
                case SelectionKind.ColumnRange:
                    startRow = 0;
                    endRow = Math.Max(0, ActiveSheet.UsedRowCount - 1);
                    startColumn = SelectionStartColumn();
                    endColumn = SelectionEndColumn();
                    copiedItem = startColumn == endColumn ? "column" : "columns";
                    break;
                case SelectionKind.CellRange:
                    startRow = SelectionStartRow();
                    endRow = SelectionEndRow();
                    startColumn = SelectionStartColumn();
                    endColumn = SelectionEndColumn();
                    copiedItem = startRow == endRow && startColumn == endColumn ? "cell" : "range";
                    break;
                default:
                    startRow = _selectedRow;
                    endRow = _selectedRow;
                    startColumn = _selectedColumn;
                    endColumn = _selectedColumn;
                    copiedItem = "cell";
                    break;
            }

            text = BuildClipboardText(startRow, endRow, startColumn, endColumn);
            return true;
        }

        private string BuildClipboardText(int startRow, int endRow, int startColumn, int endColumn)
        {
            var builder = new StringBuilder();
            for (int row = startRow; row <= endRow; row++)
            {
                if (row > startRow)
                    builder.AppendLine();

                for (int column = startColumn; column <= endColumn; column++)
                {
                    if (column > startColumn)
                        builder.Append('\t');

                    builder.Append(NormalizeClipboardCellText(ActiveSheet.GetCell(row, column)));
                }
            }

            return builder.ToString();
        }

        private string SelectionLabel()
        {
            switch (_selectionKind)
            {
                case SelectionKind.RowRange:
                    if (SelectionStartRow() == SelectionEndRow())
                        return "Row " + (SelectionStartRow() + 1).ToString(CultureInfo.InvariantCulture);

                    return "Rows " + (SelectionStartRow() + 1).ToString(CultureInfo.InvariantCulture)
                        + ":" + (SelectionEndRow() + 1).ToString(CultureInfo.InvariantCulture);
                case SelectionKind.ColumnRange:
                    if (SelectionStartColumn() == SelectionEndColumn())
                        return "Col " + XlsxSpreadsheetFormat.GetColumnName(SelectionStartColumn());

                    return "Cols " + XlsxSpreadsheetFormat.GetColumnName(SelectionStartColumn())
                        + ":" + XlsxSpreadsheetFormat.GetColumnName(SelectionEndColumn());
                case SelectionKind.CellRange:
                    string start = CellName(SelectionStartRow(), SelectionStartColumn());
                    string end = CellName(SelectionEndRow(), SelectionEndColumn());
                    return start == end ? start : start + ":" + end;
                default:
                    return CellName(_selectedRow, _selectedColumn);
            }
        }

        private int SelectionStartRow()
        {
            return Math.Min(_selectionAnchorRow, _selectionEndRow);
        }

        private int SelectionEndRow()
        {
            return Math.Max(_selectionAnchorRow, _selectionEndRow);
        }

        private int SelectionStartColumn()
        {
            return Math.Min(_selectionAnchorColumn, _selectionEndColumn);
        }

        private int SelectionEndColumn()
        {
            return Math.Max(_selectionAnchorColumn, _selectionEndColumn);
        }

        private int GetRowHeaderWidth(SpreadsheetWorksheet sheet)
        {
            return Math.Max(6,
                Math.Max(sheet.RowCount, _selectedRow + 1)
                    .ToString(CultureInfo.InvariantCulture).Length + 2);
        }

        private static int GetCellWidth(int width)
        {
            return width >= 120 ? 16 : 12;
        }

        private static int GetVisibleColumnCount(int width, int rowHeaderWidth, int cellWidth)
        {
            return Math.Max(1, (width - rowHeaderWidth) / (cellWidth + 1));
        }

        private static bool IsWithin(int value, int start, int end)
        {
            return value >= start && value <= end;
        }

        private static bool IsAsyncKeyDown(int virtualKey)
        {
            return (GetAsyncKeyState(virtualKey) & unchecked((short)0x8000)) != 0;
        }

        private static int GetWheelNotches(int buttonState)
        {
            short delta = unchecked((short)((buttonState >> 16) & 0xffff));
            if (delta == 0)
                return 0;

            int notches = delta / WheelDelta;
            if (notches == 0)
                notches = delta > 0 ? 1 : -1;

            return notches;
        }

        private static bool IsCtrlKey(ConsoleKeyInfo key, ConsoleKey consoleKey, char keyChar)
        {
            return ((key.Modifiers & ConsoleModifiers.Control) != 0 && key.Key == consoleKey)
                   || key.KeyChar == keyChar;
        }

        private static bool IsSaveKey(ConsoleKeyInfo key)
        {
            bool ctrl = (key.Modifiers & ConsoleModifiers.Control) != 0;
            bool shift = (key.Modifiers & ConsoleModifiers.Shift) != 0;
            return IsCtrlKey(key, ConsoleKey.S, '\x13')
                   || (ctrl && shift && (key.KeyChar == 'S' || key.KeyChar == 's'));
        }

        private static string NormalizeClipboardCellText(string value)
        {
            return (value ?? string.Empty)
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Replace('\t', ' ');
        }

        private static bool TrySetClipboardText(string text, out string error)
        {
            error = string.Empty;

            try
            {
                RunSta(() =>
                {
                    System.Windows.Forms.Clipboard.SetText(text ?? string.Empty, System.Windows.Forms.TextDataFormat.UnicodeText);
                });
                return true;
            }
            catch (Exception ex)
            {
                error = "copy failed: " + ex.Message;
                return false;
            }
        }

        private static void RunSta(Action action)
        {
            Exception error = null;

            var thread = new Thread(() =>
            {
                try
                {
                    action();
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
        }

        // ── Rendering utilities ───────────────────────────────────────────────

        /// <summary>
        /// Appends <paramref name="content"/> (may contain ANSI escapes) followed by spaces
        /// to fill up to <paramref name="width"/> printable columns, then a newline.
        /// Content that exceeds <paramref name="width"/> visible columns is truncated so the
        /// line never wraps to a second terminal row.
        /// </summary>
        private static void AppendFilledLine(StringBuilder buf, string content, int width, string bgEscape)
        {
            int printLen = CountPrintable(content);
            if (printLen > width)
            {
                content  = TrimANSI(content, width);
                printLen = width;
            }

            buf.Append(content);
            int pad = width - printLen;
            if (pad > 0)
            {
                buf.Append(bgEscape);
                buf.Append(new string(' ', pad));
            }
            buf.Append("\r\n");
        }

        /// <summary>
        /// Truncates a string that may contain ANSI SGR escape sequences so that no more
        /// than <paramref name="maxVisible"/> printable (non-escape) characters are kept.
        /// Escape sequences that had already started are still emitted so terminal colour
        /// state stays consistent.
        /// </summary>
        private static string TrimANSI(string s, int maxVisible)
        {
            if (maxVisible <= 0) return string.Empty;
            int printLen = CountPrintable(s);
            if (printLen <= maxVisible) return s;

            var result = new StringBuilder(s.Length);
            int count  = 0;
            bool inEsc = false;
            var escBuf = new StringBuilder(20);

            foreach (char c in s)
            {
                if (inEsc)
                {
                    escBuf.Append(c);
                    if (c == 'm')
                    {
                        result.Append(escBuf);
                        escBuf.Clear();
                        inEsc = false;
                    }
                }
                else if (c == '\x1b')
                {
                    inEsc = true;
                    escBuf.Clear();
                    escBuf.Append(c);
                }
                else
                {
                    if (count >= maxVisible) break;
                    result.Append(c);
                    count++;
                }
            }

            return result.ToString();
        }

        /// <summary>Counts visible (non-ANSI-escape) characters in a string.</summary>
        private static int CountPrintable(string s)
        {
            int count  = 0;
            bool inEsc = false;
            foreach (char c in s)
            {
                if (inEsc)
                {
                    if (c == 'm') inEsc = false;
                }
                else if (c == '\x1b')
                {
                    inEsc = true;
                }
                else
                {
                    count++;
                }
            }
            return count;
        }

        private static string CellName(int row, int column) =>
            XlsxSpreadsheetFormat.GetColumnName(column) + (row + 1).ToString(CultureInfo.InvariantCulture);

        private static string NormalizeCellText(string value) =>
            (value ?? string.Empty)
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Replace('\t', ' ');

        private static string Trim(string text, int width)
        {
            text = text ?? string.Empty;
            if (width <= 0 || text.Length <= width) return text;
            if (width == 1) return "~";
            return text.Substring(0, width - 1) + "~";
        }

        private static string Pad(string text, int width)
        {
            text = text ?? string.Empty;
            if (text.Length > width) return Trim(text, width);
            return text.PadRight(Math.Max(0, width));
        }

        private static string Center(string text, int width)
        {
            text = Trim(text ?? string.Empty, width);
            int left = Math.Max(0, (width - text.Length) / 2);
            return new string(' ', left) + text.PadRight(width - left);
        }

        // ── Native console input ─────────────────────────────────────────────

        private static bool TryGetConsoleInputMode(IntPtr inputHandle, out int mode)
        {
            mode = 0;
            return IsValidConsoleHandle(inputHandle) && GetConsoleMode(inputHandle, out mode);
        }

        private static int DisableNativeConsoleSelectionMode(int mode)
        {
            return (mode | EnableExtendedFlags | EnableMouseInput | EnableWindowInput | EnableProcessedInput) &
                ~EnableQuickEditMode;
        }

        private static bool TryPeekConsoleInput(IntPtr inputHandle, out InputRecord record)
        {
            record = default(InputRecord);
            if (!IsValidConsoleHandle(inputHandle))
                return false;

            var records = new InputRecord[1];
            int count;
            if (!PeekConsoleInput(inputHandle, records, records.Length, out count) || count <= 0)
                return false;

            record = records[0];
            return true;
        }

        private static bool TryReadConsoleInputRecord(IntPtr inputHandle, out InputRecord record)
        {
            record = default(InputRecord);
            if (!IsValidConsoleHandle(inputHandle))
                return false;

            var records = new InputRecord[1];
            int count;
            if (!ReadConsoleInput(inputHandle, records, records.Length, out count) || count <= 0)
                return false;

            record = records[0];
            return true;
        }

        private static bool IsValidConsoleHandle(IntPtr inputHandle)
        {
            return inputHandle != IntPtr.Zero && inputHandle.ToInt64() != -1;
        }

        private static bool IsConsoleKeyAvailable()
        {
            try
            {
                return Console.KeyAvailable;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, int dwMode);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PeekConsoleInput(
            IntPtr hConsoleInput,
            [Out] InputRecord[] lpBuffer,
            int nLength,
            out int lpNumberOfEventsRead);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadConsoleInput(
            IntPtr hConsoleInput,
            [Out] InputRecord[] lpBuffer,
            int nLength,
            out int lpNumberOfEventsRead);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private enum SelectionKind
        {
            None,
            CellRange,
            RowRange,
            ColumnRange
        }

        private enum GridHitKind
        {
            Cell,
            RowHeader,
            ColumnHeader
        }

        private struct GridPosition
        {
            public GridPosition(GridHitKind kind, int row, int column)
            {
                Kind = kind;
                Row = row;
                Column = column;
            }

            public GridHitKind Kind;
            public int Row;
            public int Column;
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode, Size = 20)]
        private struct InputRecord
        {
            [FieldOffset(0)]
            public short EventType;

            [FieldOffset(4)]
            public KeyEventRecord KeyEvent;

            [FieldOffset(4)]
            public MouseEventRecord MouseEvent;

            [FieldOffset(4)]
            public WindowBufferSizeRecord WindowBufferSizeEvent;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Size = 16)]
        private struct KeyEventRecord
        {
            [MarshalAs(UnmanagedType.Bool)]
            public bool KeyDown;
            public short RepeatCount;
            public short VirtualKeyCode;
            public short VirtualScanCode;
            [MarshalAs(UnmanagedType.U2)]
            public char UnicodeChar;
            public int ControlKeyState;
        }

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        private struct MouseEventRecord
        {
            public Coord MousePosition;
            public int ButtonState;
            public int ControlKeyState;
            public int EventFlags;
        }

        [StructLayout(LayoutKind.Sequential, Size = 4)]
        private struct WindowBufferSizeRecord
        {
            public Coord Size;
        }

        [StructLayout(LayoutKind.Sequential, Size = 4)]
        private struct Coord
        {
            public short X;
            public short Y;
        }
    }
}
