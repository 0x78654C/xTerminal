<p align="center">

  <img src="https://github.com/0x78654C/xTerminal/blob/main/media/ico.bmp">
</p>

# xTerminal
 A linux like terminal for windows in C# with some extras ;).

This is a simple terminal in C#  based on https://github.com/willharrison/ProgrammingWithWill

For SpeedTest I use this library https://github.com/JoyMoe/SpeedTest.Net

![alt text](https://github.com/0x78654C/xTerminal/blob/main/media/1.bmp?raw=true)

## Requirements:

.NET Core 2.0

.NET Standard 2.0

.NET Framework 4.7.2

 For Roslyn C# code runner usce NuGet command in Commands project:
 ```
 Install-Package Microsoft.CodeAnalysis.CSharp -pre
 ```

 ## Commands
This is the full list of commands that can be used in xTerminal:

    ------------------------ System ------------------------
    ls        -- List directories and files on a directory. Use -h for additional parameters.
                   -h  : Displays this message.
                   -d  : Display duplicate files in a directory and subdirectories.
                         Example1: ls -d <directory_path>
                         Example2: ls -d <directory_path> -o <file_to_save>
                   -s  : Displays size of files in current directory and subdirectories.
                   -c  : Counts files and directories and subdirectories from current directory.
                   -cf : Counts files from current directory and subdirectories with name containing a specific text.
                         Example: ls -cf <search_text>
                   -cd : Counts directories from current directory and subdirectories with name containing a specific text.
                         Example: ls -cd <search_text>
                   -hl : Highlights specific files/directories with by a specific text. Ex.: ls -hl <higlighted_text>
                   -o  : Saves the output to a file. Ex.: ls -o <file_to_save>
    hcmd      -- Displays a list of previous commands typed in terminal. Ex.: hcmd 10 -> displays last 10 commands used. 
    chistory  -- Clears the current history of commands!
    start     -- Starts an application. Ex.: start start <file_name> OR start <file_name> -p <file_paramters>.
                 Can use following parameter:
                   -h : Display this message.
                   -u : Can run process with different user.
                        Example1: start -u <file_name>
                        Example2: start -u <file_name> -p <file_paramters>
    clear     -- Cleares the console.
    cd        -- Sets the currnet directory. (cd .. for parent directory).
    odir      -- Open current directory or other directory path provided with Windows Explorer.
    ps        -- Opens Windows Powershell. It can use PowerShell comands:
                 Example: ps <ps_command_>
    cmd       -- Opens Windows Command Prompt. It can use Command Prompt comands:
                 Example: ps <cmd_commmand>
    reboot    -- It force reboots the Windows OS.
    shutdown  -- It force shutdown the Windows OS.
    logoff    -- It force logoff current user.
    bios      -- Displays BIOS information on local machine or remote. Use -h for additional parameters.
                   -h : Displays this message.
                   -r : Displays BIOS information on a remote pc.
    sinfo     -- Displays Storage devices information on local machine or remote. Use -h for additional parameters.
                   -h : Displays this message.
                   -r : Displays Storage devices information on a remote pc.
    hex       -- Display a hex dump of a file.
                   -o : Saves the output to a file. Ex.: hex <file_name> -o <file_to_be_saved>

    ---------------------- File System ---------------------
    cat       -- Displays the content of a file. Use -h for additional parameters.
                   -h   : Displays this message.
                   -s   : Output lines containing a provided text from a file.
                          Example: cat -s <search_text> <file_search_in>
                   -so  : Saves the lines containing a provided text from a file.
                          Example: cat -so <search_text> <file_search_in> -o <file_to_save>
                   -sa  : Output lines containing a provided text from all files in current directory and subdirectories.
                          Example1: cat -sa <search_text>
                          Example2: cat -sa <search_text> <part_of_file_name> 
                   -sao : Saves the lines containing a provided text from all files in current directory and subdirectories.
                          Example1: cat -sao <search_text> -o <file_to_save>
                          Example2: cat -sao <search_text> <part_of_file_name> -o <file_to_save>
                   -sm  : Output lines containing a provided text from multiple fies in current directory.
                          Example: cat -sm <search_text> <file_search_in1;file_search_in2;file_search_in_n> 
                   -smo : Saves the lines containing a provided text from multiple files in current directory.
                          Example: cat -smo <search_text> <file_search_in1;file_search_in2;file_search_in_n> -o <file_to_save>
                   -lc  : Counts all the lines(without empty lines) in all files on current directory and subdirectories.
                   -lfc : Counts all the lines(without empty lines) that contains a specific text in file name in current directory and subdirectories.
                          Example: cat -lfc <file_name_text>
    mkdir     -- It creates a directory in the current place.
    mkfile    -- It creates a file in the current place.
    fcopy     -- Copies a file with CRC checksum control.  Use -h for additional parameters.
                   -h  : displays this message
                   -ca <destination_directory> : copy all files from current directory in a specific directory
                   -ca : copy source files in same directory
    frename   -- Renames a file in a specific directory(s).
                 Example: frename <old_file_name> -o <new_file_name>
    fmove     -- Moves a file with CRC checksum control. Use -h for additional parameters.
                   -ma <destination_directory> : moves all files from current directory in a specific directory
    edit      -- Opens a file in Notepad(default). To set a new text editor you must use following command: edit set ""Path to editor""
    del       -- Deletes a file or folder without recover. Use -h for additional parameters.
                   -h  : Displayes this message. 
                   -a  : Deletes all files and directories in current directory. 
                   -af : Deletes all files in current directory. 
                   -ad : Deletes all directories in current directory. 
    cp        -- Check file/folder permissions.
    md5       -- Checks the md5 checksum of a file.

    ---------------------- Networking ----------------------
    ifconfig  -- Display onboard Network Interface Cards configuration (Ethernet and Wireless)
    ispeed    -- Checks the internet speed with Google.
    icheck    -- Checks if a Domain or IP address is online.
    extip     -- Displays the current external IP address.
    wget      -- Download files from a specific website.
                    -h : Display this message.
                    -o : Save to a specific directory.
                         Example2: wget <url> -o <directory_path>
    speedtest -- Makes an internet speed test based on speedtest.net API.
    email     -- Email sender client for Microsoft (all), Yahoo, Gmail!
    ping      -- Pings a IP/Hostname. Ex.: ping google.com or ping google.com -r 10 (for 10 replies).
    
    ---------------- C# Core Runner and Add-ons -------------
    ccs       -- Compiles and runs in memory C# code directly from a file using Roslyn. Usage:
                 Example 1: ccs <file_name> 
                 Example 2: ccs <file_name> -p <parameter> 
    !         -- Run or add custom C# code addons as a command. Use -h for additional help.
                    -h     :  Displays help message.
                    -p     :  Uses command with parameters.
                              Example: ! <command_name> -p <parameters>
                    -add   :  Adds new code from a file and stores in Add-ons directory under xTerminal.exe
                              current directory with a command name.
                              Example: ! -add <file_name_with_code> -c <command_name>|<command_description>
                    -del   :  Deletes an Add-on.
                              Example: ! -del <command_name>
                    -list  :  Display the list of the saved add-ons with description.

    -------------------- UI Customization -------------------
    ui        -- Customize the PS1(Prompt String 1). Use -h for additional help.
                    ::Predifined Colors: darkred, darkgreen, darkyellow, darkmagenta, darkcyan, darkgray, darkblue,
                                         red, green, yellow, white, magenta, cyan, black, gray, blue 
                    ::Predifined indicators: > , ->, =>, $, >>

                    -h : Displys this help message.
                    -u : Enables or disables current user@machine information with a predifined color from list:
                         Example1: ui -u -c <color> :e  -- enables information with a predifined color from list.
                         Example2: ui -u -c <color> :d  -- disables information (need to specify color anyway).
                    -i : Changes command indicator and sets a predifined color from list:
                         Example1: ui -i -c <color> -s <indicator>  -- sets a custom indicator from predifined list with a predifined color from list. 
                         Example2: ui -i -c <color> -s  -- sets default indicator($) with a predifined color from list. 
                    -cd : Changes current directory with a predifined color from list:
                         Example1: ui -cd <color> -- sets a predifined color from list to current directory path.

    ------------------------ Games --------------------------
    flappy    -- Play Flappy Birds in console!(Created by Phan Phu Hao https://github.com/haophancs/cs-flappybird-game)
    snake     -- Play Snake game in console!(Ceated by https://github.com/mkbmain)



## Usage of C# Code runner and add-ons:

 For both ccs command and ! -add the code must be formatted and runned using the following example:

 ```C#
 using System;
// You can add more dependencies.

namespace Test_Code
{
   public class Test
   {	
	  public void Main(string arg)
	  {
		// Do the stuff.
	  }
   }
}

 ```
 xTerminal splits the code in following parts:
  - namespace : Takes the name of the namespace. In this case: Test_Code 
  - public class : Takes the name of the class. In this case: Test 
  - Main : Is defined as entry point for the code to run.

## More Samples

* Using hex command: 

![alt text](https://github.com/0x78654C/xTerminal/blob/main/media/2.bmp?raw=true)

* Using NeoVim in xTerminal:

![alt text](https://github.com/0x78654C/xTerminal/blob/main/media/3.bmp?raw=true)

