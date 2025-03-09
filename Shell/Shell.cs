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
using System.Diagnostics;

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
        private static string s_intercept = "";
        private static int s_ctrlCount = 0;
        private static string s_historyFilePath = GlobalVariables.terminalWorkDirectory;
        private static string s_passwordManagerDirectory = GlobalVariables.passwordManagerDirectory;
        private static string s_backgroundCommandsPidList = GlobalVariables.bgProcessListFile;
        private static List<string> s_listReg = new List<string>() { "UI" };
        private static string s_historyFile = GlobalVariables.historyFile;
        private static string s_addonDir = GlobalVariables.addonDirectory;
        private static string s_regUI = "";
        private static string s_regUIcd = "";
        private static string s_regUIsc = "";
        private static string s_indicator = "$";
        private static string s_indicatorColor = "white";
        private static string s_userColor = "green";
        private static int s_userEnabled = 1;
        private static int s_commandsRuned = 0;
        private static string s_cdColor = "cyan";
        private static string s_historyLimitSize = "2000";
        private static int s_ctrlKey = 1;
        private static int s_xKey = 0;
        private static string s_terminalTitle = $"xTerminal {Application.ProductVersion}";
        private static string s_aliasFile = GlobalVariables.aliasFile;
        private static bool s_isCDVisible = true;
        private static List<string> _history = new List<string>();
        public static bool HistoryEnabled { get; set; }
        public static IAutoCompleteHandler AutoCompletionHandler { private get; set; }
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

            // Creating the background command process list file.
            if (!File.Exists(s_backgroundCommandsPidList))
                File.WriteAllText(s_backgroundCommandsPidList, string.Empty);


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

                s_commandsRuned = 0;
                if (c != null || !string.IsNullOrWhiteSpace(GlobalVariables.aliasParameters))
                {
                    if (!string.IsNullOrWhiteSpace(GlobalVariables.aliasParameters))
                        command = GlobalVariables.aliasParameters;
                    // Pipe line command execution.
                    if (command.Contains("|") && !command.Contains("||") && !command.Contains("alias") && !command.EndsWith("&"))
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

                    // Run multiple commands if previous one succeed.
                    else if (command.Contains("&&"))
                    {
                        var commandSplit = command.Split("&&");
                        RunDoubleAndCommands(commandSplit);
                    }

                    // Run multiple commands if previous one failed.
                    else if (command.Contains("||"))
                    {
                        var commandSplit = command.Split("||");
                        RunParalelCommands(commandSplit);
                    }

                    // Run multiple commands.
                    else if (command.Contains(";"))
                    {
                        var commandSplit = command.Split(";");
                        RunContinousCommands(commandSplit);
                    }

                    // Run command in background.
                    else if (command.EndsWith("&"))
                    {
                        var commandSplit = command.Split("&")[0];
                        var cmdExecute = commandSplit.Trim();
                        c = Commands.CommandRepository.GetCommand(cmdExecute);
                        var bgCommands = new BGCommands();
                        bgCommands.Command = cmdExecute;
                        bgCommands.ExecuteCommand();
                    }
                    else
                        c.Execute(command);

                    // Reset alias parameters.
                    GlobalVariables.aliasParameters = string.Empty;
                    GlobalVariables.aliasRunFlag = false;
                    GlobalVariables.isErrorCommand = false;
                    GlobalVariables.aliasInParameter.Clear();
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine($"{e.Message}. Check commmand!");
                GlobalVariables.pipeCmdOutput = string.Empty;
                GlobalVariables.isErrorCommand = false;
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
        /// Run || commands
        /// </summary>
        /// <param name="commands"></param>
        private void RunParalelCommands(string[] commands)
        {
            foreach (var cmd in commands)
            {
                var cmdExecute = cmd.Trim();
                var c = Commands.CommandRepository.GetCommand(cmdExecute);
                if (GlobalVariables.isErrorCommand)
                    c.Execute(cmdExecute);
                if (s_commandsRuned == 0)
                    c.Execute(cmdExecute);
                s_commandsRuned++;
            }
        }


        /// <summary>
        /// Run && commands
        /// </summary>
        /// <param name="commands"></param>
        private void RunDoubleAndCommands(string[] commands)
        {
            foreach (var cmd in commands)
            {
                var cmdExecute = cmd.Trim();
                var c = Commands.CommandRepository.GetCommand(cmdExecute);
                if (GlobalVariables.isErrorCommand)
                {
                    GlobalVariables.isErrorCommand = false;
                    break;
                }
                c.Execute(cmdExecute);
            }
        }

        /// <summary>
        /// Run ; commands
        /// </summary>
        /// <param name="commands"></param>
        private void RunContinousCommands(string[] commands)
        {
            foreach (var cmd in commands)
            {
                var cmdExecute = cmd.Trim();
                var c = Commands.CommandRepository.GetCommand(cmdExecute);
                c.Execute(cmdExecute);
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
        /// CTRL+X key event.
        /// </summary>
        /// <param name="e"></param>
        static void KeyDown(KeyEventArgs e)
        {
            string keycode = e.KeyCode.ToString().ToLower();
            s_intercept += AutoSuggestion.KeyConvertor(keycode, "d", 2, string.Empty, () => s_ctrlCount = 0);
            s_intercept += AutoSuggestion.KeyConvertor(keycode, "numpad", 7, string.Empty, () => s_ctrlCount = 0);
            s_intercept += AutoSuggestion.KeyConvertor(keycode, "oemminus", 8, "-", () => s_ctrlCount = 0);
            s_intercept += AutoSuggestion.KeyConvertor(keycode, "oemplus", 7, "+", () => s_ctrlCount = 0);
            s_intercept += AutoSuggestion.KeyConvertor(keycode, "add", 3, "+", () => s_ctrlCount = 0);
            s_intercept += AutoSuggestion.KeyConvertor(keycode, "substract", 9, "-", () => s_ctrlCount = 0);
            s_intercept += AutoSuggestion.KeyConvertor(keycode, "multiply", 9, "*", () => s_ctrlCount = 0);
            s_intercept += AutoSuggestion.KeyConvertor(keycode, "decimal", 7, ".", () => s_ctrlCount = 0);
            s_intercept += AutoSuggestion.KeyConvertor(keycode, "oemperiod", 9, ".", () => s_ctrlCount = 0);
            s_intercept += AutoSuggestion.KeyConvertor(keycode, "decimal", 7, "-", () => s_ctrlCount = 0);
            s_intercept += AutoSuggestion.KeyConvertor(keycode, "oemquestion", 11, "-", () => s_ctrlCount = 0);

            if (e.KeyCode.ToString().Length == 1)
            {
                //Reset flags for reuse.
                s_xKey = 0;
            }



            if (e.KeyData == Keys.X)
                s_xKey = DateTime.Now.Second;


            if (e.KeyData.ToString() == "RControlKey" || e.KeyData.ToString() == "LControlKey")
            {
                s_ctrlKey = DateTime.Now.Second;
                s_ctrlCount++;
            }


            if ((s_xKey == s_ctrlKey) && GlobalVariables.eventKeyFlagX)
            {
                e.Handled = true;
                GlobalVariables.eventKeyFlagX = false;
                GlobalVariables.eventCancelKey = true;

                //Reset flags for reuse.
                s_xKey = 0;
                s_ctrlKey = 1;
            }
        }

        /// <summary>
        /// Function for clear running background command processes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Shell_Close(object sender, EventArgs e)
        {
            if (File.Exists(s_backgroundCommandsPidList))
            {
                var listBgRemain = "";
                var readBGList = File.ReadAllLines(s_backgroundCommandsPidList);
                File.WriteAllText(s_backgroundCommandsPidList, string.Empty);
                if (readBGList.Length == 0)
                    return;
                foreach (var line in readBGList)
                {
                    var splitPid = Int32.Parse(line.Split("PID: ")[1]);
                    Process.GetProcessById(splitPid).Kill();
                    var isActive = Process.GetProcesses().Any(p => p.Id == splitPid);
                    if (!isActive)
                        listBgRemain += line + Environment.NewLine;
                }
                File.WriteAllText(s_backgroundCommandsPidList, listBgRemain);
            }
        }

        //Entry point of shell
        public void Run(string[] args)
        {
            // Hook key event on KeyDown press.
            InterceptKeys.SetupHook(KeyDown);
            InterceptKeys.ReleaseHook();

            // Check if current path subkey exists in registry. 
            RegistryManagement.CheckRegKeysStart(s_listReg, GlobalVariables.regKeyName, "", false);

            // Setting up the title.
            Console.Title = s_terminalTitle;

            // Start xTerminal close event.
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(Shell_Close);

            // Read commands history
            if (File.Exists(s_historyFile))
            {
                var historyStored = File.ReadAllText(s_historyFile);
                FileSystem.ReadStringLine(ref _history, historyStored, true);
            }

            if (ExecuteParamCommands(args)) { return; };

            // We loop until exit commands is hit
            do
            {
                //Load predifined settings.
                SettingsLoad();

                // We se the color and user loged in on console.
                SetConsoleUserConnected(s_currentDirectory, s_accountName, s_computerName, s_regUI, s_regUIcd);

                // Reading user imput.
                try
                {
                    s_input = Read();
                }
                catch
                {
                    s_input = "";
                    Console.WriteLine();
                }


                //cleaning input
                s_input = s_input.Trim();

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

        /// <summary>
        /// Read command input and check keys handler.
        /// </summary>
        /// <param name="prompt"></param>
        /// <param name="default"></param>
        /// <returns></returns>
        public static string Read(string prompt = "", string @default = "")
        {
            Console.Write(prompt);
            KeyHandler keyHandler = new KeyHandler(new Core.Abstractions.Console2(), _history, AutoCompletionHandler);
            string text = GetText(keyHandler);
            if (String.IsNullOrWhiteSpace(text) && !String.IsNullOrWhiteSpace(@default))
            {
                text = @default;
            }
            if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrEmpty(text))
            {
                text = text.Replace("\b", "");
                text = text.Replace("\0", "");
                text = text.Replace("\t", "");
                text = text.Replace("\r", "");
                text = text.Replace("\n", "");
                text = text.Replace("\u0018", "");
                _history.Add(text);
            }
            return text;
        }

        /// <summary>
        /// Get text from input key.
        /// </summary>
        /// <param name="keyHandler"></param>
        /// <returns></returns>
        private static string GetText(KeyHandler keyHandler)
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
            while (keyInfo.Key != ConsoleKey.Enter)
            {
                keyHandler.Handle(keyInfo);
                keyInfo = Console.ReadKey(true);
                if (keyInfo.Key != ConsoleKey.Tab)
                    GlobalVariables.tabPressCount = 0;
            }
            Console.WriteLine();
            return keyHandler.Text;
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
                    tempList.Add($"<< {DateTime.UtcNow} >> {commandInput}");
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

