﻿using System;

namespace Commands.TerminalCommands.ConsoleSystem
{
    public class Help : ITerminalCommand
    {
        public string Name => "help";

        public void Execute(string arg)
        {

            string helpMGS = @"
xTerminal Copyright @ 2020-2021 0x078654c
This is the full list of commands that can be used in xTerminal:

    ------------------------ System -----------------------
    ls        -- List directories and files on a directory. Use -h for additional parameters.
    ch        -- Displays a list of previous commands typed in terminal. Ex.: hcmd 10 -> displays last 10 commands used. 
    chistory  -- Clears the current history of commands!
    start     -- Starts an application. Use -h for additional help.
    pkill     -- Kills a running process by name or id. Use -h for additional help.
    clear     -- Cleares the console.
    cd        -- Sets the currnet directory. (cd .. for parent directory).
    odir      -- Open current directory or other directory path provided with Windows Explorer.
    ps        -- Opens Windows Powershell.
    cmd       -- Opens Windows Command Prompt.
    reboot    -- It force reboots the Windows OS.
    shutdown  -- It force shutdown the Windows OS.
    logoff    -- It force logoff current user.
    lock      -- Locks the screen(similar to Win+L key combination).
    bios      -- Displays BIOS information on local machine or remote. Use -h for additional parameters.
    sinfo     -- Displays Storage devices information on local machine or remote. Use -h for additional parameters.
    hex       -- Display a hex dump of a file.
                 -o  : Saves the output to a file. Ex.: hex <file_name> -o <file_to_be_saved>
    pcinfo    -- Display System Information.
    nt        -- Starts new xTerminal console.
                   -u : Starts new xTerminal console with other user option.

    ---------------------- File System ---------------------
    cat       -- Displays the content of a file. Use -h for additional parameters.
    mkdir     -- It creates a directory in the current place.
    mkfile    -- It creates a file in the current place.
    fcopy     -- Copies a file with CRC checksum control.  Use -h for additional parameters.
    frename   -- Renames a file in a specific directory(s).
                 Example: frename <old_file_name> -o <new_file_name>
    fmove     -- Moves a file with CRC checksum control. Use -h for additional parameters.
    edit      -- Opens a file in Notepad(default). 
                 To set a new text editor you must use following command: edit set ""Path to editor""
    del       -- Deletes a file or folder without recover.  Use -h for additional parameters.
    cp        -- Check file/folder permissions.
    md5       -- Checks the md5 checksum of a file. Use -h for additional help.

    ---------------------- Networking ----------------------
    ifconfig  -- Display onboard Network Interface Cards configuration (Ethernet and Wireless)
    ispeed    -- Checks the internet speed with Google.
    icheck    -- Checks if a Domain or IP address is online.
    extip     -- Displays the current external IP address.
    wget      -- Download files from a specific website. Use -h for additional help.
    speedtest -- Makes an internet speed test based on speedtest.net API.
    email     -- Email sender client for Microsoft (all), Yahoo, Gmail!
    ping      -- Pings a IP/Hostname. Ex.: ping google.com or ping google.com -r 10 (for 10 replies).

    -----------------C# Core Runner and Add-ons -------------
    ccs       -- Compiles and runs in memory C# code directly from a file using Roslyn. Usage:
                 Example 1: ccs <file_name> 
                 Example 2: ccs <file_name> -p <parameter> 
    !         -- Run or add custom C# code addons as a command. Use -h for additional help.

    -------------------- UI Customization -------------------
    ui        -- Customize the PS1(Prompt String 1). Use -h for additional help.

    -------------------- Password Manager -------------------
    pwm       -- A simple password manager to store localy the authentification data encrypted for 
                 a application using Rijndael AES-256 and Argon2 for password hash.
                 Disclaimer: Use it at your OWN risk.
                 Use -h for additional help.

    ------------------------ Games -------------------------
    flappy    -- Play Flappy Birds in console!
    snake     -- Play Snake game in console!
 

                        ";

            Console.WriteLine(helpMGS);
        }
    }
}
