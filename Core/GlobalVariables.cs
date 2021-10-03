using System;

namespace Core
{
    /// <summary>
    /// Declaration of necessary global variables
    /// </summary>
    public class GlobalVariables
    {
        public static string currentLocation = Environment.CurrentDirectory + @"\Data\curDir.ini";                              // Current Location file path
        public static string editorPath = Environment.CurrentDirectory + @"\Data\cEditor.ini";                                  // Current editor file path
        public static string regKeyName = "xTerminal";
        public static string regCurrentDirectory = "CurrentDirectory";
        public static string regCurrentEitor = "CurrentEditor";
        public static string rootPath = "C:\\";  // Get current users profile folder path.
        public static string terminalTitle = "xTerminal v1.0";  // Terminal title
    }
}
