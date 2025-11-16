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

            // Cache size to avoid FileInfo on every repaint
            public long? SizeBytes { get; set; }
        }

        private class SearchItem
        {
            public string Path { get; set; }
            public bool IsDirectory { get; set; }
        }

        // History entry: remember path + selection + scroll for Backspace
        private class HistoryEntry
        {
            public string Path { get; set; }
            public int SelectedIndex { get; set; }
            public int ScrollOffset { get; set; }
        }

        private string _currentRoot;
        private readonly Stack<HistoryEntry> _historyBack = new Stack<HistoryEntry>();
        private bool _suppressHistory = false;

        // main list state (left pane)
        private readonly List<Item> _items = new List<Item>();
        private int _selectedIndex = 0;
        private int _scrollOffset = 0;          // first visible index in main list

        // search mode state
        private bool _searchMode = false;
        private readonly List<SearchItem> _searchResults = new List<SearchItem>();
        private int _searchSelectedIndex = 0;
        private int _searchScrollOffset = 0;    // first visible index in search results

        // only reload directory listing when needed
        private bool _itemsDirty = true;

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
        private void RenderMainScreen()
        {
            Console.Clear();

            EnsureItemsLoaded();

            int width = Math.Max(Console.WindowWidth, 60);
            int height = Math.Max(Console.WindowHeight, 20);

            int headerLines = 4;
            int contentTop = headerLines;
            int contentHeight = height - headerLines;
            if (contentHeight < 5) contentHeight = 5;

            // Layout: [LEFT LIST][|][RIGHT INFO]
            int totalWidth = width;
            int leftPaneFull = totalWidth / 2;     // includes separator column
            int leftWidth = leftPaneFull - 1;      // actual text width
            if (leftWidth < 20) leftWidth = 20;

            int separatorCol = leftWidth;          // '|' column
            int rightStartCol = separatorCol + 1;  // info pane starts after '|'
            int rightWidth = totalWidth - rightStartCol;
            if (rightWidth < 20) rightWidth = 20;

            // ---------- Header (trimmed, no wrapping) ----------
            string title = "xFile Explorer";
            WriteTrimmedAtColor(0, 0, title, width, ConsoleColor.Yellow);

            WriteTrimmedAt(0, 1, new string('=', width), width);

            string currentFolderLine = $"Current folder: {_currentRoot}";
            WriteTrimmedAtColor(0, 2, currentFolderLine, width, ConsoleColor.Green);

            string helpLine =
                "↑/↓: move | Home/End: top/bottom | Enter: open | Backspace: back | PageUp: up | Del: delete | /: search | Tab: drives | `: quit";
            WriteTrimmedAtColor(0, 3, helpLine, width, ConsoleColor.Cyan);
            // ---------------------------------------------------

            if (_items.Count > 0)
                _selectedIndex = Clamp(_selectedIndex, 0, _items.Count - 1);
            else
                _selectedIndex = 0;

            // Adjust scroll so selected item is always visible
            int maxOffset = Math.Max(0, _items.Count - contentHeight);
            if (_selectedIndex < _scrollOffset)
                _scrollOffset = _selectedIndex;
            else if (_selectedIndex >= _scrollOffset + contentHeight)
                _scrollOffset = _selectedIndex - contentHeight + 1;
            _scrollOffset = Clamp(_scrollOffset, 0, maxOffset);

            // Draw left pane (list)
            for (int row = 0; row < contentHeight; row++)
            {
                int y = contentTop + row;
                Console.SetCursorPosition(0, y);

                int idx = _scrollOffset + row;
                string text = "";
                ConsoleColor color = ConsoleColor.Gray;

                if (idx >= 0 && idx < _items.Count)
                {
                    var item = _items[idx];
                    string name = Path.GetFileName(item.Path);
                    if (string.IsNullOrEmpty(name)) name = item.Path;

                    if (item.IsDirectory)
                    {
                        text = "[D] " + name;
                        color = ConsoleColor.Green;      // folders = green
                    }
                    else
                    {
                        string size = "";
                        if (item.SizeBytes.HasValue)
                            size = $"  {item.SizeBytes.Value / 1024} KB";

                        text = "[F] " + name + size;
                        color = ConsoleColor.White;      // files = white
                    }
                }

                bool selected = (idx == _selectedIndex);
                WritePadded(text, leftWidth, selected, color);

                // Separator
                Console.SetCursorPosition(separatorCol, y);
                Console.Write("|");
            }

            // Draw right pane (info-only)
            RenderInfoPane(rightStartCol, contentTop, rightWidth, contentHeight);
        }

        private void EnsureItemsLoaded()
        {
            if (!_itemsDirty) return;
            LoadItems();
            _itemsDirty = false;
        }

        private void LoadItems()
        {
            _items.Clear();

            try
            {
                foreach (var d in Directory.GetDirectories(_currentRoot))
                {
                    _items.Add(new Item
                    {
                        Path = d,
                        IsDirectory = true,
                        SizeBytes = null
                    });
                }
            }
            catch { }

            try
            {
                foreach (var f in Directory.GetFiles(_currentRoot))
                {
                    long? size = null;
                    try
                    {
                        var fi = new FileInfo(f);
                        size = fi.Length;
                    }
                    catch { }

                    _items.Add(new Item
                    {
                        Path = f,
                        IsDirectory = false,
                        SizeBytes = size
                    });
                }
            }
            catch { }
        }

        private void RenderInfoPane(int left, int top, int width, int height)
        {
            // Clear info area
            for (int row = 0; row < height; row++)
            {
                ClearLineAt(left, top + row, width);
            }

            // Heading stays default color
            WriteTrimmedAt(left, top, "Info", width);
            WriteTrimmedAt(left, top + 1, new string('-', Math.Max(10, width - 1)), width);

            if (_items.Count == 0 || _selectedIndex < 0 || _selectedIndex >= _items.Count)
            {
                WriteTrimmedAt(left, top + 2, "(no items)", width);
                return;
            }

            var item = _items[_selectedIndex];

            if (item.IsDirectory)
                ShowFolderDetails(item.Path, left, top + 2, width, height - 2);
            else
                ShowFileDetails(item.Path, left, top + 2, width);
        }

        /// <summary>
        /// Set current directory.
        /// </summary>
        /// <param name="path"></param>
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
                case ConsoleKey.Oem3: // `
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
                    if (_items.Count > 0)
                        _selectedIndex = 0;
                    return true;

                case ConsoleKey.End:
                    if (_items.Count > 0)
                        _selectedIndex = _items.Count - 1;
                    return true;

                case ConsoleKey.Enter:
                    if (_items.Count > 0 &&
                        _selectedIndex >= 0 &&
                        _selectedIndex < _items.Count)
                    {
                        var item = _items[_selectedIndex];
                        if (item.IsDirectory)
                        {
                            NavigateTo(item.Path);
                            SetCurretnDirectory(item.Path);
                            _selectedIndex = 0;
                            _scrollOffset = 0;
                        }
                        else
                        {
                            OpenFile(item.Path);
                        }
                    }
                    return true;

                case ConsoleKey.Backspace:
                    // Go back and restore last selection in that folder
                    GoBack();
                    return true;

                case ConsoleKey.PageUp:
                    GoUp();
                    _selectedIndex = 0;
                    _scrollOffset = 0;
                    return true;

                case ConsoleKey.Oem2: // '/'
                    DoSearch();
                    return true;

                case ConsoleKey.Tab:
                    SwitchDrive();
                    _selectedIndex = 0;
                    _scrollOffset = 0;
                    return true;

                case ConsoleKey.Delete:
                    DeleteSelectedItem();
                    return true;

                default:
                    // Letter/number quick navigation in main list
                    if (char.IsLetterOrDigit(key.KeyChar))
                    {
                        JumpToItemStartingWith(char.ToLowerInvariant(key.KeyChar));
                    }
                    return true;
            }
        }

        // Delete currently selected item in main list
        private void DeleteSelectedItem()
        {
            if (_items.Count == 0 ||
                _selectedIndex < 0 ||
                _selectedIndex >= _items.Count)
                return;

            var item = _items[_selectedIndex];
            string name = Path.GetFileName(item.Path);
            if (string.IsNullOrEmpty(name)) name = item.Path;

            Console.Clear();
            Console.WriteLine(item.IsDirectory
                ? $"Delete folder and all its contents?\n{item.Path}"
                : $"Delete file?\n{item.Path}");
            Console.Write("Are you sure? (y/N): ");

            var key = Console.ReadKey(true);
            Console.WriteLine();

            if (key.KeyChar != 'y' && key.KeyChar != 'Y')
            {
                Console.WriteLine("Delete cancelled.");
                Console.WriteLine("Press any key to return...");
                Console.ReadKey(true);
                return;
            }

            try
            {
                if (item.IsDirectory)
                    Directory.Delete(item.Path, recursive: true);
                else
                    File.Delete(item.Path);

                Console.WriteLine("Deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting: {ex.Message}");
            }

            Console.WriteLine("Press any key to return...");
            Console.ReadKey(true);

            // After this, next RenderMainScreen will reload items from disk
            if (_selectedIndex >= _items.Count - 1)
                _selectedIndex = Math.Max(0, _selectedIndex - 1);

            _itemsDirty = true;
        }

        // Jump to next item whose name starts with given character
        private void JumpToItemStartingWith(char ch)
        {
            if (_items.Count == 0) return;

            int count = _items.Count;
            int start = _selectedIndex + 1;
            if (start >= count) start = 0;

            // First pass: from next item to end
            for (int i = start; i < count; i++)
            {
                string name = Path.GetFileName(_items[i].Path);
                if (!string.IsNullOrEmpty(name) &&
                    char.ToLowerInvariant(name[0]) == ch)
                {
                    _selectedIndex = i;
                    return;
                }
            }

            // Second pass: from beginning to current selection
            for (int i = 0; i <= _selectedIndex; i++)
            {
                string name = Path.GetFileName(_items[i].Path);
                if (!string.IsNullOrEmpty(name) &&
                    char.ToLowerInvariant(name[0]) == ch)
                {
                    _selectedIndex = i;
                    return;
                }
            }
        }

        // ========================= DRIVE SWITCH =========================

        private void SwitchDrive()
        {
            var drives = DriveInfo.GetDrives()
                                  .Where(d => d.IsReady)
                                  .ToArray();

            if (drives.Length == 0)
                return;

            Console.Clear();
            Console.WriteLine("Available drives");
            Console.WriteLine(new string('=', 40));
            Console.WriteLine();

            for (int i = 0; i < drives.Length; i++)
            {
                var d = drives[i];
                string line =
                    $"{i + 1}. {d.Name} ({d.DriveType}) " +
                    $"{FormatSize(d.TotalFreeSpace)} free of {FormatSize(d.TotalSize)}";
                Console.WriteLine(line);
            }

            Console.WriteLine();
            Console.Write("Select drive number (Enter to cancel): ");
            string input = Console.ReadLine();

            if (!int.TryParse(input, out int idx))
                return;

            if (idx < 1 || idx > drives.Length)
                return;

            string newRoot = drives[idx - 1].RootDirectory.FullName;
            NavigateTo(newRoot);
            _scrollOffset = 0;
            _itemsDirty = true;
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
            _searchScrollOffset = 0;

            Console.Clear();
            Console.WriteLine("Search");
            Console.WriteLine(new string('=', 40));
            Console.WriteLine($"Base folder: {_currentRoot}");
            Console.Write("Search term (part of file/folder name): ");

            string term = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(term))
            {
                _searchMode = false;
                return;
            }

            term = term.Trim();
            Console.WriteLine();
            Console.WriteLine("Searching... (this may take a while)");
            Console.WriteLine();

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
                        {
                            _searchResults.Add(new SearchItem { Path = d, IsDirectory = true });
                        }

                        pending.Push(d);
                    }

                    foreach (var f in Directory.GetFiles(dir))
                    {
                        string name = Path.GetFileName(f);
                        if (!string.IsNullOrEmpty(name) &&
                            name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _searchResults.Add(new SearchItem { Path = f, IsDirectory = false });
                        }
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
            Console.Clear();

            int width = Math.Max(Console.WindowWidth, 60);
            int height = Math.Max(Console.WindowHeight, 20);
            int headerLines = 4;
            int contentTop = headerLines;
            int contentHeight = height - headerLines;
            if (contentHeight < 5) contentHeight = 5;

            // ---------- Search header: trimmed ----------
            WriteTrimmedAtColor(0, 0, "Search Results", width, ConsoleColor.Yellow);
            WriteTrimmedAt(0, 1, new string('=', width), width);

            string baseFolder = $"Base folder: {_currentRoot}";
            WriteTrimmedAt(0, 2, baseFolder, width);

            string help = "↑/↓: move | Home/End: top/bottom | Enter: open/navigate | Del: delete | Esc/Q: exit search | Backspace: back | U: up";
            WriteTrimmedAt(0, 3, help, width);
            // ------------------------------------------------

            if (_searchResults.Count > 0)
                _searchSelectedIndex = Clamp(_searchSelectedIndex, 0, _searchResults.Count - 1);
            else
                _searchSelectedIndex = 0;

            // Scroll handling for search list
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
                string text = "";
                ConsoleColor color = ConsoleColor.Gray;

                if (idx >= 0 && idx < _searchResults.Count)
                {
                    var item = _searchResults[idx];
                    string type = item.IsDirectory ? "[D]" : "[F]";
                    text = $"{type} {item.Path}";
                    color = item.IsDirectory ? ConsoleColor.Green : ConsoleColor.White;
                }

                bool selected = (idx == _searchSelectedIndex);
                WritePadded(text, width, selected, color);
            }
        }

        private bool HandleSearchKey(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.Q:      // exit search, not app
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
                    if (_searchResults.Count > 0)
                        _searchSelectedIndex = 0;
                    return true;

                case ConsoleKey.End:
                    if (_searchResults.Count > 0)
                        _searchSelectedIndex = _searchResults.Count - 1;
                    return true;

                case ConsoleKey.Enter:
                    if (_searchResults.Count > 0 &&
                        _searchSelectedIndex >= 0 &&
                        _searchSelectedIndex < _searchResults.Count)
                    {
                        var item = _searchResults[_searchSelectedIndex];
                        if (item.IsDirectory)
                        {
                            NavigateTo(item.Path);
                            _selectedIndex = 0;
                            _scrollOffset = 0;
                            _searchMode = false;
                        }
                        else
                        {
                            OpenFile(item.Path);
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
                    _scrollOffset = 0;
                    _searchMode = false;
                    return true;

                case ConsoleKey.Delete:
                    DeleteSearchSelectedItem();
                    return true;

                default:
                    // Letter/number quick navigation in search results
                    if (char.IsLetterOrDigit(key.KeyChar))
                    {
                        JumpToSearchStartingWith(char.ToLowerInvariant(key.KeyChar));
                    }
                    return true;
            }
        }

        // Delete selected item in search results
        private void DeleteSearchSelectedItem()
        {
            if (_searchResults.Count == 0 ||
                _searchSelectedIndex < 0 ||
                _searchSelectedIndex >= _searchResults.Count)
                return;

            var item = _searchResults[_searchSelectedIndex];
            string name = Path.GetFileName(item.Path);
            if (string.IsNullOrEmpty(name)) name = item.Path;

            Console.Clear();
            Console.WriteLine(item.IsDirectory
                ? $"Delete folder and all its contents?\n{item.Path}"
                : $"Delete file?\n{item.Path}");
            Console.Write("Are you sure? (y/N): ");

            var key = Console.ReadKey(true);
            Console.WriteLine();

            if (key.KeyChar != 'y' && key.KeyChar != 'Y')
            {
                Console.WriteLine("Delete cancelled.");
                Console.WriteLine("Press any key to return...");
                Console.ReadKey(true);
                return;
            }

            try
            {
                if (item.IsDirectory)
                    Directory.Delete(item.Path, recursive: true);
                else
                    File.Delete(item.Path);

                Console.WriteLine("Deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting: {ex.Message}");
            }

            Console.WriteLine("Press any key to return...");
            Console.ReadKey(true);

            // Remove from search results list
            _searchResults.RemoveAt(_searchSelectedIndex);
            if (_searchSelectedIndex >= _searchResults.Count)
                _searchSelectedIndex = Math.Max(0, _searchSelectedIndex - 1);

            _itemsDirty = true;
        }

        // Jump to next search result whose name starts with given character
        private void JumpToSearchStartingWith(char ch)
        {
            if (_searchResults.Count == 0) return;

            int count = _searchResults.Count;
            int start = _searchSelectedIndex + 1;
            if (start >= count) start = 0;

            // First pass: from next result to end
            for (int i = start; i < count; i++)
            {
                string name = Path.GetFileName(_searchResults[i].Path);
                if (!string.IsNullOrEmpty(name) &&
                    char.ToLowerInvariant(name[0]) == ch)
                {
                    _searchSelectedIndex = i;
                    return;
                }
            }

            // Second pass: from beginning to current selection
            for (int i = 0; i <= _searchSelectedIndex; i++)
            {
                string name = Path.GetFileName(_searchResults[i].Path);
                if (!string.IsNullOrEmpty(name) &&
                    char.ToLowerInvariant(name[0]) == ch)
                {
                    _searchSelectedIndex = i;
                    return;
                }
            }
        }

        // ========================= NAVIGATION & DETAILS =========================

        private void NavigateTo(string newRoot)
        {
            try
            {
                newRoot = Path.GetFullPath(newRoot);
                if (!Directory.Exists(newRoot))
                    return;

                if (!_suppressHistory && _currentRoot != null)
                {
                    _historyBack.Push(new HistoryEntry
                    {
                        Path = _currentRoot,
                        SelectedIndex = _selectedIndex,
                        ScrollOffset = _scrollOffset
                    });
                }

                _currentRoot = newRoot;
                _itemsDirty = true;
            }
            catch { }
        }

        private void GoBack()
        {
            if (_historyBack.Count == 0)
                return;

            var entry = _historyBack.Pop();

            _suppressHistory = true;
            NavigateTo(entry.Path);
            SetCurretnDirectory(entry.Path);
            _suppressHistory = false;

            // restore selection and scroll for that folder
            _selectedIndex = entry.SelectedIndex;
            _scrollOffset = entry.ScrollOffset;

            _itemsDirty = true;
        }

        private void GoUp()
        {
            try
            {
                string parent = Directory.GetParent(_currentRoot)?.FullName;
                if (parent == null)
                    return;

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
                if (!File.Exists(filePath))
                    return;

                var info = new FileInfo(filePath);
                Console.Clear();
                Console.WriteLine("File details");
                Console.WriteLine(new string('=', 60));
                Console.WriteLine($"Name:         {info.Name}");
                Console.WriteLine($"Full path:    {info.FullName}");
                Console.WriteLine($"Size:         {info.Length} bytes");
                Console.WriteLine($"Last modified:{info.LastWriteTime}");
                Console.WriteLine();

                Console.Write("Open with default application? (y/N): ");
                var key = Console.ReadKey(true);
                Console.WriteLine();

                if (key.KeyChar == 'y' || key.KeyChar == 'Y')
                {
                    var psi = new ProcessStartInfo(filePath)
                    {
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(filePath)
                    };
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening file: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to return...");
            Console.ReadKey(true);
        }

        private void ShowFileDetails(string filePath, int left, int top, int width)
        {
            try
            {
                var info = new FileInfo(filePath);

                ClearLineAt(left, top, width);
                WriteTrimmedAtColor(left, top, "Type: File", width, ConsoleColor.Red);

                ClearLineAt(left, top + 1, width);
                WriteTrimmedAtColor(left, top + 1, $"Name: {info.Name}", width, ConsoleColor.Red);

                ClearLineAt(left, top + 2, width);
                WriteTrimmedAtColor(left, top + 2, $"Size: {info.Length} bytes", width, ConsoleColor.Red);

                ClearLineAt(left, top + 3, width);
                WriteTrimmedAtColor(left, top + 3, $"Last modified: {info.LastWriteTime}", width, ConsoleColor.Red);

                ClearLineAt(left, top + 4, width);
                string full = $"Path: {info.FullName}";
                WriteTrimmedAtColor(left, top + 4, full, width, ConsoleColor.Red);
            }
            catch
            {
                ClearLineAt(left, top, width);
                WriteTrimmedAtColor(left, top, "(unable to read file details)", width, ConsoleColor.Red);
            }
        }

        // Shows folder info + contents in info pane
        private void ShowFolderDetails(string folderPath, int left, int top, int width, int maxLines)
        {
            int line = 0;

            void WriteLineColored(string text, ConsoleColor color)
            {
                if (line >= maxLines) return;
                ClearLineAt(left, top + line, width);
                WriteTrimmedAtColor(left, top + line, text, width, color);
                line++;
            }

            string[] dirs = Array.Empty<string>();
            string[] files = Array.Empty<string>();

            try
            {
                if (Directory.Exists(folderPath))
                {
                    dirs = Directory.GetDirectories(folderPath);
                    files = Directory.GetFiles(folderPath);
                }
            }
            catch
            {
                // ignore
            }

            // Header info (red)
            WriteLineColored("Type: Folder", ConsoleColor.Red);
            WriteLineColored($"Path: {folderPath}", ConsoleColor.Red);
            WriteLineColored($"Subfolders: {dirs.Length}", ConsoleColor.Red);
            WriteLineColored($"Files: {files.Length}", ConsoleColor.Red);

            if (line >= maxLines)
                return;

            WriteLineColored("Contents:", ConsoleColor.Red);

            // List subfolders (green)
            foreach (var d in dirs)
            {
                if (line >= maxLines) break;
                string name = Path.GetFileName(d);
                if (string.IsNullOrEmpty(name)) name = d;
                string text = "[D] " + name;
                ClearLineAt(left, top + line, width);
                WriteTrimmedAtColor(left, top + line, text, width, ConsoleColor.Green);
                line++;
            }

            // List files (white)
            foreach (var f in files)
            {
                if (line >= maxLines) break;
                string name = Path.GetFileName(f);
                if (string.IsNullOrEmpty(name)) name = f;
                string text = "[F] " + name;
                ClearLineAt(left, top + line, width);
                WriteTrimmedAtColor(left, top + line, text, width, ConsoleColor.White);
                line++;
            }

            if (!Directory.Exists(folderPath) && line == 0)
            {
                WriteLineColored("(unable to read folder contents)", ConsoleColor.Red);
            }
        }

        // ========================= HELPERS =========================

        private static void WritePadded(string text, int width, bool selected, ConsoleColor normalColor)
        {
            var oldFg = Console.ForegroundColor;
            var oldBg = Console.BackgroundColor;

            if (selected)
            {
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.ForegroundColor = ConsoleColor.Black;
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

            if (text.Length > maxWidth)
                text = text.Substring(0, maxWidth);

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
