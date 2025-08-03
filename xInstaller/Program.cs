/*
      Description: xTerminal installer

      This app is distributed under the MIT License.
      Copyright © 2022 - 2025 x_coding. All rights reserved.

      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
      IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
      FITNESS FOR A PARTICULAR PURPOSE AND NON INFRINGEMENT. IN NO EVENT SHALL THE
      AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
      LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
      OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
      SOFTWARE.
*/

using Raylib_cs;
using System.Numerics;
using System.Diagnostics;
using Microsoft.Win32;
using System.Security.Principal;
using System.Runtime.InteropServices;
using IWSh = IWshRuntimeLibrary;
using System.Runtime.Versioning;

namespace xInstaller
{
    [SupportedOSPlatform("Windows")]
    internal class Program
    {
        static long s_totalBytes = 0;
        static long s_copiedBytes = 0;
        static bool s_isCopyingDone = false;
        static string s_statusPrint = "";
        static bool s_isShortAsked = false;
        const string _bgPath1 = @"resources\banner.png";
        const string _bgPath2 = @"resources\bg1.png";
        const string _bgPath3 = @"resources\bg2.png";
        const string _bgPath4 = @"resources\bg3.png";
        const string _iconPath = @"resources\xTerminal.png";
        const string _sourceDirX64 = @"data\x64\";
        const string _sourceDirX86 = @"data\x86\";
        const string _uninstaller = @"data\Uninstaller\xUninstaller.exe";
        static string s_destDirectory = $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Programs\xTerminal";
        static string s_profilePath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\xTerminal";
        static bool s_isAdmin = IsLoggedUserAdmin();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);

        /// <summary>
        /// Entry point
        /// </summary>
        public static void Main()
        {
            var isButtonClicked = false;
            var currentBackground = 0;
            var switchTime = 5.0f;
            var timer = 0.0f;
            var progress = 0.0f;
            var sourceDir = "";

            Raylib.SetTraceLogLevel(TraceLogLevel.None);

            if (Environment.Is64BitOperatingSystem)
                sourceDir = _sourceDirX64;
            else
                sourceDir = _sourceDirX86;

            Image icon = Raylib.LoadImage(_iconPath);
            if (s_isAdmin)
                Raylib.InitWindow(800, 480, "xTerminal Installer : Administrator");
            else
                Raylib.InitWindow(800, 480, "xTerminal Installer");
            Raylib.SetTargetFPS(60);
            Raylib.SetWindowIcon(icon);
            Raylib.UnloadImage(icon);

            Texture2D[] backgrounds = new Texture2D[4]{
                Raylib.LoadTexture(_bgPath1),
                Raylib.LoadTexture(_bgPath2),
                Raylib.LoadTexture(_bgPath3),
                Raylib.LoadTexture(_bgPath4)
             };

            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.White);


                // Set multiple backgrounds
                float delta = Raylib.GetFrameTime();
                timer += delta;

                if (timer >= switchTime)
                {
                    timer = 0;
                    currentBackground = (currentBackground + 1) % backgrounds.Length;
                }


                Texture2D bg = backgrounds[currentBackground];
                Raylib.DrawTexturePro(
                    bg,
                    new Rectangle(0, 0, bg.Width, bg.Height),
                    new Rectangle(0, 0, 800, 398),
                    new Vector2(0, 0),
                    0.0f,
                    Color.White
                );

                // Install button action.
                if (InstallButton(new Rectangle(690, 420, 82, 40), "Install", Color.LightGray))
                {
                    Raylib.DrawText($"", 22, 460, 13, Color.DarkGray);
                    s_statusPrint = "";
                    progress = 0.0f;
                    isButtonClicked = true;
                    s_isCopyingDone = false;
                    s_isShortAsked = false;
                    CopyFiles(sourceDir, s_destDirectory);
                    if (!s_statusPrint.Contains("installed"))
                        CopyUninstaller(_uninstaller, s_profilePath);
                }

                // Progress bar variables.     		
                var barX = 20;
                var barY = 424;
                var barWidth = 643;
                var barHeigth = 30;
                Raylib.DrawRectangle(barX, barY, barWidth, barHeigth, Color.LightGray);

