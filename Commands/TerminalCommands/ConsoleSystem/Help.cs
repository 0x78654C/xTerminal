using System;
using Core;

namespace Commands.TerminalCommands.ConsoleSystem
{
    public class Help : ITerminalCommand
    {
        public string Name => "help";

        public void Execute(string arg)
        {

            string helpMGS = $@"
----------------------------------------------------------------
xTerminal Copyright @ 2020-2023 0x078654c
Version: {GlobalVariables.version.Substring(0, GlobalVariables.version.Length - 2)}
Contact: xcoding.dev@gmail.com
----------------------------------------------------------------

This is the full list of commands that can be used in xTerminal:

    ------------------------ System -----------------------
    ls        -- List directories and files on a directory. Use -h for additional parameters.
    ch        -- Displays a list of previous commands typed in terminal. Ex.: ch 10 -> displays last 10 commands used. 
    chistory  -- Clears the current history of commands!
    start     -- Starts an application. Use -h for additional help.
    pkill     -- Kills a running process by name or id. Use -h for additional help.
    clear     -- Clears the console.
    cd        -- Sets the current directory. (cd .. for parent directory, cd ../.. and so on for multi backward directory navigation).
    odir      -- Open current directory or other directory path provided with Windows Explorer.
    ps        -- Opens Windows Powershell.
    cmd       -- Opens Windows Command Prompt.
    reboot    -- Forces reboot of the Windows OS.
    shutdown  -- Forces shutdown of the Windows OS.
    logoff    -- Forces logoff of the current user.
    lock      -- Locks the screen (similar to Win+L key combination).
    bios      -- Displays BIOS information on local machine or remote. Use -h for additional parameters.
    sinfo     -- Displays Storage devices information on local machine or remote. Use -h for additional parameters.
    hex       -- Displays a hex dump of a file. Use -h for additional parameters.
    pcinfo    -- Displays System Information.
    nt        -- Starts new xTerminal console. Use -h for additional parameters.
    alias     -- Create alias commands for built in xTerminal commands. Use -h for additional parameters.
    shred     -- Overwrites and deletes a file that will be difficult to recover after. Use -h for additional help.
    fsig      -- Check file type singatures (magic numbers). Use -h for additional help.

    ---------------------- File System ---------------------
    cat       -- Displays the content of a file. Use -h for additional parameters.
    mkdir     -- Creates a directory in the current place.
    mkfile    -- Creates a file in the current place.
    fcopy     -- Copies a file with CRC checksum control.  Use -h for additional parameters.
    frename   -- Renames a file in a specific directory(s).
                 Example: frename <old_file_name> -o <new_file_name>
    fmove     -- Moves a file with CRC checksum control. Use -h for additional parameters.
    edit      -- Opens a file in Notepad(default). 
                 To set a new text editor you must use following command: edit set ""Path to editor""
    del       -- Deletes a file or folder without recover.  Use -h for additional parameters.
    cp        -- Check file/folder permissions.
    md5       -- Checks the md5 checksum of a file. Use -h for additional help.
    sort      -- Sorts ascending/desceding data in a file. Use -h for additional help.
    locate    -- Searches for files and directories, in the current directory and subdirectories that contains a specific text. Use -h for additional help.
    echo      -- Write/append data to a file. Use -h for additional help.
    diff      -- Outputs the difference between two files. Use -h for additional help.
                
    ---------------------- Networking ----------------------
    ifconfig  -- Display onboard Network Interface Cards configuration (Ethernet and Wireless)
    ispeed    -- Checks the internet speed with Google.
    icheck    -- Checks if a Domain or IP address is online.
    extip     -- Displays the current external IP address.
    wget      -- Download files from a specific website. Use -h for additional help.
    email     -- Email sender client for Microsoft (all), Yahoo, Gmail!
    ping      -- Pings a IP/Hostname. Use -h for additional parameters.
    cport     -- Checks if a specific port is open/closed on a Hostname/IP.  Use -h for additional parameters.
    wol       -- Sends Wake over LAN packet to a machine.  Use -h for additional parameters.

    -----------------C# Core Runner and Add-ons -------------
    ccs       -- Compiles and runs in memory C# code directly from a file using Roslyn. Usage:
                 Example 1: ccs <file_name> 
                 Example 2: ccs <file_name> -p <parameter> 
    !         -- Run or add custom C# code add-ons as a command. Use -h for additional help.

    -------------------- UI Customization -------------------
    ui        -- Customize the PS1(Prompt String 1). Use -h for additional help.

    -------------------- Password Manager -------------------
    pwm       -- A simple password manager to store locally the authentication data encrypted for 
                 a application using Rijndael AES-256 and Argon2 for password hash.
                 Disclaimer: Use it at your OWN risk.
                 Use -h for additional help.

    ------------------------ OpenAI -------------------------
    cgpt      -- Ask OpenAI(chatGPT) questions and display answer in terminal.
                 Use -h for additional help.

    ------------------------ Games --------------------------
    flappy    -- Play Flappy Birds in console!
    snake     -- Play Snake game in console!

    ------------------------Support -------------------------
    If you like this application and want to support the project just buy your self a coffee and have a nice day ;).

                        ";

            Console.WriteLine(helpMGS);
        }
    }
}
