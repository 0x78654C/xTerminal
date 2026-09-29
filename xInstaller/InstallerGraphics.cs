using System.Numerics;
using Raylib_cs;

namespace xInstaller
{
    internal partial class Program
    {
        private const int WindowWidth = 960;
        private const int WindowHeight = 600;
        private const int TitleBarHeight = 44;
        private const int HeroHeight = 480;
        private static readonly Rectangle s_installButtonBounds = new(732, 518, 188, 48);
        private static readonly Color s_background = new(6, 10, 18, 255);
        private static readonly Color s_panel = new(11, 20, 32, 245);
        private static readonly Color s_panelLine = new(41, 70, 88, 255);
        private static readonly Color s_textPrimary = new(230, 243, 249, 255);
        private static readonly Color s_textSecondary = new(144, 169, 186, 255);
        private static readonly Color s_accent = new(79, 225, 242, 255);
        private static readonly Color s_accentHover = new(157, 246, 255, 255);
        private static readonly Color s_magenta = new(212, 93, 210, 255);
        private static readonly Color s_warning = new(255, 178, 107, 255);
        private static readonly Color s_success = new(119, 232, 183, 255);
        private static Font s_interfaceFont;
        private static Font s_monoFont;

        private static void LoadInstallerFonts()
        {
            var fontDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            s_interfaceFont = LoadInstallerFont(Path.Combine(fontDirectory, "segoeui.ttf"));
            s_monoFont = LoadInstallerFont(Path.Combine(fontDirectory, "consola.ttf"));
        }

        private static Font LoadInstallerFont(string path)
        {
            var font = File.Exists(path) ? Raylib.LoadFontEx(path, 64, null, 0) : Raylib.GetFontDefault();
            if (font.Texture.Id == 0)
                return Raylib.GetFontDefault();

            Raylib.SetTextureFilter(font.Texture, TextureFilter.Bilinear);
            return font;
        }

        private static void UnloadInstallerFonts()
        {
            var defaultId = Raylib.GetFontDefault().Texture.Id;
            if (s_interfaceFont.Texture.Id != 0 && s_interfaceFont.Texture.Id != defaultId)
                Raylib.UnloadFont(s_interfaceFont);
            if (s_monoFont.Texture.Id != 0 && s_monoFont.Texture.Id != defaultId)
                Raylib.UnloadFont(s_monoFont);
        }

        private static Color WithAlpha(Color color, int alpha) => new(color.R, color.G, color.B, (byte)Math.Clamp(alpha, 0, 255));

        private static void Text(string text, float x, float y, float size, Color color, bool mono = false)
        {
            Raylib.DrawTextEx(mono ? s_monoFont : s_interfaceFont, text, new Vector2(x, y), size, mono ? 0.6f : 0, color);
        }

        private static float TextWidth(string text, float size, bool mono = false)
        {
            return Raylib.MeasureTextEx(mono ? s_monoFont : s_interfaceFont, text, size, mono ? 0.6f : 0).X;
        }

        // Fit by rendered width so long versions and installation paths stay inside their panels.
        private static string FitText(string text, float width, float size, bool mono = false)
        {
            if (TextWidth(text, size, mono) <= width)
                return text;

            for (var length = text.Length - 1; length > 0; length--)
            {
                var left = (length + 1) / 2;
                var right = length / 2;
                var candidate = text[..left] + "..." + (right > 0 ? text[^right..] : "");
                if (TextWidth(candidate, size, mono) <= width)
                    return candidate;
            }
            return "...";
        }

        private static void DrawInstallerHero(Texture2D appLogo, float timer)
        {
            DrawBackdrop(timer);
            DrawTitleBar(appLogo);

            Text("WINDOWS SHELL / SETUP", 40, 74, 12, s_accent, true);
            Text("xTerminal", 37, 94, 54, s_textPrimary);
            Text("Your command center. Ready for launch.", 40, 158, 20, s_textSecondary);

            DrawMetadata(40, "VERSION", s_xTerminalVersion, 152);
            DrawMetadata(220, "PLATFORM", Environment.Is64BitOperatingSystem ? "Windows / x64" : "Windows / x86", 152);
            DrawMetadata(400, "SESSION", s_isAdmin ? "Administrator" : "Current user", 152);

            DrawDestinationPanel();
            DrawTerminalCore(timer);

            Raylib.DrawCircle(45, 457, 3, s_accent);
            Text("LOCAL INSTALLATION", 57, 450, 11, s_textSecondary, true);
            Raylib.DrawLine(206, 457, 355, 457, s_panelLine);
            Text("SHORTCUT OPTIONS AFTER SETUP", 370, 450, 11, s_textSecondary, true);
        }

