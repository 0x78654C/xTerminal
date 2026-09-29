/*
      Description: xTerminal installer

      This app is distributed under the MIT License.
      Copyright © 2022 - 2026 x_coding. All rights reserved.

      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
      IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
      FITNESS FOR A PARTICULAR PURPOSE AND NON INFRINGEMENT. IN NO EVENT SHALL THE
      AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
      LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
      OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
      SOFTWARE.
*/

using Microsoft.Win32;
using Raylib_cs;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Security.Cryptography;
using IWSh = IWshRuntimeLibrary;

namespace xInstaller
{
    [SupportedOSPlatform("Windows")]
    internal partial class Program
    {
        static long s_totalBytes = 0;
        static long s_copiedBytes = 0;
        static bool s_isCopyingDone = false;
        static string s_statusPrint = "";
        static bool s_isShortAsked = false;
        static string s_xTerminalVersion = "UNKNOWN";
        const string _iconPath = @"resources\xTerminal.png";
        const string _latestIconPath = @"media\xterminal_logo.png";
        const string _sourceDirX64 = @"data\x64\";
        const string _sourceDirX86 = @"data\x86\";
        const string _uninstaller = @"data\Uninstaller\xUninstaller.exe";
        static string s_destDirectory = $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Programs\xTerminal";
        static string s_profilePath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\xTerminal";
        static bool s_isAdmin = IsLoggedUserAdmin();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);

        private const int GWL_EXSTYLE = -20;
        private const long WS_EX_APPWINDOW = 0x00040000L;
        private const long WS_EX_TOOLWINDOW = 0x00000080L;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        private enum WindowAction
        {
            None,
            Minimize,
            Close
        }

        /// <summary>
        /// Entry point
        /// </summary>
        public static void Main()
        {
            var isButtonClicked = false;
            var timer = 0.0f;
            var progress = 0.0f;
            var sourceDir = "";
            var shouldClose = false;
            var isDraggingWindow = false;
            var dragOffset = Vector2.Zero;

            Raylib.SetTraceLogLevel(TraceLogLevel.None);

            if (Environment.Is64BitOperatingSystem)
                sourceDir = ResolvePayloadDirectory(_sourceDirX64);
            else
                sourceDir = ResolvePayloadDirectory(_sourceDirX86);

            var uninstallerPath = ResolvePayloadFile(_uninstaller);
            VerifyPayloadManifest(sourceDir, uninstallerPath);

            s_xTerminalVersion = GetFileVersionLabel(Path.Combine(sourceDir, "xTerminal.exe"));

            var iconPath = ResolveResourcePath(_latestIconPath);
            if (string.IsNullOrEmpty(iconPath))
                iconPath = ResolveResourcePath(_iconPath);
            Raylib.SetConfigFlags(ConfigFlags.UndecoratedWindow | ConfigFlags.Msaa4xHint);
            Raylib.InitWindow(WindowWidth, WindowHeight, s_isAdmin ? "xTerminal Installer : Administrator" : "xTerminal Installer");
            Raylib.SetTargetFPS(60);
            LoadInstallerFonts();

            if (!string.IsNullOrEmpty(iconPath))
            {
                Image icon = Raylib.LoadImage(iconPath);
                Raylib.SetWindowIcon(icon);
                Raylib.UnloadImage(icon);
            }

            EnsureTaskbarIcon();

            Texture2D appLogo = LoadTextureIfExists(_latestIconPath);
            if (!IsTextureReady(appLogo))
                appLogo = LoadTextureIfExists(_iconPath);
            if (IsTextureReady(appLogo))
                Raylib.SetTextureFilter(appLogo, TextureFilter.Bilinear);

            while (!Raylib.WindowShouldClose() && !shouldClose)
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(s_background);


                // Keep animation continuous; no periodic background/timer reset.
                timer = (float)Raylib.GetTime();
                DrawInstallerHero(appLogo, timer);
                HandleWindowDrag(ref isDraggingWindow, ref dragOffset);

                var windowAction = DrawWindowControls();
                if (windowAction == WindowAction.Minimize)
                    Raylib.MinimizeWindow();
                else if (windowAction == WindowAction.Close)
                    shouldClose = true;

                var isAlreadyInstalled = s_statusPrint.Contains("installed");
                var isInstalling = isButtonClicked && !isAlreadyInstalled && !s_isCopyingDone;

                if (isInstalling)
                    progress = s_totalBytes > 0 ? Math.Clamp((float)s_copiedBytes / s_totalBytes, 0f, 1f) : 0f;
                else if (s_isCopyingDone && !isAlreadyInstalled)
                    progress = 1f;

                DrawInstallerFooter(GetStatusText(isButtonClicked, isInstalling, s_isCopyingDone, isAlreadyInstalled, timer), progress, isInstalling, s_isCopyingDone, isAlreadyInstalled, timer);

                // Install button action.
                if (InstallButton(s_installButtonBounds, isInstalling ? "INSTALLING" : "INSTALL", !isInstalling))
                {
                    s_statusPrint = "";
                    s_totalBytes = 0;
                    s_copiedBytes = 0;
                    progress = 0.0f;
                    isButtonClicked = true;
                    s_isCopyingDone = false;
                    s_isShortAsked = false;
                    CopyFiles(sourceDir, s_destDirectory);
                    if (!s_statusPrint.Contains("installed"))
                        CopyUninstaller(uninstallerPath, s_profilePath);
                }

                // Start progress bar only if clicked install button.
                if (s_isCopyingDone && !s_statusPrint.Contains("installed") && !s_isShortAsked)
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
                Raylib.EndDrawing();
            }


