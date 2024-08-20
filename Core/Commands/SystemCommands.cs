using System.Diagnostics;

namespace Core.Commands
{
    public static class SystemCommands
    {
        /// <summary>
        /// Shutdown command using command promt.
        /// </summary>
        public static void ShutDownCmd(bool force, string remotePC="")
        {
            var process = new Process();

            var arg = "";
            if(string.IsNullOrEmpty(remotePC))
                arg = (force) ? "/c shutdown /s /f /t 1": "/c shutdown /s /t 1";
           else
                arg = (force) ? $@"/c shutdown /s /m {remotePC} /f /t 1" : $@"/c shutdown /s /m {remotePC} /t 1";
            process.StartInfo = new ProcessStartInfo("cmd.exe")
            {
                UseShellExecute = false,
                Arguments = arg

            };
            process.Start();
            process.WaitForExit();
        }

        /// <summary>
        /// Reboot command using command promt.
        /// </summary>
        public static void RebootCmd(bool force, string remotePC="")
        {
            var arg = "";
            if (string.IsNullOrEmpty(remotePC))
                arg = (force) ? "/c shutdown /s /f /t 1" : "/c shutdown /s /t 1";
            else
                arg = (force) ? $@"/c shutdown /s /m {remotePC} /f /t 1" : $@"/c shutdown /s /m {remotePC} /t 1";
            var process = new Process();
            process.StartInfo = new ProcessStartInfo("cmd.exe")
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
            process.StartInfo = new ProcessStartInfo("cmd.exe")
            {
                UseShellExecute = false,
                Arguments = "/c shutdown /l /f"

            };
            process.Start();
            process.WaitForExit();
        }

        /// <summary>
        /// Screen Lock using command promt.
        /// </summary>
        public static void LockCmd()
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo("cmd.exe")
            {
                UseShellExecute = false,
                Arguments = "/c Rundll32.exe user32.dll,LockWorkStation"

            };
            process.Start();
            process.WaitForExit();
        }

        /// <summary>
        /// Sleep/Hibernate.
        /// </summary>
        public static void SleepCcmd()
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo("cmd.exe")
            {
                UseShellExecute = false,
                Arguments = "/c Rundll32.exe powrprof.dll,SetSuspendState"

            };
            process.Start();
            process.WaitForExit();
        }

    }
}
