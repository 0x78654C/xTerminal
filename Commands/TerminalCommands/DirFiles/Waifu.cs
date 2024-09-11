using Core;
using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using WaifuManager = Core.DirFiles.WaifuManage;

namespace Commands.TerminalCommands.DirFiles
{
    [SupportedOSPlatform("windows")]
    public class Waifu : ITerminalCommand
    {
        public string Name => "waifu";
        private string _currentLocation;
        private List<string> _params = ["-cb", "-u", "-b", "-p", "-o", "-e", "-h", "-db", "-df","-gf","-lb"];
        private static string s_helpMessage = $@"Usage of waifu command:
    
    -cb : Create bucket.
    -u  : Upload file (From path or URL).
        -b : specify bucket token (optional). Specify bucket token.
        -p : specify file password (optional). Specify password.
        -o : one time download (optional)
        -e : expire download link. A string containing a number and a unit (1d = 1day). Valid units are m, h and d
        -h : hide file name.
    -db : Delete bucket. Example : waifu -db <bucket_token>
    -lb : List all files from bucket with detailed information: waifu -lb <bucket_token>
    -df : Delete file. Example : waifu -df <file_token>
    -gf : Get updated file information. waifu -gf <file_token>
";
        public void Execute(string arg)
        {
            try
            {


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
                    var isParamPresent = _params.Any(param => desData.Contains(param));
                    if (isParamPresent)
                    {
                        var paramPresent = _params.Where(param => desData.Contains(param)).Select(x => x).FirstOrDefault();
                        fileUrl = desData.SplitByText(paramPresent, 0).Trim();
                    }
                    else
                        fileUrl = desData.Trim();

                    if(arg.Contains("-o"))
                        oneTimeDownload = true;

                    if (arg.Contains("-h"))
                        hideFileName = true;

                    if (arg.Contains("-p "))
                        password = arg.GetParamValue("-p ");

                    if (arg.Contains("-e "))
                        expire = arg.GetParamValue("-e ");

                    if (arg.Contains("-b "))
                        bucket = arg.GetParamValue("-b ");

                   waifu.URLorFile = fileUrl;
                   waifu.Upload(bucket,oneTimeDownload,expire,hideFileName,password);
                }
            }
            catch (Exception ex)
            {
                if(ex.Message.Contains("Unknown token"))
                {
                    FileSystem.ErrorWriteLine("Bucket/File token was already removed! Use -h for more information!");
                    return;
                }
                FileSystem.ErrorWriteLine($"{ex.Message}. Use -h for more information!");
            }

        }
    }
}
