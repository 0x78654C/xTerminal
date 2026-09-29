using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using Core.SystemTools;

namespace Core.DirFiles
{
    [SupportedOSPlatform("Windows")]
    public class FileExplorer
    {
        private const long PreviewByteLimit = 64 * 1024;
        private const int FrameIntervalMs = 16;
        private const int DetailDelayMs = 120;
        private const int RowsPerWheelNotch = 3;

        private readonly StringBuilder _frame = new StringBuilder(32768);
        private bool _rendering;
        private int _frameWidth;
        private int _frameHeight;
        private int _wheelRemainder;
        private bool _detailsPending = true;
        private long _detailsDue;
        private List<string> _cachedPreview;
        private int _cachedPreviewLines;

        private class Item
        {
            public string Path { get; set; }
            public bool IsDirectory { get; set; }
            public long? SizeBytes { get; set; }
        }

        private class SearchItem
        {
            public string Path { get; set; }
            public bool IsDirectory { get; set; }
        }

        private class HistoryEntry
        {
            public string Path { get; set; }
            public int SelectedIndex { get; set; }
            public int ScrollOffset { get; set; }
        }

        // ── Theme ──────────────────────────────────────────────────────────
        private static readonly ConsoleColor ClrTitle     = ConsoleColor.Cyan;
        private static readonly ConsoleColor ClrBorder    = ConsoleColor.DarkGray;
        private static readonly ConsoleColor ClrPath      = ConsoleColor.Yellow;
        private static readonly ConsoleColor ClrHelp      = ConsoleColor.DarkGray;
        private static readonly ConsoleColor ClrInfoLabel = ConsoleColor.DarkYellow;
        private static readonly ConsoleColor ClrInfoValue = ConsoleColor.White;

        // Match the XTE explorer's 256-color file palette.
        private const int CTitle = 45;
        private const int CNormal = 252;
        private const int CError = 203;
        private const int COperator = 220;
        private const int CSharpType = 68;
        private const int CRustKeyword = 81;
        private const int CSourceFlow = 39;
        private const int CSearch = 227;
        private const int CVariable = 219;
        private const int CPreprocessor = 183;
        private const string Reset = "\x1b[0m";

        private string _currentRoot;
        private readonly Stack<HistoryEntry> _historyBack = new Stack<HistoryEntry>();
        private bool _suppressHistory = false;

        private readonly List<Item> _items = new List<Item>();
        private int _selectedIndex = 0;
        private int _scrollOffset = 0;

        private bool _searchMode = false;
        private readonly List<SearchItem> _searchResults = new List<SearchItem>();
        private int _searchSelectedIndex = 0;
        private int _searchScrollOffset = 0;

        private bool _itemsDirty = true;
        private int _lastRenderWidth  = -1;
        private int _lastRenderHeight = -1;

        // Partial-update tracking
        private int _lastSelectedIndex = -2;
        private int _lastScrollOffset  = -2;

        // Info-pane filesystem cache (avoids re-listing on every keypress)
        private string   _cachedFolderPath  = null;
        private string[] _cachedFolderDirs  = Array.Empty<string>();
        private string[] _cachedFolderFiles = Array.Empty<string>();
        private string   _cachedFilePath    = null;
        private FileInfo _cachedFileInfo    = null;

        // Status-bar cache
        private string _cachedDriveRoot = null;
        private string _cachedFreeStr   = "";
        private int    _cachedDirCount  = 0;
        private int    _cachedFileCount = 0;

        public FileExplorer(string startPath)
        {
            _currentRoot = NormalizeDirectoryPath(startPath);
            _itemsDirty = true;
        }

        public void Run()
        {
            using var virtualTerminalOutput = VirtualTerminalOutput.Enable();
            using var input = new FileExplorerInput();
            bool running = true;
            bool dirty = true;
            long nextFrame = 0;
            bool oldCursorVisible = Console.CursorVisible;
            Console.Write("\x1b[?1049h"); // alternate screen
            Console.CursorVisible = false;

            try
            {
                while (running)
                {
                    EnsureItemsLoaded();
                    // Bound each batch so a held key cannot starve rendering. Actions
                    // end the batch, preserving their order relative to navigation.
                    for (int i = 0; i < 64 && input.TryRead(out var key, out int repeats, out int wheel); i++)
                    {
                        int previousSelection = _selectedIndex;
                        if (wheel != 0)
                            dirty |= ScrollByWheelDelta(wheel, PageSize());
                        if (key.HasValue)
                        {
                            bool movement = IsMovementKey(key.Value);
                            int count = movement ? repeats : 1;
                            for (int repeat = 0; repeat < count && running; repeat++)
                                running = _searchMode ? HandleSearchKey(key.Value) : HandleMainKey(key.Value);
                            dirty = true;
                            if (previousSelection != _selectedIndex)
                                DeferDetails();
                            if (!movement)
                                break;
                        }
                        else if (previousSelection != _selectedIndex)
                            DeferDetails();
                    }

                    if (!running)
                        break;

                    long now = Environment.TickCount64;
                    var size = WindowSize();
                    dirty |= size.width != _lastRenderWidth || size.height != _lastRenderHeight;
                    dirty |= !_searchMode && size.width >= 60 && size.height >= 20 &&
                        _detailsPending && now >= _detailsDue;
                    if (dirty && now >= nextFrame)
                    {
                        RenderFrame();
                        dirty = false;
                        nextFrame = Environment.TickCount64 + FrameIntervalMs;
                    }
                    Thread.Sleep(8);
                }
                SetCurrentDirectory(_currentRoot);
            }
            finally
            {
                try { Console.Write("\x1b[?1049l"); } catch { } // restore normal screen
                try { Console.CursorVisible = oldCursorVisible; } catch { }
            }
        }

        private void RenderFrame()
        {
            _frame.Clear();
            (_frameWidth, _frameHeight) = WindowSize();
            _rendering = true;
            try
            {
                if (_searchMode) RenderSearchScreen();
                else RenderMainScreen();
                Console.Write(_frame.ToString());
            }
            finally { _rendering = false; }
        }

        private void DeferDetails()
        {
            _detailsPending = true;
            _detailsDue = Environment.TickCount64 + DetailDelayMs;
        }

        private static int PageSize() => Math.Max(1, WindowSize().height - 5);

        private static bool IsMovementKey(ConsoleKeyInfo key) =>
            key.Key == ConsoleKey.UpArrow || key.Key == ConsoleKey.DownArrow ||
            key.Key == ConsoleKey.Home || key.Key == ConsoleKey.End ||
            key.Key == ConsoleKey.PageDown ||
            (key.Key == ConsoleKey.PageUp && (key.Modifiers & ConsoleModifiers.Control) != 0);

