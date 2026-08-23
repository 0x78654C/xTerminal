using Core.Network;
using Core.SystemTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Core.Updater
{
    [SupportedOSPlatform("windows")]
    public class GitHubAPI
    {
        const string owner = "0x78654c";
        const string repo = "xTerminal";


        /// <summary>
        /// Compares two version strings and returns true if the latestVersion is newer than the currentVersion.
        /// </summary>
        /// <param name="currentVersion"></param>
        /// <param name="latestVersion"></param>
        /// <returns></returns>
        public bool IsNewerVersion(string currentVersion, string latestVersion)
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

                var release = realeases.FirstOrDefault();
                if (IsNewerVersion(version, GetVersionFromTag(release.TagName ?? "0.0.0")))
                {
                    GlobalVariables.isNewVersion = true;
                    GlobalVariables.versionNew = GetVersionFromTag(release.TagName ?? "0.0.0");
                }
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine($"Error checking for new versions: {ex.Message}");
            }
        }


        /// <summary>
        /// Update check for new version available on GitHub.
        /// </summary>
        public void CheckUpdate()
        {
            try
            {
                if (NetWork.IntertCheck())
                {
                    var pathExecutable = Path.GetDirectoryName(Application.ExecutablePath);
                    var xterminalDll = @$"{pathExecutable}\xTerminal.dll";
                    var xUpdaterExe = @$"{pathExecutable}\xUpdater.exe";
                    var xUpdateNew = @$"{GlobalVariables.unpackUpdate}\xUpdateNew.exe";

                    var verExe = File.Exists(xterminalDll) ? AssemblyName.GetAssemblyName(xterminalDll).Version.ToString() : "File does not exist!";
                    var arch = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                    var githubAPI = new GitHubAPI();
                    Task.Run(() => githubAPI.CheckNewVersions(verExe, arch)).Wait();
                    if (GlobalVariables.isNewVersion)
                    {
                        FileSystem.ColorConsoleText(ConsoleColor.Cyan, "A new version of xTerminal is available on GitHub:\n");
                        FileSystem.ColorConsoleText(ConsoleColor.Yellow, $"\nCurrent version: {verExe}\n" +
                        $"New version: {GlobalVariables.versionNew}\n");
                        FileSystem.ColorConsoleText(ConsoleColor.Cyan, "\nDo you want to update? Yes [Y]/ No [N]: ");
                        var key = Console.ReadKey();
                        Console.WriteLine();

                        if (key.KeyChar.ToString().Equals("Y", StringComparison.OrdinalIgnoreCase))
                            ProcessStart.ProcessExecute(xUpdaterExe, pathExecutable, true, false, false, true, "");
                        else
                            Console.Clear();
                    }
                    else
                    {
                        // Copy new updater.
                        if (File.Exists(xUpdateNew))
                        {
                            var verNew = AssemblyName.GetAssemblyName(xUpdateNew).Version.ToString();
                            var verOld = AssemblyName.GetAssemblyName(xUpdaterExe).Version.ToString();
                            var isNewUpdate = githubAPI.IsNewerVersion(verNew, verOld);
                            if (isNewUpdate)
                                File.Copy(xUpdateNew, xUpdaterExe, true);
                        }
                    }
                    // Delete unpackUpdate directory after update finishes.
                    if (Directory.Exists(GlobalVariables.unpackUpdate))
                    {
                        Directory.Delete(GlobalVariables.unpackUpdate, true);
                        Directory.CreateDirectory(GlobalVariables.unpackUpdate);
                    }
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine($"checking for new version: {e.Message}");
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
