using Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SystemCmd = Core.Commands.SystemCommands;

namespace Shell
{

    public class Shell
    {
        //declaring variables
        public static string dlocation = null;
        private static readonly string s_accountName = Environment.UserName;    //extract current loged username
        private static readonly string s_computerName = Environment.MachineName; //extract machine name
        private static string s_input = null;
        private static string s_historyFilePath = $"C:\\Users\\{s_accountName}\\AppData\\Local\\xTerminal";
        private static List<string> s_listReg = new List<string>() { "CurrentDirectory" };
        private static string s_historyFile = s_historyFilePath + "\\History.db";
        private static string s_count = null;
        private static string[] s_hLines = null;
        private static string s_hContent = null;
        private static string[] s_pLines = null;
        private static string s_cID = null;
        private static int s_iID = 0;
        private static string[] s_cContent = null;
        private static string[] s_clines = null;
        private static string s_oID = null;
        private static int s_ioID = 0;
        private static int s_uPcount;

        //-------------------------------
        //Define the shell commands 
        private Dictionary<string, string> Aliases = new Dictionary<string, string>
        {
            { "cmd", "cmd"  },
            { "ps", "powershell"  },
            { "speedtest", @".\Tools\netcoreapp3.1\TestNet.exe"  }
        };
        //-----------------------

        // We check if history file has any data in it.
        private static bool CheckHistoryFileLength(string historyFileName)
        {
            if (!File.Exists(historyFileName))
            {
                Console.WriteLine("History file not exists!");
                return false;
            }
            using (StringReader stringReader = new StringReader(historyFileName))
            {
                string historFileData = stringReader.ReadToEnd();
                if (historFileData.Length > 0)
                {
                    return true;
                }
                Console.WriteLine("No commands in list!");
                return false;
            }
        }


        //Entry point of shell

        public void Run()
        {
            // Check if current path subkey exists in registry. 
            RegistryManagement.CheckRegKeysStart(s_listReg, GlobalVariables.regKeyName, "", false);
            RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory, @"C:\"); // write root path of c


            // Creating the history file directory in USERPROFILE\AppData\Local if not exist.


            if (!Directory.Exists(s_historyFilePath))
            {
                Directory.CreateDirectory(s_historyFilePath);
            }

            //creating history file if not exist
            // Output NIC's configuration
            if (!File.Exists(s_historyFile))
            {
                File.WriteAllText(s_historyFile, "0|0" + Environment.NewLine);
            }

            //Reading lines and count
            s_cContent = File.ReadAllLines(s_historyFile);

            foreach (var line in s_cContent)
            {
                s_clines = line.Split('|');
                s_oID = s_clines[0];
                s_ioID = Convert.ToInt32(s_oID);
                s_uPcount = s_ioID;
            }


            // We loop unti exit commands is hit
            do
            {
                //reading current location
                dlocation = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory);
                if (dlocation == "")
                {
                    RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory, @"C:\");
                }

                // We se the color and user loged in on console.
                SetConsoleUserConnected(dlocation, s_accountName, s_computerName);

                //reading user imput
                s_input = Console.ReadLine();

                //cleaning input
                s_input = s_input.Trim();


                // New command implementation by Scott

                var c = Commands.CommandRepository.GetCommand(s_input);
                if (c != null)
                {
                    c.Execute(s_input);
                }
                //----------------------------------------



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
                    // Open current directory in Windows Explorer.
                    else if (s_input == "odir")
                    {
                        FileSystem.OpenCurrentDiretory(dlocation);
                    }
                    //Clear command history file
                    else if (s_input == "chistory")
                    {
                        if (File.Exists(s_historyFile))
                        {
                            File.WriteAllText(s_historyFile, string.Empty);
                            Console.WriteLine("Command history log cleared!");
                        }
                        else
                        {
                            Console.WriteLine("File '" + s_historyFile + "' dose not exist!");
                        }
                    }
                    else if (s_input.Contains("speedtest"))
                    {
                        Execute(s_input, "", true);
                    }
                    else if (s_input.Contains("cmd"))
                    {
                        Execute(s_input, "", true);
                    }
                    else if (s_input.Contains("ps"))
                    {
                        Execute(s_input, "", true);
                    }
                }

            } while (s_input != "exit");
        }

        //------------------

        //process execute 
        public void Execute(string input, string args, bool waitForExit)
        {
            if (Aliases.Keys.Contains(input))
            {
                var process = new Process();

                if (input == "cmd" || input == "ps")
                {
                    process.StartInfo = new ProcessStartInfo(Aliases[input])
                    {
                        UseShellExecute = false,
                        WorkingDirectory = dlocation
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

            // return 1;
        }
        //------------------------

        // We set the name of the current user logged in and machine on console.
        private static void SetConsoleUserConnected(string currentLocation, string accountName, string computerName)
        {
            if (currentLocation.Contains(GlobalVariables.rootPath) && currentLocation.Length == 3)
            {
                FileSystem.ColorConsoleText(ConsoleColor.Green, $"{accountName }@{computerName}");
                FileSystem.ColorConsoleText(ConsoleColor.White, ":");
                FileSystem.ColorConsoleText(ConsoleColor.Cyan, $"~");
                FileSystem.ColorConsoleText(ConsoleColor.White, "$ ");
            }
            else
            {
                FileSystem.ColorConsoleText(ConsoleColor.Green, $"{accountName }@{computerName}");
                FileSystem.ColorConsoleText(ConsoleColor.White, ":");
                FileSystem.ColorConsoleText(ConsoleColor.Cyan, $"{currentLocation}~");
                FileSystem.ColorConsoleText(ConsoleColor.White, "$ ");
                Console.Title = $"{GlobalVariables.terminalTitle} | {currentLocation}";//setting up the new title
            }
        }

        /// <summary>
        /// Write terminal input commands to history file. 
        /// </summary>
        /// <param name="historyFile"></param>
        /// <param name="commandInput"></param>
        private void WriteHistoryCommandFile(string historyFile, string commandInput)
        {
            //reading file content
            s_hContent = File.ReadAllText(historyFile);

            //cheking if file is empy
            if (s_hContent == string.Empty)
            {
                //writing dummy line if file is emtpy
                File.WriteAllText(historyFile, "0|0" + Environment.NewLine);
            }
            s_hLines = File.ReadAllLines(historyFile); //reading lines from file
            foreach (var line in s_hLines)
            {
                //parsing every line
                s_pLines = line.Split('|');

                //reading first string until space to determinate the last id
                s_cID = s_pLines[0];

                //convert first string to int                       
                s_iID = Convert.ToInt32(s_cID);
            }
            // Checking if input is empty or starts with 'hcmd' for history display. If true do not write in.
            if ((commandInput != string.Empty) && (!commandInput.StartsWith("hcmd")))
            {
                //increment value for new saved comnad
                s_iID++;

                //converting int back to string to be saved 
                s_count = Convert.ToString(s_iID);

                //writing command with id to file
                File.AppendAllText(historyFile, s_count + "|" + commandInput + Environment.NewLine);
            }

            //reading all lines for count save
            s_cContent = File.ReadAllLines(historyFile);

            foreach (var line in s_cContent)
            {
                //spliting the lines to get id's
                s_clines = line.Split('|');

                //reading first part with id's
                s_oID = s_clines[0];

                //converting to int32 for future use
                s_ioID = Convert.ToInt32(s_oID);
                s_uPcount = s_ioID;
            }
        }
    }
}