        private bool MoveSelection(int delta, int rows)
        {
            return _searchMode
                ? MoveListSelection(delta, _searchResults.Count, rows, ref _searchSelectedIndex, ref _searchScrollOffset)
                : MoveListSelection(delta, _items.Count, rows, ref _selectedIndex, ref _scrollOffset);
        }

        private static bool MoveListSelection(int delta, int count, int rows, ref int selected, ref int offset)
        {
            int oldSelected = selected;
            int oldOffset = offset;
            rows = Math.Max(1, rows);
            selected = (int)Math.Clamp((long)selected + delta, 0, Math.Max(0, count - 1));
            offset = Clamp(offset, Math.Max(0, selected - rows + 1), selected);
            offset = Clamp(offset, 0, Math.Max(0, count - rows));
            return selected != oldSelected || offset != oldOffset;
        }

        private bool ScrollByWheelDelta(int delta, int rows)
        {
            // Accumulate partial notches from high-resolution wheels/trackpads.
            _wheelRemainder += delta;
            int notches = _wheelRemainder / 120;
            _wheelRemainder %= 120;
            return notches != 0 && MoveSelection(-notches * RowsPerWheelNotch, rows);
        }

        public static void ColorConsoleTextLine(ConsoleColor color, string text)
        {
            ConsoleColor currentForeground = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = currentForeground;
        }

        public static void ColorConsoleText(ConsoleColor color, object data)
        {
            ConsoleColor currentForeground = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(data);
            Console.ForegroundColor = currentForeground;
        }

        // ========================= MAIN SCREEN =========================

        // Returns true when the full screen needs repainting.
        private bool ConditionalClear(int width, int height)
        {
            if (width != _lastRenderWidth || height != _lastRenderHeight)
            {
                WriteOutput("\x1b[2J\x1b[H");
                _lastRenderWidth  = width;
                _lastRenderHeight = height;
                return true;
            }
            return false;
        }

        private void ForceFullRedraw()
        {
            _lastRenderWidth  = -1;
            _lastRenderHeight = -1;
            _lastSelectedIndex = -2;
            _lastScrollOffset  = -2;
            DeferDetails();
        }

        private void EnterSearchMode()
        {
            _searchMode = true;
            ForceFullRedraw();
        }

        private void ExitSearchMode()
        {
            _searchMode = false;
            ForceFullRedraw();
        }

        private void RenderMainScreen()
        {
            EnsureItemsLoaded();

            (int width, int height) = RenderSize();
            bool fullClear = ConditionalClear(width, height);

            if (width < 60 || height < 20)
            {
                RenderTooSmall(width, height);
                return;
            }

            int headerLines   = 4;
            int contentTop    = headerLines;
            int contentHeight = height - headerLines - 1;
            if (contentHeight < 4) contentHeight = 4;
            int statusBarY = contentTop + contentHeight;

            int leftWidth     = width / 2 - 1;
            if (leftWidth < 20) leftWidth = 20;
            int separatorCol  = leftWidth;
            int rightStartCol = separatorCol + 1;
            int rightWidth    = width - rightStartCol;
            if (rightWidth < 20) rightWidth = 20;

            // ── Clamp / scroll (before rendering so we know if scroll changed) ──
            if (_items.Count > 0)
                _selectedIndex = Clamp(_selectedIndex, 0, _items.Count - 1);
            else
                _selectedIndex = 0;

            int maxOffset = Math.Max(0, _items.Count - contentHeight);
            if (_selectedIndex < _scrollOffset)
                _scrollOffset = _selectedIndex;
            else if (_selectedIndex >= _scrollOffset + contentHeight)
                _scrollOffset = _selectedIndex - contentHeight + 1;
            _scrollOffset = Clamp(_scrollOffset, 0, maxOffset);

            bool scrollChanged    = _scrollOffset != _lastScrollOffset;
            bool selectionChanged = _selectedIndex != _lastSelectedIndex;
            // Partial update: only 2 rows in the left pane need repainting.
            bool canPartialUpdate = !fullClear && !scrollChanged && _lastSelectedIndex >= 0;

            // ── Header ──────────────────────────────────────────────────────
            int    count     = _items.Count;
            string counter   = count > 0 ? $"[{_selectedIndex + 1}/{count}]" : "[0/0]";
            string titleBase = " ◈ xFile Explorer";
            int    padLen    = Math.Max(0, width - titleBase.Length - counter.Length - 1);
            string titleLine = titleBase + new string(' ', padLen) + counter + " ";
            if (titleLine.Length > width) titleLine = titleLine.Substring(0, width);
            WriteTrimmedAtColor(0, 0, titleLine, width, ClrTitle);

            if (fullClear || _lastSelectedIndex < 0)
            {
                WriteTrimmedAtColor(0, 1, new string('═', width), width, ClrBorder);
                WriteTrimmedAtColor(0, 2, " ▶ " + _currentRoot, width, ClrPath);
                WriteTrimmedAtColor(0, 3,
                    " ↑↓/Wheel:move  Ctrl+↑↓:page  ↵:open  ⌫:back  PgUp:up  /:search  Tab:drives  F5:refresh  Esc:quit",
                    width, ClrHelp);
            }

            // ── Left pane ───────────────────────────────────────────────────
            if (!canPartialUpdate)
            {
                for (int row = 0; row < contentHeight; row++)
                    RenderLeftRow(_scrollOffset + row, contentTop + row, leftWidth, separatorCol);
            }
            else if (selectionChanged)
            {
                // Only repaint the two rows whose highlight state changed.
                int oldRow = _lastSelectedIndex - _scrollOffset;
                if (oldRow >= 0 && oldRow < contentHeight)
                    RenderLeftRow(_lastSelectedIndex, contentTop + oldRow, leftWidth, separatorCol);

                int newRow = _selectedIndex - _scrollOffset;
                if (newRow >= 0 && newRow < contentHeight)
                    RenderLeftRow(_selectedIndex, contentTop + newRow, leftWidth, separatorCol);
            }

            // ── Right pane ──────────────────────────────────────────────────
            if (fullClear || selectionChanged || (_detailsPending && Environment.TickCount64 >= _detailsDue))
                RenderInfoPane(rightStartCol, contentTop, rightWidth, contentHeight);

            // ── Status bar ──────────────────────────────────────────────────
            RenderStatusBar(statusBarY, width);

            _lastSelectedIndex = _selectedIndex;
            _lastScrollOffset  = _scrollOffset;
        }

