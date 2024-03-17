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
        private static List<string> s_listReg = new List<string>() { "UI" };
        private static string s_historyFile = GlobalVariables.historyFile;
        private static string s_addonDir = GlobalVariables.addonDirectory;
        private static string s_regUI = "";
        private static string s_indicator = "$";
        private static string s_indicatorColor = "white";
        private static string s_userColor = "green";
        private static int s_userEnabled = 1;
        private static string s_cdColor = "cyan";
        private static string s_historyLimitSize = "2000";
        private static int s_ctrlKey = 1;
        private static int s_xKey = 0;
        private static string s_terminalTitle = $"xTerminal {Application.ProductVersion}";
        private static string s_aliasFile = GlobalVariables.aliasFile;

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
        /// CTRL+X key event.
        /// </summary>
        /// <param name="e"></param>
        static void KeyDown(KeyEventArgs e)
        {
            bool topMost = TopMost.ApplicationIsActivated();
            if (topMost)
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
                    s_intercept += e.KeyData.ToString().ToLower();
                    //Reset flags for reuse.
                    s_xKey = 0;
                }

                if (e.KeyCode == Keys.Back && !string.IsNullOrEmpty(s_intercept))
                    s_intercept = s_intercept.Substring(0, s_intercept.Length - 1);

                if (e.KeyCode == Keys.Space)
                    s_intercept += " ";

                if (e.KeyCode == Keys.Tab)
                    s_intercept += " ";

                if (e.KeyData == Keys.Enter)
                {
                    s_intercept = "";
                    s_ctrlCount = 0;
                }

                if (e.KeyData == Keys.X)
                    s_xKey = DateTime.Now.Second;


                if (e.KeyData.ToString() == "RControlKey" || e.KeyData.ToString() == "LControlKey")
                {
                    s_ctrlKey = DateTime.Now.Second;
                    s_ctrlCount++;

                    if (s_ctrlCount == 2 && !string.IsNullOrEmpty(s_intercept))
                    {
                        //Auto sugestion commands
                        Core.Commands.AutoSuggestionCommands.FileDirSuggestion(s_intercept, "cd", s_currentDirectory, false);
                        Core.Commands.AutoSuggestionCommands.FileDirSuggestion(s_intercept, "odir", s_currentDirectory, false);
                        Core.Commands.AutoSuggestionCommands.FileDirSuggestion(s_intercept, "ls", s_currentDirectory, false);
                        Core.Commands.AutoSuggestionCommands.FileDirSuggestion(s_intercept, "hex", s_currentDirectory, true);
                        Core.Commands.AutoSuggestionCommands.FileDirSuggestion(s_intercept, "./", s_currentDirectory, true);
                        Core.Commands.AutoSuggestionCommands.FileDirSuggestion(s_intercept, "ccs", s_currentDirectory, true);
                        Core.Commands.AutoSuggestionCommands.FileDirSuggestion(s_intercept, "fcopy", s_currentDirectory, true);
                        Core.Commands.AutoSuggestionCommands.FileDirSuggestion(s_intercept, "mv", s_currentDirectory, true);
                        Core.Commands.AutoSuggestionCommands.FileDirSuggestion(s_intercept, "fmove", s_currentDirectory, true);
                        Core.Commands.AutoSuggestionCommands.FileDirSuggestion(s_intercept, "del", s_currentDirectory, false);
                        Core.Commands.AutoSuggestionCommands.FileDirSuggestion(s_intercept, "del", s_currentDirectory, true);
                        Core.Commands.AutoSuggestionCommands.FileDirSuggestion(s_intercept, "edit", s_currentDirectory, true);
                        Core.Commands.AutoSuggestionCommands.FileDirSuggestion(s_intercept, "cp", s_currentDirectory, false);
                        Core.Commands.AutoSuggestionCommands.FileDirSuggestion(s_intercept, "cp", s_currentDirectory, true);
                        Core.Commands.AutoSuggestionCommands.FileDirSuggestion(s_intercept, "md5", s_currentDirectory, true);
                        Core.Commands.AutoSuggestionCommands.FileDirSuggestion(s_intercept, "sort", s_currentDirectory, true);
                        Core.Commands.AutoSuggestionCommands.FileDirSuggestion(s_intercept, "cat", s_currentDirectory, true);

                        //Reset flags.
                        s_ctrlCount = 0;
                        s_intercept = GlobalVariables.commandOut;
                    }
                }



                if ((s_xKey == s_ctrlKey) && GlobalVariables.eventKeyFlagX)
                {
                    GlobalVariables.eventKeyFlagX = false;
                    GlobalVariables.eventCancelKey = true;

                    //Reset flags for reuse.
                    s_xKey = 0;
                    s_ctrlKey = 1;
                }
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
            s_terminalTitle = s_terminalTitle.Substring(0, s_terminalTitle.Length - 2);
            Console.Title = s_terminalTitle;

            if (ExecuteParamCommands(args)) { return; };

            // We loop until exit commands is hit
            do
            {
                //Load predifined settings.
                SettingsLoad();

                // We se the color and user loged in on console.
                SetConsoleUserConnected(s_currentDirectory, s_accountName, s_computerName, s_regUI);

                //reading user imput
                s_input = Console.ReadLine();

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

                    //rebooting the machine command
                    if (s_input == "reboot")
                    {
                        SystemCmd.RebootCmd();
                    }

                    //shuting down the machine command
                    else if (s_input == "shutdown")
                    {
                        SystemCmd.ShutDownCmd();
                    }
                    //log off the machine command
                    else if (s_input == "logoff")
                    {
                        SystemCmd.LogoffCmd();
                    }
                    else if (s_input == "lock")
                    {
                        SystemCmd.LockCmd();
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
                        }catch(Exception e)
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
        private static void SetConsoleUserConnected(string currentLocation, string accountName, string computerName, string uiSettings)
        {
            if (uiSettings != "")
            {
                UISettingsParse(uiSettings);
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

        private static void SetUser(string accountName, string computerName, string currentLocation, bool currentDir)
        {
            if (currentDir == false)
            {
                if (s_userEnabled == 1)
                {
                    if (s_userColor != "green")
                    {
                        FileSystem.ColorConsoleText(SetConsoleColor.SetConsoleColor(s_userColor), $"{accountName}@{computerName}:");
                    }
                    else
                    {
                        FileSystem.ColorConsoleText(ConsoleColor.Green, $"{accountName}@{computerName}:");
                    }
                }

                if (s_cdColor != "cyan")
                {
                    FileSystem.ColorConsoleText(SetConsoleColor.SetConsoleColor(s_cdColor), $"~");
                }
                else
                {
                    FileSystem.ColorConsoleText(ConsoleColor.Cyan, $"~");
                }
                if (!string.IsNullOrEmpty(s_indicator))
                {
                    if (s_indicatorColor != "white")
                    {
                        FileSystem.ColorConsoleText(SetConsoleColor.SetConsoleColor(s_indicatorColor), $" {s_indicator} ");
                    }
                    else
                    {
                        FileSystem.ColorConsoleText(ConsoleColor.White, $" {s_indicator} ");
                    }
                }
                else
                {
                    FileSystem.ColorConsoleText(ConsoleColor.White, " $ ");
                }
                return;
            }
            if (s_userEnabled == 1)
            {
                if (s_userColor != "green")
                {
                    FileSystem.ColorConsoleText(SetConsoleColor.SetConsoleColor(s_userColor), $"{accountName}@{computerName}:");
                }
                else
                {
                    FileSystem.ColorConsoleText(ConsoleColor.Green, $"{accountName}@{computerName}:");
                }
            }
            if (s_cdColor != "cyan")
            {
                FileSystem.ColorConsoleText(SetConsoleColor.SetConsoleColor(s_cdColor), $"{currentLocation}~");
            }
            else
            {
                FileSystem.ColorConsoleText(ConsoleColor.Cyan, $"{currentLocation}~");
            }
            if (!string.IsNullOrEmpty(s_indicator))
            {
                if (s_indicatorColor != "white")
                {
                    FileSystem.ColorConsoleText(SetConsoleColor.SetConsoleColor(s_indicatorColor), $" {s_indicator} ");
                }
                else
                {
                    FileSystem.ColorConsoleText(ConsoleColor.White, $" {s_indicator} ");
                }
            }
            else
            {
                FileSystem.ColorConsoleText(ConsoleColor.White, " $ ");
            }
        }
        private static void UISettingsParse(string settings)
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
        }

        /// <summary>
        /// Write terminal input commands to history file. 
        /// </summary>
        /// <param name="historyFile"></param>
        /// <param name="commandInput"></param
        private void WriteHistoryCommandFile(string historyFile, string commandInput)
        {
            s_historyLimitSize  = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regHistoryLimitSize);
            int historyLimitSize = GlobalVariables.historyLimitSize;
            if (s_historyLimitSize != "")
                 historyLimitSize = Int32.Parse(s_historyLimitSize);
            int countLines = File.ReadAllLines(historyFile).Count();
            var lines = File.ReadAllLines(historyFile).Skip(countLines - historyLimitSize);
            List<string> tempList = new List<string>();

            for (int i = 0; i < lines.Count(); i++)
            {
                if(!string.IsNullOrEmpty(lines.ElementAt(i)))
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

