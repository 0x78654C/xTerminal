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


        static void Main(string[] args)
        {
            try
            {
                var result = MessageBox(IntPtr.Zero, "Do you want to uninstall xTerminal?", "xTerminal-Uninstaller", 0x00000001);
                if (result == 1)
                {
                    // Kill xTerminal process.
                    var processKiller = new ProcessManager();
                    processKiller.KillProcess("xTerminal");

                    // Delete directory
                    if (Directory.Exists(s_installPath))
                        Directory.Delete(s_installPath, true);

                    // Delete from registry. (remove from installed apps)
                    const string uninstallRegPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
                    RegistryManagement.regKey_Delete(uninstallRegPath, "xTerminal");

                    // Final message
                    MessageBox(IntPtr.Zero, "xTerminal was removed!", "xTerminal-Uninstaller", 0x00000001);
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
