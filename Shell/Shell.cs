﻿using Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SystemCmd = Core.Commands.SystemCommands;
using SetConsoleColor = Core.SystemTools.UI;
using System.Threading.Tasks;

namespace Shell
{

    public class Shell
    {
        //declaring variables
        public static string dlocation = null;
        private static readonly string s_accountName = GlobalVariables.accountName;    //extract current loged username
        private static readonly string s_computerName = GlobalVariables.computerName; //extract machine name
        private static string s_input = null;
        private static string s_historyFilePath = GlobalVariables.historyFilePath;
        private static List<string> s_listReg = new List<string>() { "CurrentDirectory", "UI" };
        private static string s_historyFile = GlobalVariables.historyFile;
        private static string s_addonDir = GlobalVariables.addonDirectory;
        private static string s_regUI = "";
        private static string s_indicator = "$";
        private static string s_indicatorColor = "white";
        private static string s_userColor = "green";
        private static int s_userEnabled = 1;
        private static string s_cdColor = "cyan";

        //-------------------------------

        //Define the shell commands 
        private Dictionary<string, string> Aliases = new Dictionary<string, string>
        {
            { "cmd", "cmd"  },
            { "ps", "powershell"  },
            { "speedtest", @".\Tools\netcoreapp3.1\TestNet.exe"  }
        };
        //-----------------------


        //Entry point of shell

        public void Run()
        {
            // Check if current path subkey exists in registry. 
            RegistryManagement.CheckRegKeysStart(s_listReg, GlobalVariables.regKeyName, "", false);
            RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory, GlobalVariables.rootPath); // write root path of c


            // Creating the history file directory in USERPROFILE\AppData\Local if not exist.
            Directory.CreateDirectory(s_historyFilePath);

            // Creating the addon directory for C# code script scomands if not exist.
            Directory.CreateDirectory(s_addonDir);


            //creating history file if not exist
            if (!File.Exists(s_historyFile))
            {
                File.WriteAllText(s_historyFile, Environment.NewLine);
            }

            // We loop unti exit commands is hit
            do
            {
                //reading current location
                dlocation = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory);

                if (dlocation == "")
                {
                    RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory, GlobalVariables.rootPath);
                }

                // Reading UI settings.
                s_regUI = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regUI);
                if (s_regUI == "")
                {
                    RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regUI, @"green;1|white;$|cyan");
                }


                // We se the color and user loged in on console.
                SetConsoleUserConnected(dlocation, s_accountName, s_computerName, s_regUI);

                //reading user imput
                s_input = Console.ReadLine();

                //cleaning input
                s_input = s_input.Trim();


                if (File.Exists(s_historyFile))
                {

                    WriteHistoryCommandFile(s_historyFile, s_input);

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
                    else if (s_input.Contains("speedtest"))
                    {
                        Execute(s_input, "", true);
                    }
                    else if (s_input.StartsWith("cmd"))
                    {
                        Execute(s_input, s_input, true);
                    }
                    else if (s_input.StartsWith("ps"))
                    {
                        Execute(s_input, s_input, true);
                    }
                }

                // New command implementation by Scott
                var c = Commands.CommandRepository.GetCommand(s_input);
                if (c != null)
                {
                    c.Execute(s_input);
                }
                //----------------------------------------
                GC.Collect();

            } while (s_input != "exit");
        }

        //------------------

        //process execute 
        public void Execute(string input, string args, bool waitForExit)
        {
            input = input.Split(' ')[0];
            if (Aliases.Keys.Contains(input))
            {
                var process = new Process();

                if (input == "cmd" || input == "ps")
                {
                    args = args.Replace("cmd ", "");
                    args = args.Replace("ps ", "");
                    process.StartInfo = new ProcessStartInfo(Aliases[input])
                    {
                        UseShellExecute = false,
                        WorkingDirectory = dlocation,
                        Arguments = "/c " + args
                    };
                    process.Start();
                    if (waitForExit)
                        process.WaitForExit();
                    return;
                }
                process.StartInfo = new ProcessStartInfo(Aliases[input])
                {
                    UseShellExecute = false,
                    Arguments = args
                };
                if (File.Exists(Aliases[input]))
                {
                    process.Start();
                    if (waitForExit)
                        process.WaitForExit();
                }
                else
                    FileSystem.ErrorWriteLine($"Couldn't find file \"{Aliases[input]}\" to execute. Reinstalling should fix the issue ");
                return;
            }
        }
        //------------------------

        // We set the name of the current user logged in and machine on console.
        private static void SetConsoleUserConnected(string currentLocation, string accountName, string computerName, string uiSettings)
        {
            if (uiSettings != "")
            {
                UISettingsParse(uiSettings);
            }

            if (currentLocation.Contains(GlobalVariables.rootPath) && currentLocation.Length == 3)
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
                        FileSystem.ColorConsoleText(SetConsoleColor.SetConsoleColor(s_userColor), $"{accountName }@{computerName}:");
                    }
                    else
                    {
                        FileSystem.ColorConsoleText(ConsoleColor.Green, $"{accountName }@{computerName}:");
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
                        FileSystem.ColorConsoleText(SetConsoleColor.SetConsoleColor(s_indicatorColor), $"{s_indicator} ");
                    }
                    else
                    {
                        FileSystem.ColorConsoleText(ConsoleColor.White, $"{s_indicator} ");
                    }
                }
                else
                {
                    FileSystem.ColorConsoleText(ConsoleColor.White, "$ ");
                }
                return;
            }
            if (s_userEnabled == 1)
            {
                if (s_userColor != "green")
                {
                    FileSystem.ColorConsoleText(SetConsoleColor.SetConsoleColor(s_userColor), $"{accountName }@{computerName}:");
                }
                else
                {
                    FileSystem.ColorConsoleText(ConsoleColor.Green, $"{accountName }@{computerName}:");
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
                    FileSystem.ColorConsoleText(SetConsoleColor.SetConsoleColor(s_indicatorColor), $"{s_indicator} ");
                }
                else
                {
                    FileSystem.ColorConsoleText(ConsoleColor.White, $"{s_indicator} ");
                }
            }
            else
            {
                FileSystem.ColorConsoleText(ConsoleColor.White, "$ ");
            }
            Console.Title = $"{GlobalVariables.terminalTitle} | {currentLocation}";//setting up the new title
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
            int countLines = File.ReadAllLines(historyFile).Count();
            var lines = File.ReadAllLines(historyFile).Skip(countLines - 99);
            List<string> tempList = new List<string>();

            for (int i = 0; i < lines.Count(); i++)
            {
                tempList.Add(lines.ElementAt(i));
            }

            if (!commandInput.Contains("hcmd") && !commandInput.Contains("chistory"))
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