        // Renders one row of the left file list (text + separator glyph).
        private void RenderLeftRow(int idx, int y, int leftWidth, int separatorCol)
        {
            string       text  = "";
            int color = CNormal;

            if (idx >= 0 && idx < _items.Count)
            {
                var    item = _items[idx];
                string name = Path.GetFileName(item.Path);
                if (string.IsNullOrEmpty(name)) name = item.Path;

                if (item.IsDirectory)
                {
                    text  = "▶ " + name;
                    color = CTitle;
                }
                else
                {
                    string sz = item.SizeBytes.HasValue
                        ? "  " + FormatSize(item.SizeBytes.Value)
                        : "";
                    text  = "· " + name + sz;
                    color = FileColor(item.Path);
                }
            }

            WriteListRow(0, y, text, leftWidth, idx == _selectedIndex && idx < _items.Count, color);
            WriteTrimmedAtColor(separatorCol, y, "║", 1, ClrBorder);
        }

        private void RenderStatusBar(int y, int width)
        {
            if (y >= RenderSize().height) return;

            // Re-read drive free space only when the directory changes.
            if (_currentRoot != _cachedDriveRoot)
            {
                _cachedDriveRoot = _currentRoot;
                _cachedFreeStr   = "";
                try
                {
                    string root = Path.GetPathRoot(_currentRoot);
                    if (root != null)
                    {
                        var di = new DriveInfo(root);
                        if (di.IsReady)
                            _cachedFreeStr = $"  │  Free: {FormatSize(di.AvailableFreeSpace)}";
                    }
                }
                catch { }
            }

            string status = $"  {_cachedDirCount} folder{(_cachedDirCount != 1 ? "s" : "")} · {_cachedFileCount} file{(_cachedFileCount != 1 ? "s" : "")}{_cachedFreeStr}";
            if (status.Length > width) status = status.Substring(0, width);
            else status = status.PadRight(width);

            WriteAt(0, y, status, width, "\x1b[30;100m");
        }

        private void EnsureItemsLoaded()
        {
            if (!_itemsDirty) return;
            LoadItems();
            _itemsDirty = false;
            // Invalidate detail caches; force full redraw on next frame.
            _cachedFolderPath  = null;
            _cachedFilePath    = null;
            _cachedPreview = null;
            _cachedDriveRoot = null;
            _lastSelectedIndex = -2;
            _lastScrollOffset  = -2;
            DeferDetails();
        }

