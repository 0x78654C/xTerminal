using Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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
            { "ls", @".\Tools\FileSystem\ListDirectories.exe" },
            { "clear", @".\Tools\Internal\Clear.exe" },
            { "extip", @".\Tools\Network\Externalip.exe" },
            { "ispeed", @".\Tools\Network\InternetSpeed.exe" },
            { "icheck", @".\Tools\Network\CheckDomain.exe" },
            { "md5", @".\Tools\FileSystem\CheckMD5.exe"  },
            { "fcopy", @".\Tools\FileSystem\FCopy.exe"  },
            { "fmove", @".\Tools\FileSystem\FMove.exe"  },
            { "frename", @".\Tools\FileSystem\FRename.exe"  },
            { "cmd", "cmd"  },
            { "ps", "powershell"  },
            { "cd", @".\Tools\FileSystem\CDirectory.exe"  },
            { "cat", @".\Tools\FileSystem\StringView.exe"  },
            { "del", @".\Tools\FileSystem\Delete.exe"  },
            { "mkdir", @".\Tools\FileSystem\MakeDirectory.exe"  },
            { "mkfile", @".\Tools\FileSystem\MKFile.exe"  },
            { "speedtest", @".\Tools\netcoreapp3.1\TestNet.exe"  },
            { "email", @".\Tools\Network\eMailS.exe"  },
            { "wget", @".\Tools\Network\WGet.exe"  },
            { "edit", @".\Tools\FileSystem\xEditor.exe"  },
            { "cp", @".\Tools\FileSystem\CheckPermission.exe"  },
            { "bios", @".\Tools\Hardware\BiosInfo.exe"  },
            { "sinfo", @".\Tools\Hardware\sdc.exe"  },
            { "flappy", @".\Tools\Game\FlappyBirds.exe"  }
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

        /// <summary>
        /// Output the commands from history.
        /// </summary>
        /// <param name="historyFileName">Path to history command file.</param>
        /// <param name="linesNumber">Number of commnands to be displayed.</param>
        private static void OutputHistoryCommands(string historyFileName, int linesNumber)
        {

            if (CheckHistoryFileLength(historyFileName) == false)
            {
                return;
            }

            if (linesNumber > 100)
            {
                Console.WriteLine("Only 100 commands can be displayed!");
                return;
            }

            int index = 1;
            string line;
            var lines = File.ReadLines(historyFileName);
            int countLines = lines.Count();

            do
            {
                line = lines.Skip(countLines - index).FirstOrDefault();
                if (line == null)
                {
                    index++;
                    continue;
                }
                line = line.Split('|').Skip(1).FirstOrDefault();
                if (line != null)
                {
                    FileSystem.ColorConsoleText(ConsoleColor.White, "--> ");
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Magenta, line);
                }
                index++;

            } while (index != linesNumber + 1);
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

                // History commands display.
                if (s_input.StartsWith("hcmd"))
                {
                    try
                    {
                        string cmd = s_input.Split(' ').Skip(1).FirstOrDefault();

                        if (Int32.TryParse(cmd, out var position))
                        {
                            OutputHistoryCommands(s_historyFile, position);
                        }
                        else
                        {
                            OutputHistoryCommands(s_historyFile, 1);
                        }
                    }
                    catch (Exception e)
                    {
                        FileSystem.ErrorWriteLine($"Error: {e.Message}");
                    }
                }

                // Start application commnad

                if (s_input.StartsWith("start"))
                {
                    s_input = s_input.Replace("start ", "");
                    if (s_input.Contains(@"\"))
                    {
                        StartApplication(s_input);
                    }
                    else
                    {
                        StartApplication(dlocation + @"\\" + s_input);
                    }
                }

                if (s_input == "ifconfig")
                    Console.WriteLine(NetWork.ShowNicConfiguragion());

                if (File.Exists(s_historyFile))
                {


                    WriteHistoryCommandFile(s_historyFile, s_input);

                    //help command

                    if (s_input == "help")
                    {
                        string helpMGS = @"
xTerminal v1.0 Copyright @ 2020-2021 0x078654c
This is the full list of commands that can be used in xTerminal:

    ls -- List directories and files on a directory. (ls -s for size display)
    hcmd -- Displays a list of previous commands typed in terminal. Ex.: hcmd 10 -> displays last 10 commands used. 
    clear --  Cleares the console.
    cd -- Sets the currnet directory.
    ispeed -- Checks the internet speed with Google.
    icheck -- Checks if a Domain or IP address is online.
    md5 -- Checks the MD5 checksum of a file.
    extip -- Displays the current external IP address.
    wget -- Download files from a specific website.
    fcopy -- Copies a file with CRC checksum control.
    fmove -- Moves a file with md5 checksum control.
    frename -- Renames a file in a specific directory(s).
    cmd --  Opens Windows Command Prompt.
    ps -- Opens Windowns Powershell.
    cat -- Displays the content of a file.
    del -- Deletes a file or folder without recover.
    reboot -- It force reboots the Windows OS.
    shutdown --  It force shutdown the Windows OS.
    logoff -- It force logoff current user.
    bios -- Displays BIOS information on local machine or remote.
    sinfo -- Displays Storage devices information on local machine or remote.
    mkdir -- It creates a directory in the current place.
    mkfile -- It creates a file in the current place.
    edit -- Opens a file in Notepad(default). To set a new text editor you must use following command: edit set ""Path to editor""
    cp -- Check file/folder permissions.
    speedtest -- Makes an internet speed test based on speedtest.net API.
    email -- Email sender client for Microsoft (all), Yahoo, Gmail!
    chistory -- Clears the current history of commands!
    start -- Starts an application. Ex.: start C:\Windows\System32\notepad.exe
    flappy -- Play Flappy Birds in console!
 

                        ";

                        Console.WriteLine(helpMGS);
                    }
                    //rebooting the machine command
                    else if (s_input == "reboot")
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
                    //editor set
                    else if (s_input.Contains("edit set"))
                    {
                        SetTextEditor(s_input);
                    }
                    else
                    {
                        if (s_input.Contains(" "))
                        {
                            string[] dInput = s_input.Split(' ');

                            //counting the spaces in input command
                            string _ck = Regex.Matches(s_input, " ").Count.ToString();
                            int _ch = Int32.Parse(_ck);

                            if (_ch == 1)// check one space char in input
                            {
                                ExecuteWithArgs(dInput[0], dInput[1], true); //execute commands with 1 arg
                            }
                            else if (_ch == 2)// check one space char in input
                            {
                                ExecuteWithArgs2(dInput[0], dInput[1], dInput[2], true); //execute commands with 2 args
                            }
                        }
                        else
                        {
                            Execute(s_input, true); //run simple command
                        }
                    }
                }


            } while (s_input != "exit");

        }


        //------------------

        //process execute 
        public void Execute(string input, bool waitForExit)
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
                process.StartInfo = new ProcessStartInfo(Aliases[input])
                {
                    UseShellExecute = false,
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

        //process execute 
        public void ProcessExecute(string input, string arguments)
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo(input)
            {
                Arguments = arguments
            };
            if (File.Exists(input))
                process.Start();
            else
                FileSystem.ErrorWriteLine($"Couldn't find file \"{input}\" to execute");
            return;
        }

        //process execute  with 1 arg
        public int ExecuteWithArgs(string input, string args, bool waitForExit)
        {
            if (Aliases.Keys.Contains(input))
            {
                var process = new Process();
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

                return 0;
            }

            return 1;
        }
        //------------------------

        //process execute  with 2 arg
        public int ExecuteWithArgs2(string input, string args, string args2, bool waitForExit)
        {
            if (Aliases.Keys.Contains(input))
            {
                var process = new Process();
                process.StartInfo = new ProcessStartInfo(Aliases[input])
                {
                    UseShellExecute = false,
                    Arguments = args + " " + args2
                };
                if (File.Exists(Aliases[input]))
                {
                    process.Start();
                    if (waitForExit)
                        process.WaitForExit();
                }
                else
                    FileSystem.ErrorWriteLine($"Couldn't find file \"{Aliases[input]}\" to execute. Reinstalling should fix the issue ");
                return 0;
            }

            return 1;
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


        /// <summary>
        /// Set specific text editor for 'edit' command.
        /// </summary>
        /// <param name="inputCommand"></param>
        private void SetTextEditor(string inputCommand)
        {
            string[] dInput = inputCommand.Split(' ');
            //counting the spaces in input command
            string _ck = Regex.Matches(inputCommand, " ").Count.ToString();
            int _ch = Int32.Parse(_ck);
            try
            {
                if (_ch > 1)
                {
                    if (inputCommand.Contains(@"\"))
                    {
                        string[] cInput = inputCommand.Split('"');
                        ExecuteWithArgs2(dInput[0], dInput[1], "\"" + @cInput[1] + "\"", true); //execute commands with 1 arg
                    }
                    else
                    {

                        ExecuteWithArgs2(dInput[0], dInput[1], dInput[2], true); //execute commands with 2 arg
                    }
                }
                else
                {
                    ExecuteWithArgs(dInput[0], dInput[1], true); //execute commands with 1 arg
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine($"{e.Message} Check command. The path must be between double commas!");
            }
        }

        private void StartApplication(string inputCommand)
        {
            string[] dInput = inputCommand.Split(' ');
            string _ck = Regex.Matches(inputCommand, " ").Count.ToString();
            int _ch = Int32.Parse(_ck);
            try
            {
                if (_ch == 1)
                {
                    if (!File.Exists(dInput[0]))
                    {
                        FileSystem.ErrorWriteLine($"File {dInput[0]} does not exist!");
                        return;
                    }
                    ProcessExecute(dInput[0], dInput[1]); //execute commands with 2 arg
                }
                else
                {
                    if (!File.Exists(dInput[0]))
                    {
                        FileSystem.ErrorWriteLine($"File {dInput[0]} does not exist!");
                        return;
                    }
                    ProcessExecute(inputCommand, "");
                }

            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }
    }
}

