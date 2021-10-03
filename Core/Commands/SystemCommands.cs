using System.Diagnostics;

namespace Core.Commands
{
    public static class SystemCommands
    {
        /// <summary>
        /// Shutdown command using command promt.
        /// </summary>
        public static void ShutDownCmd()
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo("cmd.exe")
            {
                UseShellExecute = false,
                Arguments = "/c shutdown /s /f /t 1"

            };
            process.Start();
            process.WaitForExit();
        }

        /// <summary>
        /// Reboot command using command promt.
        /// </summary>
        public static void RebootCmd()
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo("cmd.exe")
            {
                UseShellExecute = false,
                Arguments = "/c shutdown /r /f /t 1"

            };
            process.Start();
            process.WaitForExit();
        }

        /// <summary>
        /// Logoff current loged user using command promt.
        /// </summary>
        public static void LogoffCmd()
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo("cmd.exe")
            {
                UseShellExecute = false,
                Arguments = "/c shutdown /l /f"

            };
            process.Start();
            process.WaitForExit();
        }
    }
}