        private void LoadItems()
        {
            _items.Clear();
            _cachedDirCount = 0;
            _cachedFileCount = 0;
            try
            {
                // Enumeration supplies cached file metadata, avoiding one extra
                // filesystem query per file just to display its size.
                foreach (var entry in new DirectoryInfo(_currentRoot).EnumerateFileSystemInfos())
                {
                    bool directory = (entry.Attributes & FileAttributes.Directory) != 0;
                    long? size = null;
                    if (!directory && entry is FileInfo file)
                    {
                        try { size = file.Length; } catch { }
                    }
                    _items.Add(new Item { Path = entry.FullName, IsDirectory = directory, SizeBytes = size });
                    if (directory) _cachedDirCount++;
                    else _cachedFileCount++;
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            _items.Sort((a, b) =>
            {
                int byType = b.IsDirectory.CompareTo(a.IsDirectory);
                return byType != 0 ? byType : StringComparer.OrdinalIgnoreCase.Compare(a.Path, b.Path);
            });
        }

        private void RenderInfoPane(int left, int top, int width, int height)
        {
            for (int row = 0; row < height; row++)
                ClearLineAt(left, top + row, width);

            string heading = "─ Details " + new string('─', Math.Max(0, width - 10));
            WriteTrimmedAtColor(left, top, heading, width, ClrBorder);

            if (_items.Count == 0 || _selectedIndex < 0 || _selectedIndex >= _items.Count)
            {
                _detailsPending = false;
                WriteTrimmedAtColor(left, top + 1, "  (empty)", width, ClrBorder);
                return;
            }

            var item = _items[_selectedIndex];
            if (_detailsPending && Environment.TickCount64 < _detailsDue)
            {
                WriteTrimmedAtColor(left, top + 1, "  " + Path.GetFileName(item.Path), width, ClrInfoValue);
                WriteTrimmedAtColor(left, top + 2, "  Loading preview...", width, ClrBorder);
                return;
            }
            _detailsPending = false;
            if (item.IsDirectory)
                ShowFolderDetails(item.Path, left, top + 1, width, height - 1);
            else
                ShowFileDetails(item.Path, left, top + 1, width, height - 1);
        }

        private void SetCurrentDirectory(string path)
        {
            string directory = NormalizeDirectoryPath(path);
            if (!Path.EndsInDirectorySeparator(directory))
                directory += Path.DirectorySeparatorChar;
            File.WriteAllText(GlobalVariables.currentDirectory, directory);
        }

        private static string NormalizeDirectoryPath(string path)
        {
            string directory = Path.GetFullPath(path);
            int rootLength = Path.GetPathRoot(directory).Length;
            // Keep drive/UNC roots intact, but remove trailing separators from
            // folders so GetParent always moves up one level.
            while (directory.Length > rootLength && Path.EndsInDirectorySeparator(directory))
                directory = directory.Substring(0, directory.Length - 1);
            return directory;
        }

        private bool HandleMainKey(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.Oem3:
                case ConsoleKey.Escape:
                    return false;

                case ConsoleKey.UpArrow:
                    MoveSelection((key.Modifiers & ConsoleModifiers.Control) != 0 ? -PageSize() : -1, PageSize());
                    return true;

                case ConsoleKey.DownArrow:
                    MoveSelection((key.Modifiers & ConsoleModifiers.Control) != 0 ? PageSize() : 1, PageSize());
                    return true;

                case ConsoleKey.Home:
                    if (_items.Count > 0) _selectedIndex = 0;
                    return true;

                case ConsoleKey.End:
                    if (_items.Count > 0) _selectedIndex = _items.Count - 1;
                    return true;

                case ConsoleKey.Enter:
                    if (_items.Count > 0 && _selectedIndex >= 0 && _selectedIndex < _items.Count)
                    {
                        var item = _items[_selectedIndex];
                        if (item.IsDirectory)
                        {
                            NavigateTo(item.Path);
                            _selectedIndex = 0;
                            _scrollOffset  = 0;
                        }
                        else
                        {
                            OpenFile(item.Path);
                            ForceFullRedraw();
                        }
                    }
                    return true;

                case ConsoleKey.Backspace:
                    GoBack();
                    return true;

                case ConsoleKey.PageUp:
                    if ((key.Modifiers & ConsoleModifiers.Control) != 0)
                    {
                        MoveSelection(-PageSize(), PageSize());
                        return true;
                    }
                    GoUp();
                    _selectedIndex = 0;
                    _scrollOffset  = 0;
                    return true;

                case ConsoleKey.PageDown:
                    MoveSelection(PageSize(), PageSize());
                    return true;

                case ConsoleKey.F5:
                    _itemsDirty = true;
                    return true;

                case ConsoleKey.Oem2:
                    DoSearch();
                    return true;

                case ConsoleKey.Tab:
                    SwitchDrive();
                    _selectedIndex = 0;
                    _scrollOffset  = 0;
                    return true;

                case ConsoleKey.Delete:
                    DeleteSelectedItem();
                    return true;

                default:
                    if (char.IsLetterOrDigit(key.KeyChar))
                        JumpToItemStartingWith(char.ToLowerInvariant(key.KeyChar));
                    return true;
            }
        }

        private void DeleteSelectedItem()
        {
            if (_items.Count == 0 || _selectedIndex < 0 || _selectedIndex >= _items.Count)
                return;

            var item = _items[_selectedIndex];

            RenderModal(
                item.IsDirectory ? " ◈ Delete Folder" : " ◈ Delete File",
                $"  Path: {item.Path}",
                item.IsDirectory ? "  WARNING: This will delete the folder and ALL its contents!" : "",
                ConsoleColor.Red
            );

            Console.Write("  Are you sure? (y/N): ");
            var key = Console.ReadKey(true);
            Console.WriteLine();

            if (key.KeyChar != 'y' && key.KeyChar != 'Y')
            {
                ColorConsoleTextLine(ClrBorder, "  Cancelled.");
                Console.WriteLine("  Press any key to return...");
                Console.ReadKey(true);
                return;
            }

            try
            {
                if (item.IsDirectory)
                    Directory.Delete(item.Path, recursive: true);
                else
                    File.Delete(item.Path);

                ColorConsoleTextLine(ConsoleColor.Green, "  Deleted successfully.");
            }
            catch (Exception ex)
            {
                ColorConsoleTextLine(ConsoleColor.Red, $"  Error: {ex.Message}");
            }

            Console.WriteLine("  Press any key to return...");
            Console.ReadKey(true);

            if (_selectedIndex >= _items.Count - 1)
                _selectedIndex = Math.Max(0, _selectedIndex - 1);
            _itemsDirty = true;
        }

        private void JumpToItemStartingWith(char ch)
        {
            if (_items.Count == 0) return;
            int count = _items.Count;
            int start = _selectedIndex + 1;
            if (start >= count) start = 0;

            for (int i = start; i < count; i++)
            {
                string name = Path.GetFileName(_items[i].Path);
                if (!string.IsNullOrEmpty(name) && char.ToLowerInvariant(name[0]) == ch)
                { _selectedIndex = i; return; }
            }
            for (int i = 0; i <= _selectedIndex; i++)
            {
                string name = Path.GetFileName(_items[i].Path);
                if (!string.IsNullOrEmpty(name) && char.ToLowerInvariant(name[0]) == ch)
                { _selectedIndex = i; return; }
            }
        }

        // ========================= DRIVE SWITCH =========================

        private void SwitchDrive()
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToArray();
            if (drives.Length == 0) return;

            int  selected  = 0;
            bool running   = true;
            int  lastWidth = -1;

            while (running)
            {
                (int width, int height) = WindowSize();
                if (width != lastWidth)
                {
                    Console.Clear();
                    lastWidth = width;
                }
                else
                {
                    Console.SetCursorPosition(0, 0);
                }

                WriteTrimmedAtColor(0, 0, " ◈ Select Drive", width, ClrTitle);
                WriteTrimmedAtColor(0, 1, new string('═', width), width, ClrBorder);
                WriteTrimmedAtColor(0, 2, " ↑↓:move  ↵:select  Esc:cancel", width, ClrHelp);
                WriteTrimmedAtColor(0, 3, new string('─', width), width, ClrBorder);

                for (int i = 0; i < drives.Length; i++)
                {
                    var    d    = drives[i];
                    string line = $"  {d.Name}  ({d.DriveType})  " +
                                  $"{FormatSize(d.AvailableFreeSpace)} free of {FormatSize(d.TotalSize)}";
                    Console.SetCursorPosition(0, 4 + i);
                    WritePadded(line, width, i == selected, CNormal);
                }

                int clearFrom = 4 + drives.Length;
                int totalRows = height;
                for (int r = clearFrom; r < totalRows; r++)
                {
                    Console.SetCursorPosition(0, r);
                    Console.Write(new string(' ', width));
                }

                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        if (selected > 0) selected--;
                        break;
                    case ConsoleKey.DownArrow:
                        if (selected < drives.Length - 1) selected++;
                        break;
                    case ConsoleKey.Enter:
                        NavigateTo(drives[selected].RootDirectory.FullName);
                        _scrollOffset = 0;
                        _itemsDirty   = true;
                        ForceFullRedraw();
                        running = false;
                        break;
                    case ConsoleKey.Escape:
                        ForceFullRedraw();
                        running = false;
                        break;
                }
            }
        }

        private static string FormatSize(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (bytes >= GB) return $"{bytes / (double)GB:0.0} GB";
            if (bytes >= MB) return $"{bytes / (double)MB:0.0} MB";
            if (bytes >= KB) return $"{bytes / (double)KB:0.0} KB";
            return $"{bytes} B";
        }

        // ========================= SEARCH MODE =========================

