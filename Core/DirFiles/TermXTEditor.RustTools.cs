using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Core.DirFiles
{
    public sealed partial class TermXTEditor
    {
        private static string RustupHome()
        {
            return Environment.GetEnvironmentVariable("RUSTUP_HOME") ??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".rustup");
        }

        private static string FindBundledRustAnalyzer(string extensionsDirectory)
        {
            try
            {
                if (!Directory.Exists(extensionsDirectory)) return null;
                const string prefix = "rust-lang.rust-analyzer-";
                return Directory.EnumerateDirectories(extensionsDirectory, prefix + "*")
                    .Select(directory => new
                    {
                        Executable = Path.Combine(directory, "server", "rust-analyzer.exe"),
                        Version = Version.TryParse(Path.GetFileName(directory).Substring(prefix.Length).Split('-')[0], out Version version)
                            ? version : new Version(0, 0)
                    })
                    .Where(candidate => File.Exists(candidate.Executable))
                    .OrderByDescending(candidate => candidate.Version)
                    .Select(candidate => candidate.Executable).FirstOrDefault();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { return null; }
        }

        private static void ConfigureRustAnalyzerTools(ProcessStartInfo startInfo, string root)
        {
            string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var directories = new List<string>(path.Split(Path.PathSeparator));
            string cargoHome = Environment.GetEnvironmentVariable("CARGO_HOME") ??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cargo");
            string proxies = Path.Combine(cargoHome, "bin");
            // Prefer rustup's proxies: they apply all of rustup's toolchain overrides.
            if (File.Exists(Path.Combine(proxies, "cargo.exe")) && File.Exists(Path.Combine(proxies, "rustc.exe")))
                directories.Add(proxies);
            if (!RustExecutableInDirectories(directories, "cargo.exe") || !RustExecutableInDirectories(directories, "rustc.exe"))
            {
                string installed = FindInstalledRustToolchain(root, RustupHome(), Environment.GetEnvironmentVariable("RUSTUP_TOOLCHAIN"));
                if (installed != null) directories.Insert(0, installed);
            }
            // Apply discovery only to the server and its children, never the user's PATH.
            startInfo.Environment["PATH"] = string.Join(Path.PathSeparator, directories);
        }

        private static bool RustExecutableInDirectories(IEnumerable<string> directories, string executable)
        {
            return directories.Any(directory => !string.IsNullOrWhiteSpace(directory) &&
                File.Exists(Path.Combine(directory.Trim('"'), executable)));
        }

        private static async Task<object> CreateRustStandaloneProjectAsync(string root, string file, CancellationToken cancellation)
        {
            var info = new ProcessStartInfo
            {
                WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            ConfigureRustAnalyzerTools(info, root);
            info.FileName = info.Environment["PATH"].Split(Path.PathSeparator)
                .Where(directory => !string.IsNullOrWhiteSpace(directory))
                .Select(directory => Path.Combine(directory.Trim('"'), "rustc.exe"))
                .FirstOrDefault(File.Exists) ?? throw new IOException("Rust compiler not found. Install Rust with rustup.");
            info.ArgumentList.Add("--print");
            info.ArgumentList.Add("sysroot");
            using var compiler = Process.Start(info);
            try
            {
                Task<string> output = compiler.StandardOutput.ReadToEndAsync(cancellation);
                Task<string> error = compiler.StandardError.ReadToEndAsync(cancellation);
                await compiler.WaitForExitAsync(cancellation).WaitAsync(TimeSpan.FromSeconds(30), cancellation).ConfigureAwait(false);
                string sysroot = (await output.ConfigureAwait(false)).Trim();
                string failure = (await error.ConfigureAwait(false)).Trim();
                if (compiler.ExitCode != 0 || !Directory.Exists(sysroot))
                    throw new IOException("Cannot locate Rust standard library: " + failure);
                string sources = Path.Combine(sysroot, "lib", "rustlib", "src", "rust", "library");
                if (!Directory.Exists(sources))
                    throw new IOException("Rust standard-library sources missing. Run: rustup component add rust-src");
                // An inline project gives standalone files a stable source root and
                // sysroot, without creating Cargo.toml or changing the user's files.
                return new
                {
                    sysroot, sysroot_src = sources,
                    crates = new[] { new { root_module = file, edition = "2021", deps = Array.Empty<object>(), is_workspace_member = true } }
                };
            }
            finally
            {
                try { if (!compiler.HasExited) compiler.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { }
                catch (System.ComponentModel.Win32Exception) { }
            }
        }

        private static string FindInstalledRustToolchain(string root, string rustupHome, string environmentOverride)
        {
            try
            {
                string settingsPath = Path.Combine(rustupHome, "settings.toml");
                string[] settings = File.Exists(settingsPath) ? File.ReadAllLines(settingsPath) : Array.Empty<string>();
                string selected = environmentOverride;
                if (string.IsNullOrWhiteSpace(selected))
                {
                    // Rustup stores its directory overrides in settings.toml. The nearest
                    // override or toolchain file wins while walking toward the drive root.
                    Dictionary<string, string> overrides = ReadRustupDirectoryOverrides(settings);
                    for (var directory = new DirectoryInfo(root); directory != null; directory = directory.Parent)
                    {
                        if (overrides.TryGetValue(directory.FullName, out selected)) break;
                        string toolchainFile = Path.Combine(directory.FullName, "rust-toolchain");
                        if (!File.Exists(toolchainFile)) toolchainFile = Path.Combine(directory.FullName, "rust-toolchain.toml");
                        if (!File.Exists(toolchainFile)) continue;
                        string[] lines = File.ReadAllLines(toolchainFile);
                        selected = ReadRustSetting(lines, "channel");
                        string customPath = ReadRustSetting(lines, "path");
                        if (customPath != null) return RustToolchainBin(customPath);
                        if (selected == null && Path.GetFileName(toolchainFile) == "rust-toolchain")
                            selected = lines.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
                        // Do not silently substitute a different compiler for a pinned project.
                        if (selected == null) return null;
                        break;
                    }
                }
                if (string.IsNullOrWhiteSpace(selected)) selected = ReadRustSetting(settings, "default_toolchain");
                if (string.IsNullOrWhiteSpace(selected)) return null;
                string toolchains = Path.Combine(rustupHome, "toolchains");
                string exact = RustToolchainBin(Path.IsPathRooted(selected) ? selected : Path.Combine(toolchains, selected));
                if (exact != null) return exact;
                string host = ReadRustSetting(settings, "default_host_triple");
                return host == null ? null : RustToolchainBin(Path.Combine(toolchains, selected + "-" + host));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException) { return null; }
        }

        private static string RustToolchainBin(string toolchain)
        {
            string bin = Path.Combine(toolchain, "bin");
            return File.Exists(Path.Combine(bin, "cargo.exe")) && File.Exists(Path.Combine(bin, "rustc.exe")) ? bin : null;
        }

        // Only read scalar settings used for locating existing tools. Cargo/rustup still
        // own project configuration; xte neither changes settings nor installs toolchains.
        private static string ReadRustSetting(IEnumerable<string> lines, string key)
        {
            foreach (string line in lines)
            {
                int equals = line.IndexOf('=');
                if (equals < 0 || line.Substring(0, equals).Trim() != key) continue;
                return ReadRustSettingString(line.Substring(equals + 1).Trim());
            }
            return null;
        }

        private static Dictionary<string, string> ReadRustupDirectoryOverrides(IEnumerable<string> lines)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool inOverrides = false;
            foreach (string value in lines)
            {
                string line = value.Trim();
                if (line.StartsWith("[", StringComparison.Ordinal)) { inOverrides = line == "[overrides]"; continue; }
                if (!inOverrides) continue;
                int equals = line.LastIndexOf('=');
                if (equals < 0) continue;
                string directory = ReadRustSettingString(line.Substring(0, equals).Trim());
                string toolchain = ReadRustSettingString(line.Substring(equals + 1).Trim());
                if (directory != null && toolchain != null) result[directory] = toolchain;
            }
            return result;
        }

        private static string ReadRustSettingString(string value)
        {
            if (value.Length < 2 || (value[0] != '"' && value[0] != '\'')) return null;
            char quote = value[0];
            for (int end = 1; end < value.Length; end++)
            {
                if (quote == '"' && value[end] == '\\') { end++; continue; }
                if (value[end] != quote) continue;
                if (quote == '\'') return value.Substring(1, end - 1);
                try { return JsonSerializer.Deserialize<string>(value.Substring(0, end + 1)); }
                catch (JsonException) { return null; }
            }
            return null;
        }
    }
}
