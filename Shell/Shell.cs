using Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SetConsoleColor = Core.SystemTools.UI;
using ProccessManage = Core.SystemTools.ProcessStart;
using SystemCmd = Core.Commands.SystemCommands;
using System.Runtime.Versioning;
using Core.SystemTools;
using Core.Commands;
using Commands;

namespace Shell
{
    [SupportedOSPlatform("windows")]
    public class Shell
    {
        //declaring variables
        public static string s_currentDirectory = null;
        private static readonly string s_accountName = GlobalVariables.accountName;    //extract current loged username
        private static readonly string s_computerName = GlobalVariables.computerName; //extract machine name
        private static string s_input = null;
        private static string s_historyFilePath = GlobalVariables.terminalWorkDirectory;
        private static string s_passwordManagerDirectory = GlobalVariables.passwordManagerDirectory;
        private static List<string> s_listReg = new List<string>() { "UI" };
        private static string s_historyFile = GlobalVariables.historyFile;
        private static string s_addonDir = GlobalVariables.addonDirectory;
        private static string s_regUI = "";
        private static string s_regUIcd = "";
        private static string s_indicator = "$";
        private static string s_indicatorColor = "white";
        private static string s_userColor = "green";
        private static int s_userEnabled = 1;
        private static string s_cdColor = "cyan";
        private static string s_historyLimitSize = "2000";
        private static string s_terminalTitle = $"xTerminal {Application.ProductVersion}";
        private static string s_aliasFile = GlobalVariables.aliasFile;
        private static bool s_isCDVisible = true;
        private static List<string> commandHistory = new List<string>();
        private static int historyIndex = -1;  // Tracks the current position in the history
        private static int lastWindowWidth = Console.WindowWidth;
        //-------------------------------


        // Function for store current path in directory by current process id.
        private void StoreCurrentDirectory()
        {
            if (!File.Exists(GlobalVariables.currentDirectory))
                File.WriteAllText(GlobalVariables.currentDirectory, GlobalVariables.rootPath);
        }

        /// <summary>
        /// Load predefined settings.
        /// </summary>
        private void SettingsLoad()
        {
            // Creating the history file directory in USERPROFILE\AppData\Local if not exist.
            if (!Directory.Exists(s_historyFilePath))
                Directory.CreateDirectory(s_historyFilePath);

            // Creating history file if not exist
            if (!File.Exists(s_historyFile))
                File.WriteAllText(s_historyFile, string.Empty);

            // Creating the Password Manager directory for storing the encrypted files.
            if (!Directory.Exists(s_passwordManagerDirectory))
                Directory.CreateDirectory(s_passwordManagerDirectory);

            // Store current directory with current process id.
            StoreCurrentDirectory();

            // Creating the addon directory for C# code script scomands if not exist.
            if (!Directory.Exists(s_addonDir))
                Directory.CreateDirectory(s_addonDir);

            //reading current location
            s_currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);

            if (s_currentDirectory == "")
            {
                File.WriteAllText(GlobalVariables.currentDirectory, GlobalVariables.rootPath);
            }

