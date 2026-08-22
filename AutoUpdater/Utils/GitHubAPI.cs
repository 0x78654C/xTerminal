using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.Versioning;
using System.Text.Json.Serialization;

namespace AutoUpdater.Utils
{
    [SupportedOSPlatform("windows")]
    public class GitHubAPI
    {
        const string owner = "0x78654c";
        const string repo = "xTerminal";
        private string _sha256Hash = "";
        private string _downloadPath = "";
        private string _downloadLink = "";


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
            try
            {
                if (File.Exists(zipFilePath))
                {
                    var fileInfo = new FileInfo(zipFilePath);
                    Console.WriteLine($"Unpacking: {fileInfo.Name} ....");
                    ZipFile.ExtractToDirectory(zipFilePath, extractPath, true);
                    var extractedFiles = $"{extractPath}\\xTerminal.exe";
                    if (File.Exists(extractedFiles))
                    {
                        success = true;
                        File.Delete(zipFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                UI.ErrorWriteLine($"Unpacking zip file: {ex.Message}");
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
            try
            {
                success = false;
                if (!Directory.Exists(destinationPath))
                    Directory.CreateDirectory(destinationPath);
                var client = new HttpClient();
                var getUri = UriSafety.CreateHttpUri(url);
                var fileName = UriSafety.GetSafeDownloadPath(getUri, destinationPath);
                var fileInfo = new FileInfo(fileName);
                Console.WriteLine($"Downloading: {fileInfo.Name} ....");
                var response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                var fs = new FileStream(fileName, FileMode.Create);
                response.Content.CopyToAsync(fs).Wait();
                fs.Flush();
                fs.Close();
                if (File.Exists(fileName))
                {
                    var shsum = Encryption.GetSHA256(fileName);
                    bool isValid = shsum.Equals(_sha256Hash.Replace("sha256:", ""), StringComparison.OrdinalIgnoreCase);
                    success = isValid;
                    _downloadPath = fileName;
                }
            }
            catch (Exception ex)
            {
                success = false;
                UI.ErrorWriteLine($"Downloading file: {ex.Message}");
            }
        }


        /// <summary>
        /// Check repo newst version.
        /// </summary>
        /// <param name="version"></param>
        /// <param name="arhitecture"></param>
        /// <returns></returns>
        public async Task CheckNewVersions(string version, string arhitecture)
        {
            try
            {
                using var client = new HttpClient()
                {
                    BaseAddress = new Uri("https://api.github.com/")
                };

                client.DefaultRequestHeaders.UserAgent.ParseAdd("ReleaseLister/1.0");

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

                var realeases = await client.GetFromJsonAsync<List<Release>>(
                    $"repos/{owner}/{repo}/releases?per_page=1"
                    );


                foreach (var release in realeases)
                {
                    if (!IsNewerVersion(version, GetVersionFromTag(release.TagName ?? "0.0.0")))
                        break;
                    foreach (var asset in release.Assets)
                        if (asset.DownloadUrl.Contains(arhitecture) && asset.Name.StartsWith("xTerminal"))
                        {
                            _downloadLink = asset.DownloadUrl;
                            _sha256Hash = asset.Digest;
                        }
                }
            }
            catch (Exception ex)
            {
                UI.ErrorWriteLine($"Error checking for new versions: {ex.Message}");
            }
        }

        /// <summary>
        /// Download and unpack the new update.
        /// </summary>
        public void DownloadUpdate(string xTerminalPath)
        {
            try
            {
                var isValidDownload = false;
                var isUnpacked = false;
                DownloadFile(_downloadLink, GlobalVariables.unpackUpdate, out isValidDownload);
                UnpackZip(_downloadPath, GlobalVariables.unpackUpdate, out isUnpacked);
                var fileSize = new DirectoryInfo(GlobalVariables.unpackUpdate).EnumerateFiles("*").Sum(f => f.Length);
                if (fileSize > 0)
                {
                    Console.WriteLine($"Copy files....");
                    ClearFolder(xTerminalPath);
                    Thread.Sleep(2000);
                    CopyNewFiles(GlobalVariables.unpackUpdate, xTerminalPath);
                    Thread.Sleep(2000);
                    Console.WriteLine($"Finished update. Starting xTerminal....");
                    if (File.Exists(Path.Combine(xTerminalPath, "xTerminal.exe")))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = Path.Combine(xTerminalPath, "xTerminal.exe"),
                            UseShellExecute = true
                        });
                    }
                    else {
                        UI.ErrorWriteLine($"xTerminal.exe not found in {xTerminalPath}");
                        Console.ReadKey();
                    }
                    Thread.Sleep(2000);
                }
            }
            catch (Exception ex)
            {
                UI.ErrorWriteLine(ex.ToString());
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Deletes all files and subdirectories within the specified folder path, effectively clearing the folder's contents.
        /// </summary>
        /// <param name="folderPath"></param>
        public static void ClearFolder(string folderPath)
        {
            foreach (var file in Directory.GetFiles(folderPath))
            {
                if (!file.Contains("xUpdater.exe"))
                    File.Delete(file);
            }

            foreach (var directory in Directory.GetDirectories(folderPath))
                Directory.Delete(directory, recursive: true);
        }

        /// <summary>
        /// Copys all files and subdirectories from the source path to the destination path, creating the destination directory if it does not exist. Existing files in the destination will be overwritten.
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        public void CopyNewFiles(string sourcePath, string destinationPath)
        {
            try
            {
                if (!Directory.Exists(destinationPath))
                    Directory.CreateDirectory(destinationPath);

                // Copy files
                foreach (string file in Directory.GetFiles(sourcePath))
                {
                    string destinationFile = Path.Combine(
                        destinationPath,
                        Path.GetFileName(file));
                    // Skip copying xUpdater.exe to avoid overwriting the updater itself
                    if (file.Contains("xUpdater.exe"))
                        continue;
                    File.Copy(file, destinationFile, overwrite: true);
                }

                // Copy subfolders recursively
                foreach (string folder in Directory.GetDirectories(sourcePath))
                {
                    string destinationSubfolder = Path.Combine(
                        destinationPath,
                        Path.GetFileName(folder));

                    CopyNewFiles(folder, destinationSubfolder);
                }
            }
            catch (Exception ex)
            {
                UI.ErrorWriteLine($"Error copying file: {ex.Message}");
            }
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