        private static void DrawBackdrop(float timer)
        {
            Raylib.DrawRectangleGradientV(0, TitleBarHeight, WindowWidth, HeroHeight - TitleBarHeight,
                new Color(9, 17, 29, 255), s_background);
            Raylib.BeginScissorMode(582, TitleBarHeight, WindowWidth - 582, HeroHeight - TitleBarHeight);
            Raylib.DrawCircleGradient(750, 225, 250, new Color(24, 108, 140, 35), Color.Blank);
            Raylib.DrawCircleGradient(912, 410, 170, new Color(106, 30, 115, 22), Color.Blank);

            for (var x = 600; x < WindowWidth; x += 24)
                Raylib.DrawLine(x, TitleBarHeight, x, HeroHeight, WithAlpha(s_accent, 6));
            for (var y = 58; y < HeroHeight; y += 24)
                Raylib.DrawLine(600, y, WindowWidth, y, WithAlpha(s_accent, 6));

            for (var i = 0; i < 48; i++)
            {
                var x = 595 + (i * 71 % 355);
                var y = 58 + (i * 113 % 390);
                var alpha = (int)(35 + 25 * MathF.Sin(timer * 0.6f + i));
                Raylib.DrawPixel(x, y, WithAlpha(s_accent, alpha));
            }

            // A quiet perspective grid anchors the hologram without running through the copy.
            for (var i = -5; i <= 5; i++)
                Raylib.DrawLineEx(new Vector2(750 + i * 13, 338), new Vector2(750 + i * 82, HeroHeight), 1, WithAlpha(s_accent, 15));
            for (var i = 0; i < 6; i++)
            {
                var y = 345 + i * i * 5;
                Raylib.DrawLine(610, y, WindowWidth, y, WithAlpha(s_accent, 14));
            }
            Raylib.EndScissorMode();
            Raylib.DrawRectangleLinesEx(new Rectangle(0.5f, 0.5f, WindowWidth - 1, WindowHeight - 1), 1, s_panelLine);
        }

        private static void DrawTitleBar(Texture2D appLogo)
        {
            Raylib.DrawRectangle(1, 1, WindowWidth - 2, TitleBarHeight - 1, new Color(8, 14, 23, 255));
            Raylib.DrawLine(1, TitleBarHeight, WindowWidth - 1, TitleBarHeight, s_panelLine);
            Raylib.DrawRectangle(20, TitleBarHeight, 62, 2, s_accent);
            if (IsTextureReady(appLogo))
            {
                Raylib.DrawTexturePro(appLogo, new Rectangle(0, 0, appLogo.Width, appLogo.Height),
                    new Rectangle(20, 10, 24, 24), Vector2.Zero, 0, Color.White);
            }
            else
            {
                Text(">_", 20, 12, 20, s_accent, true);
            }
            Text("xTerminal", 54, 12, 17, s_textPrimary);
            Text("/ INSTALLER", 137, 16, 11, s_textSecondary, true);
            Text("WINDOWS / LOCAL SETUP", WindowWidth - 280, 16, 11, s_textSecondary, true);
        }

        private static void DrawMetadata(int x, string label, string value, int width)
        {
            Raylib.DrawLine(x, 207, x + width, 207, s_panelLine);
            Text(label, x, 220, 11, s_textSecondary, true);
            Text(FitText(value, width, 18), x, 237, 18, s_textPrimary);
        }

        private static void DrawDestinationPanel()
        {
            var panel = new Rectangle(40, 282, 512, 146);
            DrawAngledPanel(panel, 14, s_panel, s_panelLine, 1);
            Raylib.DrawRectangle(40, 301, 2, 24, s_accent);
            Text("INSTALLATION DIRECTORY", 60, 299, 11, s_accent, true);
            Text(FitText(s_destDirectory, 470, 14, true), 60, 322, 14, s_textPrimary, true);
            Raylib.DrawLine(60, 351, 532, 351, s_panelLine);
            Text("USER PROFILE", 60, 366, 11, s_textSecondary, true);
            Text(FitText(s_profilePath, 470, 14, true), 60, 389, 14, s_textPrimary, true);
        }

