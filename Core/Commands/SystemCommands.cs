using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace Core.Commands
{
    [SupportedOSPlatform("Windows")]
    public static class SystemCommands
    {
        private static readonly string s_shutdownPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System), "shutdown.exe");

        // Validates UNC hostnames (\\server), plain hostnames, and IPv4 addresses.
        private static readonly Regex s_remoteTargetRegex = new Regex(
            @"^(\\\\)?[a-zA-Z0-9][a-zA-Z0-9\-\.]{0,62}$|^(\d{1,3}\.){3}\d{1,3}$",
            RegexOptions.Compiled);

        /// <summary>
        /// Shutdown command.
        /// </summary>
        public static void ShutDownCmd(bool force, string remotePC="")
        {
            if (!string.IsNullOrEmpty(remotePC) && !s_remoteTargetRegex.IsMatch(remotePC.TrimStart('\\')))
            {
                FileSystem.ErrorWriteLine("Invalid remote computer name or address.");
                return;
            }

            string arg;
            if (string.IsNullOrEmpty(remotePC))
                arg = force ? "/s /f /t 1" : "/s /t 1";
            else
                arg = force ? $"/s /m {remotePC} /f /t 1" : $"/s /m {remotePC} /t 1";

            var process = new Process();
            process.StartInfo = new ProcessStartInfo(s_shutdownPath)
            {
                UseShellExecute = false,
                Arguments = arg
            };
            process.Start();
            process.WaitForExit();
        }

        /// <summary>
        /// Reboot command.
        /// </summary>
        public static void RebootCmd(bool force, string remotePC="")
        {
            if (!string.IsNullOrEmpty(remotePC) && !s_remoteTargetRegex.IsMatch(remotePC.TrimStart('\\')))
            {
                FileSystem.ErrorWriteLine("Invalid remote computer name or address.");
                return;
            }

            string arg;
            if (string.IsNullOrEmpty(remotePC))
                arg = force ? "/r /f /t 1" : "/r /t 1";
            else
                arg = force ? $"/r /m {remotePC} /f /t 1" : $"/r /m {remotePC} /t 1";

            var process = new Process();
            process.StartInfo = new ProcessStartInfo(s_shutdownPath)
            {
                UseShellExecute = false,
                Arguments = arg
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
            process.StartInfo = new ProcessStartInfo(s_shutdownPath)
            {
                UseShellExecute = false,
                Arguments = "/l /f"

            };
            process.Start();
            process.WaitForExit();
        }

        /// <summary>
        /// Screen Lock using command promt.
        /// </summary>
        public static void LockCmd()
        {
            var rundll32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "Rundll32.exe");
            var process = new Process();
            process.StartInfo = new ProcessStartInfo(rundll32)
            {
                UseShellExecute = false,
                Arguments = "user32.dll,LockWorkStation"

            };
            process.Start();
            process.WaitForExit();
        }

        /// <summary>
        /// Sleep/Hibernate.
        /// </summary>
        public static void SleepCcmd()
        {
            var rundll32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "Rundll32.exe");
            var process = new Process();
            process.StartInfo = new ProcessStartInfo(rundll32)
            {
                UseShellExecute = false,
                Arguments = "powrprof.dll,SetSuspendState"

            };
            process.Start();
            process.WaitForExit();
        }
        
        /// <summary>
        /// SSH command — executes ssh directly to avoid shell injection via cmd.exe.
        /// </summary>
        public static void SSHCmd(string input)
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo("ssh")
            {
                UseShellExecute = false,
                Arguments = input
            };
            process.Start();
            process.WaitForExit();
        }
    }
}

