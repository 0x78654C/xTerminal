using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using Core;

namespace Shell
{

    public class Shell
    {
        //declaring variables
        public static string dlocation = null;
        static string AccountName = Environment.UserName;    //extract current loged username
        static string ComputerName = Environment.MachineName; //extract machine name
        static string keyStrokes;                            //current key stroke string
        static List<string> listChars = new List<string>(); //list of input characters
        static List<string> listCurrentDir = new List<string>();//list of directories and files
        static int countList = 0;                        //index for directory/file listing
        static int cList = 0;                            //PageUp key execution counter
        static string line = "";                         //output the line from final list
        BackgroundWorker woker;                         //Declare backgroudwoker for key input listener


        //-------------------------------
        //Define the shell commands 
        private Dictionary<string, string> Aliases = new Dictionary<string, string>
        {
            { "ls", @".\Tools\FileSystem\ListDirectories.exe" },
            { "clear", @".\Tools\Internal\Clear.exe" },
            { "extip", @".\Tools\Network\Externalip.exe" },
            { "ispeed", @".\Tools\Network\InternetSpeed.exe" },
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
            { "flappy", @".\Tools\Game\FlappyBirds.exe"  }



        };
        //-----------------------


       /*
       //Userd for key event check listing files and directories to be suggested for autocomplete
        private static void KeyDown(KeyEventArgs e)
        {


            //test : output key string format
            //Console.WriteLine(e.KeyCode);
            //-------------------------

            //we read every char in line
            keyStrokes = e.KeyCode.ToString();

            //add chars to list
            if (e.KeyCode != Keys.Delete && e.KeyCode != Keys.Back && e.KeyCode != Keys.Enter && e.KeyCode != Keys.Capital && e.KeyCode != Keys.Left && e.KeyCode != Keys.Right)
                listChars.Add(keyStrokes);

            //compbine chars
            string outPutChars = string.Join("", listChars);

            //clean the output for space and tab
            outPutChars = outPutChars.Replace("Space", " ");
            outPutChars = outPutChars.Replace("LShiftKey", "");



            //check if keycode is PageUp key presed and use for execution command
            if (e.KeyCode == Keys.PageUp)
            {
                //read current location
                string cDir = File.ReadAllText(FileSystem.CurrentLocation);

                if (Directory.Exists(cDir))
                {
                    //get list of files and directoriles
                    var files = Directory.GetFiles(cDir);
                    var directories = Directory.GetDirectories(cDir);
                                       //---------------------------

                    foreach (var dir in directories)
                    {
                        //increment counter for indexing directories
                        countList++;

                        //replace separator for spliting (seems is illegal if I use normal on)
                        string d = dir.Replace(@"\", "/");

                        //we count the number of separators
                        MatchCollection matchDir = Regex.Matches(d, "/");
                        int parseDir = matchDir.Count;

                        //we split every line by separator
                        string[] dirs = d.Split('/');

                        //we add to list the last part
                        listCurrentDir.Add(dirs[parseDir]);

                    }
                    foreach (var file in files)
                    {
                        //increment counter for indexing files
                        countList++;

                        //replace separator for spliting (seems is illegal if I use normal on)
                        string f = file.Replace(@"\", "/");

                        //we count the number of separators
                        MatchCollection matchDir = Regex.Matches(f, @"/");
                        int parseFile = matchDir.Count;

                        //we split every line by separator
                        string[] fS = f.Split('/');

                        //we add to list the last part
                        listCurrentDir.Add(fS[parseFile]);
                    }
                }
                else
                {
                    Console.Write($"Directory '{cDir}' dose not exist!");
                }


                //we increment the PageUp key execution
                cList++;

                //check if index of files/directories is bigger than key execution count
                if (countList >= cList)
                {

                    //save the next lines from list
                    line = listCurrentDir.Skip(cList - 1).Take(1).First();


                    Console.WriteLine(line);
                }
                else
                {
                    //clear PaguUp key execution counter if is bigger than countList
                    cList = 0;

                    //clear index counter
                    countList = 0;


                    line = listCurrentDir.Skip(cList - 1).Take(1).First();


                    //we clear the list
                    listChars.Clear();
                    listCurrentDir.Clear();
                    //----------------
                }



                e.Handled = false;
            }
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
       */

        //Entry point of shell

        public void Run()
        {
            string input = null;
            string _historyFile = Directory.GetCurrentDirectory() + @"\Data\History.db";
            string count = null;
            string[] hLines = null;
            string hContent = null;
            string[] pLines = null;
            string cID = null;
            int iID = 0;
            string[] cContent = null;
            string[] clines = null;
            string oID = null;
            int ioID = 0;
            int uPcount = 0;

            /*
            //start key event press listen and file/folder listing
            woker = new BackgroundWorker();
            woker.DoWork += Woker_DoWork;
            woker.RunWorkerAsync();
            //TODO: will see if use
             */


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
                dlocation = File.ReadAllText(FileSystem.CurrentLocation);

                Console.Write(AccountName + "@" + ComputerName + $" ({dlocation})" + " $ ");

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
                //go back to last command from last point of 'ku' used
                else if (input == "kd")
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
                            Console.WriteLine(outCommand);

                        }
                        else
                        {
                            //restoring ioID to the maximum numbers of lines
                            ioID = uPcount;

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


                    //rebooting the machine command

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
    md5 -- Chceks the md5 checksum of a file.
    extip -- Displays the current external IP address.
    wget -- Download files from a specific website.
    fcopy -- Copies a file with md5 checksum control.
    frename -- Renames a file in a specific directory(s).
    fmove -- Moves a file with md5 checksum contorl.
    cmd --  Opens Windows Command Promt.
    ps -- Opens Windowns Powershell.
    cat -- Displays the content of a file.
    del -- Deletes a file or folder without recover.
    reboot -- It force reboots the Windows OS.
    shutdown --  It force shutdown the Windows OS.
    logoff -- It force logoff current user.
    mkdir -- It creates a directory in the curent place.
    mkfile -- It creates a file in the curent place.
    edit -- Opens a file in Notepad(for now).
    cp -- Check file/folder permissions.
    speedtest -- Makes an internet speed test based on speedtest.net API.
    email -- Email sender client for Microsoft (all), Yahoo, Gmail!
    chistory -- Clears the current history of commands!
    flappy -- Play Flappy Birds in console!
 

                        ";

                        Console.WriteLine(helpMGS);
                    }
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
                            Console.WriteLine("File '"+_historyFile+"' dose not exist!");
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
                process.StartInfo = new ProcessStartInfo(Aliases[input])
                {
                    UseShellExecute = false
                };

                process.Start();
                process.WaitForExit();

                return 0;
            }

            Console.WriteLine($"xTerminal command '{input}' not found");
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

            Console.WriteLine($"xTerminal command '{input}' not found");
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

            Console.WriteLine($"xTerminal Command '{input}' not found");
            return 1;
        }
        //------------------------

    }

}