        private static void DrawTerminalCore(float timer)
        {
            var center = new Vector2(750, 231);
            DrawCornerBrackets(new Rectangle(598, 81, 304, 304), 16, WithAlpha(s_accent, 80));
            Text("TERMINAL / CORE", 617, 90, 11, s_accent, true);
            Text("01", 864, 90, 11, s_textSecondary, true);

            Raylib.DrawCircleGradient((int)center.X, (int)center.Y, 128, WithAlpha(s_accent, 17), Color.Blank);
            DrawOrbit(center, 123, 0, 360, 1, WithAlpha(s_accent, 35));
            DrawOrbit(center, 112, timer * 8, 108, 2, WithAlpha(s_accent, 190));
            DrawOrbit(center, 112, timer * 8 + 128, 48, 2, WithAlpha(s_accent, 70));
            DrawOrbit(center, 112, timer * 8 + 196, 102, 2, WithAlpha(s_accent, 160));
            DrawOrbit(center, 100, -timer * 5 + 35, 76, 1, WithAlpha(s_magenta, 170));
            DrawOrbit(center, 100, -timer * 5 + 145, 158, 1, WithAlpha(s_accent, 65));

            for (var i = 0; i < 60; i++)
            {
                var angle = i * MathF.Tau / 60;
                var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                var major = i % 5 == 0;
                Raylib.DrawLineEx(center + direction * (major ? 128 : 132), center + direction * 136,
                    1, WithAlpha(s_accent, major ? 130 : 45));
            }

            var orbitAngle = timer * 0.14f;
            var node = center + new Vector2(MathF.Cos(orbitAngle), MathF.Sin(orbitAngle)) * 123;
            Raylib.DrawCircleGradient((int)node.X, (int)node.Y, 9, WithAlpha(s_accent, 90), Color.Blank);
            Raylib.DrawCircleV(node, 2.5f, s_accent);

            var terminal = new Rectangle(680, 182, 140, 98);
            DrawAngledPanel(terminal, 12, new Color(8, 23, 35, 255), WithAlpha(s_accent, 190), 1.5f);
            Raylib.DrawLine(693, 203, 807, 203, WithAlpha(s_accent, 45));
            for (var i = 0; i < 3; i++)
                Raylib.DrawCircle(695 + i * 8, 193, 1.5f, WithAlpha(s_accent, 100 + i * 40));
            DrawGlowLine(new Vector2(710, 220), new Vector2(732, 238), s_accent);
            DrawGlowLine(new Vector2(732, 238), new Vector2(710, 256), s_accent);
            DrawGlowLine(new Vector2(751, 256), new Vector2(782, 256), s_magenta);

            Raylib.DrawLine(750, 373, 750, 394, WithAlpha(s_accent, 75));
            Raylib.DrawCircle(750, 397, 2, s_accent);
            const string caption = "A MODERN SHELL FOR WINDOWS";
            Text(caption, 750 - TextWidth(caption, 11, true) / 2, 411, 11, s_textSecondary, true);
        }

        private static void DrawOrbit(Vector2 center, float radius, float startDegrees, float sweepDegrees, float thickness, Color color)
        {
            var segments = Math.Max(8, (int)(sweepDegrees / 4));
            var previous = center + new Vector2(MathF.Cos(startDegrees * MathF.PI / 180), MathF.Sin(startDegrees * MathF.PI / 180)) * radius;
            for (var i = 1; i <= segments; i++)
            {
                var angle = (startDegrees + sweepDegrees * i / segments) * MathF.PI / 180;
                var point = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                Raylib.DrawLineEx(previous, point, thickness, color);
                previous = point;
            }
        }

        private static void DrawGlowLine(Vector2 start, Vector2 end, Color color)
        {
            Raylib.DrawLineEx(start, end, 12, WithAlpha(color, 12));
            Raylib.DrawLineEx(start, end, 7, WithAlpha(color, 30));
            Raylib.DrawLineEx(start, end, 3, color);
        }

