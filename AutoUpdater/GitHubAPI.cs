/*
    Autoupdater for xTerminal. This class interacts with the GitHub API to check for updates, download the latest release, and unpack it if necessary.
 */

using Core;
using Core.Encryption;
using Core.Network;
using System;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AutoUpdater
{
    public class GitHubAPI
    {
        const string owner = "0x78654c";
        const string repo = "xTerminal";
        private string _sha256Hash = "";
        private string _downloadPath = "";


        /// <summary>
        /// Compares two version strings and returns true if the latestVersion is newer than the currentVersion.
        /// </summary>
        /// <param name="currentVersion"></param>
        /// <param name="latestVersion"></param>
        /// <returns></returns>
        private bool IsNewerVersion(string currentVersion, string latestVersion)
        {
            var current = new Version(currentVersion);
            var latest = new Version(latestVersion);
            return latest > current;
        }

        /// <summary>
        /// Get version from tag, removing the leading 'v' if present.
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        private string GetVersionFromTag(string tag)
        {
            if (tag.StartsWith("v"))
            {
                return tag.Substring(1);
            }
            return tag;
        }

        /// <summary>
        /// Extracts the contents of a zip file to a specified directory and checks if the extraction was successful by verifying the existence of a specific file.
        /// </summary>
        /// <param name="zipFilePath"></param>
        /// <param name="extractPath"></param>
        /// <param name="success"></param>
        private void UnpackZip(string zipFilePath, string extractPath, out bool success)
        {
            success = false;
            if (File.Exists(zipFilePath))
            {
                ZipFile.ExtractToDirectory(zipFilePath, extractPath);
                Console.WriteLine($"Extracted to: {extractPath}");
                var extractedFiles = $"{extractPath}\\xTerminal.exe";
                if (File.Exists(extractedFiles))
                {
                    success = true;
                    File.Delete(zipFilePath);
                }
            }
        }

        /// <summary>
        /// Downloads a file from a specified URL and saves it to a specified destination path. If the destination directory does not exist, it creates it.
        /// After downloading, it checks if the file exists and prints the download location.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="destinationPath"></param>
        private void DownloadFile(string url, string destinationPath, out bool success)
        {
            success = false;
            if (!Directory.Exists(destinationPath))
                Directory.CreateDirectory(destinationPath);
            var client = new HttpClient();
            var getUri = UriSafety.CreateHttpUri(url);
            var fileName = UriSafety.GetSafeDownloadPath(getUri, destinationPath);
            var response = client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();
            var fs = new FileStream(fileName, FileMode.Create);
            response.Content.CopyToAsync(fs).Wait();
            fs.Flush();
            fs.Close();
            if (File.Exists(fileName))
            {
                var shsum = HashAlgo.GetSHA256(fileName);
                bool isValid = shsum.Equals(_sha256Hash.Replace("sha256:", ""), StringComparison.OrdinalIgnoreCase);
                success = isValid;
                _downloadPath = fileName;   
            }
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="version"></param>
        /// <param name="arhitecture"></param>
        /// <returns></returns>
        public async Task ListReporsetories(string version, string arhitecture)
        {
            var isValidDownload = false;
            var isUnpacked = false;
            using var client = new HttpClient()
            {
                BaseAddress = new Uri("https://api.github.com/")
            };

            client.DefaultRequestHeaders.UserAgent.ParseAdd("ReleaseLister/1.0");

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            var realeases = await client.GetFromJsonAsync<List<Release>>(
                $"repos/{owner}/{repo}/releases?per_page=1"
                );

            var zip = "";

            foreach (var release in realeases)
            {
                if (IsNewerVersion(version, GetVersionFromTag(release.TagName ?? "0.0.0")))
                    break;

                foreach (var asset in release.Assets)
                    if (asset.DownloadUrl.Contains(arhitecture) && asset.Name.StartsWith("xTerminal"))
                    {
                        zip = asset.DownloadUrl;
                        _sha256Hash = asset.Digest;
                    }
            }

            if (string.IsNullOrEmpty(zip))
                return;

            DownloadFile(zip, GlobalVariables.unpackUpdate, out isValidDownload);
            UnpackZip(_downloadPath, GlobalVariables.unpackUpdate, out isUnpacked);
        }

        /// <summary>
        /// Provides information about a release asset, including its name, download URL, and digest.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="DownloadUrl"></param>
        /// <param name="Digest"></param>
        public sealed record ReleaseAsset(
            [property: JsonPropertyName("name")] string Name,
            [property: JsonPropertyName("browser_download_url")] string DownloadUrl,
            [property: JsonPropertyName("digest")] string Digest
        );


        /// <summary>
        /// Represents a GitHub release, including its name, tag name, download URLs for zip and tarball formats, and a list of associated assets.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="TagName"></param>
        /// <param name="ZipballUrl"></param>
        /// <param name="TarballUrl"></param>
        /// <param name="Assets"></param>
        public sealed record Release(
            [property: JsonPropertyName("name")] string? Name,
            [property: JsonPropertyName("tag_name")] string? TagName,
            [property: JsonPropertyName("zipball_url")] string? ZipballUrl,
            [property: JsonPropertyName("tarball_url")] string? TarballUrl,
            [property: JsonPropertyName("assets")] List<ReleaseAsset> Assets
        );

    }
}
