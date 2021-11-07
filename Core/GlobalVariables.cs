using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Core
{
    /// <summary>
    /// Declaration of necessary global variables
    /// </summary>
    public class GlobalVariables
    {
        private static string process = Process.GetCurrentProcess().Id.ToString();
        public static string regKeyName = "xTerminal";
        public static string regCurrentEitor = "CurrentEditor";
        public static string regUI = "UI";
        public static string rootPath = Path.GetPathRoot(Environment.SystemDirectory);
        public static string terminalTitle = "xTerminal v1.2.3";
        public static readonly string accountName = Environment.UserName;
        public static readonly string computerName = Environment.MachineName;
        public static string terminalWorkDirectory = $"{rootPath}Users\\{accountName}\\AppData\\Local\\xTerminal";
        public static string passwordManagerDirectory = $"{terminalWorkDirectory}\\Pwm\\";
        public static string currentDirectory = terminalWorkDirectory + $"\\{process}cDir.t";
        public static string uiSettings = terminalWorkDirectory + $"\\{process}ui.t";
        public static string historyFile = terminalWorkDirectory + "\\History.db";
        public static string addonDirectory = Directory.GetCurrentDirectory() + "\\Add-ons";
        public static List<string> excludeDirectories = new List<string>() { "System Volume Information", "$Recycle.Bin" };
        public static List<string> excludeFiles = new List<string>() { "pagefile.sys" };
    }
}