        private static void DrawCornerBrackets(Rectangle bounds, int length, Color color)
        {
            var left = (int)bounds.X;
            var top = (int)bounds.Y;
            var right = (int)(bounds.X + bounds.Width);
            var bottom = (int)(bounds.Y + bounds.Height);
            Raylib.DrawLine(left, top, left + length, top, color);
            Raylib.DrawLine(left, top, left, top + length, color);
            Raylib.DrawLine(right - length, top, right, top, color);
            Raylib.DrawLine(right, top, right, top + length, color);
            Raylib.DrawLine(left, bottom, left + length, bottom, color);
            Raylib.DrawLine(left, bottom - length, left, bottom, color);
            Raylib.DrawLine(right - length, bottom, right, bottom, color);
            Raylib.DrawLine(right, bottom - length, right, bottom, color);
        }

        private static WindowAction DrawWindowControls()
        {
            if (DrawChromeButton(new Rectangle(WindowWidth - 100, 8, 36, 28), WindowAction.Minimize))
                return WindowAction.Minimize;
            if (DrawChromeButton(new Rectangle(WindowWidth - 60, 8, 36, 28), WindowAction.Close))
                return WindowAction.Close;
            return WindowAction.None;
        }

        private static bool DrawChromeButton(Rectangle bounds, WindowAction action)
        {
            var hover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), bounds);
            var color = hover ? action == WindowAction.Close ? s_warning : s_accent : s_textSecondary;
            if (hover)
                Raylib.DrawRectangleRec(bounds, WithAlpha(color, 20));
            var center = new Vector2(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
            if (action == WindowAction.Minimize)
                Raylib.DrawLineEx(center + new Vector2(-5, 3), center + new Vector2(5, 3), 1.5f, color);
            else
            {
                Raylib.DrawLineEx(center + new Vector2(-4, -4), center + new Vector2(4, 4), 1.5f, color);
                Raylib.DrawLineEx(center + new Vector2(4, -4), center + new Vector2(-4, 4), 1.5f, color);
            }
            return hover && Raylib.IsMouseButtonPressed(MouseButton.Left);
        }

