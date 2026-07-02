using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Core.SystemTools
{
    public sealed class NuGetPackageReference
    {
        public NuGetPackageReference(string id, string version)
        {
            Id = id ?? string.Empty;
            Version = version ?? string.Empty;
        }

        public string Id { get; }
        public string Version { get; }

        public string DisplayName
        {
            get
            {
                return string.IsNullOrWhiteSpace(Version)
                    ? Id
                    : Id + " " + Version;
            }
        }
    }

    public sealed class RoslynReferenceSet
    {
        public RoslynReferenceSet(
            List<MetadataReference> references,
            List<string> assemblyPaths,
            List<string> warnings)
        {
            References = references ?? new List<MetadataReference>();
            AssemblyPaths = assemblyPaths ?? new List<string>();
            Warnings = warnings ?? new List<string>();
        }

        public List<MetadataReference> References { get; }
        public List<string> AssemblyPaths { get; }
        public List<string> Warnings { get; }
    }

    public class Roslyn
    {
        public const string NuGetDirectivePrefix = "// nuget:";

        private const string AlternateNuGetDirectivePrefix = "// xte-nuget:";
        private const int DotNetRestoreTimeoutMs = 120000;
        private static readonly Regex s_packageIdRegex = new Regex(
            @"^[A-Za-z0-9][A-Za-z0-9_.-]{0,127}$",
            RegexOptions.Compiled);
        private static readonly Regex s_packageVersionRegex = new Regex(
            @"^[A-Za-z0-9][A-Za-z0-9.\-+*]{0,127}$",
            RegexOptions.Compiled);

        /// <summary>
        /// Get runtime managed assembly references.
        /// </summary>
        public static List<MetadataReference> References()
        {
            return ReferenceSet(null, null).References;
        }

        /// <summary>
        /// Get runtime managed assembly references plus NuGet package references declared in C# source.
        /// </summary>
        public static List<MetadataReference> References(string sourcePath, string sourceText)
        {
            return ReferenceSet(sourcePath, sourceText).References;
        }

        public static RoslynReferenceSet ReferenceSet(string sourcePath, string sourceText)
        {
            var references = TrustedPlatformReferences();
            var assemblyPaths = new List<string>();
            var warnings = new List<string>();
            var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (NuGetPackageReference package in GetNuGetPackageReferences(sourceText))
            {
                if (!TryResolveNuGetPackageReferencePaths(package, out List<string> packagePaths, out string error))
                {
                    if (!string.IsNullOrWhiteSpace(error))
                        warnings.Add(error);

                    continue;
                }

                foreach (string path in packagePaths)
                {
                    if (!addedPaths.Add(path))
                        continue;

                    try
                    {
                        references.Add(MetadataReference.CreateFromFile(path));
                        assemblyPaths.Add(path);
                    }
                    catch (Exception ex)
                    {
                        warnings.Add("Could not reference " + path + ": " + ex.Message);
                    }
                }
            }

            return new RoslynReferenceSet(references, assemblyPaths, warnings);
        }

        public static IReadOnlyList<NuGetPackageReference> GetNuGetPackageReferences(string sourceText)
        {
            var packages = new List<NuGetPackageReference>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(sourceText))
                return packages;

            using (var reader = new StringReader(sourceText))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!TryParseNuGetDirectiveLine(line, out NuGetPackageReference package))
                        continue;

                    if (seen.Add(package.Id))
                        packages.Add(package);
                }
            }

            return packages;
        }

        public static bool TryCreateNuGetPackageDirective(
            string packageId,
            string version,
            out string directive,
            out string error)
        {
            directive = string.Empty;
            error = string.Empty;

            packageId = (packageId ?? string.Empty).Trim();
            version = (version ?? string.Empty).Trim();

            if (!IsValidNuGetPackageId(packageId))
            {
                error = "Invalid NuGet package id.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(version) && !IsValidNuGetPackageVersion(version))
            {
                error = "Invalid NuGet package version.";
                return false;
            }

            directive = string.IsNullOrWhiteSpace(version)
                ? NuGetDirectivePrefix + " " + packageId
                : NuGetDirectivePrefix + " " + packageId + " " + version;
            return true;
        }

        public static bool TryRestoreNuGetPackage(
            NuGetPackageReference package,
            string workingDirectory,
            out NuGetPackageReference restoredPackage,
            out string message)
        {
            restoredPackage = package;
            message = string.Empty;

            if (package == null || !IsValidNuGetPackageId(package.Id))
            {
                message = "Invalid NuGet package id.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(package.Version) && !IsValidNuGetPackageVersion(package.Version))
            {
                message = "Invalid NuGet package version.";
                return false;
            }

            if (!ShouldResolveLatestStablePackage(package) &&
                TryResolveNuGetPackageReferencePaths(package, out List<string> installedPaths, out _) &&
                installedPaths.Count > 0 &&
                TryGetInstalledPackageVersion(package, out string installedVersion) &&
                !string.IsNullOrWhiteSpace(installedVersion))
            {
                restoredPackage = new NuGetPackageReference(package.Id, installedVersion);
                message = "NuGet package already available: " + restoredPackage.DisplayName;
                return true;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "xterminal-nuget-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(tempDir);
                string projectPath = Path.Combine(tempDir, "restore.csproj");
                File.WriteAllText(
                    projectPath,
                    "<Project Sdk=\"Microsoft.NET.Sdk\">" + Environment.NewLine +
                    "  <PropertyGroup>" + Environment.NewLine +
                    "    <TargetFramework>" + GetCurrentRestoreTargetFramework() + "</TargetFramework>" + Environment.NewLine +
                    "  </PropertyGroup>" + Environment.NewLine +
                    "</Project>" + Environment.NewLine,
                    Encoding.UTF8);

                var startInfo = new ProcessStartInfo("dotnet")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = IsUsableDirectory(workingDirectory) ? workingDirectory : tempDir
                };

                startInfo.ArgumentList.Add("add");
                startInfo.ArgumentList.Add(projectPath);
                startInfo.ArgumentList.Add("package");
                startInfo.ArgumentList.Add(package.Id);

                if (!string.IsNullOrWhiteSpace(package.Version))
                {
                    startInfo.ArgumentList.Add("--version");
                    startInfo.ArgumentList.Add(package.Version);
                }

                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        message = "Could not start dotnet.";
                        return false;
                    }

                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();

                    if (!process.WaitForExit(DotNetRestoreTimeoutMs))
                    {
                        TryKill(process);
                        message = "NuGet restore timed out.";
                        return false;
                    }

                    string output = outputTask.GetAwaiter().GetResult();
                    string error = errorTask.GetAwaiter().GetResult();
                    if (process.ExitCode != 0)
                    {
                        message = RestoreFailureMessage(package, output, error);
                        if (string.IsNullOrWhiteSpace(message))
                            message = "dotnet add package failed with exit code " + process.ExitCode + ".";
                        return false;
                    }
                }

                string resolvedVersion = TryReadPackageVersionFromProject(projectPath, package.Id);
                if (string.IsNullOrWhiteSpace(resolvedVersion) &&
                    TryGetInstalledPackageVersion(package, out installedVersion))
                {
                    resolvedVersion = installedVersion;
                }

                restoredPackage = new NuGetPackageReference(package.Id, resolvedVersion);
                message = "NuGet package ready: " + restoredPackage.DisplayName;
                return true;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                message = "Could not run dotnet: " + ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
            finally
            {
                TryDeleteDirectory(tempDir);
            }
        }

        public static bool IsNuGetCertificateValidationError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            string lower = message.ToLowerInvariant();
            bool mentionsCertificate =
                lower.Contains("certificate") ||
                lower.Contains("x.509") ||
                lower.Contains("x509") ||
                lower.Contains("ssl");
            bool mentionsValidation =
                lower.Contains("chain") ||
                lower.Contains("validation") ||
                lower.Contains("remote certificate") ||
                lower.Contains("untrustedroot") ||
                lower.Contains("partialchain") ||
                lower.Contains("revocation") ||
                lower.Contains("not trusted") ||
                lower.Contains("trust");

            return mentionsCertificate && mentionsValidation;
        }

        public static string NuGetCertificateValidationMessage(string details)
        {
            string suffix = string.IsNullOrWhiteSpace(details)
                ? string.Empty
                : " Details: " + details.Trim();

            return "NuGet HTTPS certificate validation failed. Fix Windows/.NET certificate trust or the configured NuGet source, then run :nuget restore." + suffix;
        }

        public static bool IsNuGetPackageNotFoundError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            string lower = message.ToLowerInvariant();
            return lower.Contains("nu1101") ||
                lower.Contains("nu1102") ||
                lower.Contains("unable to find package") ||
                lower.Contains("no packages exist with this id") ||
                lower.Contains("no versions available for the package") ||
                lower.Contains("package was not found");
        }

        public static string NuGetPackageNotFoundMessage(NuGetPackageReference package, string details)
        {
            string packageName = package == null ? string.Empty : package.DisplayName;
            string prefix = package == null || string.IsNullOrWhiteSpace(package.Version)
                ? "NuGet package not found"
                : "NuGet package/version not found";
            string suffix = string.IsNullOrWhiteSpace(details)
                ? string.Empty
                : " Details: " + details.Trim();

            if (string.IsNullOrWhiteSpace(packageName))
                return prefix + ". Check the package name and configured NuGet sources." + suffix;

            if (package == null || string.IsNullOrWhiteSpace(package.Version))
                return prefix + ": " + packageName + ". Check the package name and configured NuGet sources." + suffix;

            return prefix + ": " + packageName + ". Check the package name, version, and configured NuGet sources." + suffix;
        }

        private static bool ShouldResolveLatestStablePackage(NuGetPackageReference package)
        {
            return package != null && string.IsNullOrWhiteSpace(package.Version);
        }

        public static bool TryResolveNuGetPackageReferencePaths(
            NuGetPackageReference package,
            out List<string> assemblyPaths,
            out string error)
        {
            assemblyPaths = new List<string>();
            error = string.Empty;

            var warnings = new List<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            ResolveNuGetPackage(package, assemblyPaths, addedPaths, visited, warnings, 0);

            if (assemblyPaths.Count > 0)
                return true;

            error = warnings.Count == 0
                ? "NuGet package not found: " + (package == null ? string.Empty : package.DisplayName)
                : warnings[0];
            return false;
        }

        private static List<MetadataReference> TrustedPlatformReferences()
        {
            var references = new List<MetadataReference>();
            var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;

            if (!string.IsNullOrWhiteSpace(trustedAssemblies))
            {
                foreach (string reference in trustedAssemblies.Split(Path.PathSeparator))
                {
                    AddMetadataReference(references, added, reference);
                }
            }

            AddMetadataReference(references, added, typeof(object).Assembly.Location);
            AddMetadataReference(references, added, typeof(Console).Assembly.Location);
            AddMetadataReference(references, added, typeof(Enumerable).Assembly.Location);
            return references;
        }

        private static void AddMetadataReference(
            List<MetadataReference> references,
            HashSet<string> added,
            string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path) || !added.Add(path))
                return;

            references.Add(MetadataReference.CreateFromFile(path));
        }

        private static bool TryParseNuGetDirectiveLine(
            string line,
            out NuGetPackageReference package)
        {
            package = null;
            if (string.IsNullOrWhiteSpace(line))
                return false;

            string trimmed = line.TrimStart();
            string body = string.Empty;

            if (trimmed.StartsWith(NuGetDirectivePrefix, StringComparison.OrdinalIgnoreCase))
            {
                body = trimmed.Substring(NuGetDirectivePrefix.Length).Trim();
            }
            else if (trimmed.StartsWith(AlternateNuGetDirectivePrefix, StringComparison.OrdinalIgnoreCase))
            {
                body = trimmed.Substring(AlternateNuGetDirectivePrefix.Length).Trim();
            }
            else if (trimmed.StartsWith("#r", StringComparison.OrdinalIgnoreCase))
            {
                int nugetIndex = trimmed.IndexOf("nuget:", StringComparison.OrdinalIgnoreCase);
                if (nugetIndex < 0)
                    return false;

                body = trimmed.Substring(nugetIndex + "nuget:".Length).Trim();
                body = body.Trim().Trim('"', '\'');
            }
            else
            {
                return false;
            }

            return TryParseNuGetDirectiveBody(body, out package);
        }

        private static bool TryParseNuGetDirectiveBody(
            string body,
            out NuGetPackageReference package)
        {
            package = null;
            if (string.IsNullOrWhiteSpace(body))
                return false;

            body = body.Trim();
            int commentIndex = body.IndexOf(" //", StringComparison.Ordinal);
            if (commentIndex >= 0)
                body = body.Substring(0, commentIndex).Trim();

            string[] tokens = body
                .Replace(",", " ")
                .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0)
                return false;

            string id = tokens[0].Trim();
            string version = string.Empty;

            for (int i = 1; i < tokens.Length; i++)
            {
                string token = tokens[i].Trim();
                if (token.StartsWith("version=", StringComparison.OrdinalIgnoreCase))
                {
                    version = token.Substring("version=".Length).Trim();
                    break;
                }

                if (token.StartsWith("--", StringComparison.Ordinal))
                    continue;

                version = token;
                break;
            }

            if (!IsValidNuGetPackageId(id))
                return false;

            package = new NuGetPackageReference(id, version);
            return true;
        }

        private static bool IsValidNuGetPackageId(string packageId)
        {
            return !string.IsNullOrWhiteSpace(packageId) &&
                s_packageIdRegex.IsMatch(packageId.Trim());
        }

        private static bool IsValidNuGetPackageVersion(string version)
        {
            return !string.IsNullOrWhiteSpace(version) &&
                s_packageVersionRegex.IsMatch(version.Trim());
        }

        private static bool TryGetInstalledPackageVersion(
            NuGetPackageReference package,
            out string installedVersion)
        {
            installedVersion = string.Empty;
            if (package == null)
                return false;

            string packageDir = FindPackageDirectory(package.Id, package.Version, out installedVersion);
            return !string.IsNullOrWhiteSpace(packageDir);
        }

        private static void ResolveNuGetPackage(
            NuGetPackageReference package,
            List<string> assemblyPaths,
            HashSet<string> addedPaths,
            HashSet<string> visited,
            List<string> warnings,
            int depth)
        {
            if (package == null || depth > 64)
                return;

            if (!IsValidNuGetPackageId(package.Id))
            {
                warnings.Add("Invalid NuGet package id: " + package.Id);
                return;
            }

            string packageDir = FindPackageDirectory(package.Id, package.Version, out string resolvedVersion);
            if (string.IsNullOrWhiteSpace(packageDir))
            {
                warnings.Add("NuGet package not found: " + package.DisplayName + ". Use :nuget restore.");
                return;
            }

            string visitKey = package.Id + "@" + resolvedVersion;
            if (!visited.Add(visitKey))
                return;

            foreach (string path in SelectPackageAssemblyPaths(packageDir))
            {
                if (addedPaths.Add(path))
                    assemblyPaths.Add(path);
            }

            foreach (NuGetPackageReference dependency in GetPackageDependencies(packageDir))
            {
                ResolveNuGetPackage(dependency, assemblyPaths, addedPaths, visited, warnings, depth + 1);
            }
        }

        private static string FindPackageDirectory(
            string packageId,
            string version,
            out string resolvedVersion)
        {
            resolvedVersion = string.Empty;
            string packagesRoot = GetGlobalNuGetPackagesPath();
            if (string.IsNullOrWhiteSpace(packagesRoot) || !Directory.Exists(packagesRoot))
                return string.Empty;

            string packageRoot = Path.Combine(packagesRoot, packageId.ToLowerInvariant());
            if (!Directory.Exists(packageRoot))
            {
                packageRoot = Directory.GetDirectories(packagesRoot)
                    .FirstOrDefault(path => string.Equals(
                        Path.GetFileName(path),
                        packageId,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (string.IsNullOrWhiteSpace(packageRoot) || !Directory.Exists(packageRoot))
                return string.Empty;

            string[] versionDirectories = Directory.GetDirectories(packageRoot);
            if (versionDirectories.Length == 0)
                return string.Empty;

            string normalizedVersion = (version ?? string.Empty).Trim();
            if (IsExactVersion(normalizedVersion))
            {
                foreach (string directory in versionDirectories)
                {
                    string directoryVersion = Path.GetFileName(directory);
                    if (string.Equals(directoryVersion, normalizedVersion, StringComparison.OrdinalIgnoreCase))
                    {
                        resolvedVersion = directoryVersion;
                        return directory;
                    }
                }
            }

            string best = versionDirectories
                .Where(path => string.IsNullOrWhiteSpace(normalizedVersion) ||
                    SatisfiesVersionRange(Path.GetFileName(path), normalizedVersion))
                .OrderByDescending(path => Path.GetFileName(path), NuGetVersionComparer.Instance)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(best))
                return string.Empty;

            resolvedVersion = Path.GetFileName(best);
            return best;
        }

        private static bool IsExactVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return false;

            return version.IndexOfAny(new[] { '[', ']', '(', ')', ',', '*', ' ' }) < 0 &&
                !version.StartsWith(">", StringComparison.Ordinal) &&
                !version.StartsWith("<", StringComparison.Ordinal) &&
                !version.StartsWith("=", StringComparison.Ordinal);
        }

        private static bool SatisfiesVersionRange(string version, string range)
        {
            if (string.IsNullOrWhiteSpace(range))
                return true;

            range = range.Trim();
            if (IsExactVersion(range))
                return string.Equals(version, range, StringComparison.OrdinalIgnoreCase);

            if (range.StartsWith(">=", StringComparison.Ordinal))
                return NuGetVersionComparer.Instance.Compare(version, range.Substring(2).Trim()) >= 0;

            if (range.StartsWith(">", StringComparison.Ordinal))
                return NuGetVersionComparer.Instance.Compare(version, range.Substring(1).Trim()) > 0;

            if (range.StartsWith("<=", StringComparison.Ordinal))
                return NuGetVersionComparer.Instance.Compare(version, range.Substring(2).Trim()) <= 0;

            if (range.StartsWith("<", StringComparison.Ordinal))
                return NuGetVersionComparer.Instance.Compare(version, range.Substring(1).Trim()) < 0;

            if ((range.StartsWith("[", StringComparison.Ordinal) || range.StartsWith("(", StringComparison.Ordinal)) &&
                (range.EndsWith("]", StringComparison.Ordinal) || range.EndsWith(")", StringComparison.Ordinal)))
            {
                bool includeLower = range[0] == '[';
                bool includeUpper = range[range.Length - 1] == ']';
                string body = range.Substring(1, range.Length - 2);
                string[] parts = body.Split(',');
                string lower = parts.Length > 0 ? parts[0].Trim() : string.Empty;
                string upper = parts.Length > 1 ? parts[1].Trim() : string.Empty;

                if (!string.IsNullOrWhiteSpace(lower))
                {
                    int lowerCompare = NuGetVersionComparer.Instance.Compare(version, lower);
                    if (lowerCompare < 0 || (lowerCompare == 0 && !includeLower))
                        return false;
                }

                if (!string.IsNullOrWhiteSpace(upper))
                {
                    int upperCompare = NuGetVersionComparer.Instance.Compare(version, upper);
                    if (upperCompare > 0 || (upperCompare == 0 && !includeUpper))
                        return false;
                }

                return true;
            }

            return true;
        }

        private static string GetGlobalNuGetPackagesPath()
        {
            string overridePath = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
            if (!string.IsNullOrWhiteSpace(overridePath))
                return Environment.ExpandEnvironmentVariables(overridePath);

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return string.IsNullOrWhiteSpace(userProfile)
                ? string.Empty
                : Path.Combine(userProfile, ".nuget", "packages");
        }

        private static List<string> SelectPackageAssemblyPaths(string packageDir)
        {
            List<string> paths = SelectPackageAssemblyPaths(packageDir, "lib");
            if (paths.Count == 0)
                paths = SelectPackageAssemblyPaths(packageDir, "ref");

            return paths;
        }

        private static List<string> SelectPackageAssemblyPaths(string packageDir, string assetFolder)
        {
            string root = Path.Combine(packageDir, assetFolder);
            if (!Directory.Exists(root))
                return new List<string>();

            string[] directDlls = Directory.GetFiles(root, "*.dll", SearchOption.TopDirectoryOnly);
            if (directDlls.Length > 0)
                return directDlls.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();

            string bestFramework = Directory.GetDirectories(root)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .OrderByDescending(name => FrameworkScore(name))
                .FirstOrDefault(name => FrameworkScore(name) >= 0);

            if (string.IsNullOrWhiteSpace(bestFramework))
                return new List<string>();

            return Directory.GetFiles(Path.Combine(root, bestFramework), "*.dll", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<NuGetPackageReference> GetPackageDependencies(string packageDir)
        {
            var dependencies = new List<NuGetPackageReference>();
            string nuspec = Directory.GetFiles(packageDir, "*.nuspec", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(nuspec))
                return dependencies;

            try
            {
                XDocument document = XDocument.Load(nuspec);
                XNamespace ns = document.Root == null ? XNamespace.None : document.Root.Name.Namespace;
                XElement metadata = document.Root == null ? null : document.Root.Element(ns + "metadata");
                XElement dependencyRoot = metadata == null ? null : metadata.Element(ns + "dependencies");
                if (dependencyRoot == null)
                    return dependencies;

                List<XElement> groups = dependencyRoot.Elements(ns + "group").ToList();
                IEnumerable<XElement> dependencyElements;
                if (groups.Count > 0)
                {
                    XElement group = SelectBestDependencyGroup(groups);
                    dependencyElements = group == null
                        ? Enumerable.Empty<XElement>()
                        : group.Elements(ns + "dependency");
                }
                else
                {
                    dependencyElements = dependencyRoot.Elements(ns + "dependency");
                }

                foreach (XElement dependency in dependencyElements)
                {
                    string id = (string)dependency.Attribute("id") ?? string.Empty;
                    string version = (string)dependency.Attribute("version") ?? string.Empty;
                    string exclude = (string)dependency.Attribute("exclude") ?? string.Empty;

                    if (exclude.IndexOf("compile", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;

                    if (IsValidNuGetPackageId(id))
                        dependencies.Add(new NuGetPackageReference(id, version));
                }
            }
            catch
            {
            }

            return dependencies;
        }

        private static XElement SelectBestDependencyGroup(List<XElement> groups)
        {
            XElement fallback = null;
            XElement best = null;
            int bestScore = -1;

            foreach (XElement group in groups)
            {
                string targetFramework = (string)group.Attribute("targetFramework") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(targetFramework))
                {
                    fallback = group;
                    continue;
                }

                int score = FrameworkScore(targetFramework);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = group;
                }
            }

            return bestScore >= 0 ? best : fallback;
        }

        private static int FrameworkScore(string targetFramework)
        {
            string framework = NormalizeFramework(targetFramework);
            if (string.IsNullOrWhiteSpace(framework))
                return -1;

            if (framework.StartsWith("netstandard", StringComparison.Ordinal))
                return 1000 + ParseFrameworkVersionScore(framework.Substring("netstandard".Length));

            if (framework.StartsWith("netcoreapp", StringComparison.Ordinal))
            {
                int versionScore = ParseFrameworkVersionScore(framework.Substring("netcoreapp".Length));
                return versionScore <= CurrentNetVersionScore() ? 2000 + versionScore : -1;
            }

            if (framework.StartsWith("net", StringComparison.Ordinal))
            {
                string versionPart = framework.Substring(3);
                int dashIndex = versionPart.IndexOf('-');
                bool windowsSpecific = dashIndex >= 0 &&
                    versionPart.Substring(dashIndex + 1).StartsWith("windows", StringComparison.Ordinal);
                if (dashIndex >= 0)
                    versionPart = versionPart.Substring(0, dashIndex);

                int versionScore = ParseFrameworkVersionScore(versionPart);
                if (versionScore >= 500 && versionScore <= CurrentNetVersionScore())
                    return 3000 + versionScore + (windowsSpecific ? 50 : 0);

                if (versionScore > 0 && versionScore < 500)
                    return 500 + versionScore;
            }

            return -1;
        }

        private static string NormalizeFramework(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string framework = value.Trim().ToLowerInvariant();
            framework = framework.Replace(" ", string.Empty);

            if (framework.StartsWith(".netstandard", StringComparison.Ordinal))
                return "netstandard" + ExtractVersion(framework);

            if (framework.StartsWith(".netcoreapp", StringComparison.Ordinal))
                return "netcoreapp" + ExtractVersion(framework);

            if (framework.StartsWith(".netframework", StringComparison.Ordinal))
                return "net" + ExtractVersion(framework).Replace(".", string.Empty);

            return framework;
        }

        private static string ExtractVersion(string value)
        {
            int versionIndex = value.IndexOf("version=v", StringComparison.Ordinal);
            if (versionIndex >= 0)
                return value.Substring(versionIndex + "version=v".Length);

            Match match = Regex.Match(value, @"\d+(\.\d+)*");
            return match.Success ? match.Value : string.Empty;
        }

        private static int ParseFrameworkVersionScore(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            string normalized = value.Trim().ToLowerInvariant();
            int dashIndex = normalized.IndexOf('-');
            if (dashIndex >= 0)
                normalized = normalized.Substring(0, dashIndex);

            if (normalized.IndexOf('.') >= 0)
            {
                string[] parts = normalized.Split('.');
                int major = ParseInt(parts, 0);
                int minor = ParseInt(parts, 1);
                return major * 100 + minor;
            }

            if (normalized.Length >= 2 && int.TryParse(normalized, out int compact))
            {
                int major = compact / 10;
                int minor = compact % 10;
                return major * 100 + minor;
            }

            return 0;
        }

        private static int ParseInt(string[] values, int index)
        {
            if (values == null || index < 0 || index >= values.Length)
                return 0;

            return int.TryParse(values[index], out int value) ? value : 0;
        }

        private static int CurrentNetVersionScore()
        {
            string targetFramework = AppContext.TargetFrameworkName ?? string.Empty;
            int versionIndex = targetFramework.IndexOf("Version=v", StringComparison.OrdinalIgnoreCase);
            if (versionIndex >= 0)
                return ParseFrameworkVersionScore(targetFramework.Substring(versionIndex + "Version=v".Length));

            return 1000;
        }

        private static string GetCurrentRestoreTargetFramework()
        {
            int versionScore = CurrentNetVersionScore();
            int major = Math.Max(5, versionScore / 100);
            int minor = Math.Max(0, versionScore % 100);
            return minor == 0
                ? "net" + major + ".0"
                : "net" + major + "." + minor;
        }

        private static bool IsUsableDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                return Directory.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        private static string TryReadPackageVersionFromProject(string projectPath, string packageId)
        {
            try
            {
                XDocument document = XDocument.Load(projectPath);
                foreach (XElement packageReference in document.Descendants("PackageReference"))
                {
                    string include = (string)packageReference.Attribute("Include") ?? string.Empty;
                    if (!string.Equals(include, packageId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    string version = (string)packageReference.Attribute("Version") ??
                        (string)packageReference.Element("Version") ??
                        string.Empty;
                    return version.Trim();
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string RestoreFailureMessage(NuGetPackageReference package, string output, string error)
        {
            string allText = JoinNonEmpty(error, output);
            if (IsNuGetCertificateValidationError(allText))
                return NuGetCertificateValidationMessage(FirstUsefulErrorLine(allText));

            if (IsNuGetPackageNotFoundError(allText))
                return NuGetPackageNotFoundMessage(package, FirstUsefulErrorLine(allText));

            return FirstUsefulErrorLine(allText);
        }

        private static string JoinNonEmpty(params string[] values)
        {
            var builder = new StringBuilder();
            foreach (string value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                if (builder.Length > 0)
                    builder.AppendLine();

                builder.Append(value.Trim());
            }

            return builder.ToString();
        }

        private static string FirstUsefulErrorLine(string text)
        {
            string firstUseful = string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                return firstUseful;

            using (var reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (string.IsNullOrWhiteSpace(firstUseful))
                        firstUseful = line;

                    if (line.StartsWith("error", StringComparison.OrdinalIgnoreCase) ||
                        line.IndexOf(" NU", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        line.StartsWith("NU", StringComparison.OrdinalIgnoreCase) ||
                        IsNuGetPackageNotFoundError(line) ||
                        IsNuGetCertificateValidationError(line))
                    {
                        return line;
                    }
                }
            }

            return firstUseful;
        }

        private static void TryKill(Process process)
        {
            try
            {
                process.Kill(true);
            }
            catch
            {
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch
            {
            }
        }

        private sealed class NuGetVersionComparer : IComparer<string>
        {
            public static readonly NuGetVersionComparer Instance = new NuGetVersionComparer();

            public int Compare(string x, string y)
            {
                if (string.Equals(x, y, StringComparison.OrdinalIgnoreCase))
                    return 0;

                SplitVersion(x, out string xMain, out string xPrerelease);
                SplitVersion(y, out string yMain, out string yPrerelease);

                int main = CompareMainVersion(xMain, yMain);
                if (main != 0)
                    return main;

                bool xStable = string.IsNullOrWhiteSpace(xPrerelease);
                bool yStable = string.IsNullOrWhiteSpace(yPrerelease);
                if (xStable && !yStable)
                    return 1;

                if (!xStable && yStable)
                    return -1;

                return string.Compare(xPrerelease, yPrerelease, StringComparison.OrdinalIgnoreCase);
            }

            private static void SplitVersion(string value, out string main, out string prerelease)
            {
                value = (value ?? string.Empty).Trim();
                int buildIndex = value.IndexOf('+');
                if (buildIndex >= 0)
                    value = value.Substring(0, buildIndex);

                int prereleaseIndex = value.IndexOf('-');
                if (prereleaseIndex >= 0)
                {
                    main = value.Substring(0, prereleaseIndex);
                    prerelease = value.Substring(prereleaseIndex + 1);
                    return;
                }

                main = value;
                prerelease = string.Empty;
            }

            private static int CompareMainVersion(string left, string right)
            {
                string[] leftParts = (left ?? string.Empty).Split('.');
                string[] rightParts = (right ?? string.Empty).Split('.');
                int count = Math.Max(leftParts.Length, rightParts.Length);

                for (int i = 0; i < count; i++)
                {
                    int leftValue = ParseVersionPart(leftParts, i);
                    int rightValue = ParseVersionPart(rightParts, i);
                    int comparison = leftValue.CompareTo(rightValue);
                    if (comparison != 0)
                        return comparison;
                }

                return 0;
            }

            private static int ParseVersionPart(string[] parts, int index)
            {
                if (parts == null || index < 0 || index >= parts.Length)
                    return 0;

                string part = parts[index];
                int end = 0;
                while (end < part.Length && char.IsDigit(part[end]))
                    end++;

                return end > 0 && int.TryParse(part.Substring(0, end), out int value)
                    ? value
                    : 0;
            }
        }
    }
}
