using System;
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
        public static string regCurrentDirectory = "CurrentDirectory";
        public static string regCurrentEitor = "CurrentEditor";
        public static string regUI = "UI";
        public static string rootPath = Path.GetPathRoot(Environment.SystemDirectory);  // Get current users profile folder path.
        public static string terminalTitle = "xTerminal v1.0";  // Terminal title
        public static readonly string accountName = Environment.UserName;
        public static readonly string computerName = Environment.MachineName; //extract machine name
        public static string historyFilePath = $"{rootPath}Users\\{accountName}\\AppData\\Local\\xTerminal";
        public static string currentDirectory = historyFilePath + $"\\{process}cDir.t";
        public static string uiSettings = historyFilePath + $"\\{process}ui.t";
        public static string historyFile = historyFilePath + "\\History.db";
        public static string addonDirectory = Directory.GetCurrentDirectory() + "\\Add-ons";
    }
}