                // Start progress bar only if clicked install button.
                if (s_isCopyingDone && !s_statusPrint.Contains("installed"))
                {
                    Raylib.DrawText($"Done!", 22, 460, 13, Color.DarkGray);
                    if (!s_isShortAsked)
                    {
                        var result = MessageBox(IntPtr.Zero, "Do you want to create shortcut on desktop for xTerminal?", "xTerminal-Installer", 0x00000004 | 0x00000020);
                        if (result == 6)
                        {
                            var pathX = $"{s_destDirectory}\\xTerminal.exe";
                            if (File.Exists(pathX))
                                CreateShortcut(pathX);
                        }

                        var resultStartMenu = MessageBox(IntPtr.Zero, "Do you want to add xTerminal to Start Menu?", "xTerminal-Installer", 0x00000004 | 0x00000020);
                        if (resultStartMenu == 6)
                        {
                            var pathX = $"{s_destDirectory}\\xTerminal.exe";
                            if (File.Exists(pathX))
                                CreateShortcut(pathX, true);
                        }
                        s_isShortAsked = true;
                    }
                }
                else
                {
                    if (isButtonClicked && !s_statusPrint.Contains("installed") && !s_isCopyingDone)
                    {
                        Raylib.DrawText($"Copying files ....", 22, 460, 13, Color.DarkGray);
                        progress = s_totalBytes > 0 ? (float)s_copiedBytes / s_totalBytes : 0f;
                        var filledWidth = (int)(barWidth * (progress));
                        Raylib.DrawRectangle(barX, barY, filledWidth, barHeigth, Color.Green);
                        InstallButton(new Rectangle(690, 420, 82, 40), "Install", Color.Gray);
                    }
                }
                if (s_statusPrint.Contains("installed") && isButtonClicked)
                    Raylib.DrawText($"xTerminal is already installed!", 22, 460, 15, Color.DarkGray);
                Raylib.EndDrawing();
            }


            // Unload background
            foreach (var bg in backgrounds)
                Raylib.UnloadTexture(bg);

            Raylib.CloseWindow();
        }

        /// <summary>
        /// Install button declaration.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static bool InstallButton(Rectangle bounds, string text, Color color)
        {
            Raylib.DrawRectangleRec(bounds, color);
            Raylib.DrawRectangleLinesEx(bounds, 2, Color.Black);
            Raylib.DrawText(text, (int)(bounds.X + 10), (int)(bounds.Y + 10), 20, Color.Black);
            return Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), bounds) && Raylib.IsMouseButtonPressed(MouseButton.Left);
        }

        /// <summary>
        /// Function for copy files.
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="destDir"></param>
        public static void CopyFiles(string sourceDir, string destDir)
        {
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            var sdirLen = new DirectoryInfo(sourceDir).GetFiles("*", SearchOption.AllDirectories).Length;
            var desLen = new DirectoryInfo(destDir).GetFiles("*", SearchOption.AllDirectories).Length;

            if (sdirLen == desLen)
            {
                Version fileVersion;
                if (Environment.Is64BitOperatingSystem)
                    fileVersion = GetFileVersion($"{_sourceDirX64}xTerminal.exe");
                else
                    fileVersion = GetFileVersion($"{_sourceDirX86}xTerminal.exe");

                var destVersion = GetFileVersion($"{s_destDirectory}\\xTerminal.exe");

                var versionCompare = fileVersion.CompareTo(destVersion);

                s_statusPrint = "xTerminal is allready installed!";

                if (File.Exists($"{s_destDirectory}\\xTerminal.exe") && versionCompare < 0)
                {
                    MessageBox(IntPtr.Zero, $"You already have the newest version for xTerminal?", "xTerminal-Installer", 0x00000004 | 0x00000020);
                    return;
                }

                if (File.Exists($"{s_destDirectory}\\xTerminal.exe") && versionCompare > 0)
                {
                    var resultUpdate = MessageBox(IntPtr.Zero, $"You current xTerminal version is {destVersion.ToString()}. Do you want to update it at version {fileVersion.ToString()}?", "xTerminal-Installer", 0x00000004 | 0x00000020);
                    if (resultUpdate != 6)
                        return;
                    else
                        s_statusPrint = "";
                }
                else
                {
                    var result = MessageBox(IntPtr.Zero, "xTerminal is allready installed. Do you want to repair it?", "xTerminal-Installer", 0x00000004 | 0x00000020);
                    if (result != 6)
                        return;
                    else
                        s_statusPrint = "";
                }
            }

            // Write unsintall registry.
            Reg_Uninstall();

            var files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
                s_totalBytes += new FileInfo(file).Length;

            Thread copyThread = new Thread(() =>
            {
                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(sourceDir, file);
                    var targetPath = Path.Combine(destDir, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                    using (FileStream source = File.OpenRead(file))
                    using (FileStream dest = File.Create(targetPath))
                    {
                        byte[] buffer = new byte[81920]; // 80 KB buffer
                        int bytesRead;
                        while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            dest.Write(buffer, 0, bytesRead);
                            s_copiedBytes += bytesRead;
                        }
                    }
                }
                s_isCopyingDone = true;
            });
            copyThread.Start();
        }

        /// <summary>
        /// Copy uininstaller in user data profile folder.
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="destDir"></param>
        private static void CopyUninstaller(string sourceFile, string destDir)
        {
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            var destFile = $@"{destDir}\xUninstaller.exe";

            Thread copyThread = new Thread(() =>
            {
                using (FileStream source = File.OpenRead(sourceFile))
                using (FileStream dest = File.Create(destFile))
                {
                    byte[] buffer = new byte[81920]; // 80 KB buffer
                    int bytesRead;
                    while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                        dest.Write(buffer, 0, bytesRead);
                }
            });
            copyThread.Start();
        }

        /// <summary>
        /// Get file version from file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static Version GetFileVersion(string filePath)
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
            return new Version(versionInfo.FileVersion);
        }


        /// <summary>
        ///  Write registry key.
        /// </summary>
        private static void Reg_Uninstall()
        {
            try
            {
                var pathReg = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\xTerminal";
                var userProfile = $@"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}";
                var date = DateTime.Now.ToString("yyyyMMdd");
                var fileVersion = "1.0";
                if (Environment.Is64BitOperatingSystem)
                    fileVersion = FileVersionInfo.GetVersionInfo($"{_sourceDirX64}xTerminal.exe").FileVersion;
                else
                    fileVersion = FileVersionInfo.GetVersionInfo($"{_sourceDirX86}xTerminal.exe").FileVersion;

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(pathReg))
                {
                    key.SetValue("DisplayName", "xTerminal");
                    key.SetValue("DisplayVersion", fileVersion);
                    key.SetValue("InstallLocation", $@"{userProfile}\AppData\Local\Programs\xTerminal");
                    key.SetValue("DisplayIcon", $@"{userProfile}\AppData\Local\Programs\xTerminal\icon.ico");
                    key.SetValue("InstallDate", date);
                    key.SetValue("UninstallString", $@"{userProfile}\AppData\Local\xTerminal\xUninstaller.exe");
                    key.SetValue("Publisher", "x_Coding");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erorr" + ex.ToString());
            }
        }

        /// <summary>
        /// Function for check if user has administrator rights
        /// </summary>
        /// <returns></returns>
        private static bool IsLoggedUserAdmin()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        /// <summary>
        ///  Create shortcut function.
        /// </summary>
        /// <param name="filePath"></param>
        private static void CreateShortcut(string filePath, bool inStartMenu = false)
        {
            var desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var fileWithoutExtPath = Path.GetFileNameWithoutExtension(filePath);
            string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            var finalPath = "";
            if (inStartMenu)
                finalPath = Path.Combine(startMenuPath, "Programs", $"{fileWithoutExtPath}.lnk");
            else
                finalPath = Path.Combine(desktopFolder, $"{fileWithoutExtPath}.lnk");
            IWSh.IWshShortcut shortcut;
            if (Environment.Is64BitOperatingSystem)
            {
                IWSh.WshShell wshShell = new IWSh.WshShell();
                shortcut = (IWSh.IWshShortcut)wshShell.CreateShortcut(finalPath);
            }
            else
            {
                IWSh.WshShellClass wshShell = new IWSh.WshShellClass();
                shortcut = (IWSh.IWshShortcut)wshShell.CreateShortcut(finalPath);
            }
            shortcut.TargetPath = filePath;
            shortcut.WorkingDirectory = s_destDirectory;
            shortcut.IconLocation = filePath;
            shortcut.Save();
        }
    }
}