            // Reading cport time out setting and set default vaule if is emtpy.
            string timeOut = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCportTimeOut);
            if (timeOut == "")
            {
                RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCportTimeOut, "500");
            }

            // Reading UI settings.
            s_regUI = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regUI);
            if (s_regUI == "")
            {
                RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regUI, @"green;1|white;$|cyan");
            }

            // Reading UI CD settings.
            s_regUIcd = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regUIcd);
            if (s_regUIcd == "")
            {
                RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regUIcd, @"True");
                s_regUIcd = "True";
            }

            // Reading UI success color settings.
            GlobalVariables.successColorOutput = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regUIsc);
            if (GlobalVariables.successColorOutput == "")
            {
                RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regUIsc, "Gray");
                GlobalVariables.successColorOutput = "Gray";
            }

            // Reading history limit size.
            s_historyLimitSize = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regHistoryLimitSize);
            if (s_historyLimitSize == "")
            {
                RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regHistoryLimitSize, GlobalVariables.historyLimitSize.ToString());
            }


            // Title display application name, version + current directory.
            Console.Title = $"{s_terminalTitle} | {s_currentDirectory}";

            // Store xTerminal version.
            GlobalVariables.version = Application.ProductVersion;
        }

        /// <summary>
        /// Execute predifined xTerminal commands.
        /// </summary> 
        private void ExecuteCommands(string command)
        {
            try
            {
                // Display running command on title.
                Console.Title = command;

                // Run xTerminal predifined commands.
                var c = Commands.CommandRepository.GetCommand(command);
                CheckAliasCommandRun(GlobalVariables.aliasParameters);

                if (c != null || !string.IsNullOrWhiteSpace(GlobalVariables.aliasParameters))
                {
                    if (!string.IsNullOrWhiteSpace(GlobalVariables.aliasParameters))
                        command = GlobalVariables.aliasParameters;

                    // Pipe line command execution.
                    if (command.Contains("|") && !command.Contains("alias"))
                    {
                        GlobalVariables.isPipeCommand = true;
                        var commandSplit = command.Split('|');
                        GlobalVariables.pipeCmdCount = commandSplit.Count() - 1;
                        GlobalVariables.pipeCmdCountTemp = GlobalVariables.pipeCmdCount;
                        var count = 0;
                        foreach (var cmd in commandSplit)
                        {
                            var cmdExecute = cmd.Trim();
                            c = Commands.CommandRepository.GetCommand(cmdExecute);

                            c.Execute(cmdExecute);

                            count++;
                            GlobalVariables.pipeCmdCount--;
                        }
                        GlobalVariables.isPipeCommand = false;
                    }
                    else
                        c.Execute(command);
                    GlobalVariables.aliasParameters = string.Empty;
                    GlobalVariables.aliasRunFlag = false;
                    GlobalVariables.aliasInParameter.Clear();
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine($"{e.Message}. Check commmand!");
                GlobalVariables.pipeCmdOutput = string.Empty;
                GlobalVariables.pipeCmdCount = 0;
                GlobalVariables.pipeCmdCountTemp = 0;
            }
        }

        /// <summary>
        /// Return info message if alias command parameter is wrong xterminal commmand.
        /// </summary>
        /// <param name="aliasParameter"></param>
        private void CheckAliasCommandRun(string aliasParameter)
        {
            if (string.IsNullOrEmpty(aliasParameter) && GlobalVariables.aliasRunFlag)
            {
                Console.Error.WriteLine("Check alias command parameter format!");
                GlobalVariables.aliasRunFlag = false;
            }
        }

        /// <summary>
        /// Arguments handler for parameter usage.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private List<string> ParamHandler(string[] args)
        {
            List<string> argList = new List<string>();
            foreach (var arg in args)
            {
                argList.Add(arg);
            }
            return argList;
        }

        /// <summary>
        /// Execute xTerminal commands via parameters.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private bool ExecuteParamCommands(string[] args)
        {
            try
            {
                string param = string.Join(" ", ParamHandler(args));
                if (!string.IsNullOrEmpty(param))
                {
                    SettingsLoad();
                    ExecuteCommands(param);
                    GlobalVariables.pipeCmdOutput = string.Empty;
                    GlobalVariables.pipeCmdCount = 0;
                    return true;
                }
                return false;
            }
            catch { return false; }
        }




        /// <summary>
        /// Read commands from keyborad by keystroke with autosugestions for several commands.
        /// </summary>
        /// <returns></returns>
        private static string ReadCommand()
        {
            // string command = string.Empty;
            int tabPressCount = 0;
            int cursorPosition = 0;
            List<char> command = new List<char>();
            commandHistory.Clear();
            var outCompletion = "";
            var historyStored = File.ReadAllText(s_historyFile);
            FileSystem.ReadStringLine(ref commandHistory, historyStored);
            lastWindowWidth = Console.WindowWidth;
            RedrawCommand(command, cursorPosition);

            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.CursorVisible = false;
                    Console.WriteLine(); // Move to the next line
                    GlobalVariables.lengthPS1 = 0;
                    Console.CursorVisible = true;
                    break;
                }
                else if (key.Key == ConsoleKey.Tab)
                {
                    tabPressCount++;
                    if (tabPressCount == 2)
                    {
                        var c = new string(command.ToArray());
                        AutoSuggestionCommands.FileDirSuggestion(c, "cd", s_currentDirectory, false, ref outCompletion);
                        AutoSuggestionCommands.FileDirSuggestion(c, "odir", s_currentDirectory, false, ref outCompletion);
                        AutoSuggestionCommands.FileDirSuggestion(c, "ls", s_currentDirectory, false, ref outCompletion);
                        AutoSuggestionCommands.FileDirSuggestion(c, "hex", s_currentDirectory, true, ref outCompletion);
                        AutoSuggestionCommands.FileDirSuggestion(c, "./", s_currentDirectory, true, ref outCompletion);
                        AutoSuggestionCommands.FileDirSuggestion(c, "ccs", s_currentDirectory, true, ref outCompletion);
                        AutoSuggestionCommands.FileDirSuggestion(c, "fcopy", s_currentDirectory, true, ref outCompletion);
                        AutoSuggestionCommands.FileDirSuggestion(c, "mv", s_currentDirectory, true, ref outCompletion);
                        AutoSuggestionCommands.FileDirSuggestion(c, "fmove", s_currentDirectory, true, ref outCompletion);
                        AutoSuggestionCommands.FileDirSuggestion(c, "del", s_currentDirectory, false, ref outCompletion);
                        AutoSuggestionCommands.FileDirSuggestion(c, "del", s_currentDirectory, true, ref outCompletion);
                        AutoSuggestionCommands.FileDirSuggestion(c, "edit", s_currentDirectory, true, ref outCompletion);
                        AutoSuggestionCommands.FileDirSuggestion(c, "cp", s_currentDirectory, false, ref outCompletion);
                        AutoSuggestionCommands.FileDirSuggestion(c, "cp", s_currentDirectory, true, ref outCompletion);
                        AutoSuggestionCommands.FileDirSuggestion(c, "md5", s_currentDirectory, true, ref outCompletion);
                        AutoSuggestionCommands.FileDirSuggestion(c, "sort", s_currentDirectory, true, ref outCompletion);
                        AutoSuggestionCommands.FileDirSuggestion(c, "cat", s_currentDirectory, true, ref outCompletion);
                        tabPressCount = 0;
                        var countOut = outCompletion.ToList().Count;
                        if (countOut > 0)
                        {
                            var getCommandStr = string.Join("", command);
                            var getCommand = getCommandStr.Split(' ')[0];
                            var paramCommand = getCommandStr.SplitByText($"{getCommand} ", 1);
                            Console.CursorVisible = false;
                            command.Clear();
                            foreach (var item in getCommand)
                                command.Insert(command.Count, item);
                            command.Insert(command.Count, ' ');
                            foreach (var item in outCompletion)
                                command.Insert(command.Count, item);
                            Console.SetCursorPosition(getCommandStr.Length + GlobalVariables.lengthPS1, Console.CursorTop);
                            foreach (var paramChar in getCommandStr)
                                Console.Write('\b');
                            Console.Write(new string(command.ToArray()));
                            Console.CursorVisible = true;
                            cursorPosition = countOut + getCommand.Length + 1;
                        }
                        else
                            cursorPosition = command.Count;
                    }
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    try
                    {
                        if (command.Count > 0)
                        {
                            command.RemoveAt(cursorPosition - 1);
                            cursorPosition--;
                            RedrawCommand(command, cursorPosition);
                        }
                    }
                    catch { }
                    tabPressCount = 0;
                }
                else if (key.Key == ConsoleKey.Delete)
                {
                    // Delete the character at the current caret position
                    if (cursorPosition < command.Count)
                    {
                        command.RemoveAt(cursorPosition); // Remove the character at the caret
                        RedrawCommand(command, cursorPosition); // Redraw the command
                    }
                }
                else if (key.Key == ConsoleKey.UpArrow)
                {
                    // Navigate backward in history
                    if (commandHistory.Count > 0 && historyIndex < commandHistory.Count - 1)
                    {
                        historyIndex++;
                        string historyCommand = commandHistory[commandHistory.Count - 1 - historyIndex];
                        command = new List<char>(historyCommand.ToCharArray());
                        cursorPosition = command.Count;  // Move the caret to the end
                        RedrawCommand(command, cursorPosition);
                    }
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    // Navigate forward in history
                    if (historyIndex > 0)
                    {
                        historyIndex--;
                        string historyCommand = commandHistory[commandHistory.Count - 1 - historyIndex];
                        command = new List<char>(historyCommand.ToCharArray());
                        cursorPosition = command.Count;  // Move the caret to the end
                        RedrawCommand(command, cursorPosition);
                    }
                    else if (historyIndex == 0)
                    {
                        historyIndex = -1;
                        command.Clear();
                        cursorPosition = 0;
                        RedrawCommand(command, cursorPosition);
                    }
                }
                else if (key.Key == ConsoleKey.LeftArrow)
                {
                    if (cursorPosition > 0)
                    {
                        cursorPosition--;
                        // Calculate cursor position for wrapped lines
                        int totalCursorPosition = cursorPosition + GlobalVariables.lengthPS1;
                        int cursorLeft = totalCursorPosition % Console.WindowWidth;
                        int cursorTop = Console.CursorTop + (totalCursorPosition / Console.WindowWidth);

                        // Ensure valid cursor position
                        if (cursorLeft < 0) cursorLeft = 0;
                        if (cursorLeft >= Console.WindowWidth) cursorLeft = Console.WindowWidth - 1;
                        if (cursorTop >= Console.BufferHeight) cursorTop = Console.BufferHeight - 1;

                        Console.SetCursorPosition(cursorLeft, cursorTop);
                    }
                }
                else if (key.Key == ConsoleKey.RightArrow)
                {
                    if (cursorPosition < command.Count)
                    {
                        cursorPosition++;
                        // Calculate cursor position for wrapped lines
                        int totalCursorPosition = cursorPosition + GlobalVariables.lengthPS1;
                        int cursorLeft = totalCursorPosition % Console.WindowWidth;
                        int cursorTop = Console.CursorTop + (totalCursorPosition / Console.WindowWidth);

                        // Ensure valid cursor position
                        if (cursorLeft < 0) cursorLeft = 0;
                        if (cursorLeft >= Console.WindowWidth) cursorLeft = Console.WindowWidth - 1;
                        if (cursorTop >= Console.BufferHeight) cursorTop = Console.BufferHeight - 1;

                        Console.SetCursorPosition(cursorLeft, cursorTop);
                    }
                }
                else if (key.Key == ConsoleKey.Home)
                {
                    cursorPosition = 0;
                    Console.SetCursorPosition(cursorPosition + GlobalVariables.lengthPS1, Console.CursorTop);
                }
                else if (key.Key == ConsoleKey.End)
                {
                    cursorPosition = command.Count;
                    Console.SetCursorPosition(cursorPosition + GlobalVariables.lengthPS1, Console.CursorTop);
                }
                else
                {
                    command.Insert(cursorPosition, key.KeyChar);
                    cursorPosition++;
                    RedrawCommand(command, cursorPosition);
                    tabPressCount = 0; // Reset tab press count on other keypress
                }
            }
            GlobalVariables.lengthPS1 = 0;
            return new string(command.ToArray());
        }


        /// <summary>
        /// Redraws the entire command and moves the caret to the correct position.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cursorPosition"></param>
        static void RedrawCommand(List<char> command, int cursorPosition)
        {
            // Hide caret while redrawing.
            Console.CursorVisible = false;

            // Save the current cursor top to restore it later
            int initialCursorTop = Console.CursorTop;

            // Calculate the prompt length and window width
            int promptLength = GlobalVariables.lengthPS1;
            int windowWidth = lastWindowWidth;

            // Move the cursor to the start of the input area (after the prompt)
            Console.SetCursorPosition(0, initialCursorTop);

            // Redraw the prompt and the command
            SetConsoleUserConnected(s_currentDirectory, s_accountName, s_computerName, s_regUI, s_regUIcd);
            var cmdStr = string.Join("", command.ToArray());
            Console.Write(cmdStr);

            // Clear any extra characters from the previous input
            int commandLength = command.Count;
            int totalLength = promptLength + commandLength;
            int remainingSpaces = windowWidth - ((promptLength + commandLength) % windowWidth);
            if (remainingSpaces > 0 && remainingSpaces < windowWidth)
                Console.Write(new string(' ', remainingSpaces - 1)); // Clear residual characters

            // Ensure cursor position is within valid bounds
            int totalCursorPosition = cursorPosition + promptLength;
            int cursorLeft = totalCursorPosition % windowWidth;
            int cursorTop = initialCursorTop + (totalCursorPosition / windowWidth);

            // Ensure the cursor position stays within valid bounds
            if (cursorLeft < 0) cursorLeft = 0;
            if (cursorLeft >= windowWidth) cursorLeft = windowWidth - 1;  // Avoid going out of the right bound
            if (cursorTop >= Console.BufferHeight) cursorTop = Console.BufferHeight - 1; // Avoid going out of bottom bound

            // Set the cursor to the calculated position
            Console.SetCursorPosition(cursorLeft, cursorTop);

            // Show caret again.
            Console.CursorVisible = true;
        }

        //Entry point of shell
        public void Run(string[] args)
        {
            // Check if current path subkey exists in registry. 
            RegistryManagement.CheckRegKeysStart(s_listReg, GlobalVariables.regKeyName, "", false);

            // Setting up the title.
            Console.Title = s_terminalTitle;

            if (ExecuteParamCommands(args)) { return; };

            // We loop until exit commands is hit
            do
            {
                //Load predifined settings.
                SettingsLoad();

                // We se the color and user loged in on console.
                SetConsoleUserConnected(s_currentDirectory, s_accountName, s_computerName, s_regUI, s_regUIcd);

                //reading user imput
                s_input = ReadCommand();

                //cleaning input
                s_input = s_input.Trim();

                // Add the command to history only if it's not empty
                if (!string.IsNullOrWhiteSpace(s_input))
                {
                    commandHistory.Add(s_input);
                    historyIndex = -1; // Reset the history index after executing a command
                }

                if (File.Exists(s_historyFile))
                {
                    // Don't store in history with + commands.
                    if (!s_input.StartsWith("+"))
                        WriteHistoryCommandFile(s_historyFile, s_input);

                    string command = string.Empty;
                    if (File.Exists(s_aliasFile))
                    {
                        var aliasCommands = JsonManage.ReadJsonFromFile<AliasC[]>(s_aliasFile);
                        command = aliasCommands.Where(f => f.CommandName == s_input).FirstOrDefault()?.CommandName?.Trim() ?? string.Empty;
                    }

                    //log off the machine command
                    if (s_input == "logoff")
                    {
                        SystemCmd.LogoffCmd();
                    }
                    else if (s_input == "exit")
                    {
                        FileSystem.SuccessWriteLine("xTerminal shutting down...");
                        Environment.Exit(0);
                    }
                    else if (s_input == "lock")
                    {
                        SystemCmd.LockCmd();
                    }
                    else if (s_input == "sleep")
                    {
                        SystemCmd.SleepCcmd();
                    }
                    else if (s_input.StartsWith("cmd") && !command.StartsWith("cmd"))
                    {
                        ProccessManage.Execute(s_input, s_input);
                    }
                    else if (s_input.StartsWith("ps") && !command.StartsWith("ps"))
                    {
                        ProccessManage.Execute(s_input, s_input);
                    }
                    // Run commands from history
                    else if (s_input.StartsWith("+"))
                    {
                        var cleanCommandNumebr = s_input.Replace("+", "").Trim();
                        try
                        {
                            bool isDigit = Char.IsDigit(cleanCommandNumebr.ToCharArray()[0]);
                            if (isDigit)
                            {
                                int position = Int32.Parse(cleanCommandNumebr);
                                var historyCommand = HistoryCommands.GetHistoryCommand(s_historyFile, position).Trim();
                                s_input = historyCommand;
                                WriteHistoryCommandFile(s_historyFile, s_input);
                            }
                            else
                            {
                                FileSystem.ErrorWriteLine("Command position must be a positive number!");
                            }
                        }
                        catch (Exception e)
                        {
                            FileSystem.ErrorWriteLine($"Command position must be a positive number if run the + command. {e.Message}");
                        }
                    }
                }

                // New command implementation by Scott.
                if (GlobalVariables.autoSuggestion)
                {
                    GlobalVariables.autoSuggestion = false;
                }
                else
                {
                    ExecuteCommands(s_input);
                    GlobalVariables.pipeCmdOutput = string.Empty;
                    GlobalVariables.pipeCmdCount = 0;
                    GlobalVariables.pipeCmdCountTemp = 0;
                }
                GC.Collect();

            } while (s_input != "exit");
        }


        // We set the name of the current user logged in and machine on console.
        private static void SetConsoleUserConnected(string currentLocation, string accountName, string computerName, string uiSettings, string uiCD)
        {
            if (uiSettings != "")
            {
                UISettingsParse(uiSettings, uiCD);
            }

            if (currentLocation == GlobalVariables.rootPath)
            {
                SetUser(accountName, computerName, currentLocation, false);
            }
            else
            {
                SetUser(accountName, computerName, currentLocation, true);
            }
        }

        /// <summary>
        /// Set user color on console.
        /// </summary>
        /// <param name="accountName"></param>
        /// <param name="computerName"></param>
        /// <param name="currentLocation"></param>
        /// <param name="currentDir"></param>
        private static void SetUser(string accountName, string computerName, string currentLocation, bool currentDir)
        {
            GlobalVariables.lengthPS1 = 0;
            if (currentDir == false)
            {
                if (s_userEnabled == 1)
                {
                    if (s_userColor != "green")
                    {

                        var ps = $"{accountName}@{computerName}:";
                        GlobalVariables.lengthPS1 += ps.Length;
                        FileSystem.ColorConsoleText(SetConsoleColor.SetConsoleColor(s_userColor), ps);

                    }
                    else
                    {
                        var ps = $"{accountName}@{computerName}:";
                        GlobalVariables.lengthPS1 += ps.Length;
                        FileSystem.ColorConsoleText(ConsoleColor.Green, ps);
                    }
                }

                if (s_cdColor != "cyan")
                {
                    var ps = $"~";
                    GlobalVariables.lengthPS1 += ps.Length;
                    FileSystem.ColorConsoleText(SetConsoleColor.SetConsoleColor(s_cdColor), ps);
                }
                else
                {
                    var ps = $"~";
                    GlobalVariables.lengthPS1 += ps.Length;
                    FileSystem.ColorConsoleText(ConsoleColor.Cyan, ps);
                }
                if (!string.IsNullOrEmpty(s_indicator))
                {
                    if (s_indicatorColor != "white")
                    {
                        var ps = $" {s_indicator} ";
                        GlobalVariables.lengthPS1 += ps.Length;
                        FileSystem.ColorConsoleText(SetConsoleColor.SetConsoleColor(s_indicatorColor), ps);
                    }
                    else
                    {
                        var ps = $" {s_indicator} ";
                        GlobalVariables.lengthPS1 += ps.Length;
                        FileSystem.ColorConsoleText(ConsoleColor.White, ps);
                    }
                }
                else
                {
                    var ps = " $ ";
                    GlobalVariables.lengthPS1 += ps.Length;
                    FileSystem.ColorConsoleText(ConsoleColor.White, ps);
                }
                return;
            }
            if (s_userEnabled == 1)
            {
                if (s_userColor != "green")
                {
                    var ps = $"{accountName}@{computerName}:";
                    GlobalVariables.lengthPS1 += ps.Length;
                    FileSystem.ColorConsoleText(SetConsoleColor.SetConsoleColor(s_userColor), ps);
                }
                else
                {
                    var ps = $"{accountName}@{computerName}:";
                    GlobalVariables.lengthPS1 += ps.Length;
                    FileSystem.ColorConsoleText(ConsoleColor.Green, ps);
                }
            }
            if (s_cdColor != "cyan")
            {
                if (s_isCDVisible)
                {
                    var ps = $"{currentLocation}~";
                    GlobalVariables.lengthPS1 += ps.Length;
                    FileSystem.ColorConsoleText(SetConsoleColor.SetConsoleColor(s_cdColor), ps);
                }
                else
                {
                    var ps = $"~";
                    GlobalVariables.lengthPS1 += ps.Length;
                    FileSystem.ColorConsoleText(SetConsoleColor.SetConsoleColor(s_cdColor), ps);
                }
            }
            else
            {
                if (s_isCDVisible)
                {
                    var ps = $"{currentLocation}~";
                    GlobalVariables.lengthPS1 += ps.Length;
                    FileSystem.ColorConsoleText(ConsoleColor.Cyan, ps);
                }
                else
                {
                    var ps = $"~";
                    GlobalVariables.lengthPS1 += ps.Length;
                    FileSystem.ColorConsoleText(ConsoleColor.Cyan, ps);
                }
            }
            if (!string.IsNullOrEmpty(s_indicator))
            {
                if (s_indicatorColor != "white")
                {
                    var ps = $" {s_indicator} ";
                    GlobalVariables.lengthPS1 += ps.Length;
                    FileSystem.ColorConsoleText(SetConsoleColor.SetConsoleColor(s_indicatorColor), ps);
                }
                else
                {
                    var ps = $" {s_indicator} ";
                    GlobalVariables.lengthPS1 += ps.Length;
                    FileSystem.ColorConsoleText(ConsoleColor.White, ps);
                }
            }
            else
            {
                var ps = " $ ";
                GlobalVariables.lengthPS1 += ps.Length;
                FileSystem.ColorConsoleText(ConsoleColor.White, ps);
            }
        }

        private static void UISettingsParse(string settings, string uiCD)
        {
            var parseSettings = settings.Split('|');

            string userSetting = parseSettings[0];
            string indicatorSetting = parseSettings[1];

            // Setting the current directorycolor.
            s_cdColor = parseSettings[2];

            // Setting the user settings.
            s_userEnabled = Int32.Parse(userSetting.Split(';')[1]);
            s_userColor = userSetting.Split(';')[0];

            // Setting the indicator settings.
            s_indicator = indicatorSetting.Split(';')[1];
            s_indicatorColor = indicatorSetting.Split(';')[0];

            // Setting the visibilaty for current directory.
            s_isCDVisible = bool.Parse(uiCD);
        }

        /// <summary>
        /// Write terminal input commands to history file. 
        /// </summary>
        /// <param name="historyFile"></param>
        /// <param name="commandInput"></param
        private void WriteHistoryCommandFile(string historyFile, string commandInput)
        {
            s_historyLimitSize = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regHistoryLimitSize);
            int historyLimitSize = GlobalVariables.historyLimitSize;
            if (s_historyLimitSize != "")
                historyLimitSize = Int32.Parse(s_historyLimitSize);
            int countLines = File.ReadAllLines(historyFile).Count();
            var lines = File.ReadAllLines(historyFile).Skip(countLines - historyLimitSize);
            List<string> tempList = new List<string>();

            for (int i = 0; i < lines.Count(); i++)
            {
                if (!string.IsNullOrEmpty(lines.ElementAt(i)))
                    tempList.Add(lines.ElementAt(i));
            }

            if (!commandInput.StartsWith("ch") && !commandInput.StartsWith("chistory"))
            {
                if (!string.IsNullOrWhiteSpace(commandInput) && !string.IsNullOrEmpty(commandInput))
                {
                    tempList.Add(commandInput);
                    int countCommands = tempList.Count;
                    string outCommands = "";
                    for (int i = 0; i < countCommands; i++)
                    {
                        outCommands += tempList.ElementAt(i) + Environment.NewLine;
                    }
                    File.WriteAllText(historyFile, outCommands);
                }
            }
            tempList.Clear();
        }
    }
}

