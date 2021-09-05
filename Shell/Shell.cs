using Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Shell
{

    public class Shell
    {
        //declaring variables
        public static string dlocation = null;
        static string AccountName = Environment.UserName;    //extract current loged username
        static string ComputerName = Environment.MachineName; //extract machine name
        BackgroundWorker woker;                         //Declare backgroudwoker for key input listener

        string input = null;
        static string _historyFile = Directory.GetCurrentDirectory() + @"\Data\History.db";
        static string count = null;
        static string[] hLines = null;
        static string hContent = null;
        static string[] pLines = null;
        static string cID = null;
        static int iID = 0;
        static string[] cContent = null;
        static string[] clines = null;
        static string oID = null;
        static int ioID = 0;
        static int uPcount = 0;

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



        //Userd for key event check listing files and directories to be suggested for autocomplete
        private static void KeyDown(KeyEventArgs e)
        {

            //check if keycode is PageUp key presed and use for execution command
            if (e.KeyCode == Keys.PageUp)
            {
                if (File.Exists(_historyFile))
                {
                    //checking ioID is grater than 0(since id 0 is dummy line and is not needed )
                    //and lower or equal to maximum count of lines                    
                    if (ioID > 0 && ioID <= uPcount)
                    {
                        //returning the line determinated by the inceremented ioID
                        string readCommands = File.ReadLines(_historyFile).Skip(ioID).Take(1).First();

                        //parsing the line
                        string[] parseCommands = readCommands.Split('|');

                        //returning the part of line after the char '|'
                        //from the parsed line so we don't display the id of line
                        string outCommand = parseCommands[1];

                        //output the final parsed line

                        Console.Write("\n{0}", outCommand);

                        //decrement the ioID by 1
                        ioID--;
                    }
                }


            }

            else if (e.KeyCode == Keys.PageDown)
            {
                if (File.Exists(_historyFile))
                {
                    //checking id ioID is lower and not equal than maximum count of lines
                    if (uPcount > ioID && ioID != uPcount)
                    {
                        //incrementing ioID by 1
                        ioID++;

                        //returning the line determinated by the inceremented ioID
                        string readCommands = File.ReadLines(_historyFile).Skip(ioID).Take(1).First();

                        //parsing the line
                        string[] parseCommands = readCommands.Split('|');

                        //returning the part of line after the char '|'
                        //from the parsed line so we don't display the id of line
                        string outCommand = parseCommands[1];

                        //output the final parsed line
                        Console.Write("\n{0}", outCommand);

                    }
                    else
                    {
                        //restoring ioID to the maximum numbers of lines
                        ioID = uPcount;

                    }

                }
            }

            e.Handled = true;

        }

        /// <summary>
        /// On press key intercept
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Woker_DoWork(object sender, DoWorkEventArgs e)
        {
            InterceptKeys.SetupHook(KeyDown);
            InterceptKeys.ReleaseHook();
        }


        //Entry point of shell

        public void Run()
        {

            woker = new BackgroundWorker();
            woker.DoWork += Woker_DoWork;
            woker.RunWorkerAsync();



            //creating history file if not exist
            if (!File.Exists(_historyFile))
            {
                File.WriteAllText(_historyFile, "0|0" + Environment.NewLine);
            }

            //Reading lines and count
            cContent = File.ReadAllLines(_historyFile);

            foreach (var line in cContent)
            {
                clines = line.Split('|');
                oID = clines[0];
                ioID = Convert.ToInt32(oID);
                uPcount = ioID;
            }

            do
            {

                //reading current location
                dlocation = File.ReadAllText(GlobalVariables.currentLocation);
                SetConsoleUserConnected(dlocation);

                //reading user imput
                input = Console.ReadLine();

                //cleaning input
                input.Trim();

                #region History showing on key up and down
                //show the commands from last line to first line
                if (input == "ku")
                {
                    if (File.Exists(_historyFile))
                    {

                        //check if are commands in list
                        string fileRead = File.ReadAllText(_historyFile);
                        if (fileRead.Length == 0)
                        {
                            Console.WriteLine("No commands in list!");
                        }
                        else
                        {

                            //checking ioID is grater than 0(since id 0 is dummy line and is not needed )
                            //and lower or equal to maximum count of lines
                            if (ioID > 0 && ioID <= uPcount)
                            {
                                //returning the line determinated by the inceremented ioID
                                string readCommands = File.ReadLines(_historyFile).Skip(ioID).Take(1).First();

                                //parsing the line
                                string[] parseCommands = readCommands.Split('|');

                                //returning the part of line after the char '|'
                                //from the parsed line so we don't display the id of line
                                string outCommand = parseCommands[1];

                                //output the final parsed line
                                Console.WriteLine(outCommand);

                                //decrement the ioID by 1
                                ioID--;
                            }
                        }
                    }
                }
                //go back to last command from last point of 'ku' used
                else if (input == "kd")
                {
                    if (File.Exists(_historyFile))
                    {
                        //check if are commands in list
                        string fileRead = File.ReadAllText(_historyFile);
                        if (fileRead.Length == 0)
                        {
                            Console.WriteLine("No commands in list!");
                        }
                        else
                        {

                            //checking id ioID is lower and not equal than maximum count of lines
                            if (uPcount > ioID && ioID != uPcount)
                            {
                                //incrementing ioID by 1
                                ioID++;

                                //returning the line determinated by the inceremented ioID
                                string readCommands = File.ReadLines(_historyFile).Skip(ioID).Take(1).First();

                                //parsing the line
                                string[] parseCommands = readCommands.Split('|');

                                //returning the part of line after the char '|'
                                //from the parsed line so we don't display the id of line
                                string outCommand = parseCommands[1];

                                //output the final parsed line
                                Console.WriteLine(outCommand);

                            }
                            else
                            {
                                //restoring ioID to the maximum numbers of lines
                                ioID = uPcount;

                            }
                        }
                    }
                }
                #endregion
                else
                {
                    #region Command History save and execute
                    if (File.Exists(_historyFile))
                    {
                        //reading file content
                        hContent = File.ReadAllText(_historyFile);

                        //cheking if file is empy
                        if (hContent == string.Empty)
                        {
                            //writing dummy line if file is emtpy
                            File.WriteAllText(_historyFile, "0|0" + Environment.NewLine);
                        }
                        hLines = File.ReadAllLines(_historyFile); //reading lines from file
                        foreach (var line in hLines)
                        {
                            //parsing every line
                            pLines = line.Split('|');

                            //reading first string until space to determinate the last id
                            cID = pLines[0];

                            //convert first string to int                       
                            iID = Convert.ToInt32(cID);
                        }
                        if (input != string.Empty) //checking if input is empty to not write blank commands
                        {
                            //increment value for new saved comnad
                            iID++;

                            //converting int back to string to be saved 
                            count = Convert.ToString(iID);

                            //writing command with id to file
                            File.AppendAllText(_historyFile, count + "|" + input + Environment.NewLine);
                        }
                    }
                    //reading all lines for count save
                    cContent = File.ReadAllLines(_historyFile);

                    foreach (var line in cContent)
                    {
                        //spliting the lines to get id's
                        clines = line.Split('|');

                        //reading first part with id's
                        oID = clines[0];

                        //converting to int32 for future use
                        ioID = Convert.ToInt32(oID);
                        uPcount = ioID;

                    }

                    #endregion


                    //help command

                    if (input == "help")
                    {
                        string helpMGS = @"
xTerminal v1.0 Copyright @ 2020 0x078654c
This is the full list of commands that can be used in xTerminal:

    ls -- List directories and files on a directory.
    ku -- Displays used commands (backword)
    kd -- Displays used commands (forward)
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
    flappy -- Play Flappy Birds in console!
 

                        ";

                        Console.WriteLine(helpMGS);
                    }
                    //rebooting the machine command
                    else if (input == "reboot")
                    {
                        var process = new Process();
                        process.StartInfo = new ProcessStartInfo("cmd.exe")
                        {
                            UseShellExecute = false,
                            Arguments = "/c shutdown /r /f /t 1"

                        };

                        process.Start();
                        process.WaitForExit();

                    }

                    //shuting down the machine command
                    else if (input == "shutdown")
                    {
                        var process = new Process();
                        process.StartInfo = new ProcessStartInfo("cmd.exe")
                        {
                            UseShellExecute = false,
                            Arguments = "/c shutdown /s /f /t 1"

                        };

                        process.Start();
                        process.WaitForExit();

                    }
                    //log off the machine command
                    else if (input == "logoff")
                    {
                        var process = new Process();
                        process.StartInfo = new ProcessStartInfo("cmd.exe")
                        {
                            UseShellExecute = false,
                            Arguments = "/c shutdown /l /f"

                        };

                        process.Start();
                        process.WaitForExit();
                    }

                    //Clear command history file
                    else if (input == "chistory")
                    {
                        if (File.Exists(_historyFile))
                        {
                            File.WriteAllText(_historyFile, string.Empty);
                            Console.WriteLine("Command history log cleared!");
                        }
                        else
                        {
                            Console.WriteLine("File '" + _historyFile + "' dose not exist!");
                        }
                    }
                    //editor set
                    else if (input.Contains("edit set"))
                    {
                        string[] dInput = input.Split(' ');
                        //counting the spaces in input command
                        string _ck = Regex.Matches(input, " ").Count.ToString();
                        int _ch = Int32.Parse(_ck);

                        if (_ch > 1)
                        {
                            if (input.Contains(@"\"))
                            {
                                string[] cInput = input.Split('"');
                                ExecuteWithArgs2(dInput[0], dInput[1], "\"" + @cInput[1] + "\""); //execute commands with 2 arg
                            }
                            else
                            {

                                ExecuteWithArgs2(dInput[0], dInput[1], dInput[2]); //execute commands with 1 arg
                            }
                        }
                        else
                        {
                            ExecuteWithArgs(dInput[0], dInput[1]); //execute commands with 1 arg
                        }
                    }
                    else
                    {
                        if (input.Contains(" "))
                        {
                            string[] dInput = input.Split(' ');

                            //counting the spaces in input command
                            string _ck = Regex.Matches(input, " ").Count.ToString();
                            int _ch = Int32.Parse(_ck);

                            if (_ch == 1)// check one space char in input
                            {
                                ExecuteWithArgs(dInput[0], dInput[1]); //execute commands with 1 arg
                            }
                            else if (_ch == 2)// check one space char in input
                            {
                                ExecuteWithArgs2(dInput[0], dInput[1], dInput[2]); //execute commands with 2 args
                            }
                        }
                        else
                        {
                            Execute(input); //run simple command
                        }
                    }
                }


            } while (input != "exit");

        }


        //------------------

        //process execute 
        public int Execute(string input)
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
                    process.WaitForExit();
                    return 0;
                }
                process.StartInfo = new ProcessStartInfo(Aliases[input])
                {
                    UseShellExecute = false,
                };

                process.Start();
                process.WaitForExit();

                return 0;
            }

            return 1;
        }
        //------------------------

        //process execute  with 1 arg
        public int ExecuteWithArgs(string input, string args)
        {
            if (Aliases.Keys.Contains(input))
            {
                var process = new Process();
                process.StartInfo = new ProcessStartInfo(Aliases[input])
                {
                    UseShellExecute = false,
                    Arguments = args
                };

                process.Start();
                process.WaitForExit();

                return 0;
            }

            return 1;
        }
        //------------------------

        //process execute  with 2 arg
        public int ExecuteWithArgs2(string input, string args, string args2)
        {
            if (Aliases.Keys.Contains(input))
            {
                var process = new Process();
                process.StartInfo = new ProcessStartInfo(Aliases[input])
                {
                    UseShellExecute = false,
                    Arguments = args + " " + args2
                };

                process.Start();
                process.WaitForExit();

                return 0;
            }

            return 1;
        }
        //------------------------

        // We set the name of the current user logged in and machine on console.
        private static void SetConsoleUserConnected(string currentLocation)
        {
            if (currentLocation.Contains(GlobalVariables.rootPath) && currentLocation.Length == 3)
            {
                FileSystem.ColorConsoleText(ConsoleColor.Green, $"{AccountName }@{ComputerName}");
                FileSystem.ColorConsoleText(ConsoleColor.White, ":");
                FileSystem.ColorConsoleText(ConsoleColor.Cyan, $"~");
                FileSystem.ColorConsoleText(ConsoleColor.White, "$ ");
            }
            else
            {
                FileSystem.ColorConsoleText(ConsoleColor.Green, $"{AccountName }@{ComputerName}");
                FileSystem.ColorConsoleText(ConsoleColor.White, ":");
                FileSystem.ColorConsoleText(ConsoleColor.Cyan, $"{dlocation}~");
                FileSystem.ColorConsoleText(ConsoleColor.White, "$ ");
                Console.Title = $"{GlobalVariables.terminalTitle} | {dlocation}";//setting up the new title
            }
        }
    }
}

