<p align="center">
  <img src="https://github.com/0x78654C/xTerminal/blob/main/media/ico.png">
</p>

# xTerminal
A Linux like terminal for windows in C# with some extras ;).

![alt text](https://github.com/0x78654C/xTerminal/blob/main/media/1.bmp?raw=true)

## Requirements:

.NET 6 Runtime

 For Roslyn C# code runner use NuGet command in Commands project:
 ```
 Install-Package Microsoft.CodeAnalysis.CSharp -pre
 ```

 For Password Manager use NuGet command in Commands project:
 ```
 Install-Package Konscious.Security.Cryptography.Argon2
 ```

 For DiffPlex use NuGet command in Commands project:
  ```
 Install-Package DiffPlex -Version 1.7.1
 ```

 ## Auto suggestion for files and folders
xTerminal accepts auto suggestion for file and folder (depends on command use case) on following comands: 
cd, cat, ls, odir, hex, frename, fcopy, fmove, md5, edit, start, cp, del, ccs, sort

Example:
 ```
 ~> cd win
 ```
 Double press CTRL key and will suggest you all the directories that starts with the letters 'win' from current location.

  ```
 ~> start ap
 ```
 Double press 'CTRL' key and will suggest you all the files that starts with the letters 'ap' from current location.

 ## Commands
This is the full list of commands that can be used in xTerminal:

    ------------------------ System ------------------------
    ls        -- List directories and files on a directory. Use -h for additional parameters.
                   -h  : Displays this message.
                   -d  : Display duplicate files in a directory and subdirectories.
                         Example1: ls -d <directory_path>
                         Example2: ls -d -e <directory_path> (scans for duplicate files with same extension)
                         Example3: ls -d <directory_path> -o <file_to_save>
                         Example4: ls -d -e <directory_path> -o <file_to_save>  (scans for duplicate files with same extension)
                   -s  : Displays size of files in current directory and subdirectories.
                   -se : List recursively files and directories containing a specific text.
                         Example1: ls -se <search_text>
                         Example2: ls -se <search_text> -o <file_to_save>
                   -c  : Counts files and directories and subdirectories from current directory.
                   -cf : Counts files from current directory and subdirectories with name containing a specific text.
                         Example: ls -cf <search_text>
                   -cd : Counts directories from current directory and subdirectories with name containing a specific text.
                         Example: ls -cd <search_text>
                   -ct : Display creation date time of files and folders from current directory.
                   -hl : Highlights specific files/directories with by a specific text. Ex.: ls -hl <higlighted_text>
                   -o  : Saves the output to a file. Ex.: ls -o <file_to_save>
    ch        -- Displays a list of previous commands typed in terminal. Ex.: ch 10 -> displays last 10 commands used. 
    chistory  -- Clears the current history of commands!
    start     -- Starts an application. Ex.: start <file_name> OR start <file_name> -p <file_paramters>.
                 Can be used with the following parameters:
                   -h    : Displays this message.
                   -u    : Can run process with different user.
                   -we   : Wait for process to exit.
                   -param: Start process with specified parameters.
                         Example1: start -u <file_name>
                         Example2: start -u <file_name> -param <file_paramters>
                 Both examples can be used with -we parameter.
    pkill     -- Kills a running process by name or id.
                 Example1: pkill <process_name>
                 Example2: pkill -i <process_id>
    clear     -- Clears the console.
    cd        -- Sets the currnet directory. (cd .. for parent directory).
    odir      -- Open current directory or other directory path provided with Windows Explorer.
    ps        -- Opens Windows Powershell. It can use PowerShell commands:
                 Example: ps <ps_command_>
    cmd       -- Opens Windows Command Prompt. It can use Command Prompt commands:
                 Example: ps <cmd_commmand>
    reboot    -- It force reboots the Windows OS.
    shutdown  -- It force shutdown the Windows OS.
    logoff    -- It force logoff current user.
    lock      -- Locks the screen(similar to Win+L key combination).
    bios      -- Displays BIOS information on local machine or remote. Use -h for additional parameters.
                   -h : Displays this message.
                   -r : Displays BIOS information on a remote pc.
    sinfo     -- Displays Storage devices information on local machine or remote. Use -h for additional parameters.
                   -h : Displays this message.
                   -r : Displays Storage devices information on a remote pc.
    hex       -- Display a hex dump of a file.
                   -o : Saves the output to a file. Ex.: hex <file_name> -o <file_to_be_saved>
    pcinfo    -- Display System Information.
    nt        -- Starts new xTerminal console.
                   -u : Starts new xTerminal console with other user option.
    alias     -- Create alias commands for built in xTerminal commands.
                   -add   :  Creates a alias command with parammeters (alias <commandName>|<parameters>).
                             Example: alias -add lz|ls -s (Creates a command lz that will run parameter ls -s)
                   -del   :  Deletes a alias command.
                             Example: alias -del lz (Deletes lz command and parameters for it.)
                   -update:  Update a alias command.
                             Example: alias -update lz|ls -ct (Updates command lz with new parameters. Works if command already exist!)
                   -list  :  List all alias commands.
                   -clear :  Clears all alias commands.
                 Attention: Alias commands cannot overwrite terminal commands!

    ---------------------- File System ---------------------
    cat       -- Displays the content of a file. Use -h for additional parameters.
                   -h   : Displays this message.
                   -n   : Displays first N lines from a file.
                          Example: cat -n 10 <path_of_file_name>
                   -l   : Displays data between two lines.
                          Example: cat -l 10-20 <path_of_file_name>
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
                   -sm  : Output lines containing a provided text from multiple files in current directory.
                          Example: cat -sm <search_text> <file_search_in1;file_search_in2;file_search_in_n> 
                   -smo : Saves the lines containing a provided text from multiple files in current directory.
                          Example: cat -smo <search_text> <file_search_in1;file_search_in2;file_search_in_n> -o <file_to_save>
                   -lc  : Counts all the lines(without empty lines) in all files on current directory and subdirectories.
                   -lfc : Counts all the lines(without empty lines) that contains a specific text in file name in current directory and subdirectories.
                          Example: cat -lfc <file_name_text>
                   -con : Concatenate text files to a single file.
                          Example: cat -con file1;file2;file3 -o fileOut
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
    md5       -- Checks the md5 checksum of a file. Use -h for additional parameters.
                     md5 <file_name> : Display the MD5 CheckSUM of a file.
                     md5 -d <dire_name> : Display the MD5 CheckSUM list of all the files in a directory and subdirectories.
                     md5 -d <dire_name> -o <save_to_file> : Saves the MD5 CheckSUM list of all the files in a directory and subdirectories.
    sort      -- Sorts ascending/descending data in a file. Use -h for additional help.
                 Example 1: sort -a filePath  (Sort data ascending and displays it.)
                 Example 2: sort -a filePath -o saveFilePath  (Sort data ascending and saves it to a file.)
                 Example 3: sort -d filePath  (Sort data descending and displays it.)
                 Example 4: sort -d filePath -o saveFilePath  (Sort data descending and saves it to a file.)
    locate    -- Searches for files and directories, in the current directory and subdirectories that contains a specific text.
                 Example 1: locate <text> (Displays searched files/directories from current directory and subdirectories that includes a specific text.)
                 Example 2: locate <text> -o <save_to_file> (Stores in to a file the searched files/directories from current directory and subdirectories that includes a specific text.)                   
                 Parameters:
                     -s  : Displays searched files/directories from the current directory and subdirectories that starts with a specific text.
                            Example 1: locate -s <text>
                            Example 2: locate -s <text> -o <save_to_file>
                     -e  : Displays searched files/directories from the current directory and subdirectories that ends with a specific text.
                            Example 1: locate -e <text>
                            Example 2: locate -e <text> -o <save_to_file>
                     -eq : Displays searched files/directories from the current directory and subdirectories that equals a specific text.
                            Example 1: locate -eq <text>
                            Example 2: locate -eq <text> -o <save_to_file>
    echo      -- Write/append data to a file.
                 Example 1: echo hello world > path_to_file (Write data to file.)
                 Example 2: echo hello world >> path_to_file (Append data to file.)
    diff      -- Outputs the difference between two files.
                 Example 1: diff first_file_name;second_file_name                               : Display the difference from second file in comparison to first file.
                 Example 2: diff first_file_name;second_file_name -verbose                      : Display the entire second file with the difference in comparison to first file.
                 Example 3: diff first_file_name;second_file_name -f save_to_file_name          : Saves to file the difference from second file in comparison to first file.
                 Example 4: diff first_file_name;second_file_name -f save_to_file_name -verbose : Saves to file the entire second file with the marked difference in comparison to first file.

    ---------------------- Networking ----------------------
    ifconfig  -- Display onboard Network Interface Cards configuration (Ethernet and Wireless)
    ispeed    -- Checks the internet speed with Google.
    icheck    -- Checks if a Domain or IP address is online.
    extip     -- Displays the current external IP address.
    wget      -- Download files from a specific website.
                    -h : Display this message.
                    -o : Save to a specific directory.
                         Example2: wget <url> -o <directory_path>
                 WGet command can be used with --noping parameter to disable ping check on hostname/ip.
                 Example: wget <url> -o <directory_path> --noping
    email     -- Email sender client for Microsoft (all), Yahoo, Gmail!
    ping      -- Pings a IP/Hostname.
                 Example 1: ping google.com  (for normal ping with 4 replies)
                 Example 2: ping google.com -t 10  (for 10 replies)
                 Example 3: ping google.com -t  (infinite replies)
                 Ping with -t can be canceled with CTRL+X key combination.
    cport     -- Checks if a specific port is open/closed on a Hostname/IP.  Use -h for additional parameters.
                 Example 1: cport IPAddress/HostName -p 80   (checks if port 80 is open)
                 Example 2: cport IPAddress/HostName -p 1-200   (checks if any port is open from 1 to 200)
                 Example 3: cport stimeout 100   (Set check port time out in milliseconds. Default value is 500.)
                 Example 4: cport rtimeout   (Reads the current time out value)
                 Cport check command can be used with --noping parameter to disable ping check on hostname/ip.
                 Example: cport IPAddress/HostName -p 80 --noping
    wol       -- Sends Wake over LAN packet to a machine.
                 Example 1:  wol -ip IP_Address -mac MAC_Address                   : sends wake packet for ip/mac.
                 Example 2:  wol -ip IP_Address -mac MAC_Address -port number_port : sends wake packet for ip/mac and custom WOL port.

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
                    ::Predefined Colors: darkred, darkgreen, darkyellow, darkmagenta, darkcyan, darkgray, darkblue,
                                         red, green, yellow, white, magenta, cyan, gray, blue 
                    ::Predefined indicators: > , ->, =>, $, >>

                    -h : Displays this help message.
                    -u : Enables or disables current user@machine information with a predefined color from list:
                         Example1: ui -u -c <color> :e  -- enables information with a predefined color from list.
                         Example2: ui -u -c <color> :d  -- disables information (need to specify color anyway).
                    -i : Changes command indicator and sets a predefined color from list:
                         Example1: ui -i -c <color> -s <indicator>  -- sets a custom indicator from predefined list with a predefined color from list. 
                         Example2: ui -i -c <color> -s  -- sets default indicator($) with a predefined color from list. 
                    -cd : Changes current directory with a predefined color from list:
                         Example1: ui -cd <color> -- sets a predefined color from list to current directory path.
 
    -------------------- Password Manager -------------------
    pwm       -- A simple password manager to store locally the authentication data encrypted for 
                 a application using Rijndael AES-256 and Argon2 for password hash.
                 Disclaimer: Use it at your OWN risk.
                 Useage of password manager commands:
                     -h       : Display this message.
                     -createv : Create a new vault.
                     -delv    : Deletes an existing vault.
                     -listv   : Displays the current vaults.
                     -addapp  : Adds a new application to vault.
                     -dela    : Deletes an existing application in a vault.
                     -updatea  : Updates account's password for an application in a vault.
                     -lista   : Displays the existing applications in a vault.

    ------------------------ OpenAI -------------------------
    cgpt      -- Ask OpenAI(chatGPT) questions and display answer in terminal.
                 Example 1: cgpt -setkey key_from_openai (Store the API key provided by OpenAI.com)
                 Example 2: cgpt question_you_want_to_ask (Display the answer for your question)

    ------------------------ Games --------------------------
    flappy    -- Play Flappy Birds in console!(Created by Phan Phu Hao https://github.com/haophancs/cs-flappybird-game)
    snake     -- Play Snake game in console!(Created by https://github.com/mkbmain)

All xTerminal commands can be used from other terminals as <b>Command Line Arguments</b>. Example: 
 ```
 C:\work\xTerminal\xTerminal.exe extip
 ```

## Usage of C# Code runner and add-ons:

 For both ccs command and ! -add the code must be formatted and runned using the following example:

 ```C#
 using System;
// You can add more dependencies.

namespace Test_Code
{
   class Test
   {	
	  static void Main(string[] args)
	  {
		// Do the stuff.
	  }
   }
}
```

## Usage of the Password Manager:

As most of the password managers we start by creating a vault where we store the applications data.
For that we use following command:
 ```
 pwm -createv
 ```
 You will be asked for the vault name and master password. Master password must meet the following complexity:
 ```
 Password must be at least 12 characters, and must include at least one upper case letter, one lower case letter, one numeric digit, one special character and no space!
 ````

 To see if the vault is created we list the current existing vaults by typing in console the following command:
 ```
 pwm -listv
 ```

 Adding the application information just type:
 ```
 pwm -addapp
 ```
 You will be prompted for vault name, master password to login in it, application name to be added, account name and password to be stored.

 To list the accounts from a specific application or entire vault lists type: 
 ```
 pwm -lista
 ```
 
 To delete a specific account from an application just use:
 ```
 pwm -dela
 ```

 To update password for a specific account in a application type:
 ```
 pwm -updatea
 ```

 To delete a vault type:
 ```
 pwm -delv
 ```


## More Samples

* Using the Password Manager: 

![alt text](https://github.com/0x78654C/xTerminal/blob/main/media/4.png?raw=true)

* Using hex command: 

![alt text](https://github.com/0x78654C/xTerminal/blob/main/media/2.bmp?raw=true)

* Using NeoVim in xTerminal:

![alt text](https://github.com/0x78654C/xTerminal/blob/main/media/3.bmp?raw=true)

* Auto suggestion for files and folders :

![alt text](https://github.com/0x78654C/xTerminal/blob/main/media/5.bmp?raw=true)

