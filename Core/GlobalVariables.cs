using System;

namespace Core
{
    /// <summary>
    /// Declaration of necessary global variables
    /// </summary>
    public class GlobalVariables
    {
        public static string regKeyName = "xTerminal";
        public static string regCurrentDirectory = "CurrentDirectory";
        public static string regCurrentEitor = "CurrentEditor";
        public static string rootPath = "C:\\";  // Get current users profile folder path.
        public static string terminalTitle = "xTerminal v1.0";  // Terminal title
        public static readonly string accountName = Environment.UserName;
        public static readonly string computerName = Environment.MachineName; //extract machine name
        public static string historyFilePath = $"C:\\Users\\{accountName}\\AppData\\Local\\xTerminal";
        public static string historyFile = historyFilePath + "\\History.db";
    }
}
