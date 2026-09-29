using System;
using System.Runtime.InteropServices;

namespace Core.DirFiles
{
    // Keep mouse events out of Console.KeyAvailable, which discards them.
    internal sealed class FileExplorerInput : IDisposable
    {
        private const int EnableWindowInput = 0x0008;
        private const int EnableMouseInput = 0x0010;
        private const int EnableQuickEditMode = 0x0040;
        private const int EnableExtendedFlags = 0x0080;
        private const int EnableVirtualTerminalInput = 0x0200;
        private const short KeyEvent = 1;
        private const short MouseEvent = 2;
        private const int MouseWheeled = 4;
        private readonly IntPtr _handle = GetStdHandle(-10);
        private readonly InputRecord[] _records = new InputRecord[1];
        private readonly int _originalMode;
        private readonly bool _enabled;

        public FileExplorerInput()
        {
            if (GetConsoleMode(_handle, out _originalMode))
            {
                // Enable window/mouse events and disable Quick Edit and VT input.
                int mode = (_originalMode | EnableExtendedFlags | EnableMouseInput | EnableWindowInput) &
                    ~(EnableQuickEditMode | EnableVirtualTerminalInput);
                _enabled = SetConsoleMode(_handle, mode);
            }
        }

        public bool TryRead(out ConsoleKeyInfo? key, out int repeats, out int wheelDelta)
        {
            key = null;
            repeats = 1;
            wheelDelta = 0;
            if (!_enabled)
            {
                if (!Console.KeyAvailable)
                    return false;
                key = Console.ReadKey(true);
                return true;
            }

            if (!PeekConsoleInput(_handle, _records, 1, out int count) || count == 0 ||
                !ReadConsoleInput(_handle, _records, 1, out count) || count == 0)
                return false;

            InputRecord record = _records[0];
            if (record.EventType == KeyEvent && record.KeyDown != 0 && record.VirtualKeyCode <= 255)
            {
                int state = record.ControlKeyState;
                key = new ConsoleKeyInfo(record.UnicodeChar, (ConsoleKey)record.VirtualKeyCode,
                    (state & 0x10) != 0, (state & 0x03) != 0, (state & 0x0c) != 0);
                repeats = Math.Max(1, (int)record.RepeatCount);
            }
            else if (record.EventType == MouseEvent && (record.MouseFlags & MouseWheeled) != 0)
            {
                wheelDelta = (short)(record.ButtonState >> 16);
            }
            return true;
        }

        public void Dispose()
        {
            if (_enabled)
                SetConsoleMode(_handle, _originalMode);
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode, Size = 20)]
        private struct InputRecord
        {
            [FieldOffset(0)] public short EventType;
            [FieldOffset(4)] public int KeyDown;
            [FieldOffset(8)] public ushort RepeatCount;
            [FieldOffset(10)] public ushort VirtualKeyCode;
            [FieldOffset(14)] public char UnicodeChar;
            [FieldOffset(16)] public int ControlKeyState;
            [FieldOffset(8)] public int ButtonState;
            [FieldOffset(16)] public int MouseFlags;
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(int handle);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetConsoleMode(IntPtr handle, out int mode);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetConsoleMode(IntPtr handle, int mode);

        [DllImport("kernel32.dll", EntryPoint = "PeekConsoleInputW", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PeekConsoleInput(IntPtr handle, [Out] InputRecord[] records, int length, out int count);

        [DllImport("kernel32.dll", EntryPoint = "ReadConsoleInputW", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadConsoleInput(IntPtr handle, [Out] InputRecord[] records, int length, out int count);
    }
}
