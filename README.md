# xTerminal
 A linux like terminal for windows in C#


This is a simple terminal in C#  based on https://github.com/willharrison/ProgrammingWithWill
For SpeedTest I use this library https://github.com/JoyMoe/SpeedTest.Net

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
    edit -- Opens a file in Notepad(default). To set a new text editor you must use following command: edit set "Path to editor"
    cp -- Check file/folder permissions.
    speedtest -- Makes an internet speed test based on speedtest.net API.
    email -- Email sender client for Microsoft (all), Yahoo, Gmail!
    chistory -- Clears the current history of commands!
    flappy -- Play Flappy Birds in console!(Created by Phan Phu Hao https://github.com/haophancs/cs-flappybird-game)


Requirements:
.NET Core 2.0

.NET Standard 2.0

.NET Framework 4.7.2
