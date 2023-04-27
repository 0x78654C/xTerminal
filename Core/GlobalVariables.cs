using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

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
        public static string regCportTimeOut = "cportTimeOut";
        public static string regOpenAI_APIKey = "OpenAI_APIKey";
        public static string commandOut { get; set; }
        public static string version { get; set; }
        public static string aliasParameters = string.Empty;
        public static readonly string accountName = Environment.UserName;
        public static string rootPath = $"{Path.GetPathRoot(Environment.SystemDirectory)}Users\\{accountName}\\";
        public static readonly string computerName = Environment.MachineName;
        public static string terminalWorkDirectory = $"{Path.GetPathRoot(Environment.SystemDirectory)}Users\\{accountName}\\AppData\\Local\\xTerminal";
        public static string passwordManagerDirectory = $"{terminalWorkDirectory}\\Pwm\\";
        public static string aliasFile = $"{terminalWorkDirectory}\\alias.json";
        public static string currentDirectory = terminalWorkDirectory + $"\\{process}cDir.t";
        public static string uiSettings = terminalWorkDirectory + $"\\{process}ui.t";
        public static string historyFile = terminalWorkDirectory + "\\History.db";
        public static string addonDirectory = Application.StartupPath + "\\Add-ons";
        public static List<string> excludeDirectories = new List<string>() { "System Volume Information", "$Recycle.Bin" };
        public static List<string> excludeFiles = new List<string>() { "pagefile.sys" };
        public static bool eventCancelKey = false;
        public static bool autoSuggestion = false;
        public static bool aliasRunFlag = false;
        public static bool eventKeyFlagX = false;
        public static readonly string magicNunmbers= Application.StartupPath + @"\ext_list.txt";
    }
}