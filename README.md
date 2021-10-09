<p align="center">

  <img src="https://github.com/0x78654C/xTerminal/blob/main/media/ico.bmp">
</p>

# xTerminal
 A linux like terminal for windows in C# with some extras ;).

 [![Build status](https://ci.appveyor.com/api/projects/status/6as5ck98br6soxtk?svg=true)](https://ci.appveyor.com/project/0x78654C/xterminal)

![alt text](https://github.com/0x78654C/xTerminal/blob/main/media/1.bmp?raw=true)
![alt text](https://github.com/0x78654C/xTerminal/blob/main/media/2.bmp?raw=true)
![alt text](https://github.com/0x78654C/xTerminal/blob/main/media/3.bmp?raw=true)


This is a simple terminal in C#  based on https://github.com/willharrison/ProgrammingWithWill

For SpeedTest I use this library https://github.com/JoyMoe/SpeedTest.Net

This is the full list of commands that can be used in xTerminal:

    ------------------------ System ------------------------
    ls        -- List directories and files on a directory. Use -h for additional parameters.
    hcmd      -- Displays a list of previous commands typed in terminal. Ex.: hcmd 10 -> displays last 10 commands used. 
    chistory  -- Clears the current history of commands!
    start     -- Starts an application. Ex.: start C:\Windows\System32\notepad.exe. Can use following parameter:
                 -u  : Start with different user.
    clear     -- Cleares the console.
    cd        -- Sets the currnet directory. (cd .. for parent directory)
    odir      -- Open current directory with Windows Explorer
    ps        -- Opens Windows Powershell.
    cmd       -- Opens Windows Command Prompt.
    reboot    -- It force reboots the Windows OS.
    shutdown  -- It force shutdown the Windows OS.
    logoff    -- It force logoff current user.
    bios      -- Displays BIOS information on local machine or remote. Use -h for additional parameters.
    sinfo     -- Displays Storage devices information on local machine or remote. Use -h for additional parameters.
    hex       -- Display a hex dump of a file.
                 -o  : Saves the output to a file. Ex.: hex <file_name> -o <file_to_be_saved>

    ---------------------- File System ---------------------
    cat       -- Displays the content of a file. Use -h for additional parameters.
    mkdir     -- It creates a directory in the current place.
    mkfile    -- It creates a file in the current place.
    fcopy     -- Copies a file with CRC checksum control.  Use -h for additional parameters.
    frename   -- Renames a file in a specific directory(s).
    fmove     -- Moves a file with CRC checksum control. Use -h for additional parameters.
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
    ping      -- Pings a IP/Hostname. Ex.: ping google.com or ping google.com -r 10 (for 10 replies).
    
    --------------------- C# Core Runner --------------------
    ccs       -- Compiles and runs in memory C# code directly from a file using Roslyn. Usage:
                 Example 1: ccs <file_name> 
                 Example 2: ccs <file_name> -p <parameter> 

    ------------------------ Games --------------------------
    flappy    -- Play Flappy Birds in console!(Created by Phan Phu Hao https://github.com/haophancs/cs-flappybird-game)
    snake     -- Play Snake game in console!(Ceated by https://github.com/mkbmain)


Requirements:

.NET Core 2.0

.NET Standard 2.0

.NET Framework 4.7.2
