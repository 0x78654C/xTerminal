using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Core.SystemTools
{
    public sealed partial class ProcessListingUI
    {
        private bool _treeView;
        private readonly HashSet<ProcessIdentity> _collapsedProcesses = new();
        private readonly Dictionary<ProcessIdentity, ProcessIdentity> _treeParents = new();
        private readonly Dictionary<ProcessIdentity, string> _treePrefixes = new();
        private readonly HashSet<ProcessIdentity> _treeBranches = new();
        private ProcessSnapshot[] _treeAllProcs = Array.Empty<ProcessSnapshot>();

        public ProcessListingUI() { }

        public ProcessListingUI(bool treeView)
        {
            _treeView = treeView;
        }

        // One native snapshot per sample supplies parent IDs without per-process
        // WMI queries, including processes whose handles cannot be opened.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ProcessEntry
        {
            public uint Size;
            public uint Usage;
            public uint ProcessId;
            public UIntPtr DefaultHeapId;
            public uint ModuleId;
            public uint ThreadCount;
            public uint ParentProcessId;
            public int BasePriority;
            public uint Flags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string ExeFile;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

        [DllImport("kernel32.dll", EntryPoint = "Process32FirstW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool Process32First(IntPtr snapshot, ref ProcessEntry entry);

        [DllImport("kernel32.dll", EntryPoint = "Process32NextW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool Process32Next(IntPtr snapshot, ref ProcessEntry entry);

        private static Dictionary<int, int> CaptureParentProcessIds()
        {
            var parents = new Dictionary<int, int>();
            const uint processSnapshot = 0x00000002;
            IntPtr snapshot = CreateToolhelp32Snapshot(processSnapshot, 0);
            if (snapshot == new IntPtr(-1)) return parents;
            try
            {
                var entry = new ProcessEntry { Size = (uint)Marshal.SizeOf<ProcessEntry>() };
                if (Process32First(snapshot, ref entry))
                {
                    do { parents[(int)entry.ProcessId] = (int)entry.ParentProcessId; }
                    while (Process32Next(snapshot, ref entry));
                }
            }
            finally { CloseHandle(snapshot); }
            return parents;
        }

        private ProcessSnapshot[] BuildTreeView(ProcessSnapshot[] ordered)
        {
            var byId = ordered.ToDictionary(p => p.Id);
            var children = new Dictionary<ProcessIdentity, List<ProcessSnapshot>>();
            var roots = new List<ProcessSnapshot>();
            foreach (ProcessSnapshot process in ordered)
            {
                if (process.ParentId > 0 && process.ParentId != process.Id &&
                    byId.TryGetValue(process.ParentId, out ProcessSnapshot parent) &&
                    !(parent.StartTimeTicks > 0 && process.StartTimeTicks > 0 && parent.StartTimeTicks > process.StartTimeTicks))
                {
                    if (!children.TryGetValue(parent.Identity, out var siblings))
                        children[parent.Identity] = siblings = new List<ProcessSnapshot>();
                    siblings.Add(process);
                }
                else roots.Add(process);
            }

            _treeParents.Clear();
            _treePrefixes.Clear();
            _treeBranches.Clear();
            var visited = new HashSet<ProcessIdentity>();
            var rows = new List<(ProcessSnapshot Process, int Depth)>(ordered.Length);
            var stack = new Stack<(ProcessSnapshot Process, ProcessIdentity? Parent, int Depth, string Indent, bool Last)>();

            // Iteration avoids stack overflow for deep trees. The second pass keeps
            // orphaned or cyclic entries visible exactly once as additional roots.
            foreach (ProcessSnapshot root in roots.Concat(ordered))
            {
                if (visited.Contains(root.Identity)) continue;
                stack.Push((root, null, 0, string.Empty, true));
                while (stack.Count > 0)
                {
                    var row = stack.Pop();
                    if (!visited.Add(row.Process.Identity)) continue;
                    rows.Add((row.Process, row.Depth));
                    _treePrefixes[row.Process.Identity] = row.Parent.HasValue
                        ? row.Indent + (row.Last ? "└─ " : "├─ ") : string.Empty;
                    if (row.Parent.HasValue)
                    {
                        _treeParents[row.Process.Identity] = row.Parent.Value;
                        _treeBranches.Add(row.Parent.Value);
                    }
                    if (!children.TryGetValue(row.Process.Identity, out var branch)) continue;
                    var unvisited = branch.Where(child => !visited.Contains(child.Identity)).ToArray();
                    string indent = row.Parent.HasValue ? row.Indent + (row.Last ? "   " : "│  ") : string.Empty;
                    if (indent.Length > 48) indent = indent[^48..];
                    for (int i = unvisited.Length - 1; i >= 0; i--)
                        stack.Push((unvisited[i], row.Process.Identity, row.Depth + 1, indent, i == unvisited.Length - 1));
                }
            }

            // Switching from flat view or finding a hidden child reveals its path.
            if (_selectedIdentity.HasValue)
            {
                ProcessIdentity identity = _selectedIdentity.Value;
                while (_treeParents.TryGetValue(identity, out ProcessIdentity parent))
                {
                    _collapsedProcesses.Remove(parent);
                    identity = parent;
                }
            }

            _treeAllProcs = rows.Select(row => row.Process).ToArray();
            var visible = new List<ProcessSnapshot>(rows.Count);
            int hiddenDepth = -1;
            foreach (var row in rows)
            {
                if (hiddenDepth >= 0 && row.Depth > hiddenDepth) continue;
                hiddenDepth = _collapsedProcesses.Contains(row.Process.Identity) ? row.Depth : -1;
                visible.Add(row.Process);
            }
            return visible.ToArray();
        }

        private void ToggleTreeView()
        {
            _treeView = !_treeView;
            _sortedSource = null;
            SortedProcesses();
            Status(_treeView ? "Tree view  ·  ←/→ collapse/expand  ·  Space toggles a branch  ·  T returns to flat view"
                : "Flat process view");
        }

        private void NavigateTree(bool expand)
        {
            if (!_treeView) return;
            SortedProcesses();
            ProcessSnapshot process = SelectedProcess();
            if (process == null) return;
            bool hasChildren = _treeBranches.Contains(process.Identity);
            bool collapsed = _collapsedProcesses.Contains(process.Identity);
            if (hasChildren && collapsed == expand)
            {
                if (expand) _collapsedProcesses.Remove(process.Identity);
                else _collapsedProcesses.Add(process.Identity);
                _sortedSource = null;
                SortedProcesses();
            }
            else if (expand && hasChildren)
                SelectIndex(_selectedIndex + 1);
            else if (!expand && _treeParents.TryGetValue(process.Identity, out ProcessIdentity parent))
                SelectIndex(Array.FindIndex(_sortedProcs, p => p.Identity == parent));
        }

        private void ToggleSelectedBranch()
        {
            if (!_treeView) return;
            SortedProcesses();
            ProcessSnapshot process = SelectedProcess();
            if (process != null && _treeBranches.Contains(process.Identity))
                NavigateTree(_collapsedProcesses.Contains(process.Identity));
        }

        private string ProcessDisplayName(ProcessSnapshot process, int width)
        {
            lock (_lock)
            {
                if (!_treeView) return Clip(process.Name, width);
                string prefix = _treePrefixes.GetValueOrDefault(process.Identity, string.Empty);
                prefix += _treeBranches.Contains(process.Identity)
                    ? (_collapsedProcesses.Contains(process.Identity) ? "▸ " : "▾ ") : "  ";
                int prefixWidth = Math.Max(1, width - 6);
                if (prefix.Length > prefixWidth) prefix = "…" + prefix[^(prefixWidth - 1)..];
                return Clip(prefix + process.Name, width);
            }
        }
    }
}
