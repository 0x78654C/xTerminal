using SpeedTest.Models;

namespace SpeedTest
{
    public interface ISpeedTestClient
    {
        /// <summary>
        /// Download SpeedTest.net settings
        /// </summary>
        /// <returns>SpeedTest.net settings</returns>
        Settings GetSettings();

        /// <summary>
        /// Test latency (ping) to server
        /// </summary>
        /// <returns>Latency in milliseconds (ms)</returns>
        int TestServerLatency(Server server, int retryCount = 3);

        /// <summary>
        /// Test download speed to server
        /// </summary>
        /// <returns>Download speed in Kbps</returns>
        double TestDownloadSpeed(Server server, int simultaneousDownloads = 2, int retryCount = 2);

        /// <summary>
        /// Test upload speed to server
        /// </summary>
        /// <returns>Upload speed in Kbps</returns>
        double TestUploadSpeed(Server server, int simultaneousUploads = 2, int retryCount = 2);
    }
}