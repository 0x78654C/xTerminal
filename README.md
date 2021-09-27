# xTerminal
 A linux like terminal for windows in C#
 
![alt text](https://github.com/0x78654C/xTerminal/blob/main/media/1.bmp?raw=true)


This is a simple terminal in C#  based on https://github.com/willharrison/ProgrammingWithWill
For SpeedTest I use this library https://github.com/JoyMoe/SpeedTest.Net

This is the full list of commands that can be used in xTerminal:
    ------------------------ System ------------------------
    ls        -- List directories and files on a directory. Can use following parameters:
                 -s  : Displays size of files in current directory.
                 -c  : Counts files and directories (subdirs too) in current directory.
                 -hl : Highlights specific files/directories with by a specific text. Ex.: ls -hl higlighted_text
    hcmd      -- Displays a list of previous commands typed in terminal. Ex.: hcmd 10 -> displays last 10 commands used. 
    chistory  -- Clears the current history of commands!
    start     -- Starts an application. Ex.: start C:\Windows\System32\notepad.exe
    clear     --  Cleares the console.
    cd        -- Sets the currnet directory. (cd .. for parent directory)
    odir      -- Open current directory with Windows Explorer
    ps        -- Opens Windows Powershell.
    cmd       --  Opens Windows Command Prompt.
    reboot    -- It force reboots the Windows OS.
    shutdown  --  It force shutdown the Windows OS.
    logoff    -- It force logoff current user.
    bios      -- Displays BIOS information on local machine or remote.
    sinfo     -- Displays Storage devices information on local machine or remote.

    ---------------------- File System ---------------------
    cat       -- Displays the content of a file. Can use following parameters:
                 -h   : Displays this message.
                 -s   : Output lines containing a provided text from a file.
                 -so  : Saves the lines containing a provided text from a file.
                 -sm  : Output lines containing a provided text from multiple fies.
                 -smo : Saves the lines containing a provided text from multiple files in current path location.
    mkdir     -- It creates a directory in the current place.
    mkfile    -- It creates a file in the current place.
    fcopy     -- Copies a file with CRC checksum control.
    frename   -- Renames a file in a specific directory(s).
    fmove     -- Moves a file with CRC checksum control.
    edit      -- Opens a file in Notepad(default). To set a new text editor you must use following command: edit set ""Path to editor""
    del       -- Deletes a file or folder without recover.
    cp        -- Check file/folder permissions.
    md5       -- Checks the md5 checksum of a file.

    ---------------------- Networking ----------------------
    ifconfig  -- Display onboard Network Interface Cards configuration (Ethernet and Wireless)
    ispeed    -- Checks the internet speed with Google.
    icheck    -- Checks if a Domain or IP address is online.
    extip     -- Displays the current external IP address.
    wget      -- Download files from a specific website.
    speedtest -- Makes an internet speed test based on speedtest.net API.
    email     -- Email sender client for Microsoft (all), Yahoo, Gmail!
    
    ------------------------ Games -------------------------
    flappy    -- Play Flappy Birds in console!(Created by Phan Phu Hao https://github.com/haophancs/cs-flappybird-game)


Requirements:

.NET Core 2.0

.NET Standard 2.0

.NET Framework 4.7.2
