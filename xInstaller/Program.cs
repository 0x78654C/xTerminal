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
    internal class Program
    {
        static long s_totalBytes = 0;
        static long s_copiedBytes = 0;
        static bool s_isCopyingDone = false;
        static string s_statusPrint = "";
        static bool s_isShortAsked = false;
        static string s_xTerminalVersion = "UNKNOWN";
        const string _bgPath1 = @"resources\banner.png";
        const string _bgPath2 = @"resources\bg1.png";
        const string _bgPath3 = @"resources\bg2.png";
        const string _bgPath4 = @"resources\bg3.png";
        const string _iconPath = @"resources\xTerminal.png";
        const string _latestIconPath = @"media\xterminal_logo.png";
        const string _sourceDirX64 = @"data\x64\";
        const string _sourceDirX86 = @"data\x86\";
        const string _uninstaller = @"data\Uninstaller\xUninstaller.exe";
        const int WindowWidth = 800;
        const int WindowHeight = 480;
        const int HeroHeight = 368;
        static readonly Rectangle s_installButtonBounds = new Rectangle(620, 402, 152, 48);
        static readonly Color s_background = new Color(2, 3, 8, 255);
        static readonly Color s_panel = new Color(9, 15, 24, 252);
        static readonly Color s_panelDeep = new Color(5, 8, 14, 255);
        static readonly Color s_panelLine = new Color(72, 218, 232, 150);
        static readonly Color s_textPrimary = new Color(235, 241, 231, 255);
        static readonly Color s_textSecondary = new Color(177, 195, 201, 255);
        static readonly Color s_accent = new Color(35, 219, 230, 255);
        static readonly Color s_accentHover = new Color(247, 211, 76, 255);
        static readonly Color s_warning = new Color(236, 82, 74, 255);
        static readonly Color s_success = new Color(107, 229, 129, 255);
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
            var currentBackground = 0;
            var switchTime = 5.0f;
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
            Raylib.SetConfigFlags(ConfigFlags.UndecoratedWindow);
            Raylib.InitWindow(WindowWidth, WindowHeight, s_isAdmin ? "xTerminal Installer : Administrator" : "xTerminal Installer");
            Raylib.SetTargetFPS(60);

            if (!string.IsNullOrEmpty(iconPath))
            {
                Image icon = Raylib.LoadImage(iconPath);
                Raylib.SetWindowIcon(icon);
                Raylib.UnloadImage(icon);
            }

            EnsureTaskbarIcon();

            Texture2D[] backgrounds =
            [
                LoadTextureIfExists(_bgPath1),
                LoadTextureIfExists(_bgPath2),
                LoadTextureIfExists(_bgPath3),
                LoadTextureIfExists(_bgPath4)
            ];

            Texture2D appLogo = LoadTextureIfExists(_latestIconPath);
            if (!IsTextureReady(appLogo))
                appLogo = LoadTextureIfExists(_iconPath);

            while (!Raylib.WindowShouldClose() && !shouldClose)
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(s_background);


                // Set multiple backgrounds
                float delta = Raylib.GetFrameTime();
                timer += delta;

                if (timer >= switchTime)
                {
                    timer = 0;
                    currentBackground = (currentBackground + 1) % backgrounds.Length;
                }


                Texture2D bg = backgrounds[currentBackground];
                DrawInstallerHero(bg, appLogo, timer);
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
                if (InstallButton(s_installButtonBounds, isInstalling ? "DEPLOYING" : "DEPLOY", !isInstalling))
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


            // Unload background
            foreach (var bg in backgrounds)
                if (IsTextureReady(bg))
                    Raylib.UnloadTexture(bg);

            if (IsTextureReady(appLogo))
                Raylib.UnloadTexture(appLogo);

            Raylib.CloseWindow();
        }

        /// <summary>
        /// Install button declaration.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="text"></param>
        /// <param name="enabled"></param>
        /// <returns></returns>
        private static bool InstallButton(Rectangle bounds, string text, bool enabled)
        {
            var mouse = Raylib.GetMousePosition();
            var isHover = enabled && Raylib.CheckCollisionPointRec(mouse, bounds);
            var isPressed = isHover && Raylib.IsMouseButtonDown(MouseButton.Left);
            var fill = enabled
                ? isPressed ? new Color(145, 82, 22, 255) : isHover ? new Color(70, 50, 16, 255) : new Color(10, 38, 48, 255)
                : new Color(42, 45, 52, 255);
            var edge = enabled
                ? isHover ? s_accentHover : s_accent
                : new Color(106, 116, 122, 255);

            DrawAngledPanel(new Rectangle(bounds.X + 4, bounds.Y + 5, bounds.Width, bounds.Height), 12, new Color(0, 0, 0, 120), new Color(0, 0, 0, 0), 0);
            DrawAngledPanel(bounds, 12, fill, edge, 2.5f);
            Raylib.DrawLineEx(new Vector2(bounds.X + 18, bounds.Y + 8), new Vector2(bounds.X + bounds.Width - 24, bounds.Y + 8), 1.0f, new Color((int)edge.R, (int)edge.G, (int)edge.B, 110));
            Raylib.DrawLineEx(new Vector2(bounds.X + 24, bounds.Y + bounds.Height - 8), new Vector2(bounds.X + bounds.Width - 18, bounds.Y + bounds.Height - 8), 1.0f, new Color((int)edge.R, (int)edge.G, (int)edge.B, 90));
            DrawTextCentered(text, bounds, 17, enabled ? Color.White : new Color(190, 198, 199, 255));

            return isHover && Raylib.IsMouseButtonPressed(MouseButton.Left);
        }

        private static void DrawInstallerHero(Texture2D background, Texture2D appLogo, float timer)
        {
            var heroBounds = new Rectangle(0, 0, WindowWidth, HeroHeight);
            DrawTextureCover(background, heroBounds, new Color(115, 135, 150, 90));
            DrawHeroBackdrop(IsTextureReady(background));
            DrawHeroPattern(timer);
            DrawCockpitFrame();
            DrawBrandLockup(appLogo, timer);
            DrawTerminalPreview();
        }

        private static void DrawHeroBackdrop(bool hasImage)
        {
            for (var y = 0; y < HeroHeight; y += 3)
            {
                var t = y / (float)HeroHeight;
                var color = new Color(
                    (int)(2 + 10 * t),
                    (int)(4 + 8 * t),
                    (int)(10 + 18 * t),
                    hasImage ? 232 : 255);
                Raylib.DrawRectangle(0, y, WindowWidth, 3, color);
            }

            Raylib.DrawRectangle(0, 0, WindowWidth, HeroHeight, new Color(0, 0, 0, hasImage ? 116 : 22));
            Raylib.DrawCircleGradient(620, 116, 220, new Color(88, 24, 20, 76), new Color(0, 0, 0, 0));
            Raylib.DrawCircleGradient(206, 172, 190, new Color(28, 132, 150, 42), new Color(0, 0, 0, 0));
            Raylib.DrawRectangle(0, HeroHeight - 78, WindowWidth, 78, new Color(0, 0, 0, 156));
        }

        private static void DrawHeroPattern(float timer)
        {
            DrawStarfield(timer);
            DrawHyperspaceLines(timer);

            var scan = (timer * 42f) % 54f;
            for (var y = -54f + scan; y < HeroHeight; y += 54f)
                Raylib.DrawLineEx(new Vector2(0, y), new Vector2(WindowWidth, y + 20), 1.0f, new Color(62, 218, 235, 14));

            for (var x = 24; x < WindowWidth; x += 58)
            {
                var pulse = (int)(14 + (MathF.Sin(timer * 2.2f + x * 0.03f) + 1f) * 16f);
                Raylib.DrawLine(x, 0, x - 76, HeroHeight, new Color(62, 218, 235, pulse));
            }

            Raylib.DrawRectangle(0, HeroHeight - 1, WindowWidth, 1, new Color(247, 211, 76, 130));
        }

        private static void DrawStarfield(float timer)
        {
            for (var i = 0; i < 120; i++)
            {
                var x = (i * 73 + 19) % WindowWidth;
                var baseY = (i * 151 + 37) % HeroHeight;
                var y = (baseY + timer * (10f + i % 9)) % HeroHeight;
                var alpha = 56 + (i % 4) * 24;
                var color = i % 11 == 0 ? new Color(247, 211, 76, alpha) : new Color(220, 237, 240, alpha);

                if (i % 7 == 0)
                    Raylib.DrawLineEx(new Vector2(x, y), new Vector2(x - 3, y + 8), 1.2f, color);
                else
                    Raylib.DrawPixel(x, (int)y, color);
            }
        }

        private static void DrawHyperspaceLines(float timer)
        {
            var center = new Vector2(404, 152);
            var pulse = MathF.Sin(timer * 2.4f) * 0.5f + 0.5f;

            for (var i = 0; i < 28; i++)
            {
                var angle = i * (MathF.PI * 2f / 28f) + timer * 0.06f;
                var length = 84f + (i % 5) * 24f + pulse * 36f;
                var start = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 32f;
                var end = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * length;
                var color = i % 4 == 0 ? new Color(247, 211, 76, 24) : new Color(62, 218, 235, 22);
                Raylib.DrawLineEx(start, end, 1.1f, color);
            }
        }

        private static void DrawCockpitFrame()
        {
            DrawAngledPanel(new Rectangle(0, 0, WindowWidth, 34), 0, new Color(0, 0, 0, 186), new Color(42, 48, 58, 255), 1.0f);
            DrawAngledPanel(new Rectangle(0, HeroHeight - 28, WindowWidth, 28), 0, new Color(0, 0, 0, 190), new Color(247, 211, 76, 98), 1.0f);

            Raylib.DrawLineEx(new Vector2(0, 34), new Vector2(92, HeroHeight - 28), 7f, new Color(0, 0, 0, 152));
            Raylib.DrawLineEx(new Vector2(WindowWidth, 34), new Vector2(WindowWidth - 92, HeroHeight - 28), 7f, new Color(0, 0, 0, 152));
            Raylib.DrawLineEx(new Vector2(0, 34), new Vector2(92, HeroHeight - 28), 1.3f, new Color(62, 218, 235, 86));
            Raylib.DrawLineEx(new Vector2(WindowWidth, 34), new Vector2(WindowWidth - 92, HeroHeight - 28), 1.3f, new Color(62, 218, 235, 86));

            Raylib.DrawText("xINSTALLER // COMMAND DECK", 24, 11, 12, new Color(247, 211, 76, 235));
            Raylib.DrawText(DateTime.Now.ToString("yyyy.MM.dd HH:mm"), 600, 11, 12, new Color(156, 177, 184, 220));
        }

        private static void HandleWindowDrag(ref bool isDraggingWindow, ref Vector2 dragOffset)
        {
            var mouse = Raylib.GetMousePosition();
            var dragBounds = new Rectangle(0, 0, 704, 34);
            var isOnControls = Raylib.CheckCollisionPointRec(mouse, new Rectangle(710, 5, 68, 24));

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

        private static WindowAction DrawWindowControls()
        {
            var minimize = new Rectangle(712, 6, 28, 22);
            var close = new Rectangle(744, 6, 28, 22);

            var action = WindowAction.None;
            if (DrawChromeButton(minimize, WindowAction.Minimize))
                action = WindowAction.Minimize;
            if (DrawChromeButton(close, WindowAction.Close))
                action = WindowAction.Close;

            return action;
        }

        private static bool DrawChromeButton(Rectangle bounds, WindowAction action)
        {
            var mouse = Raylib.GetMousePosition();
            var isHover = Raylib.CheckCollisionPointRec(mouse, bounds);
            var isPressed = isHover && Raylib.IsMouseButtonDown(MouseButton.Left);
            var accent = action == WindowAction.Close ? s_warning : s_accent;
            var fill = isPressed
                ? new Color(68, 34, 28, 255)
                : isHover
                    ? new Color(25, 43, 52, 255)
                    : new Color(8, 16, 25, 245);

            DrawAngledPanel(bounds, 6, fill, isHover ? accent : new Color(62, 218, 235, 82), 1.1f);

            if (action == WindowAction.Minimize)
            {
                Raylib.DrawLineEx(
                    new Vector2(bounds.X + 8, bounds.Y + 14),
                    new Vector2(bounds.X + bounds.Width - 8, bounds.Y + 14),
                    2.0f,
                    isHover ? s_accentHover : s_textSecondary);
            }
            else
            {
                Raylib.DrawLineEx(
                    new Vector2(bounds.X + 9, bounds.Y + 7),
                    new Vector2(bounds.X + bounds.Width - 9, bounds.Y + bounds.Height - 7),
                    2.0f,
                    isHover ? s_warning : s_textSecondary);
                Raylib.DrawLineEx(
                    new Vector2(bounds.X + bounds.Width - 9, bounds.Y + 7),
                    new Vector2(bounds.X + 9, bounds.Y + bounds.Height - 7),
                    2.0f,
                    isHover ? s_warning : s_textSecondary);
            }

            return isHover && Raylib.IsMouseButtonPressed(MouseButton.Left);
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

        private static void DrawBrandLockup(Texture2D appLogo, float timer)
        {
            var panel = new Rectangle(34, 56, 732, 118);
            DrawAngledPanel(new Rectangle(panel.X + 8, panel.Y + 9, panel.Width, panel.Height), 18, new Color(0, 0, 0, 146), new Color(0, 0, 0, 0), 0);
            DrawAngledPanel(panel, 18, s_panel, new Color(62, 218, 235, 132), 1.5f);
            Raylib.DrawRectangle((int)panel.X + 18, (int)panel.Y + 16, (int)panel.Width - 38, 1, new Color(247, 211, 76, 92));

            DrawIconPanel(new Rectangle(58, 82, 60, 60), appLogo);

            Raylib.DrawText("xTerminal", 142, 78, 34, s_textPrimary);
            Raylib.DrawText("A MODERN SHELL FOR WINDOWS", 146, 116, 15, new Color(247, 211, 76, 245));
            Raylib.DrawText("Local shell installer.", 146, 141, 13, s_textSecondary);

            DrawInfoCard(398, 84, 104, "VERSION", s_xTerminalVersion, s_accentHover);
            DrawInfoCard(514, 84, 96, "ARCH", Environment.Is64BitOperatingSystem ? "x64" : "x86", s_accent);
            DrawInfoCard(622, 84, 104, "SESSION", s_isAdmin ? "ADMIN" : "USER", s_accentHover);
            DrawTechReadout(new Rectangle(516, 144, 210, 10), 0.64f + MathF.Sin(timer * 1.8f) * 0.08f, s_accent);
        }

        private static void DrawTerminalPreview()
        {
            var panel = new Rectangle(34, 184, 732, 166);
            DrawAngledPanel(new Rectangle(panel.X + 8, panel.Y + 10, panel.Width, panel.Height), 18, new Color(0, 0, 0, 148), new Color(0, 0, 0, 0), 0);
            DrawAngledPanel(panel, 18, s_panel, s_panelLine, 1.5f);

            Raylib.DrawText("INSTALL VECTOR", (int)panel.X + 22, (int)panel.Y + 18, 14, s_accentHover);
            Raylib.DrawText("LOCAL NODE // xTerminal", (int)panel.X + 556, (int)panel.Y + 19, 11, s_textSecondary);
            Raylib.DrawLineEx(new Vector2(panel.X + 20, panel.Y + 44), new Vector2(panel.X + panel.Width - 22, panel.Y + 44), 1.2f, new Color(62, 218, 235, 90));

            var rowX = (int)panel.X + 28;
            var rowY = (int)panel.Y + 54;
            var rowWidth = (int)panel.Width - 56;
            DrawSystemMapRow(rowX, rowY, rowWidth, "SOURCE PACKAGE", Environment.Is64BitOperatingSystem ? @"data\x64\xTerminal.exe" : @"data\x86\xTerminal.exe", s_accent);
            DrawSystemMapRow(rowX, rowY + 28, rowWidth, "INSTALL TARGET", TrimMiddle(s_destDirectory, 58), s_textPrimary);
            DrawSystemMapRow(rowX, rowY + 56, rowWidth, "USER PROFILE", TrimMiddle(s_profilePath, 58), s_textPrimary);
            DrawSystemMapRow(rowX, rowY + 84, rowWidth, "SYSTEM ENTRIES", @"HKCU uninstall registry + shortcut prompts", s_textPrimary);
        }

        private static void DrawTerminalLine(int x, int y, string text, Color color)
        {
            Raylib.DrawRectangle(x - 12, y + 5, 5, 5, color);
            Raylib.DrawText(text, x, y, 14, color);
            Raylib.DrawRectangle(x, y + 20, Math.Min(Raylib.MeasureText(text, 14), 158), 1, new Color((int)color.R, (int)color.G, (int)color.B, 44));
        }

        private static void DrawIconPanel(Rectangle bounds, Texture2D appLogo)
        {
            DrawAngledPanel(bounds, 12, new Color(4, 20, 29, 255), s_accent, 1.7f);
            Raylib.DrawLineEx(new Vector2(bounds.X + 10, bounds.Y + 12), new Vector2(bounds.X + bounds.Width - 10, bounds.Y + 12), 1.2f, new Color(247, 211, 76, 140));
            Raylib.DrawLineEx(new Vector2(bounds.X + 10, bounds.Y + bounds.Height - 12), new Vector2(bounds.X + bounds.Width - 10, bounds.Y + bounds.Height - 12), 1.2f, new Color(62, 218, 235, 120));

            if (IsTextureReady(appLogo))
            {
                DrawTextureContain(appLogo, new Rectangle(bounds.X + 10, bounds.Y + 10, bounds.Width - 20, bounds.Height - 20), new Color(235, 250, 250, 255));
                return;
            }

            var center = new Vector2(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
            Raylib.DrawLineEx(new Vector2(center.X - 16, center.Y - 14), new Vector2(center.X + 2, center.Y + 2), 4.0f, s_accent);
            Raylib.DrawLineEx(new Vector2(center.X + 2, center.Y + 2), new Vector2(center.X - 16, center.Y + 18), 4.0f, s_accent);
            Raylib.DrawLineEx(new Vector2(center.X + 10, center.Y + 16), new Vector2(center.X + 24, center.Y + 16), 4.0f, s_accentHover);
            Raylib.DrawCircle((int)(bounds.X + bounds.Width - 10), (int)(bounds.Y + 10), 3, s_accentHover);
        }

        private static void DrawInfoCard(int x, int y, int width, string label, string value, Color accent)
        {
            var bounds = new Rectangle(x, y, width, 46);
            Raylib.DrawRectangleRec(bounds, new Color(13, 23, 33, 238));
            Raylib.DrawRectangleLinesEx(bounds, 1, new Color((int)accent.R, (int)accent.G, (int)accent.B, 100));
            Raylib.DrawText(label, x + 10, y + 8, 10, s_textSecondary);
            Raylib.DrawText(value, x + 10, y + 23, 15, accent);
        }

        private static void DrawSystemMapRow(int x, int y, int width, string label, string value, Color valueColor)
        {
            var bounds = new Rectangle(x, y, width, 25);
            Raylib.DrawRectangleRec(bounds, new Color(13, 23, 33, 230));
            Raylib.DrawRectangleLinesEx(bounds, 1, new Color(62, 218, 235, 66));
            Raylib.DrawRectangle(x + 8, y + 8, 8, 8, valueColor);
            Raylib.DrawText(label, x + 24, y + 7, 11, s_accentHover);
            Raylib.DrawText(value, x + 170, y + 6, 12, valueColor);
        }

        private static float DrawPill(float x, float y, string text)
        {
            var fontSize = 12;
            var width = Raylib.MeasureText(text, fontSize) + 20;
            var bounds = new Rectangle(x, y, width, 24);
            DrawAngledPanel(bounds, 7, new Color(5, 23, 31, 220), new Color(62, 218, 235, 116), 1.0f);
            Raylib.DrawText(text, (int)x + 10, (int)y + 7, fontSize, new Color(220, 239, 240, 255));
            return width;
        }

        private static void DrawInstallerFooter(string statusText, float progress, bool isInstalling, bool isDone, bool isAlreadyInstalled, float timer)
        {
            Raylib.DrawRectangle(0, HeroHeight, WindowWidth, WindowHeight - HeroHeight, new Color(2, 4, 8, 255));
            Raylib.DrawRectangle(0, HeroHeight, WindowWidth, 1, new Color(247, 211, 76, 116));

            var footerBorder = isAlreadyInstalled
                ? new Color(236, 82, 74, 92)
                : isDone
                    ? new Color(107, 229, 129, (int)(62 + 44 * MathF.Abs(MathF.Sin(timer * 2.2f))))
                    : new Color(62, 218, 235, 92);
            DrawAngledPanel(new Rectangle(22, 386, 582, 70), 14, s_panelDeep, footerBorder, 1.2f);
            Raylib.DrawText(statusText.ToUpperInvariant(), 42, 399, 17, isAlreadyInstalled ? s_warning : isDone ? s_success : s_textPrimary);

            var detail = isAlreadyInstalled
                ? "Repair prompt available when deployment is re-engaged."
                : isDone
                    ? "Installed to " + TrimMiddle(s_destDirectory, 54)
                    : "Target " + TrimMiddle(s_destDirectory, 58);
            Raylib.DrawText(detail, 42, 436, 12, s_textSecondary);

            DrawProgressBar(new Rectangle(42, 424, 520, 9), progress, isInstalling, isDone, isAlreadyInstalled, timer);

            if (isInstalling || isDone)
            {
                var pct = isDone ? 100 : Math.Clamp((int)(progress * 100), 0, 100);
                Raylib.DrawText($"{pct}%", 566, 421, 13, isDone ? s_success : s_accentHover);
            }
        }

        private static void DrawProgressBar(Rectangle bounds, float progress, bool isInstalling, bool isDone, bool isAlreadyInstalled, float timer)
        {
            Raylib.DrawRectangleRec(bounds, new Color(18, 25, 31, 255));
            Raylib.DrawRectangleLinesEx(bounds, 1, new Color(62, 218, 235, 95));

            var fillProgress = isAlreadyInstalled ? 0f : isDone ? 1f : Math.Clamp(progress, 0f, 1f);
            var segments = 28;
            var gap = 3f;
            var segmentWidth = (bounds.Width - gap * (segments - 1)) / segments;
            var filledSegments = (int)MathF.Round(segments * fillProgress);
            var fillColor = isDone ? s_success : s_accent;

            for (var i = 0; i < segments; i++)
            {
                var x = bounds.X + i * (segmentWidth + gap);
                var segment = new Rectangle(x, bounds.Y, segmentWidth, bounds.Height);
                var active = i < filledSegments;
                var color = active ? fillColor : new Color(45, 58, 66, 255);

                if (isInstalling && active && i == (int)(timer * 14f) % Math.Max(filledSegments, 1))
                    color = s_accentHover;

                Raylib.DrawRectangleRec(segment, color);
            }
        }

        private static string GetStatusText(bool wasInstallClicked, bool isInstalling, bool isDone, bool isAlreadyInstalled, float timer)
        {
            if (isAlreadyInstalled && wasInstallClicked)
                return "xTerminal is already installed";
            if (isDone)
                return "Deployment complete";
            if (isInstalling)
            {
                var dotCount = (int)(timer * 3f) % 4;
                return "Transferring xTerminal payload" + new string('.', dotCount);
            }
            return "Ready to deploy xTerminal";
        }

        private static void DrawTechReadout(Rectangle bounds, float fill, Color color)
        {
            Raylib.DrawRectangleRec(bounds, new Color(22, 31, 38, 255));
            Raylib.DrawRectangleLinesEx(bounds, 1, new Color((int)color.R, (int)color.G, (int)color.B, 90));
            Raylib.DrawRectangleRec(new Rectangle(bounds.X + 2, bounds.Y + 2, (bounds.Width - 4) * Math.Clamp(fill, 0f, 1f), bounds.Height - 4), color);

            for (var x = bounds.X + 16; x < bounds.X + bounds.Width; x += 16)
                Raylib.DrawLine((int)x, (int)bounds.Y, (int)x, (int)(bounds.Y + bounds.Height), new Color(0, 0, 0, 90));
        }

        private static void DrawAngledPanel(Rectangle bounds, float cut, Color fill, Color border, float borderWidth)
        {
            Vector2[] points =
            [
                new Vector2(bounds.X + cut, bounds.Y),
                new Vector2(bounds.X + bounds.Width, bounds.Y),
                new Vector2(bounds.X + bounds.Width, bounds.Y + bounds.Height - cut),
                new Vector2(bounds.X + bounds.Width - cut, bounds.Y + bounds.Height),
                new Vector2(bounds.X, bounds.Y + bounds.Height),
                new Vector2(bounds.X, bounds.Y + cut)
            ];

            Raylib.DrawTriangle(points[0], points[1], points[5], fill);
            Raylib.DrawTriangle(points[1], points[2], points[5], fill);
            Raylib.DrawTriangle(points[2], points[4], points[5], fill);
            Raylib.DrawTriangle(points[2], points[3], points[4], fill);

            if (borderWidth <= 0)
                return;

            for (var i = 0; i < points.Length; i++)
            {
                var next = points[(i + 1) % points.Length];
                Raylib.DrawLineEx(points[i], next, borderWidth, border);
            }
        }

        private static void DrawTextCentered(string text, Rectangle bounds, int fontSize, Color color)
        {
            var textWidth = Raylib.MeasureText(text, fontSize);
            var x = (int)(bounds.X + (bounds.Width - textWidth) / 2);
            var y = (int)(bounds.Y + (bounds.Height - fontSize) / 2) - 1;
            Raylib.DrawText(text, x, y, fontSize, color);
        }

        private static void DrawTextureCover(Texture2D texture, Rectangle destination, Color tint)
        {
            if (!IsTextureReady(texture))
                return;

            var scale = MathF.Max(destination.Width / texture.Width, destination.Height / texture.Height);
            var sourceWidth = destination.Width / scale;
            var sourceHeight = destination.Height / scale;
            var source = new Rectangle(
                (texture.Width - sourceWidth) / 2f,
                (texture.Height - sourceHeight) / 2f,
                sourceWidth,
                sourceHeight);

            Raylib.DrawTexturePro(texture, source, destination, Vector2.Zero, 0.0f, tint);
        }

        private static void DrawTextureContain(Texture2D texture, Rectangle destination, Color tint)
        {
            if (!IsTextureReady(texture))
                return;

            var scale = MathF.Min(destination.Width / texture.Width, destination.Height / texture.Height);
            var width = texture.Width * scale;
            var height = texture.Height * scale;
            var target = new Rectangle(
                destination.X + (destination.Width - width) / 2f,
                destination.Y + (destination.Height - height) / 2f,
                width,
                height);

            Raylib.DrawTexturePro(
                texture,
                new Rectangle(0, 0, texture.Width, texture.Height),
                target,
                Vector2.Zero,
                0.0f,
                tint);
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

        private static string TrimMiddle(string text, int maxLength)
        {
            if (text.Length <= maxLength)
                return text;

            var sideLength = (maxLength - 3) / 2;
            return text[..sideLength] + "..." + text[^sideLength..];
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