            if (IsTextureReady(appLogo))
                Raylib.UnloadTexture(appLogo);

            UnloadInstallerFonts();
            Raylib.CloseWindow();
        }

        private static void HandleWindowDrag(ref bool isDraggingWindow, ref Vector2 dragOffset)
        {
            var mouse = Raylib.GetMousePosition();
            var dragBounds = new Rectangle(0, 0, WindowWidth - 106, TitleBarHeight);
            var isOnControls = Raylib.CheckCollisionPointRec(mouse, new Rectangle(WindowWidth - 102, 8, 82, 28));

            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, dragBounds) && !isOnControls)
            {
                isDraggingWindow = true;
                dragOffset = mouse;
            }

            if (Raylib.IsMouseButtonReleased(MouseButton.Left))
                isDraggingWindow = false;

            if (!isDraggingWindow)
                return;

            var windowPosition = Raylib.GetWindowPosition();
            var delta = mouse - dragOffset;
            Raylib.SetWindowPosition((int)(windowPosition.X + delta.X), (int)(windowPosition.Y + delta.Y));
        }

        private static void EnsureTaskbarIcon()
        {
            using var currentProcess = Process.GetCurrentProcess();
            currentProcess.Refresh();
            var windowHandle = currentProcess.MainWindowHandle;
            if (windowHandle == IntPtr.Zero)
                return;

            var exStyle = GetWindowLongPtr(windowHandle, GWL_EXSTYLE).ToInt64();
            exStyle |= WS_EX_APPWINDOW;
            exStyle &= ~WS_EX_TOOLWINDOW;
            SetWindowLongPtr(windowHandle, GWL_EXSTYLE, new IntPtr(exStyle));
            SetWindowPos(windowHandle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }

        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size == 8
                ? GetWindowLongPtr64(hWnd, nIndex)
                : new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            return IntPtr.Size == 8
                ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
                : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        private static Texture2D LoadTextureIfExists(string path)
        {
            var resolvedPath = ResolveResourcePath(path);
            return string.IsNullOrEmpty(resolvedPath) ? default : Raylib.LoadTexture(resolvedPath);
        }

        private static string ResolveResourcePath(string path)
        {
            var fileName = Path.GetFileName(path);
            string[] candidates =
            [
                Path.Combine(AppContext.BaseDirectory, path),
                Path.Combine(AppContext.BaseDirectory, "resources", fileName),
                Path.Combine(AppContext.BaseDirectory, "media", fileName),
                Path.Combine(AppContext.BaseDirectory, fileName)
            ];

            foreach (var candidate in candidates)
            {
                var fullCandidate = Path.GetFullPath(candidate);
                if (IsUnderApplicationDirectory(fullCandidate) && File.Exists(fullCandidate))
                    return fullCandidate;
            }

            return "";
        }

        private static bool IsTextureReady(Texture2D texture)
        {
            return texture.Id != 0 && texture.Width > 0 && texture.Height > 0;
        }

        private static string GetFileVersionLabel(string filePath)
        {
            if (!File.Exists(filePath))
                return "UNKNOWN";

            var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
            if (string.IsNullOrWhiteSpace(versionInfo.FileVersion))
                return "UNKNOWN";

            return Version.TryParse(versionInfo.FileVersion, out var version)
                ? version.ToString()
                : versionInfo.FileVersion;
        }

        /// <summary>
        /// Function for copy files.
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="destDir"></param>
        private static void CopyFiles(string sourceDir, string destDir)
        {
            sourceDir = Path.GetFullPath(sourceDir);
            destDir = EnsureSafeDestinationDirectory(destDir);
            ValidateDirectoryTreeNoReparse(sourceDir);

            Version fileVersion;
            Version destVersion = new Version("1.0");
            var versionCompare = 0;
            fileVersion = GetFileVersion(Path.Combine(sourceDir, "xTerminal.exe"));

            var destFile = $"{s_destDirectory}\\xTerminal.exe";
            if (File.Exists(destFile))
            {
                destVersion = GetFileVersion(destFile);
                versionCompare = fileVersion.CompareTo(destVersion);
            }

            // Kill xTerminal process.
            var processKiller = new ProcessManager();
            if (processKiller.IsProcess("xTerminal"))
            {
                var result = MessageBox(IntPtr.Zero, "xTerminal is running. Do you want to close it?", "xTerminal-Installer", 0x00000004 | 0x00000020);
                if (result != 6)
                    Environment.Exit(0);
                else
                    processKiller.KillProcess("xTerminal");
            }

            // If installed version si higher.
            if (File.Exists(destFile) && versionCompare < 0)
            {
                MessageBox(IntPtr.Zero, $"You already have the newest version for xTerminal!", "xTerminal-Installer", 0x00000000 | 0x00000030);
                s_statusPrint = "xTerminal is already installed!";
                Environment.Exit(0);
            }

            // If same version (already installed).
            if (File.Exists(destFile) && versionCompare == 0)
            {
                s_statusPrint = "xTerminal is already installed!";
                var result = MessageBox(IntPtr.Zero, "xTerminal is already installed. Do you want to repair it?", "xTerminal-Installer", 0x00000004 | 0x00000020);
                if (result != 6)
                    Environment.Exit(0);
                else
                    s_statusPrint = "";
            }

            // If installed version is lower (update).
            if (File.Exists(destFile) && versionCompare > 0)
            {
                var resultUpdate = MessageBox(IntPtr.Zero,
                    $"You current xTerminal version is {destVersion.ToString()}. Do you want to update it at version {fileVersion.ToString()}?", "xTerminal-Installer",
                    0x00000004 | 0x00000020);
                if (resultUpdate != 6)
                    Environment.Exit(0);
                else
                    s_statusPrint = "";
            }

            // Write unsintall registry.
            Reg_Uninstall(sourceDir);

            var files = EnumeratePayloadFiles(sourceDir).ToArray();
            foreach (var file in files)
                s_totalBytes += new FileInfo(file).Length;

            Thread copyThread = new Thread(() =>
            {
                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(sourceDir, file);
                    var targetPath = Path.Combine(destDir, relativePath);
                    EnsureSafeDestinationDirectory(Path.GetDirectoryName(targetPath));
                    RejectReparsePoint(targetPath);
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
            sourceFile = Path.GetFullPath(sourceFile);
            destDir = EnsureSafeDestinationDirectory(destDir);

            var destFile = $@"{destDir}\xUninstaller.exe";
            RejectReparsePoint(destFile);

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
        private static void Reg_Uninstall(string sourceDirectory)
        {
            try
            {
                var pathReg = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\xTerminal";
                var userProfile = $@"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}";
                var date = DateTime.Now.ToString("yyyyMMdd");
                var fileVersion = "1.0";
                fileVersion = FileVersionInfo.GetVersionInfo(Path.Combine(sourceDirectory, "xTerminal.exe")).FileVersion;

                using RegistryKey key = Registry.CurrentUser.CreateSubKey(pathReg);
                key.SetValue("DisplayName", "xTerminal");
                key.SetValue("DisplayVersion", fileVersion);
                key.SetValue("InstallLocation", $@"{userProfile}\AppData\Local\Programs\xTerminal");
                key.SetValue("DisplayIcon", $@"{userProfile}\AppData\Local\Programs\xTerminal\icon.ico");
                key.SetValue("InstallDate", date);
                key.SetValue("UninstallString", $@"{userProfile}\AppData\Local\xTerminal\xUninstaller.exe");
                key.SetValue("Publisher", "x_Coding");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
            }
        }

        /// <summary>
        /// Function for check if user has administrator rights
        /// </summary>
        /// <returns></returns>
        private static bool IsLoggedUserAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// Resolve the payload directory path relative to the application base directory, ensuring it exists and does not contain any reparse points.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        private static string ResolvePayloadDirectory(string relativePath)
        {
            string path = ResolveApplicationPath(relativePath);
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException("Installer payload directory was not found: " + path);
            ValidateDirectoryTreeNoReparse(path);
            return path;
        }

        /// <summary>
        /// Resolve the payload file path relative to the application base directory, ensuring it exists and is not a reparse point.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        private static string ResolvePayloadFile(string relativePath)
        {
            string path = ResolveApplicationPath(relativePath);
            if (!File.Exists(path))
                throw new FileNotFoundException("Installer payload file was not found.", path);
            RejectReparsePoint(path);
            return path;
        }

        /// <summary>
        /// Resolve a path relative to the application base directory, ensuring it does not escape the application directory.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        private static string ResolveApplicationPath(string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
                throw new InvalidDataException("Installer payload paths must be relative.");

            string path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath));
            if (!IsUnderApplicationDirectory(path))
                throw new InvalidDataException("Installer payload path escapes the application directory.");
            return path;
        }


        /// <summary>
        /// Function to check if path is under application directory.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static bool IsUnderApplicationDirectory(string path)
        {
            string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(AppContext.BaseDirectory))
                + Path.DirectorySeparatorChar;
            return path.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Ensure that the destination directory exists and does not contain any reparse points, creating it if necessary.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        private static string EnsureSafeDestinationDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidDataException("Installer destination directory is empty.");

            string fullPath = Path.GetFullPath(path);
            ValidateExistingPathNoReparse(fullPath);
            Directory.CreateDirectory(fullPath);
            ValidateExistingPathNoReparse(fullPath);
            return fullPath;
        }

        /// <summary>
        /// Function to validate existing path and check if it contains reparse point.
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="IOException"></exception>
        private static void ValidateExistingPathNoReparse(string path)
        {
            var current = new DirectoryInfo(path);
            while (current != null)
            {
                if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Installer destination contains a reparse point: " + current.FullName);
                current = current.Parent;
            }
        }

        /// <summary>
        /// validate the entire directory tree for reparse points, throwing an exception if any are found. This is used to ensure that the installer payload does not contain symbolic links or junctions that could lead to unexpected behavior during installation.
        /// </summary>
        /// <param name="root"></param>
        private static void ValidateDirectoryTreeNoReparse(string root)
        {
            foreach (var _ in EnumeratePayloadFiles(root)) { }
        }

        /// <summary>
        /// Enumerate all files in the payload directory tree, rejecting any reparse points to avoid writing through symbolic links or junctions.
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        private static IEnumerable<string> EnumeratePayloadFiles(string root)
        {
            var pending = new Stack<DirectoryInfo>();
            pending.Push(new DirectoryInfo(root));
            while (pending.Count > 0)
            {
                var directory = pending.Pop();
                if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Installer payload contains a reparse point: " + directory.FullName);

                foreach (var entry in directory.EnumerateFileSystemInfos())
                {
                    if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
                        throw new IOException("Installer payload contains a reparse point: " + entry.FullName);
                    if (entry is DirectoryInfo childDirectory)
                        pending.Push(childDirectory);
                    else if (entry is FileInfo file)
                        yield return file.FullName;
                }
            }
        }

        /// <summary>
        /// Reject reparse point to avoid writing through symbolic links or junctions.
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="IOException"></exception>
        private static void RejectReparsePoint(string path)
        {
            if ((File.Exists(path) || Directory.Exists(path))
                && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Refusing to write through a reparse point: " + path);
        }

        /// <summary>
        /// Veriy payload manifest file and check if all files are present and have correct hash.
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="uninstallerPath"></param>
        /// <exception cref="InvalidDataException"></exception>
        private static void VerifyPayloadManifest(string sourceDirectory, string uninstallerPath)
        {
            string manifestPath = ResolveApplicationPath(Path.Combine("data", "payload.sha256"));
            if (!File.Exists(manifestPath))
                throw new InvalidDataException("Installer payload hash manifest is missing: " + manifestPath);

            var expectedHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string rawLine in File.ReadLines(manifestPath))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                int separator = line.IndexOf(' ');
                if (separator != 64)
                    throw new InvalidDataException("Invalid installer payload manifest line.");
                string hash = line.Substring(0, separator);
                string relativePath = line.Substring(separator).TrimStart(' ', '*').Replace('/', Path.DirectorySeparatorChar);
                string fullPath = ResolveApplicationPath(relativePath);
                if (!expectedHashes.TryAdd(fullPath, hash))
                    throw new InvalidDataException("Duplicate installer payload manifest entry: " + relativePath);
            }

            var payloadFiles = EnumeratePayloadFiles(sourceDirectory).ToList();
            payloadFiles.Add(uninstallerPath);
            foreach (string file in payloadFiles)
            {
                if (!expectedHashes.TryGetValue(Path.GetFullPath(file), out string expected))
                    throw new InvalidDataException("Installer payload is missing from the hash manifest: " + file);

                using var stream = File.OpenRead(file);
                string actual = Convert.ToHexString(SHA256.HashData(stream));
                if (!CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(expected),
                    Convert.FromHexString(actual)))
                    throw new InvalidDataException("Installer payload hash mismatch: " + file);
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
            var finalPath = inStartMenu ? Path.Combine(startMenuPath, "Programs", $"{fileWithoutExtPath}.lnk") : Path.Combine(desktopFolder, $"{fileWithoutExtPath}.lnk");
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
