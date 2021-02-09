using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WGet
{
    class Program
    {
        /*WGet command*/


        private static string url;
        private static string url0;

        static void Main(string[] args)
        {
            BackgroundWorker worker;
            WebClient client = new WebClient();
            MatchCollection match2;
            MatchCollection match;
            //reading current location(for test no, after i make dynamic)
            string dlocation = File.ReadAllText(@".\Data\curDir.ini");
            string cLocation = Directory.GetCurrentDirectory();
   
            int parse;
            string[] parseUrl;
            string fileUrl = string.Empty;

            try
            {

                url = args[0];  //url input

                if (url.Contains(@"\"))
                {
                    string[] pUrl = url.Split(' ');
                    Console.WriteLine("Dir: "+pUrl[0]);
                    Console.WriteLine("url: " + pUrl[1]);
                    match2 = Regex.Matches(pUrl[1], "/");
                    int parse2 = match2.Count;
                    string[] parseUrl2 = pUrl[1].Split('/');
                    string fileUrl2 = parseUrl2[parse2];

                    Console.WriteLine("Downloading " + pUrl[0] + fileUrl2 + " ......");
                    client.DownloadFile(url, pUrl[0] + fileUrl);
                    Console.WriteLine("Downloaded in " + pUrl[0] + fileUrl);
                }
                else
                {
                    match = Regex.Matches(url, "/");
                    parse = match.Count;
                    parseUrl = url.Split('/');
                    fileUrl = parseUrl[parse];
                    Console.WriteLine("Downloading " + dlocation + @"\" + fileUrl + " ......");
                    client.DownloadFile(url, dlocation + @"\" + fileUrl);
                    Console.WriteLine("Downloaded in " + dlocation + @"\" + fileUrl);
                }


            }
            catch (Exception e)
            {
                url0 = args[0];
                url = args[1];
                match = Regex.Matches(url, "/");
                parse = match.Count;
                parseUrl = url.Split('/');
                fileUrl = parseUrl[parse];
                Console.WriteLine("Downloading " + url0 + fileUrl + " ......");
                client.DownloadFile(url, url0 + fileUrl);
                Console.WriteLine("Downloaded in " + url0 + fileUrl);
            }

          //  worker = new BackgroundWorker();
           // worker.DoWork += downloadWget;
           // worker.RunWorkerAsync();

        }

    }
}
