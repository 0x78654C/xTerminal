using System;
using System.Runtime.InteropServices;

namespace Core.SystemTools
{
    internal sealed class VirtualTerminalOutput : IDisposable
    {
        private const int StdOutputHandle = -11;
        private const int EnableProcessedOutput = 0x0001;
        private const int EnableVirtualTerminalProcessing = 0x0004;

        private readonly IntPtr _outputHandle;
        private readonly int _originalMode;
        private bool _restoreMode;

        private VirtualTerminalOutput(IntPtr outputHandle, int originalMode, bool restoreMode)
        {
            _outputHandle = outputHandle;
            _originalMode = originalMode;
            _restoreMode = restoreMode;
        }

        public static VirtualTerminalOutput Enable()
        {
            IntPtr outputHandle = GetStdHandle(StdOutputHandle);
            if (!IsValidConsoleHandle(outputHandle) ||
                !GetConsoleMode(outputHandle, out int originalMode))
            {
                return new VirtualTerminalOutput(IntPtr.Zero, 0, false);
            }

            int virtualTerminalMode =
                originalMode | EnableProcessedOutput | EnableVirtualTerminalProcessing;

            bool restoreMode =
                virtualTerminalMode != originalMode &&
                SetConsoleMode(outputHandle, virtualTerminalMode);

            return new VirtualTerminalOutput(outputHandle, originalMode, restoreMode);
        }

        public void Dispose()
        {
            if (!_restoreMode)
                return;

            SetConsoleMode(_outputHandle, _originalMode);
            _restoreMode = false;
        }

        private static bool IsValidConsoleHandle(IntPtr handle)
        {
            return handle != IntPtr.Zero && handle.ToInt64() != -1;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, int dwMode);
    }
}
