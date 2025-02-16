/*
    Shortcut maker class.
*/

using System;
using System.IO;
using System.Runtime.Versioning;
using IWshRuntimeLibrary;

namespace Core.SystemTools
{
    [SupportedOSPlatform("Windows")]
    public class ShortcutMaker
    {
        /// <summary>
        /// Path where to save the shortcut.
        /// </summary>
        public string PathShortcut { get; set; } = "";

        /// <summary>
        /// Current loged used desktop directory path.
        /// </summary>
        private string DesktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        /// <summary>
        /// Save shortcut to desktop or not. Default: true;
        /// </summary>
        public bool SaveDesktop { get; set; } = true;

        /// <summary>
        /// Path of file/directory that you want to create a shorctcut.
        /// </summary>
        public string Path { get; set; } = "";

        /// <summary>
        /// cTor Shortcut Maker
        /// </summary>
        public ShortcutMaker() { }

        /// <summary>
        /// Create the shortcut with the specified parameters.
        /// </summary>
        public void CreateShortcut()
        {
            var currentDirectory = System.IO.File.ReadAllText(GlobalVariables.currentDirectory);

            var pathShortcut = FileSystem.SanitizePath(PathShortcut, currentDirectory);
            if (SaveDesktop)
                pathShortcut = DesktopFolder;

            var path = FileSystem.SanitizePath(Path, currentDirectory);

            if (!Directory.Exists(pathShortcut) && !SaveDesktop)
            {
                FileSystem.ErrorWriteLine($"Directory does not exist: {pathShortcut}");
                GlobalVariables.isErrorCommand = true;
                return;
            }

            if (!FileSystem.IsFileOrDirectoryPresent(path))
            {
                FileSystem.ErrorWriteLine($"File or directory does not exist for creating the shortcut: {path}");
                GlobalVariables.isErrorCommand = true;
                return;
            }
            
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            IWshShortcut shortcut;
            var finalPath = System.IO.Path.Combine(pathShortcut, fileName + ".lnk");
            
            if (Environment.Is64BitOperatingSystem)
            {
                WshShell wshShell = new WshShell();
                shortcut = (IWshShortcut)wshShell.CreateShortcut(finalPath);
            }
            else
            {
                WshShellClass wshShell = new WshShellClass();
                shortcut = (IWshShortcut)wshShell.CreateShortcut(finalPath);
            }
            shortcut.TargetPath = path;
            shortcut.WorkingDirectory = System.IO.Path.GetDirectoryName(path);
            shortcut.IconLocation = path;
            shortcut.Save();
            FileSystem.SuccessWriteLine($"Shortcut created: {finalPath}");
        }
    }
}
