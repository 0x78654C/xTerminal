using Core;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using WaifuManager = Core.DirFiles.WaifuManage;

namespace Commands.TerminalCommands.DirFiles
{
    [SupportedOSPlatform("windows")]
    public class Waifu : ITerminalCommand
    {
        public string Name => "waifu";
        private string _currentLocation;
        private List<string> _params = ["-cb", "-u", "-b", "-p", "-o", "-e", "-h", "-db", "-df", "-gf", "-lb"];
        private static string s_helpMessage = $@"
Host files with https://waifuvault.moe/. 
WaifuVault is a temporary file hosting service that allows for file uploads that are hosted for a set amount of time.

Usage of waifu command:
    
    -cb : Create bucket.
    -u  : Upload file (From path or URL).
        -b : Specify bucket token. (optional)
        -p : Specify file password. (optional)
        -o : One time download. (optional)
        -e : Expire download link. A string containing a number and a unit (1d = 1day). Valid units are m, h and d. (optional)
        -h : Hide file name.(optional)
    -db : Delete bucket. Example : waifu -db <bucket_token>
    -lb : List all files from bucket with detailed information: waifu -lb <bucket_token>
    -df : Delete file. Example : waifu -df <file_token>
    -gf : Get uploaded file information. waifu -gf <file_token>
    -lr : List wifuvault restrictions types.

Example: waifu -u <file_path> -p <password> -b <bucket_token> -o -e 1h -h

ATTENTION what you upload. xTerminal developers takes no responsibility for what you upload.

All restriction and privacy policy information can be found here https://waifuvault.moe/
";
        public void Execute(string arg)
        {
            try
            {
                // Check if site is up.
                if (!NetWork.PingHost("waifuvault.moe"))
                {
                    FileSystem.SuccessWriteLine("https://waifuvault.moe/ seems down or no internet connection!");
                    return;
                }

                if (arg == Name && !GlobalVariables.isPipeCommand)
                {
                    FileSystem.SuccessWriteLine("Use -h for more information!");
                    return;
                }

                arg = arg.Substring(5);

                // Display help message.
                if (arg.Trim() == "-h" && !GlobalVariables.isPipeCommand)
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                var waifu = new WaifuManager();

                // Create bucket
                if (arg.Trim().StartsWith("-cb"))
                {
                    waifu.CreateBucket();
                    return;
                }

                // Delete bucket
                if (arg.Trim().StartsWith("-db"))
                {
                    var token = arg.SplitByText("-db", 1);
                    waifu.DeleteBucket(token.Trim());
                    return;
                }

                // Delete file
                if (arg.Trim().StartsWith("-df"))
                {
                    var token = arg.SplitByText("-df", 1);
                    waifu.DeleteFile(token.Trim());
                    return;
                }

                // List files from bucket.
                if (arg.Trim().StartsWith("-lb"))
                {
                    var token = arg.SplitByText("-lb", 1);
                    waifu.ListBucketFiles(token.Trim());
                    return;
                }


                // Get uploaded file info.
                if (arg.Trim().StartsWith("-gf"))
                {
                    var token = arg.SplitByText("-gf", 1);
                    waifu.GetFileInfo(token.Trim());
                    return;
                }

                // List waifuvault restrictions.
                if (arg.Trim().StartsWith("-lr"))
                {
                    waifu.ListRestrictions();
                    return;
                }


                // Upload file
                if (arg.Trim().StartsWith("-u"))
                {
                    var fileUrl = "";
                    var bucket = "";
                    var password = "";
                    var expire = "";
                    bool oneTimeDownload = false;
                    bool hideFileName = false;


                    var desData = arg.SplitByText("-u ", 1);
                    fileUrl = arg.GetParamValue("-u ");

                    if (arg.Contains("-o"))
                        oneTimeDownload = true;

                    if (arg.Contains("-h"))
                        hideFileName = true;

                    if (arg.Contains("-p "))
                    {
                        password = arg.GetParamValue("-p ");
                    }
                    if (arg.Contains("-e "))
                        expire = arg.GetParamValue("-e ");

                    if (arg.Contains("-b "))
                        bucket = arg.GetParamValue("-b ");

                    waifu.URLorFile = fileUrl;
                    waifu.Upload(bucket, oneTimeDownload, expire, hideFileName, password);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Unknown token"))
                {
                    FileSystem.ErrorWriteLine("Bucket/File token was already removed! Use -h for more information!");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }
                FileSystem.ErrorWriteLine($"{ex.Message}. Use -h for more information!");
                GlobalVariables.isErrorCommand = true;
            }

        }
    }
}
