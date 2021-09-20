# xTerminal
 A linux like terminal for windows in C#
 
![alt text](https://github.com/0x78654C/xTerminal/blob/main/media/1.bmp?raw=true)


This is a simple terminal in C#  based on https://github.com/willharrison/ProgrammingWithWill
For SpeedTest I use this library https://github.com/JoyMoe/SpeedTest.Net

This is the full list of commands that can be used in xTerminal:

    ls -- List directories and files on a directory. (ls -s for size display, ls -c for count files and directories (subdirs too)
          ls -hl text  : highlight specific files/directories with that text)
    hcmd -- Displays a list of previous commands typed in terminal. Ex.: hcmd 10 -> displays last 10 commands used. 
    clear --  Cleares the console.
    cd -- Sets the currnet directory. (cd .. for parent directory)
    ispeed -- Checks the internet speed with Google.
    icheck -- Checks if a Domain or IP address is online.
    md5 -- Checks the md5 checksum of a file.
    extip -- Displays the current external IP address.
    wget -- Download files from a specific website.
    fcopy -- Copies a file with CRC checksum control.
    frename -- Renames a file in a specific directory(s).
    fmove -- Moves a file with CRC checksum control.
    cmd --  Opens Windows Command Prompt.
    ps -- Opens Windows Powershell.
    cat -- Displays the content of a file.
    del -- Deletes a file or folder without recover.
    reboot -- It force reboots the Windows OS.
    shutdown --  It force shutdown the Windows OS.
    logoff -- It force logoff current user.
    bios -- Displays BIOS information on local machine or remote.
    sinfo -- Displays Storage devices information on local machine or remote.
    mkdir -- It creates a directory in the current place.
    mkfile -- It creates a file in the current place.
    edit -- Opens a file in Notepad(default). To set a new text editor you must use following command: edit set "Path to editor"
    cp -- Check file/folder permissions.
    speedtest -- Makes an internet speed test based on speedtest.net API.
    email -- Email sender client for Microsoft (all), Yahoo, Gmail!
    chistory -- Clears the current history of commands!
    start -- Starts an application. Ex.: start C:\Windows\System32\notepad.exe
    flappy -- Play Flappy Birds in console!(Created by Phan Phu Hao https://github.com/haophancs/cs-flappybird-game)


Requirements:

.NET Core 2.0

.NET Standard 2.0

.NET Framework 4.7.2
