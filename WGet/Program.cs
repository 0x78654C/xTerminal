using Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using CheckType = Core.FileSystem.CheckType;
namespace WGet
{
    class Program
    {
        /*WGet command*/

        // Declare global variables
        private static string s_urlFirst;
        private static string s_urlSecond;
        private static Stopwatch s_stopWatch;
        private static TimeSpan s_timeSpan;
        private static readonly WebClient s_client = new WebClient();
        private static MatchCollection s_match2;
        private static MatchCollection s_match;

        static void Main(string[] args)
        {
            s_timeSpan = new TimeSpan();
            s_stopWatch = new Stopwatch();
            
            try
            {
                s_urlFirst = args[0];  //url input

                if (s_urlFirst.Contains(@"\"))
                {
                    s_urlSecond = args[1];
                    DownloadFile(s_urlSecond, s_urlFirst);
                }
                else
                {
                  DownloadFile(s_urlFirst);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");             
            }
        }

        // Download file in current directory
        private static void DownloadFile(string urlData)
        {
            string dlocation = File.ReadAllText(GlobalVariables.currentLocation);
            int parse;
            string[] parseUrl;
            string fileUrl;
            s_timeSpan = new TimeSpan();
            s_stopWatch = new Stopwatch();
            s_match = Regex.Matches(urlData, "/");
            parse = s_match.Count;
            parseUrl = urlData.Split('/');
            fileUrl = parseUrl[parse];
            Console.WriteLine("Downloading " + dlocation + @"\" + fileUrl + " ......");
            var source = new Uri(urlData);
            s_stopWatch.Start();
            s_client.DownloadFile(source, dlocation + @"\" + fileUrl);
            s_stopWatch.Stop();
            s_timeSpan = s_stopWatch.Elapsed;
            Console.WriteLine("Downloaded in " + dlocation + @"\" + fileUrl);
            Console.WriteLine($"Elapsed download time: {s_timeSpan.Seconds} seconds ");
        }

        // Download file in other directory
        private static void DownloadFile(string urlData, string savePath)
        {
            if (!Directory.Exists(savePath))
            {
                Console.WriteLine($"Directory: {savePath} does not exist!");
                return;
            }
            
            if (!FileSystem.CheckPermission(savePath,CheckType.Directory))
            {
                return;
            }
            
            s_match2 = Regex.Matches(urlData, "/");
            int parse2 = s_match2.Count;
            string[] parseUrl2 = urlData.Split('/');
            string fileUrl2 = parseUrl2[parse2];
            Console.WriteLine("Downloading " + savePath + @"\" + fileUrl2 + " ......");
            var source = new Uri(urlData);
            s_stopWatch.Start();
            s_client.DownloadFile(source, savePath + @"\" + fileUrl2);
            s_stopWatch.Stop();
            s_timeSpan = s_stopWatch.Elapsed;
            Console.WriteLine("Downloaded in " + savePath + @"\" + fileUrl2);
            Console.WriteLine($"Elapsed download time: {s_timeSpan.Seconds} seconds ");
        }
    }
}
