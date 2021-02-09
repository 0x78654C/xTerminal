using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SpeedTest.Models;

namespace SpeedTest
{
    public class SpeedTestClient : ISpeedTestClient
    {
        private const string ConfigUrl = "https://www.speedtest.net/speedtest-config.php";

        private static readonly string[] ServersUrls = {
            "https://www.speedtest.net/speedtest-servers-static.php",
            "https://c.speedtest.net/speedtest-servers-static.php",
            "https://www.speedtest.net/speedtest-servers.php",
            "https://c.speedtest.net/speedtest-servers.php"
        };

        private static readonly int[] DownloadSizes = { 350, 750, 1500, 3000 };
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const int MaxUploadSize = 4; // 400 KB

        #region ISpeedTestClient

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException"></exception>
        public Settings GetSettings()
        {
            using var client = new SpeedTestHttpClient();
            var settings = client.GetConfig<Settings>(ConfigUrl).GetAwaiter().GetResult();

            var serversConfig = new ServersList();
            foreach (var serversUrl in ServersUrls)
            {
                try
                {
                    serversConfig = client.GetConfig<ServersList>(serversUrl).GetAwaiter().GetResult();
                    if (serversConfig.Servers.Count > 0) break;
                }
                catch
                {
                    //
                }
            }

            if (serversConfig.Servers.Count <= 0)
            {
                throw new InvalidOperationException("SpeedTest does not return any server");
            }

            var ignoredIds = settings.ServerConfig.IgnoreIds.Split(",", StringSplitOptions.RemoveEmptyEntries);
            serversConfig.CalculateDistances(settings.Client.GeoCoordinate);
            settings.Servers = serversConfig.Servers
                .Where(s => !ignoredIds.Contains(s.Id.ToString()))
                .OrderBy(s => s.Distance)
                .ToList();

            return settings;
        }

        /// <inheritdoc />
        public int TestServerLatency(Server server, int retryCount = 3)
        {
            var latencyUri = CreateTestUrl(server, "latency.txt");
            var timer = new Stopwatch();

            using var client = new SpeedTestHttpClient();

            for (var i = 0; i < retryCount; i++)
            {
                string testString;
                try
                {
                    timer.Start();
                    testString = client.GetStringAsync(latencyUri).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (WebException)
                {
                    continue;
                }
                finally
                {
                    timer.Stop();    
                }

                if (!testString.StartsWith("test=test"))
                {
                    throw new InvalidOperationException("Server returned incorrect test string for latency.txt");
                }
            }

            return (int)timer.ElapsedMilliseconds / retryCount;
        }

        /// <inheritdoc />
        public double TestDownloadSpeed(Server server, int simultaneousDownloads = 2, int retryCount = 2)
        {
            var testData = GenerateDownloadUrls(server, retryCount);

            return TestSpeed(testData, async (client, url) =>
            {
                var data = await client.GetByteArrayAsync(url).ConfigureAwait(false);
                return data.Length;
            }, simultaneousDownloads);
        }
        
        /// <inheritdoc />
        public double TestUploadSpeed(Server server, int simultaneousUploads = 2, int retryCount = 2)
        {
            var testData = GenerateUploadData(retryCount);
            return TestSpeed(testData, async (client, uploadData) =>
            {
                await client.PostAsync(server.Url, new StringContent(uploadData));
                return uploadData.Length;
            }, simultaneousUploads);
        }

        #endregion

        #region Helpers

        private static double TestSpeed<T>(IEnumerable<T> testData, Func<HttpClient, T, Task<int>> doWork, int concurrencyCount = 2)
        {
            var timer = new Stopwatch();
            var throttler = new SemaphoreSlim(concurrencyCount);

            timer.Start();
            var downloadTasks = testData.Select(async data =>
            {
                await throttler.WaitAsync().ConfigureAwait(false);
                var client = new SpeedTestHttpClient();
                try
                {
                    var size = await doWork(client, data).ConfigureAwait(false);
                    return size;
                }
                finally
                {
                    client.Dispose();
                    throttler.Release();
                }
            }).ToArray();

            Task.WaitAll(downloadTasks);
            timer.Stop();

            double totalSize = downloadTasks.Sum(task => task.Result);
            return (totalSize * 8 / 1024) / ((double)timer.ElapsedMilliseconds / 1000);
        }

        private static IEnumerable<string> GenerateUploadData(int retryCount)
        {
            var random = new Random();
            var result = new List<string>();

            for (var sizeCounter = 1; sizeCounter < MaxUploadSize+1; sizeCounter++)
            {
                var size = sizeCounter*200*1024;
                var builder = new StringBuilder(size);

                builder.AppendFormat("content{0}=", sizeCounter);

                for (var i = 0; i < size; ++i)
                {
                    builder.Append(Chars[random.Next(Chars.Length)]);
                }

                for (var i = 0; i < retryCount; i++)
                {
                    result.Add(builder.ToString());
                }
            }

            return result;
        }

        private static string CreateTestUrl(Server server, string file)
        {
            return new Uri(new Uri(server.Url), ".").OriginalString + file;
        }

        private static IEnumerable<string> GenerateDownloadUrls(Server server, int retryCount)
        {
            var downloadUriBase = CreateTestUrl(server, "random{0}x{0}.jpg?r={1}");
            foreach (var downloadSize in DownloadSizes)
            {
                for (var i = 0; i < retryCount; i++)
                {
                    yield return string.Format(downloadUriBase, downloadSize, i);
                }
            }
        }

        #endregion
    }
}
