using Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using CheckType = Core.FileSystem.CheckType;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using Core.Network;

namespace Commands.TerminalCommands.Network
{
    [SupportedOSPlatform("Windows")]
    public class WGet : ITerminalCommand
    {
        public string Name => "wget";

        /*WGet command*/

        // Declare global variables
        private static string s_urlFirst;
        private static string s_urlSecond;
        private static Stopwatch s_stopWatch;
        private static TimeSpan s_timeSpan;
        private static bool s_pingCheck = false;
        private static readonly HttpClient s_client = new HttpClient();
        private static AutoResetEvent s_resetEvent = new AutoResetEvent(false);
        private static string s_helpMessage = @"Usage: wget <url> . Or with parameters:

   -h : Display this message.
 --tls: Activate tls1,tls1.2,tls1.3 (used in end of command)
   -o : Save to a specific directory.
        Example: wget <url> -o <directory_path>

    WGet command can be used with --noping parameter to disable ping check on hostname/ip.
        Example: wget <url> -o <directory_path> --noping
";
        public void Execute(string arg)
        {
            GlobalVariables.isErrorCommand = false;
            if (arg == $"{Name} -h")
            {
                Console.WriteLine(s_helpMessage);
                return;
            }

            if (arg  == Name && !GlobalVariables.isPipeCommand)
            {
                FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                return;
            }

            if (arg.Contains("--noping"))
            {
                s_pingCheck = false;
                arg = arg.Replace("--noping", string.Empty);
            }
            else
            {
                s_pingCheck = true;
            }

            try
            {
                if (arg.Contains("--tls"))
                    ActivateTls();
                s_timeSpan = new TimeSpan();
                s_stopWatch = new Stopwatch();


                if (s_pingCheck)
                {
                    if (NetWork.IntertCheck())
                    {
                        RunWGet(arg);
                    }
                    else
                    {
                        FileSystem.ErrorWriteLine("No internet connection!");
                        GlobalVariables.isErrorCommand = true;
                    }
                }
                else
                {
                    RunWGet(arg);
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }

        /// <summary>
        /// Activate TLS
        /// </summary>
        private static void ActivateTls()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                | SecurityProtocolType.Tls11
                | SecurityProtocolType.Tls 
                | SecurityProtocolType.Tls13;
        }

        /// <summary>
        /// Run wget funtions.
        /// </summary>
        /// <param name="param"></param>
        private static void RunWGet(string param)
        {
            string input = param.Replace("wget ", string.Empty);  //url input
            Console.WriteLine(s_urlFirst);
            if (input.Contains("-o") )
            {
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0)
                    s_urlSecond = GlobalVariables.pipeCmdOutput.Trim();
                else
                    s_urlSecond = input.SplitByText("-o", 0).Trim();
                s_urlFirst = input.SplitByText("-o", 1).Trim();
                Task.Run(() => DownloadDirectory()).Wait();
                return;
            }
             if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0)
                s_urlFirst = GlobalVariables.pipeCmdOutput.Trim();
            else
                s_urlFirst = input;
            Task.Run(() => Download()).Wait();
        }


        /// <summary>
        /// Download file directly in root path.
        /// </summary>
        /// <returns></returns>
        private static async Task Download()
        {
            string dlocation = File.ReadAllText(GlobalVariables.currentDirectory); ;
            s_timeSpan = new TimeSpan();
            s_stopWatch = new Stopwatch();
            var source =  UriSafety.CreateHttpUri(s_urlFirst);
            var fileName = UriSafety.GetSafeDownloadPath(source, dlocation);
            string fileUrl = Path.GetFileName(fileName);
            Console.WriteLine($"Downloading {fileUrl} in {dlocation} .......");
            s_stopWatch.Start();
            using (var s = await s_client.GetStreamAsync(source))
            {
                using (var fs = new FileStream(fileName, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await s.CopyToAsync(fs);
                }
            }
            s_stopWatch.Stop();
            s_timeSpan = s_stopWatch.Elapsed;
            Console.WriteLine($"Downloaded in { dlocation}{ fileUrl}");
            Console.WriteLine($"Elapsed download time: {s_timeSpan.Seconds} seconds");
            s_resetEvent.Set();
        }

        /// <summary>
        /// Download file in diffrent path from root
        /// </summary>
        /// <returns></returns>
        private static async Task DownloadDirectory()
        {
            if (!Directory.Exists(s_urlFirst))
            {
                FileSystem.ErrorWriteLine($"Directory: {s_urlFirst} does not exist!");
                GlobalVariables.isErrorCommand = true;
                return;
            }

            if (!FileSystem.CheckPermission(s_urlFirst, true, CheckType.Directory))
            {
                FileSystem.ErrorWriteLine($"Access denied to directory: {s_urlFirst}");
                GlobalVariables.isErrorCommand = true;
                return;
            }

            var source = UriSafety.CreateHttpUri(s_urlSecond);
            var fileName = UriSafety.GetSafeDownloadPath(source, s_urlFirst);
            string fileUrl2 = Path.GetFileName(fileName);
            Console.WriteLine($"Downloading {fileUrl2} in {s_urlFirst}\\ .......");
            s_stopWatch.Start();
            using (var s = await s_client.GetStreamAsync(source))
            {
                using (var fs = new FileStream(fileName, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await s.CopyToAsync(fs);
                }
            }
            s_stopWatch.Stop();
            s_timeSpan = s_stopWatch.Elapsed;
            Console.WriteLine("Downloaded in " + s_urlFirst + @"\" + fileUrl2);
            Console.WriteLine($"Elapsed download time: {s_timeSpan.Seconds} seconds");
            s_resetEvent.Set();
        }
    }
}
