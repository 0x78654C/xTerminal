using Core;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using CheckType = Core.FileSystem.CheckType;
namespace Commands.TerminalCommands.Network
{
    public class WGet : ITerminalCommand
    {
        public string Name => "wget";

        /*WGet command*/

        // Declare global variables
        private static string s_urlFirst;
        private static string s_urlSecond;
        private static Stopwatch s_stopWatch;
        private static TimeSpan s_timeSpan;
        private static readonly WebClient s_client = new WebClient();
        private static MatchCollection s_match2;
        private static MatchCollection s_match;
        private static BackgroundWorker s_worker;
        private static BackgroundWorker s_workerDirectory;
        private static AutoResetEvent s_resetEvent = new AutoResetEvent(false);
        private static string s_helpMessage = @"Usage: wget <url> . Or with paramters:

   -h : Display this message.
   -o : Save to a specific directory.
        Example2: wget <url> -o <directory_path>
";
        public void Execute(string arg)
        {
            s_worker = new BackgroundWorker();
            s_worker.DoWork += DoWork_Download;
            s_worker.WorkerReportsProgress = true;

            s_workerDirectory = new BackgroundWorker();
            s_workerDirectory.DoWork += DoWork_DownloadDirectory;
            s_workerDirectory.WorkerReportsProgress = true;

            s_timeSpan = new TimeSpan();
            s_stopWatch = new Stopwatch();
            if (arg.Length == 4)
            {
                Console.WriteLine($"Use -h param for {Name} command usage!");
                return;
            }
            try
            {
                if (NetWork.IntertCheck())
                {
                    int argLenght = arg.Length - 5;
                    string input = arg.Substring(5, argLenght);  //url input
                    Console.WriteLine(s_urlFirst);
                    if (input.Contains("-o"))
                    {
                        s_urlFirst = input.SplitByText("-o", 1);
                        s_urlSecond = input.SplitByText("-o", 0);
                        s_workerDirectory.RunWorkerAsync();
                        s_resetEvent.WaitOne();
                        return;
                    }
                    if (input.StartsWith("-h"))
                    {
                        Console.WriteLine(s_helpMessage);
                        return;
                    }
                    s_worker.RunWorkerAsync();
                    s_resetEvent.WaitOne();

                }
                else
                {
                    FileSystem.ErrorWriteLine("No internet connection!");
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }
        //Download file directly in root path
        private static void DoWork_Download(object sender, DoWorkEventArgs e)
        {
            string dlocation = File.ReadAllText(GlobalVariables.currentDirectory); ;
            int parse;
            string[] parseUrl;
            string fileUrl;
            s_timeSpan = new TimeSpan();
            s_stopWatch = new Stopwatch();
            s_match = Regex.Matches(s_urlFirst, "/");
            parse = s_match.Count;
            parseUrl = s_urlFirst.Split('/');
            fileUrl = parseUrl[parse];
            Console.WriteLine($"Downloading {fileUrl} in {dlocation} .......");
            var source = new Uri(s_urlFirst);
            s_stopWatch.Start();
            s_client.DownloadFile(source, dlocation + @"\" + fileUrl);
            FileInfo fileInfo = new FileInfo(dlocation + @"\" + fileUrl);
            s_worker.ReportProgress(Convert.ToInt32(fileInfo.Length));
            s_stopWatch.Stop();
            s_timeSpan = s_stopWatch.Elapsed;
            Console.WriteLine("Downloaded in " + dlocation + @"\" + fileUrl);
            Console.WriteLine($"Elapsed download time: {s_timeSpan.Seconds} seconds ");
            s_resetEvent.Set();
        }

        // Download file in diffrent path from root
        private static void DoWork_DownloadDirectory(object sender, DoWorkEventArgs e)
        {
            if (!Directory.Exists(s_urlFirst))
            {
                FileSystem.ErrorWriteLine($"Directory: {s_urlFirst} does not exist!");
                return;
            }

            if (!FileSystem.CheckPermission(s_urlFirst, true, CheckType.Directory))
            {
                FileSystem.ErrorWriteLine($"Access denied to direcotry: {s_urlFirst}");
                return;
            }

            s_match2 = Regex.Matches(s_urlSecond, "/");
            int parse2 = s_match2.Count;
            string[] parseUrl2 = s_urlSecond.Split('/');
            string fileUrl2 = parseUrl2[parse2];
            Console.WriteLine($"Downloading {fileUrl2} in {s_urlFirst}\\ .......");
            var source = new Uri(s_urlSecond);
            s_stopWatch.Start();
            s_client.DownloadFile(source, s_urlFirst + @"\" + fileUrl2);
            FileInfo fileInfo = new FileInfo(s_urlFirst + @"\" + fileUrl2);
            s_workerDirectory.ReportProgress(Convert.ToInt32(fileInfo.Length));
            s_stopWatch.Stop();
            s_timeSpan = s_stopWatch.Elapsed;
            Console.WriteLine("Downloaded in " + s_urlFirst + @"\" + fileUrl2);
            Console.WriteLine($"Elapsed download time: {s_timeSpan.Seconds} seconds ");
            s_resetEvent.Set();
        }
    }
}