        private static bool InstallButton(Rectangle bounds, string text, bool enabled)
        {
            var hover = enabled && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), bounds);
            var pressed = hover && Raylib.IsMouseButtonDown(MouseButton.Left);
            var accent = enabled ? hover ? s_accentHover : s_accent : s_textSecondary;
            var fill = enabled ? pressed ? new Color(37, 108, 122, 255) : hover ? new Color(25, 68, 82, 255) : new Color(16, 45, 59, 255) : s_panel;
            if (enabled)
                DrawAngledPanel(new Rectangle(bounds.X - 3, bounds.Y - 3, bounds.Width + 6, bounds.Height + 6), 12, WithAlpha(accent, 7), WithAlpha(accent, 28), 1);
            DrawAngledPanel(bounds, 10, fill, accent, 1);
            const float fontSize = 16;
            var textSize = Raylib.MeasureTextEx(s_monoFont, text, fontSize, 0.6f);
            var textX = MathF.Round(bounds.X + (bounds.Width - textSize.X) / 2);
            var textY = MathF.Round(bounds.Y + (bounds.Height - textSize.Y) / 2);
            Text(text, textX, textY, fontSize, enabled ? s_textPrimary : s_textSecondary, true);
            var arrow = new Vector2(bounds.X + bounds.Width - 28, bounds.Y + bounds.Height / 2);
            Raylib.DrawLineEx(arrow + new Vector2(-5, -5), arrow, 1.5f, accent);
            Raylib.DrawLineEx(arrow, arrow + new Vector2(-5, 5), 1.5f, accent);
            return hover && Raylib.IsMouseButtonPressed(MouseButton.Left);
        }

        private static void DrawInstallerFooter(string statusText, float progress, bool isInstalling, bool isDone, bool isAlreadyInstalled, float timer)
        {
            Raylib.DrawRectangle(1, HeroHeight, WindowWidth - 2, WindowHeight - HeroHeight - 1, new Color(8, 15, 24, 255));
            Raylib.DrawLine(1, HeroHeight, WindowWidth - 1, HeroHeight, s_panelLine);
            Raylib.DrawRectangle(40, HeroHeight, 80, 2, isDone && !isAlreadyInstalled ? s_success : s_accent);
            var color = isAlreadyInstalled ? s_warning : isDone ? s_success : s_accent;
            Raylib.DrawCircle(45, 508, 3, color);
            Text(isAlreadyInstalled ? "SETUP STATUS" : isDone ? "SETUP COMPLETE" : isInstalling ? "INSTALLING" : "AWAITING YOUR COMMAND", 57, 501, 11, color, true);
            Text(FitText(statusText, 590, 20), 40, 519, 20, s_textPrimary);
            var percentage = isDone && !isAlreadyInstalled ? 100 : (int)(Math.Clamp(progress, 0, 1) * 100);
            if (isInstalling || (isDone && !isAlreadyInstalled))
            {
                var label = $"{percentage:0}%";
                Text(label, 688 - TextWidth(label, 16, true), 522, 16, color, true);
            }
            DrawProgressBar(new Rectangle(40, 552, 648, 6), progress, isInstalling, isDone, isAlreadyInstalled, timer);
            var detail = isAlreadyInstalled ? "Select Install to check repair options."
                : isDone ? "xTerminal is ready. You can close this installer."
                : isInstalling ? "Copying application files to your installation directory."
                : "Install for the current Windows user.";
            Text(detail, 40, 569, 12, s_textSecondary);
            const string buttonHint = "xTERMINAL / SETUP";
            Text(buttonHint, 826 - TextWidth(buttonHint, 10, true) / 2, 576, 10, s_textSecondary, true);
        }

        private static void DrawProgressBar(Rectangle bounds, float progress, bool isInstalling, bool isDone, bool isAlreadyInstalled, float timer)
        {
            var value = isAlreadyInstalled ? 0 : isDone ? 1 : Math.Clamp(progress, 0, 1);
            var color = isDone ? s_success : s_accent;
            const int segments = 48;
            const float gap = 3;
            var width = (bounds.Width - (segments - 1) * gap) / segments;
            for (var i = 0; i < segments; i++)
            {
                var x = bounds.X + i * (width + gap);
                Raylib.DrawRectangleRec(new Rectangle(x, bounds.Y, width, bounds.Height), new Color(29, 46, 60, 255));
                var fill = Math.Clamp(value * segments - i, 0, 1);
                if (fill > 0)
                    Raylib.DrawRectangleRec(new Rectangle(x, bounds.Y, width * fill, bounds.Height), color);
            }
            if (isInstalling && value > 0 && value < 1)
            {
                var x = bounds.X + timer * 0.35f % 1 * bounds.Width * value;
                Raylib.DrawRectangle((int)x, (int)bounds.Y, 2, (int)bounds.Height, s_textPrimary);
            }
        }

        private static string GetStatusText(bool wasInstallClicked, bool isInstalling, bool isDone, bool isAlreadyInstalled, float timer)
        {
            if (isAlreadyInstalled && wasInstallClicked)
                return "xTerminal is already installed";
            if (isDone)
                return "Installation complete";
            if (isInstalling)
                return "Installing xTerminal" + new string('.', (int)(timer * 2) % 4);
            return "Ready to install xTerminal";
        }

        private static void DrawAngledPanel(Rectangle bounds, float cut, Color fill, Color border, float borderWidth)
        {
            // Clockwise screen coordinates; reverse each triangle for Raylib's face winding.
            ReadOnlySpan<Vector2> points =
            [
                new(bounds.X + cut, bounds.Y),
                new(bounds.X + bounds.Width, bounds.Y),
                new(bounds.X + bounds.Width, bounds.Y + bounds.Height - cut),
                new(bounds.X + bounds.Width - cut, bounds.Y + bounds.Height),
                new(bounds.X, bounds.Y + bounds.Height),
                new(bounds.X, bounds.Y + cut)
            ];
            for (var i = 1; i < points.Length - 1; i++)
                Raylib.DrawTriangle(points[0], points[i + 1], points[i], fill);
            if (borderWidth > 0)
                for (var i = 0; i < points.Length; i++)
                    Raylib.DrawLineEx(points[i], points[(i + 1) % points.Length], borderWidth, border);
        }
    }
}
