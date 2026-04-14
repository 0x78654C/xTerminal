using Castle.Components.DictionaryAdapter.Xml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace Core.DirFiles
{
    [SupportedOSPlatform("Windows")]
    public class FileExplorer
    {
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
        private static readonly ConsoleColor ClrDirItem   = ConsoleColor.Cyan;
        private static readonly ConsoleColor ClrInfoLabel = ConsoleColor.DarkYellow;
        private static readonly ConsoleColor ClrInfoValue = ConsoleColor.White;
        private static readonly ConsoleColor ClrStatusFg  = ConsoleColor.Black;
        private static readonly ConsoleColor ClrStatusBg  = ConsoleColor.DarkGray;

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
            _currentRoot = Path.GetFullPath(startPath);
            _itemsDirty = true;
        }

        public void Run()
        {
            bool running = true;
            Console.CursorVisible = false;

            while (running)
            {
                if (_searchMode)
                {
                    RenderSearchScreen();
                    var key = Console.ReadKey(intercept: true);
                    running = HandleSearchKey(key);
                }
                else
                {
                    RenderMainScreen();
                    var key = Console.ReadKey(intercept: true);
                    running = HandleMainKey(key);
                }
            }

            Console.CursorVisible = true;
            Console.Clear();
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

        // Returns true when a full Console.Clear() was performed.
        private bool ConditionalClear(int width, int height)
        {
            if (width != _lastRenderWidth || height != _lastRenderHeight)
            {
                Console.Clear();
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
        }

        private void RenderMainScreen()
        {
            EnsureItemsLoaded();

            int width  = Math.Max(Console.WindowWidth, 60);
            int height = Math.Max(Console.WindowHeight, 20);
            bool fullClear = ConditionalClear(width, height);

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

            if (!canPartialUpdate)
            {
                WriteTrimmedAtColor(0, 1, new string('═', width), width, ClrBorder);
                WriteTrimmedAtColor(0, 2, " ▶ " + _currentRoot, width, ClrPath);
                WriteTrimmedAtColor(0, 3,
                    " ↑↓:move  ↵:open  ⌫:back  PgUp:up  Del:del  /:search  Tab:drives  `:quit",
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
            if (!canPartialUpdate || selectionChanged)
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
            ConsoleColor color = ConsoleColor.Gray;

            if (idx >= 0 && idx < _items.Count)
            {
                var    item = _items[idx];
                string name = Path.GetFileName(item.Path);
                if (string.IsNullOrEmpty(name)) name = item.Path;

                if (item.IsDirectory)
                {
                    text  = "▶ " + name;
                    color = ClrDirItem;
                }
                else
                {
                    string sz = item.SizeBytes.HasValue
                        ? "  " + FormatSize(item.SizeBytes.Value)
                        : "";
                    text  = "· " + name + sz;
                    color = GetFileColor(item.Path);
                }
            }

            Console.SetCursorPosition(0, y);
            WritePadded(text, leftWidth, idx == _selectedIndex, color);

            var oldFg = Console.ForegroundColor;
            Console.SetCursorPosition(separatorCol, y);
            Console.ForegroundColor = ClrBorder;
            Console.Write("║");
            Console.ForegroundColor = oldFg;
        }

        private void RenderStatusBar(int y, int width)
        {
            if (y >= Console.WindowHeight) return;

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

            Console.SetCursorPosition(0, y);
            var oldFg = Console.ForegroundColor;
            var oldBg = Console.BackgroundColor;
            Console.ForegroundColor = ClrStatusFg;
            Console.BackgroundColor = ClrStatusBg;
            Console.Write(status);
            Console.ForegroundColor = oldFg;
            Console.BackgroundColor = oldBg;
        }

        private void EnsureItemsLoaded()
        {
            if (!_itemsDirty) return;
            LoadItems();
            _itemsDirty = false;
            // Invalidate detail caches; force full redraw on next frame.
            _cachedFolderPath  = null;
            _cachedFilePath    = null;
            _lastSelectedIndex = -2;
            _lastScrollOffset  = -2;
        }

        private void LoadItems()
        {
            _items.Clear();
            _cachedDirCount  = 0;
            _cachedFileCount = 0;

            try
            {
                foreach (var d in Directory.GetDirectories(_currentRoot))
                {
                    _items.Add(new Item { Path = d, IsDirectory = true });
                    _cachedDirCount++;
                }
            }
            catch { }

            try
            {
                foreach (var f in Directory.GetFiles(_currentRoot))
                {
                    long? size = null;
                    try { size = new FileInfo(f).Length; } catch { }
                    _items.Add(new Item { Path = f, IsDirectory = false, SizeBytes = size });
                    _cachedFileCount++;
                }
            }
            catch { }
        }

        private void RenderInfoPane(int left, int top, int width, int height)
        {
            for (int row = 0; row < height; row++)
                ClearLineAt(left, top + row, width);

            string heading = "─ Details " + new string('─', Math.Max(0, width - 10));
            WriteTrimmedAtColor(left, top, heading, width, ClrBorder);

            if (_items.Count == 0 || _selectedIndex < 0 || _selectedIndex >= _items.Count)
            {
                WriteTrimmedAtColor(left, top + 1, "  (empty)", width, ClrBorder);
                return;
            }

            var item = _items[_selectedIndex];
            if (item.IsDirectory)
                ShowFolderDetails(item.Path, left, top + 1, width, height - 1);
            else
                ShowFileDetails(item.Path, left, top + 1, width, height - 1);
        }

        private void SetCurretnDirectory(string path)
        {
            if (path.EndsWith(":\\"))
                File.WriteAllText(GlobalVariables.currentDirectory, path);
            else
                File.WriteAllText(GlobalVariables.currentDirectory, path + "\\");
        }

        private bool HandleMainKey(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.Oem3:
                    return false;

                case ConsoleKey.UpArrow:
                    if (_items.Count > 0)
                        _selectedIndex = Clamp(_selectedIndex - 1, 0, _items.Count - 1);
                    return true;

                case ConsoleKey.DownArrow:
                    if (_items.Count > 0)
                        _selectedIndex = Clamp(_selectedIndex + 1, 0, _items.Count - 1);
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
                            SetCurretnDirectory(item.Path);
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
                    GoUp();
                    _selectedIndex = 0;
                    _scrollOffset  = 0;
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
                int width = Math.Max(Console.WindowWidth, 60);
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
                    WritePadded(line, width, i == selected, ConsoleColor.White);
                }

                int clearFrom = 4 + drives.Length;
                int totalRows = Math.Max(Console.WindowHeight, 20);
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
                        running = false;
                        break;
                    case ConsoleKey.Escape:
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

            int width = Math.Max(Console.WindowWidth, 60);
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
            string term = Console.ReadLine();
            Console.CursorVisible = false;
            Console.ForegroundColor = oldFg;

            if (string.IsNullOrWhiteSpace(term))
            {
                _searchMode = false;
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

            _searchMode = true;
        }

        private void RenderSearchScreen()
        {
            int width  = Math.Max(Console.WindowWidth, 60);
            int height = Math.Max(Console.WindowHeight, 20);
            ConditionalClear(width, height);

            int headerLines   = 4;
            int contentTop    = headerLines;
            int contentHeight = height - headerLines - 1;
            if (contentHeight < 4) contentHeight = 4;
            int statusBarY = contentTop + contentHeight;

            int    count   = _searchResults.Count;
            string counter = count > 0 ? $"[{_searchSelectedIndex + 1}/{count}]" : "[0/0]";
            string titleBase = " ◈ Search Results";
            int    padLen  = Math.Max(0, width - titleBase.Length - counter.Length - 1);
            string titleLine = titleBase + new string(' ', padLen) + counter + " ";
            if (titleLine.Length > width) titleLine = titleLine.Substring(0, width);
            WriteTrimmedAtColor(0, 0, titleLine, width, ClrTitle);

            WriteTrimmedAtColor(0, 1, new string('═', width), width, ClrBorder);
            WriteTrimmedAtColor(0, 2, $"  Base: {_currentRoot}", width, ClrPath);
            WriteTrimmedAtColor(0, 3,
                " ↑↓:move  ↵:open  Del:del  Esc/Q:exit  ⌫:back  U:up",
                width, ClrHelp);

            if (_searchResults.Count > 0)
                _searchSelectedIndex = Clamp(_searchSelectedIndex, 0, _searchResults.Count - 1);
            else
                _searchSelectedIndex = 0;

            int maxOffset = Math.Max(0, _searchResults.Count - contentHeight);
            if (_searchSelectedIndex < _searchScrollOffset)
                _searchScrollOffset = _searchSelectedIndex;
            else if (_searchSelectedIndex >= _searchScrollOffset + contentHeight)
                _searchScrollOffset = _searchSelectedIndex - contentHeight + 1;
            _searchScrollOffset = Clamp(_searchScrollOffset, 0, maxOffset);

            for (int row = 0; row < contentHeight; row++)
            {
                Console.SetCursorPosition(0, contentTop + row);

                int idx = _searchScrollOffset + row;
                string       text  = "";
                ConsoleColor color = ConsoleColor.Gray;

                if (idx >= 0 && idx < _searchResults.Count)
                {
                    var item = _searchResults[idx];
                    text  = (item.IsDirectory ? "▶ " : "· ") + item.Path;
                    color = item.IsDirectory ? ClrDirItem : GetFileColor(item.Path);
                }

                WritePadded(text, width, idx == _searchSelectedIndex, color);
            }

            // Status bar for search
            if (statusBarY < Console.WindowHeight)
            {
                int dirCount  = _searchResults.Count(i => i.IsDirectory);
                int fileCount = _searchResults.Count(i => !i.IsDirectory);
                string status = $"  {dirCount} folder{(dirCount != 1 ? "s" : "")} · {fileCount} file{(fileCount != 1 ? "s" : "")} matched";
                if (status.Length > width) status = status.Substring(0, width);
                else status = status.PadRight(width);

                Console.SetCursorPosition(0, statusBarY);
                var oldFg = Console.ForegroundColor;
                var oldBg = Console.BackgroundColor;
                Console.ForegroundColor = ClrStatusFg;
                Console.BackgroundColor = ClrStatusBg;
                Console.Write(status);
                Console.ForegroundColor = oldFg;
                Console.BackgroundColor = oldBg;
            }
        }

        private bool HandleSearchKey(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    _searchMode = false;
                    return true;

                case ConsoleKey.UpArrow:
                    if (_searchResults.Count > 0)
                        _searchSelectedIndex = Clamp(_searchSelectedIndex - 1, 0, _searchResults.Count - 1);
                    return true;

                case ConsoleKey.DownArrow:
                    if (_searchResults.Count > 0)
                        _searchSelectedIndex = Clamp(_searchSelectedIndex + 1, 0, _searchResults.Count - 1);
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
                            _searchMode    = false;
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
                    _searchMode = false;
                    return true;

                case ConsoleKey.U:
                    GoUp();
                    _selectedIndex = 0;
                    _scrollOffset  = 0;
                    _searchMode    = false;
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
                newRoot = Path.GetFullPath(newRoot);
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
            }
            catch { }
        }

        private void GoBack()
        {
            if (_historyBack.Count == 0) return;

            var entry = _historyBack.Pop();
            _suppressHistory = true;
            NavigateTo(entry.Path);
            SetCurretnDirectory(entry.Path);
            _suppressHistory = false;

            _selectedIndex = entry.SelectedIndex;
            _scrollOffset  = entry.ScrollOffset;
            _itemsDirty    = true;
        }

        private void GoUp()
        {
            try
            {
                string parent = Directory.GetParent(_currentRoot)?.FullName;
                if (parent == null) return;
                NavigateTo(parent);
                SetCurretnDirectory(parent);
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
                int width = Math.Max(Console.WindowWidth, 60);
                Console.Clear();

                WriteTrimmedAtColor(0, 0, " ◈ File Details", width, ClrTitle);
                WriteTrimmedAtColor(0, 1, new string('═', width), width, ClrBorder);

                int          row = 2;
                ConsoleColor fc  = GetFileColor(filePath);

                void WriteInfoRow(string label, string value, ConsoleColor vc)
                {
                    string lp = $"  {label,-12}│ ";
                    Console.SetCursorPosition(0, row);
                    var old = Console.ForegroundColor;
                    Console.ForegroundColor = ClrInfoLabel;
                    Console.Write(lp);
                    int rem = width - lp.Length;
                    if (rem > 0)
                    {
                        Console.ForegroundColor = vc;
                        string v = value ?? "";
                        if (v.Length > rem) v = v.Substring(0, rem);
                        Console.Write(v);
                    }
                    Console.ForegroundColor = old;
                    row++;
                }

                WriteInfoRow("Name",      info.Name, fc);
                WriteInfoRow("Extension", string.IsNullOrEmpty(info.Extension) ? "(none)" : info.Extension, fc);
                WriteInfoRow("Size",      FormatSize(info.Length) + $"  ({info.Length:N0} B)", ClrInfoValue);
                WriteInfoRow("Modified",  info.LastWriteTime.ToString("yyyy-MM-dd  HH:mm:ss"), ClrInfoValue);
                WriteInfoRow("Created",   info.CreationTime.ToString("yyyy-MM-dd  HH:mm:ss"), ClrInfoValue);

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

            void WriteRow(string label, string value, ConsoleColor vc)
            {
                if (line >= maxLines) return;
                ClearLineAt(left, top + line, width);
                string lp = $"  {label,-8}│ ";
                if (lp.Length >= width) { line++; return; }
                Console.SetCursorPosition(left, top + line);
                var old = Console.ForegroundColor;
                Console.ForegroundColor = ClrInfoLabel;
                Console.Write(lp);
                int rem = width - lp.Length;
                if (rem > 0)
                {
                    Console.ForegroundColor = vc;
                    string v = value ?? "";
                    if (v.Length > rem) v = v.Substring(0, rem);
                    Console.Write(v);
                }
                Console.ForegroundColor = old;
                line++;
            }

            // Cache FileInfo so we don't stat the same file on every keypress.
            if (filePath != _cachedFilePath)
            {
                _cachedFilePath = filePath;
                _cachedFileInfo = null;
                try { _cachedFileInfo = new FileInfo(filePath); } catch { }
            }

            var info = _cachedFileInfo;

            try
            {
                if (info == null) throw new Exception();

                ConsoleColor fc = GetFileColor(filePath);
                WriteRow("Type",     "File", fc);
                WriteRow("Name",     info.Name, ClrInfoValue);
                WriteRow("Ext",      string.IsNullOrEmpty(info.Extension) ? "(none)" : info.Extension, fc);
                WriteRow("Size",     FormatSize(info.Length), ClrInfoValue);
                WriteRow("Modified", info.LastWriteTime.ToString("yyyy-MM-dd HH:mm"), ClrInfoValue);
                WriteRow("Created",  info.CreationTime.ToString("yyyy-MM-dd HH:mm"), ClrInfoValue);

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

        private void ShowFolderDetails(string folderPath, int left, int top, int width, int maxLines)
        {
            int line = 0;

            void WriteRow(string label, string value, ConsoleColor vc)
            {
                if (line >= maxLines) return;
                ClearLineAt(left, top + line, width);
                string lp = $"  {label,-8}│ ";
                if (lp.Length >= width) { line++; return; }
                Console.SetCursorPosition(left, top + line);
                var old = Console.ForegroundColor;
                Console.ForegroundColor = ClrInfoLabel;
                Console.Write(lp);
                int rem = width - lp.Length;
                if (rem > 0)
                {
                    Console.ForegroundColor = vc;
                    string v = value ?? "";
                    if (v.Length > rem) v = v.Substring(0, rem);
                    Console.Write(v);
                }
                Console.ForegroundColor = old;
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
            WriteRow("Type",     "Folder", ClrDirItem);
            WriteRow("Subs",     dirs.Length.ToString(), ClrInfoValue);
            WriteRow("Files",    files.Length.ToString(), ClrInfoValue);
            WriteRow("Modified", dirInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm"), ClrInfoValue);

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
                WriteTrimmedAtColor(left, top + line, "  ▶ " + name, width, ClrDirItem);
                line++;
            }

            foreach (var f in files)
            {
                if (line >= maxLines) break;
                string name = Path.GetFileName(f);
                if (string.IsNullOrEmpty(name)) name = f;
                ClearLineAt(left, top + line, width);
                WriteTrimmedAtColor(left, top + line, "  · " + name, width, GetFileColor(f));
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
            int width = Math.Max(Console.WindowWidth, 60);
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

        private static ConsoleColor GetFileColor(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();

            if (ext == ".exe" || ext == ".msi" || ext == ".bat" || ext == ".cmd" || ext == ".ps1" || ext == ".sh")
                return ConsoleColor.Red;

            if (ext == ".txt" || ext == ".md" || ext == ".log" || ext == ".ini" || ext == ".cfg" || ext == ".conf" || ext == ".csv")
                return ConsoleColor.DarkYellow;

            if (ext == ".cs" || ext == ".py" || ext == ".js" || ext == ".ts" || ext == ".cpp" || ext == ".c" ||
                ext == ".h" || ext == ".java" || ext == ".go" || ext == ".rs" || ext == ".rb" || ext == ".php")
                return ConsoleColor.Cyan;

            if (ext == ".zip" || ext == ".rar" || ext == ".7z" || ext == ".tar" || ext == ".gz" || ext == ".bz2")
                return ConsoleColor.Yellow;

            if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif" || ext == ".bmp" ||
                ext == ".svg" || ext == ".ico" || ext == ".webp")
                return ConsoleColor.Magenta;

            if (ext == ".mp3" || ext == ".mp4" || ext == ".avi" || ext == ".mkv" || ext == ".mov" ||
                ext == ".wav" || ext == ".flac")
                return ConsoleColor.DarkMagenta;

            if (ext == ".pdf" || ext == ".doc" || ext == ".docx" || ext == ".xls" || ext == ".xlsx" ||
                ext == ".ppt" || ext == ".pptx")
                return ConsoleColor.DarkYellow;

            if (ext == ".dll" || ext == ".sys" || ext == ".lib" || ext == ".pdb")
                return ConsoleColor.DarkRed;

            return ConsoleColor.White;
        }

        private static void WritePadded(string text, int width, bool selected, ConsoleColor normalColor)
        {
            var oldFg = Console.ForegroundColor;
            var oldBg = Console.BackgroundColor;

            if (selected)
            {
                Console.BackgroundColor = ConsoleColor.DarkCyan;
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
                Console.ForegroundColor = normalColor;
            }

            if (text == null) text = "";
            if (text.Length > width)
                text = text.Substring(0, width);
            if (text.Length < width)
                text = text + new string(' ', width - text.Length);

            Console.Write(text);

            Console.ForegroundColor = oldFg;
            Console.BackgroundColor = oldBg;
        }

        private static void WriteTrimmedAt(int left, int top, string text, int width)
        {
            if (text == null) text = "";
            int maxWidth = Math.Max(0, Math.Min(width, Console.WindowWidth - left));
            if (maxWidth <= 0) return;
            if (text.Length > maxWidth) text = text.Substring(0, maxWidth);
            else if (text.Length < maxWidth) text = text.PadRight(maxWidth);
            Console.SetCursorPosition(left, top);
            Console.Write(text);
        }

        private static void WriteTrimmedAtColor(int left, int top, string text, int width, ConsoleColor color)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = color;
            WriteTrimmedAt(left, top, text, width);
            Console.ForegroundColor = old;
        }

        private static void ClearLineAt(int left, int top, int width)
        {
            int maxWidth = Math.Max(0, Math.Min(width, Console.WindowWidth - left));
            if (maxWidth <= 0) return;
            Console.SetCursorPosition(left, top);
            Console.Write(new string(' ', maxWidth));
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
