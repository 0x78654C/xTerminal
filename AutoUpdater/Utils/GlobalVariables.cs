using System;
using System.Collections.Generic;
using System.Text;

namespace AutoUpdater.Utils
{
    public class GlobalVariables
    {
        public static readonly string accountName = Environment.UserName;
        public static string rootPath = $"{Path.GetPathRoot(Environment.SystemDirectory)}Users\\{accountName}\\";
        public static readonly string computerName = Environment.MachineName;
        public static string terminalWorkDirectory = $"{Path.GetPathRoot(Environment.SystemDirectory)}Users\\{accountName}\\AppData\\Local\\xTerminal";
        public static string unpackUpdate = $"{terminalWorkDirectory}\\Unpack\\";
    }
}
