using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using IWshRuntimeLibrary;

namespace Core.SystemTools
{
    [SupportedOSPlatform("Windows")]
    public class ShortcutMaker
    {
         public ShortcutMaker() { }
        void CreateShortcut(string sendToPath, string fullPath)
        {
            //Environment.SpecialFolder.Desktop
            string fileName = Path.GetFileNameWithoutExtension(fullPath);
            IWshShortcut shortcut;
            if (Environment.Is64BitOperatingSystem)
            {
                WshShell wshShell = new WshShell();
                shortcut = (IWshShortcut)wshShell.CreateShortcut(Path.Combine(sendToPath, fileName + ".lnk"));
            }
            else
            {
                WshShellClass wshShell = new WshShellClass();
                shortcut = (IWshShortcut)wshShell.CreateShortcut(Path.Combine(sendToPath, fileName + ".lnk"));
            }
            shortcut.TargetPath = fullPath;
            shortcut.IconLocation = fullPath;
            shortcut.Save();
        }
    }
}
