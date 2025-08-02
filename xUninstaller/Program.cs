/*
 
xTerminal uninstaller.
 
*/
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
namespace xUninstaller
{
    [SupportedOSPlatform("windows")]
    internal class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);


        private static string s_installPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Programs\xTerminal";
        private static string s_profilePath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\xTerminal";
        private static string s_desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private static string s_shortcuDesktop = Path.Combine(s_desktopFolder, "xTerminal.lnk");


        static void Main(string[] args)
        {
            try
            {
                var result = MessageBox(IntPtr.Zero, "Do you want to uninstall xTerminal?", "xTerminal-Uninstaller", 0x00000004 | 0x00000020);
                if (result == 6)
                {
                    // Kill xTerminal process.
                    var processKiller = new ProcessManager();
                    processKiller.KillProcess("xTerminal");

                    // Delete directory
                    if (Directory.Exists(s_installPath))
                        Directory.Delete(s_installPath, true);
                    // Delete shortcut
                    if (File.Exists(s_shortcuDesktop))
                        File.Delete(s_shortcuDesktop);

                    // Delete from registry. (remove from installed apps)
                    const string uninstallRegPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
                    RegistryManagement.regKey_Delete(uninstallRegPath, "xTerminal");

                    // Delete xTerminal user data and uninstaller.
                    var ask = MessageBox(IntPtr.Zero, "Do you want to keep xTerminal user data?", "xTerminal-Uninstaller", 0x00000004 | 0x00000020);
                    if (ask != 6)
                    {
                        RegistryManagement.regKey_Delete("", "xTerminal", true);
                        processKiller.Delete(s_profilePath);
                    }
                    MessageBox(IntPtr.Zero, "xTerminal was uninstalled!", "xTerminal-Uninstaller", 0x00000000 | 0x00000030);
                }
            }
            catch (Exception ex)
            {
                // Error message display.
                MessageBox(IntPtr.Zero, $"Error: {ex.Message}.\nCheck log in: {s_profilePath}", "xTerminal-Uninstaller", 0x00000016);

                // Error store in log file.
                var date = DateTime.Now.ToString("ddMMyyyy_HHmm");
                var pathLogErr = $"{s_profilePath}\\{date}_errLog.log";
                File.AppendAllText(pathLogErr, $"\n\n{date}:\n{ex.Message}");
            }
        }
    }
}
