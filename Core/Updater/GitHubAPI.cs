using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.Versioning;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Core.Updater
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
        /// Check repo newst version.
        /// </summary>
        /// <param name="version"></param>
        /// <param name="arhitecture"></param>
        /// <returns></returns>
        public async Task CheckNewVersions(string version, string arhitecture)
        {
            try
            {
                GlobalVariables.isNewVersion = false;
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
                    GlobalVariables.isNewVersion = true;
                    GlobalVariables.versionNew = GetVersionFromTag(release.TagName ?? "0.0.0");
                    foreach (var asset in release.Assets)
                        if (asset.DownloadUrl.Contains(arhitecture) && asset.Name.StartsWith("xTerminal"))
                        {
                            _downloadLink = asset.DownloadUrl;
                            _sha256Hash = asset.Digest;
                        }
                }
            }
            catch(Exception ex) {
                FileSystem.ErrorWriteLine($"Error checking for new versions: {ex.Message}");
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
