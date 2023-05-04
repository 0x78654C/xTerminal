using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Core.SystemTools
{
    [SupportedOSPlatform("Windows")]
    public class SplitArguments
    {
        /*
         * Command line parser from string;
         */

        private string CommandLine { get; set; }

        [DllImport("shell32.dll", SetLastError = true)]
        static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

        public SplitArguments(string commandLine)
        {
            CommandLine = commandLine;
        }

        /// <summary>
        /// Convert string to command lines arguments.
        /// </summary>
        /// <returns>string[]</returns>
        public string[] CommandLineToArgs()
        {
            int argc;
            var argv = CommandLineToArgvW(CommandLine, out argc);
            if (argv == IntPtr.Zero)
                FileSystem.ErrorWriteLine("Something went wrong. Check parameter!");
            try
            {
                var args = new string[argc];
                for (var i = 0; i < args.Length; i++)
                {
                    var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(p);
                }
                return args;
            }
            finally
            {
                Marshal.FreeHGlobal(argv);
            }
        }
    }
}