        private void DoSearch()
        {
            _searchResults.Clear();
            _searchSelectedIndex = 0;
            _searchScrollOffset  = 0;

            int width = WindowSize().width;
            Console.Clear();

            WriteTrimmedAtColor(0, 0, " ◈ Search", width, ClrTitle);
            WriteTrimmedAtColor(0, 1, new string('═', width), width, ClrBorder);
            WriteTrimmedAtColor(0, 2, $"  Base folder: {_currentRoot}", width, ClrPath);
            WriteTrimmedAtColor(0, 3, new string('─', width), width, ClrBorder);

            Console.SetCursorPosition(0, 4);
            var oldFg = Console.ForegroundColor;
            Console.ForegroundColor = ClrInfoLabel;
            Console.Write("  Search term: ");
            Console.ForegroundColor = ClrInfoValue;
            Console.CursorVisible = true;
            int inputLeft = Console.CursorLeft;
            int inputTop  = Console.CursorTop;
            string term = ReadSearchTerm(inputLeft, inputTop, width - inputLeft);
            Console.CursorVisible = false;
            Console.ForegroundColor = oldFg;

            if (string.IsNullOrWhiteSpace(term))
            {
                ExitSearchMode();
                return;
            }

            term = term.Trim();
            Console.ForegroundColor = ClrHelp;
            Console.WriteLine("  Searching...");
            Console.ForegroundColor = oldFg;

            var pending = new Stack<string>();
            pending.Push(_currentRoot);

            while (pending.Count > 0)
            {
                string dir = pending.Pop();
                try
                {
                    foreach (var d in Directory.GetDirectories(dir))
                    {
                        string name = Path.GetFileName(d);
                        if (!string.IsNullOrEmpty(name) &&
                            name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                            _searchResults.Add(new SearchItem { Path = d, IsDirectory = true });
                        pending.Push(d);
                    }

                    foreach (var f in Directory.GetFiles(dir))
                    {
                        string name = Path.GetFileName(f);
                        if (!string.IsNullOrEmpty(name) &&
                            name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                            _searchResults.Add(new SearchItem { Path = f, IsDirectory = false });
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (PathTooLongException) { }
                catch (IOException) { }
            }

            EnterSearchMode();
        }

        private string ReadSearchTerm(int left, int top, int width)
        {
            var term = new StringBuilder();
            int inputWidth = Math.Max(1, width);

            while (true)
            {
                var key = Console.ReadKey(intercept: true);

                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        Console.WriteLine();
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

        private void RenderSearchTermInput(string term, int left, int top, int width)
        {
            int inputWidth = Math.Max(1, width);
            string visible = term.Length > inputWidth
                ? term.Substring(term.Length - inputWidth)
                : term;

            WriteTrimmedAt(left, top, visible, inputWidth);
            int cursorOffset = Math.Min(visible.Length, inputWidth - 1);
            Console.SetCursorPosition(left + cursorOffset, top);
        }

        private void RenderSearchScreen()
        {
            (int width, int height) = RenderSize();
            bool fullClear = ConditionalClear(width, height);
            if (width < 60 || height < 20)
            {
                RenderTooSmall(width, height);
                return;
            }

            int contentHeight = height - 5;
            MoveListSelection(0, _searchResults.Count, contentHeight, ref _searchSelectedIndex, ref _searchScrollOffset);
            bool fullList = fullClear || _lastSelectedIndex < 0 || _searchScrollOffset != _lastScrollOffset;
            string counter = _searchResults.Count > 0 ? $"[{_searchSelectedIndex + 1}/{_searchResults.Count}]" : "[0/0]";
            string title = " ◈ Search Results";
            WriteTrimmedAtColor(0, 0, title + new string(' ', Math.Max(0, width - title.Length - counter.Length - 1)) + counter,
                width, ClrTitle);

            if (fullClear || _lastSelectedIndex < 0)
            {
                WriteTrimmedAtColor(0, 1, new string('═', width), width, ClrBorder);
                WriteTrimmedAtColor(0, 2, "  Base: " + _currentRoot, width, ClrPath);
                WriteTrimmedAtColor(0, 3, " ↑↓/Wheel:move  Ctrl+↑↓/PgDn:page  ↵:open  Del:del  Esc:back  U:up", width, ClrHelp);
                int dirs = _searchResults.Count(i => i.IsDirectory);
                int files = _searchResults.Count - dirs;
                WriteAt(0, height - 1, $"  {dirs} folders · {files} files matched", width, "\x1b[30;100m");
            }

            if (fullList)
            {
                for (int row = 0; row < contentHeight; row++)
                    RenderSearchRow(_searchScrollOffset + row, 4 + row, width);
            }
            else if (_searchSelectedIndex != _lastSelectedIndex)
            {
                RenderSearchRow(_lastSelectedIndex, 4 + _lastSelectedIndex - _searchScrollOffset, width);
                RenderSearchRow(_searchSelectedIndex, 4 + _searchSelectedIndex - _searchScrollOffset, width);
            }
            _lastSelectedIndex = _searchSelectedIndex;
            _lastScrollOffset = _searchScrollOffset;
        }

        private void RenderSearchRow(int index, int row, int width)
        {
            string text = "";
            int color = CNormal;
            if (index >= 0 && index < _searchResults.Count)
            {
                var item = _searchResults[index];
                text = (item.IsDirectory ? "▶ " : "· ") + item.Path;
                color = item.IsDirectory ? CTitle : FileColor(item.Path);
            }
            WriteListRow(0, row, text, width, index == _searchSelectedIndex && index < _searchResults.Count, color);
        }

        private bool HandleSearchKey(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    ExitSearchMode();
                    return true;

                case ConsoleKey.UpArrow:
                    MoveSelection((key.Modifiers & ConsoleModifiers.Control) != 0 ? -PageSize() : -1, PageSize());
                    return true;

                case ConsoleKey.DownArrow:
                    MoveSelection((key.Modifiers & ConsoleModifiers.Control) != 0 ? PageSize() : 1, PageSize());
                    return true;

                case ConsoleKey.PageUp:
                    MoveSelection(-PageSize(), PageSize());
                    return true;

                case ConsoleKey.PageDown:
                    MoveSelection(PageSize(), PageSize());
                    return true;

                case ConsoleKey.Home:
                    if (_searchResults.Count > 0) _searchSelectedIndex = 0;
                    return true;

                case ConsoleKey.End:
                    if (_searchResults.Count > 0) _searchSelectedIndex = _searchResults.Count - 1;
                    return true;

                case ConsoleKey.Enter:
                    if (_searchResults.Count > 0 && _searchSelectedIndex >= 0 && _searchSelectedIndex < _searchResults.Count)
                    {
                        var item = _searchResults[_searchSelectedIndex];
                        if (item.IsDirectory)
                        {
                            NavigateTo(item.Path);
                            _selectedIndex = 0;
                            _scrollOffset  = 0;
                            ExitSearchMode();
                        }
                        else
                        {
                            OpenFile(item.Path);
                            ForceFullRedraw();
                        }
                    }
                    return true;

                case ConsoleKey.Backspace:
                    GoBack();
                    ExitSearchMode();
                    return true;

                case ConsoleKey.U:
                    GoUp();
                    _selectedIndex = 0;
                    _scrollOffset  = 0;
                    ExitSearchMode();
                    return true;

                case ConsoleKey.Delete:
                    DeleteSearchSelectedItem();
                    return true;

                default:
                    if (char.IsLetterOrDigit(key.KeyChar))
                        JumpToSearchStartingWith(char.ToLowerInvariant(key.KeyChar));
                    return true;
            }
        }

        private void DeleteSearchSelectedItem()
        {
            if (_searchResults.Count == 0 || _searchSelectedIndex < 0 || _searchSelectedIndex >= _searchResults.Count)
                return;

            var item = _searchResults[_searchSelectedIndex];

            RenderModal(
                item.IsDirectory ? " ◈ Delete Folder" : " ◈ Delete File",
                $"  Path: {item.Path}",
                item.IsDirectory ? "  WARNING: This will delete the folder and ALL its contents!" : "",
                ConsoleColor.Red
            );

            Console.Write("  Are you sure? (y/N): ");
            var key = Console.ReadKey(true);
            Console.WriteLine();

            if (key.KeyChar != 'y' && key.KeyChar != 'Y')
            {
                ColorConsoleTextLine(ClrBorder, "  Cancelled.");
                Console.WriteLine("  Press any key to return...");
                Console.ReadKey(true);
                return;
            }

            try
            {
                if (item.IsDirectory)
                    Directory.Delete(item.Path, recursive: true);
                else
                    File.Delete(item.Path);

                ColorConsoleTextLine(ConsoleColor.Green, "  Deleted successfully.");
            }
            catch (Exception ex)
            {
                ColorConsoleTextLine(ConsoleColor.Red, $"  Error: {ex.Message}");
            }

            Console.WriteLine("  Press any key to return...");
            Console.ReadKey(true);

            _searchResults.RemoveAt(_searchSelectedIndex);
            if (_searchSelectedIndex >= _searchResults.Count)
                _searchSelectedIndex = Math.Max(0, _searchSelectedIndex - 1);
            _itemsDirty = true;
        }

        private void JumpToSearchStartingWith(char ch)
        {
            if (_searchResults.Count == 0) return;
            int count = _searchResults.Count;
            int start = _searchSelectedIndex + 1;
            if (start >= count) start = 0;

            for (int i = start; i < count; i++)
            {
                string name = Path.GetFileName(_searchResults[i].Path);
                if (!string.IsNullOrEmpty(name) && char.ToLowerInvariant(name[0]) == ch)
                { _searchSelectedIndex = i; return; }
            }
            for (int i = 0; i <= _searchSelectedIndex; i++)
            {
                string name = Path.GetFileName(_searchResults[i].Path);
                if (!string.IsNullOrEmpty(name) && char.ToLowerInvariant(name[0]) == ch)
                { _searchSelectedIndex = i; return; }
            }
        }

        // ========================= NAVIGATION & DETAILS =========================

        private void NavigateTo(string newRoot)
        {
            try
            {
                newRoot = NormalizeDirectoryPath(newRoot);
                if (!Directory.Exists(newRoot)) return;

                if (!_suppressHistory && _currentRoot != null)
                {
                    _historyBack.Push(new HistoryEntry
                    {
                        Path          = _currentRoot,
                        SelectedIndex = _selectedIndex,
                        ScrollOffset  = _scrollOffset
                    });
                }

                _currentRoot = newRoot;
                _itemsDirty  = true;
                SetCurrentDirectory(newRoot);
            }
            catch { }
        }

        private void GoBack()
        {
            if (_historyBack.Count == 0) return;

            var entry = _historyBack.Pop();
            _suppressHistory = true;
            NavigateTo(entry.Path);
            _suppressHistory = false;

            _selectedIndex = entry.SelectedIndex;
            _scrollOffset  = entry.ScrollOffset;
            _itemsDirty    = true;
        }

        private void GoUp()
        {
            try
            {
                string parent = Directory.GetParent(NormalizeDirectoryPath(_currentRoot))?.FullName;
                if (parent == null) return;
                NavigateTo(parent);
                _itemsDirty = true;
            }
            catch { }
        }

        private void OpenFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return;

                var info  = new FileInfo(filePath);
                int width = WindowSize().width;
                Console.Clear();

                WriteTrimmedAtColor(0, 0, " ◈ File Details", width, ClrTitle);
                WriteTrimmedAtColor(0, 1, new string('═', width), width, ClrBorder);

                int          row = 2;
                int fc = FileColor(filePath);

                void WriteInfoRow(string label, string value, int valueColor)
                {
                    string lp = $"  {label,-12}│ ";
                    Console.SetCursorPosition(0, row);
                    var old = Console.ForegroundColor;
                    Console.ForegroundColor = ClrInfoLabel;
                    Console.Write(lp);
                    int rem = width - lp.Length;
                    if (rem > 0)
                    {
                        string v = value ?? "";
                        if (v.Length > rem) v = v.Substring(0, rem);
                        Console.Write(F(valueColor) + v + Reset);
                    }
                    Console.ForegroundColor = old;
                    row++;
                }

                WriteInfoRow("Name",      info.Name, fc);
                WriteInfoRow("Extension", string.IsNullOrEmpty(info.Extension) ? "(none)" : info.Extension, fc);
                WriteInfoRow("Size",      FormatSize(info.Length) + $"  ({info.Length:N0} B)", CNormal);
                WriteInfoRow("Modified",  info.LastWriteTime.ToString("yyyy-MM-dd  HH:mm:ss"), CNormal);
                WriteInfoRow("Created",   info.CreationTime.ToString("yyyy-MM-dd  HH:mm:ss"), CNormal);

                WriteTrimmedAtColor(0, row++, new string('─', width), width, ClrBorder);
                WriteTrimmedAtColor(0, row++, "  " + info.FullName, width, ConsoleColor.DarkGray);

                Console.SetCursorPosition(0, row + 1);
                Console.Write("  Open with default application? (y/N): ");
                var key = Console.ReadKey(true);
                Console.WriteLine();

                if (key.KeyChar == 'y' || key.KeyChar == 'Y')
                {
                    var psi = new ProcessStartInfo(filePath)
                    {
                        UseShellExecute  = true,
                        WorkingDirectory = Path.GetDirectoryName(filePath)
                    };
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                ColorConsoleTextLine(ConsoleColor.Red, $"  Error: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("  Press any key to return...");
            Console.ReadKey(true);
        }

        private void ShowFileDetails(string filePath, int left, int top, int width, int maxLines)
        {
            int line = 0;

            void WriteRow(string label, string value, int valueColor)
            {
                if (line >= maxLines) return;
                string prefix = $"  {label,-8}│ ";
                WriteTrimmedAtColor(left, top + line, prefix, width, ClrInfoLabel);
                if (prefix.Length < width)
                    WriteTrimmedAtAnsiColor(left + prefix.Length, top + line, value, width - prefix.Length, valueColor);
                line++;
            }

            // Cache FileInfo so we don't stat the same file on every keypress.
            if (filePath != _cachedFilePath)
            {
                _cachedFilePath = filePath;
                _cachedFileInfo = null;
                _cachedPreview = null;
                try { _cachedFileInfo = new FileInfo(filePath); } catch { }
            }

            var info = _cachedFileInfo;

            try
            {
                if (info == null) throw new Exception();

                int fc = FileColor(filePath);
                WriteRow("Type",     "File", fc);
                WriteRow("Name",     info.Name, CNormal);
                WriteRow("Ext",      string.IsNullOrEmpty(info.Extension) ? "(none)" : info.Extension, fc);
                WriteRow("Size",     FormatSize(info.Length), CNormal);
                WriteRow("Modified", info.LastWriteTime.ToString("yyyy-MM-dd HH:mm"), CNormal);
                WriteRow("Created",  info.CreationTime.ToString("yyyy-MM-dd HH:mm"), CNormal);

                if (line < maxLines)
                {
                    ClearLineAt(left, top + line, width);
                    WriteTrimmedAtColor(left, top + line, new string('─', Math.Max(0, width - 1)), width, ClrBorder);
                    line++;
                }
                if (line < maxLines)
                {
                    ClearLineAt(left, top + line, width);
                    WriteTrimmedAtColor(left, top + line, "  " + info.FullName, width, ConsoleColor.DarkGray);
                    line++;
                }
                if (line < maxLines)
                {
                    ClearLineAt(left, top + line, width);
                    WriteTrimmedAtColor(left, top + line, new string('─', Math.Max(0, width - 1)), width, ClrBorder);
                    line++;
                }
                if (line < maxLines)
                {
                    ClearLineAt(left, top + line, width);
                    WriteTrimmedAtColor(
                        left,
                        top + line,
                        "  Preview (first " + FormatSize(PreviewByteLimit) + " max):",
                        width,
                        ClrBorder);
                    line++;
                }

                int previewRows = maxLines - line;
                if (_cachedPreview == null || _cachedPreviewLines != previewRows)
                {
                    _cachedPreview = BuildFilePreview(filePath, info.Length, previewRows);
                    _cachedPreviewLines = previewRows;
                }
                List<string> previewLines = _cachedPreview;
                foreach (string previewLine in previewLines)
                {
                    if (line >= maxLines)
                        break;

                    ClearLineAt(left, top + line, width);
                    bool muted = previewLine.StartsWith("  (", StringComparison.Ordinal) ||
                        previewLine.StartsWith("  ...", StringComparison.Ordinal);
                    if (muted)
                        WriteTrimmedAtColor(left, top + line, previewLine, width, ConsoleColor.DarkGray);
                    else
                        WriteTrimmedAtAnsiColor(left, top + line, previewLine, width, CNormal);
                    line++;
                }
            }
            catch
            {
                if (line < maxLines)
                {
                    ClearLineAt(left, top + line, width);
                    WriteTrimmedAtColor(left, top + line, "  (unable to read)", width, ConsoleColor.DarkGray);
                }
            }
        }

        private static List<string> BuildFilePreview(string filePath, long totalBytes, int maxLines)
        {
            var lines = new List<string>();
            if (maxLines <= 0)
                return lines;

            try
            {
                if (totalBytes == 0)
                {
                    lines.Add("  (empty)");
                    return lines;
                }

                int bytesToRead = (int)Math.Min(totalBytes, PreviewByteLimit);
                byte[] buffer = new byte[bytesToRead];
                int read = 0;

                using (var stream = File.OpenRead(filePath))
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
                    lines.Add("  (empty)");
                    return lines;
                }

                if (LooksBinary(buffer, read))
                {
                    lines.Add("  (binary preview skipped)");
                    return lines;
                }

                string text = Encoding.UTF8.GetString(buffer, 0, read)
                    .Replace("\r\n", "\n")
                    .Replace('\r', '\n')
                    .Replace('\0', ' ');
                string[] fileLines = text.Split('\n');

                for (int i = 0; i < fileLines.Length && lines.Count < maxLines; i++)
                    lines.Add("  " + fileLines[i].Replace("\t", "    "));

                if (totalBytes > read && lines.Count < maxLines)
                {
                    lines.Add(
                        "  ... preview truncated at " + FormatSize(read) +
                        " of " + FormatSize(totalBytes));
                }
            }
            catch (Exception ex)
            {
                lines.Add("  Preview unavailable: " + ex.Message);
            }

            return lines;
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

        private void ShowFolderDetails(string folderPath, int left, int top, int width, int maxLines)
        {
            int line = 0;

            void WriteRow(string label, string value, int valueColor)
            {
                if (line >= maxLines) return;
                string prefix = $"  {label,-8}│ ";
                WriteTrimmedAtColor(left, top + line, prefix, width, ClrInfoLabel);
                if (prefix.Length < width)
                    WriteTrimmedAtAnsiColor(left + prefix.Length, top + line, value, width - prefix.Length, valueColor);
                line++;
            }

            // Cache directory contents — Directory.GetDirectories/GetFiles is expensive
            // and was previously called on every single keypress.
            if (folderPath != _cachedFolderPath)
            {
                _cachedFolderPath  = folderPath;
                _cachedFolderDirs  = Array.Empty<string>();
                _cachedFolderFiles = Array.Empty<string>();
                try
                {
                    if (Directory.Exists(folderPath))
                    {
                        _cachedFolderDirs  = Directory.GetDirectories(folderPath);
                        _cachedFolderFiles = Directory.GetFiles(folderPath);
                    }
                }
                catch { }
            }

            string[] dirs  = _cachedFolderDirs;
            string[] files = _cachedFolderFiles;

            var dirInfo = new DirectoryInfo(folderPath);
            WriteRow("Type",     "Folder", CTitle);
            WriteRow("Subs",     dirs.Length.ToString(), CNormal);
            WriteRow("Files",    files.Length.ToString(), CNormal);
            try { WriteRow("Modified", dirInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm"), CNormal); }
            catch (IOException) { WriteRow("Modified", "unavailable", CNormal); }
            catch (UnauthorizedAccessException) { WriteRow("Modified", "unavailable", CNormal); }

            if (line < maxLines)
            {
                ClearLineAt(left, top + line, width);
                WriteTrimmedAtColor(left, top + line, new string('─', Math.Max(0, width - 1)), width, ClrBorder);
                line++;
            }
            if (line < maxLines)
            {
                ClearLineAt(left, top + line, width);
                WriteTrimmedAtColor(left, top + line, "  Contents:", width, ClrBorder);
                line++;
            }

            foreach (var d in dirs)
            {
                if (line >= maxLines) break;
                string name = Path.GetFileName(d);
                if (string.IsNullOrEmpty(name)) name = d;
                ClearLineAt(left, top + line, width);
                WriteTrimmedAtAnsiColor(left, top + line, "  ▶ " + name, width, CTitle);
                line++;
            }

            foreach (var f in files)
            {
                if (line >= maxLines) break;
                string name = Path.GetFileName(f);
                if (string.IsNullOrEmpty(name)) name = f;
                ClearLineAt(left, top + line, width);
                WriteTrimmedAtAnsiColor(left, top + line, "  · " + name, width, FileColor(f));
                line++;
            }

            if (!Directory.Exists(folderPath) && line == 0)
            {
                ClearLineAt(left, top + line, width);
                WriteTrimmedAtColor(left, top + line, "  (unable to read)", width, ConsoleColor.DarkGray);
            }
        }

        // ========================= HELPERS =========================

        private void RenderModal(string title, string line1, string line2, ConsoleColor accentColor)
        {
            ForceFullRedraw();
            int width = WindowSize().width;
            Console.Clear();
            WriteTrimmedAtColor(0, 0, title, width, accentColor);
            WriteTrimmedAtColor(0, 1, new string('═', width), width, ClrBorder);
            WriteTrimmedAtColor(0, 2, line1, width, ClrInfoValue);
            int divRow = 3;
            if (!string.IsNullOrEmpty(line2))
            {
                WriteTrimmedAtColor(0, 3, line2, width, accentColor);
                divRow = 4;
            }
            WriteTrimmedAtColor(0, divRow, new string('─', width), width, ClrBorder);
            Console.SetCursorPosition(0, divRow + 1);
        }

        private void RenderTooSmall(int width, int height)
        {
            WriteTrimmedAtColor(0, 0, "xFile Explorer", width, ClrTitle);
            if (height > 1)
                WriteTrimmedAtColor(0, 1, "Resize the console to at least 60 x 20.", width, ClrHelp);
        }

        private static int FileColor(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();

            if (ext == ".exe" || ext == ".msi" || ext == ".bat" || ext == ".cmd" || ext == ".ps1" || ext == ".sh")
                return CError;

            if (ext == ".txt" || ext == ".md" || ext == ".log" || ext == ".ini" || ext == ".cfg" || ext == ".conf" || ext == ".csv")
                return COperator;

            if (ext == ".cs" || ext == ".csx")
                return CSharpType;

            if (ext == ".rs")
                return CRustKeyword;

            if (ext == ".py" || ext == ".js" || ext == ".ts" || ext == ".cpp" || ext == ".c" ||
                ext == ".h" || ext == ".java" || ext == ".go" || ext == ".rb" || ext == ".php")
                return CSourceFlow;

            if (ext == ".zip" || ext == ".rar" || ext == ".7z" || ext == ".tar" || ext == ".gz" || ext == ".bz2")
                return CSearch;

            if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif" || ext == ".bmp" ||
                ext == ".svg" || ext == ".ico" || ext == ".webp")
                return CVariable;

            if (ext == ".mp3" || ext == ".mp4" || ext == ".avi" || ext == ".mkv" || ext == ".mov" ||
                ext == ".wav" || ext == ".flac")
                return CPreprocessor;

            if (ext == ".pdf" || ext == ".doc" || ext == ".docx" || ext == ".xls" || ext == ".xlsx" ||
                ext == ".ppt" || ext == ".pptx")
                return COperator;

            if (ext == ".dll" || ext == ".sys" || ext == ".lib" || ext == ".pdb")
                return CError;

            return CNormal;
        }

        private static void WritePadded(string text, int width, bool selected, int normalColor)
        {
            if (text == null) text = "";
            if (text.Length > width)
                text = text.Substring(0, width);
            if (text.Length < width)
                text = text + new string(' ', width - text.Length);

            if (selected)
            {
                // ▌ accent bar on darker teal (BG_SEL2=24), then bold white on teal (BG_SEL=23) —
                // matches the wtop selection style exactly.
                string body = text.Length > 1 ? text.Substring(1) : "";
                Console.Write(
                    "\x1b[38;5;45m\x1b[48;5;24m▌" +
                    "\x1b[48;5;23m\x1b[1m\x1b[38;5;253m" + body +
                    "\x1b[0m");
            }
            else
            {
                Console.Write(F(normalColor) + text + Reset);
            }
        }

        private void WriteOutput(string text)
        {
            if (_rendering) _frame.Append(text);
            else Console.Write(text);
        }

        private void WriteAt(int left, int top, string text, int width, string style)
        {
            var size = RenderSize();
            if (left < 0 || top < 0 || top >= size.height) return;
            width = Math.Max(0, Math.Min(width, size.width - left));
            if (width == 0) return;
            text = SanitizeText(text ?? "");
            text = text.Length > width ? text.Substring(0, width) : text.PadRight(width);
            WriteOutput($"\x1b[{top + 1};{left + 1}H" + style + text + Reset);
        }

        private void WriteListRow(int left, int top, string text, int width, bool selected, int color)
        {
            if (selected)
            {
                string body = text.Length > 1 ? text.Substring(1) : "";
                WriteAt(left, top, "▌" + body, width, "\x1b[48;5;23m\x1b[1m\x1b[38;5;253m");
                WriteAt(left, top, "▌", 1, "\x1b[38;5;45m\x1b[48;5;24m");
            }
            else WriteAt(left, top, text, width, F(color));
        }

        private void WriteTrimmedAt(int left, int top, string text, int width) =>
            WriteAt(left, top, text, width, Reset);

        private void WriteTrimmedAtColor(int left, int top, string text, int width, ConsoleColor color) =>
            WriteAt(left, top, text, width, ConsoleForeground(color));

        private void WriteTrimmedAtAnsiColor(int left, int top, string text, int width, int color) =>
            WriteAt(left, top, text, width, F(color));

        private static string ConsoleForeground(ConsoleColor color)
        {
            int value = (int)color;
            int ansi = ((value & 1) << 2) | (value & 2) | ((value & 4) >> 2);
            return "\x1b[" + (ansi + (value >= 8 ? 90 : 30)) + "m";
        }

        private static string F(int color) => "\x1b[38;5;" + color + "m";

        private void ClearLineAt(int left, int top, int width) => WriteAt(left, top, "", width, Reset);

        private static string SanitizeText(string text)
        {
            // Preview contents must not inject cursor movement or terminal escapes.
            if (!text.Any(char.IsControl)) return text;
            return new string(text.Select(c => char.IsControl(c) ? ' ' : c).ToArray());
        }

        private static (int width, int height) WindowSize()
        {
            try
            {
                return (Math.Max(1, Console.WindowWidth), Math.Max(1, Console.WindowHeight));
            }
            catch
            {
                return (80, 25);
            }
        }

        private (int width, int height) RenderSize() =>
            _rendering ? (_frameWidth, _frameHeight) : WindowSize();

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
