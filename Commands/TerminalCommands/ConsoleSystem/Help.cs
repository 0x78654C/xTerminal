using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using Core;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("windows")]
    public class Help : ITerminalCommand
    {
        public string Name => "help";

        public void Execute(string arg)
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            string helpMGS = $@"
----------------------------------------------------------------
xTerminal {versionInfo.LegalCopyright}
Version: {GlobalVariables.version}
Contact: xcoding.dev@gmail.com
Source : https://github.com/0x78654C/xTerminal
----------------------------------------------------------------

This is the full list of commands that can be used in xTerminal:

    ------------------------ System -----------------------
    ls        -- List directories and files on a directory. Use -h for additional parameters.
    ch        -- Displays a list of previous commands typed in terminal. Use -h for additional help.
    chistory  -- Clears the current history of commands!
    ./        -- Starts an application. Use -h for additional help.
    kill     -- Kills a running process by name or id. Use -h for additional help.
    plist     -- List current running processes and their child processes. Use -h for additional help.
    clear     -- Clears the console.
    cd        -- Sets the current directory. (cd .. for parent directory, cd ../.. and so on for multi backward directory navigation).
    odir      -- Open current directory or other directory path provided with Windows Explorer.
    ps        -- Opens Windows Powershell.
    cmd       -- Opens Windows Command Prompt.
    reboot    -- Reboot the Windows OS. Use -h for additional parameters.
    shutdown  -- Shutdown the Windows OS. Use -h for additional parameters.
    sleep     -- Sleep/Hibernate the Windows OS.
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
    pwd       -- Prints current working directory. Use -h for additional help.
    cal       -- Display current date calendar. Use -h for additional help.
    time      -- Display current time.
    sc        -- Manage local or remote computer services. Use -h for additional help.
    fw        -- Manage local firewall rules. Use -h for additional help.
    enc       -- Set input/output encoding for xTerminal. Use -h for additional help.
    ln        -- Create shortcut of a file/folder. Use -h for additional help.
    zip       -- Create Zip archive files. Use -h for additional help.
    tee       -- Stores previous pipe command stdout to a file. Use -h for additional help. 

    ---------------------- File System ---------------------
    cat       -- Displays the content of a file. Use -h for additional parameters.
    mkdir     -- Creates a directory in the current place.
    mkfile    -- Creates a file in the current place.
    fcopy     -- Copies a file with CRC checksum control.  Use -h for additional parameters.
    mv        -- Renames a file or directory in a specific directory(s).
                 Example: mv <old_file/dir_name> -o <new_file/dir_name>
    fmove     -- Moves a file with CRC checksum control. Use -h for additional parameters.
    edit      -- Sets a text editor for open files(default is notpead). Use -h for additional help.
    del       -- Deletes a file or folder without recover.  Use -h for additional parameters.
    cp        -- Check file/folder permissions.
    md5       -- Checks the md5 checksum of a file. Use -h for additional help.
    sort      -- Sorts ascending/desceding data in a file. Use -h for additional help.
    locate    -- Searches for files and directories, in the current directory and subdirectories that contains a specific text. Use -h for additional help.
    echo      -- Write/append data to a file. Use -h for additional help.
    diff      -- Outputs the difference between two files. Use -h for additional help.
    exif      -- Extracts image metadata. Use -h for additional help.
    pjson     -- Prettify the JSON data. Use -h for additional help.
    attr      -- Displays/Sets/Removes the current attributes of a file or directory. Use -h for additional help.
    cmp       -- Check if two files are identical by comparing MD5 hash. Use -h for additional help.
    waifu     -- Host temporary files on https://waifuvault.moe/. Use -h for additional help.
                
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
    dspoof    -- The command detects MITM(man in the middle) attacks using ARP spoof method. Use -h for additional parameters.

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
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                GlobalVariables.pipeCmdOutput = helpMGS;
            else
                Console.WriteLine(helpMGS);
        }
    }
}
