using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Core.DirFiles
{
    // A read-only source workspace: opening a project never builds it or restores packages.
    // Like CIARE, this supports ordinary project items without executing MSBuild targets.
    internal sealed class CSharpProjectWorkspace
    {
        private readonly object gate = new object();
        private string cacheKey;
        private string fingerprint;
        private Snapshot snapshot;
        private DateTime nextRefresh;
        private readonly Dictionary<string, CachedTree> trees = new Dictionary<string, CachedTree>(StringComparer.OrdinalIgnoreCase);

        internal static bool IsWorkspacePath(string path)
        {
            string extension = Path.GetExtension(path);
            return extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase);
        }

        internal static List<string> ReadProjects(string path)
        {
            path = Path.GetFullPath(path);
            if (!File.Exists(path))
                throw new FileNotFoundException("Project or solution does not exist.", path);
            if (Path.GetExtension(path).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
                return new List<string> { path };

            string directory = Path.GetDirectoryName(path);
            IEnumerable<string> entries;
            if (Path.GetExtension(path).Equals(".slnx", StringComparison.OrdinalIgnoreCase))
                entries = XDocument.Load(path).Descendants().Where(e => e.Name.LocalName == "Project")
                    .Select(e => (string)e.Attribute("Path"));
            else
                entries = Regex.Matches(File.ReadAllText(path),
                    "(?m)^\\s*Project\\(\"[^\"]+\"\\)\\s*=\\s*\"[^\"]*\"\\s*,\\s*\"([^\"]+\\.csproj)\"", RegexOptions.IgnoreCase)
                    .Cast<Match>().Select(m => m.Groups[1].Value);

            return entries.Where(p => !string.IsNullOrWhiteSpace(p) && Path.GetExtension(p).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
                .Select(p => Resolve(directory, p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        internal static string GetEntryFile(string workspacePath)
        {
            var projects = ReadProjects(workspacePath);
            foreach (string projectPath in projects)
            {
                if (!File.Exists(projectPath)) continue;
                Project project = Project.Read(projectPath);
                var sources = project.SourceFiles().OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
                string entry = sources.FirstOrDefault(p => Path.GetFileName(p).Equals("Program.cs", StringComparison.OrdinalIgnoreCase))
                    ?? sources.FirstOrDefault();
                if (entry != null) return entry;
            }
            throw new InvalidOperationException("No existing C# source files were found in " + Path.GetFileName(workspacePath) + ".");
        }

        internal void Invalidate()
        {
            lock (gate)
            {
                nextRefresh = DateTime.MinValue;
                fingerprint = null;
            }
        }

        internal Snapshot GetSnapshot(string sourcePath, string workspacePath, CancellationToken cancellationToken)
        {
            if (!Path.GetExtension(sourcePath).Equals(".cs", StringComparison.OrdinalIgnoreCase)) return null;
            string key = sourcePath + "|" + workspacePath;
            lock (gate)
            {
                if (string.Equals(key, cacheKey, StringComparison.OrdinalIgnoreCase) && DateTime.UtcNow < nextRefresh)
                    return snapshot;

                cancellationToken.ThrowIfCancellationRequested();
                Project owner = FindOwner(sourcePath, workspacePath);
                var projects = new Dictionary<string, Project>(StringComparer.OrdinalIgnoreCase);
                if (owner != null) CollectProjects(owner, projects, cancellationToken);
                string currentFingerprint = GetFingerprint(projects.Values, sourcePath, cancellationToken);
                if (string.Equals(key, cacheKey, StringComparison.OrdinalIgnoreCase) && currentFingerprint == fingerprint)
                {
                    nextRefresh = DateTime.UtcNow.AddSeconds(2);
                    return snapshot;
                }
                var usedTrees = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var built = new Dictionary<string, CSharpCompilation>(StringComparer.OrdinalIgnoreCase);
                Snapshot result = null;
                if (owner != null)
                {
                    CSharpCompilation compilation = BuildProject(owner, sourcePath, projects, built,
                        new HashSet<string>(StringComparer.OrdinalIgnoreCase), usedTrees, cancellationToken);
                    result = new Snapshot(owner.Path, owner.ParseOptions, compilation);
                }
                // Do not retain another project's source text after switching files.
                foreach (string path in trees.Keys.Where(p => !usedTrees.Contains(p)).ToArray()) trees.Remove(path);
                cacheKey = key;
                fingerprint = currentFingerprint;
                snapshot = result;
                nextRefresh = DateTime.UtcNow.AddSeconds(2);
                return result;
            }
        }

        private static void CollectProjects(Project project, Dictionary<string, Project> projects, CancellationToken token)
        {
            if (projects.ContainsKey(project.Path)) return;
            projects.Add(project.Path, project);
            foreach (string path in project.ProjectReferences())
            {
                token.ThrowIfCancellationRequested();
                if (File.Exists(path) && !projects.ContainsKey(path)) CollectProjects(Project.Read(path), projects, token);
            }
        }

        private static string GetFingerprint(IEnumerable<Project> projects, string activePath, CancellationToken token)
        {
            var result = new StringBuilder();
            foreach (Project project in projects)
            {
                foreach (string path in project.InputFiles.Concat(project.SourceFiles()).Concat(project.ReferencePaths()))
                {
                    token.ThrowIfCancellationRequested();
                    if (path.Equals(activePath, StringComparison.OrdinalIgnoreCase)) continue;
                    result.Append(path).Append('|');
                    try
                    {
                        var info = new FileInfo(path);
                        if (info.Exists) result.Append(info.LastWriteTimeUtc.Ticks).Append(':').Append(info.Length);
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                    result.AppendLine();
                }
            }
            return result.ToString();
        }

        private CSharpCompilation BuildProject(Project project, string activePath, Dictionary<string, Project> projects,
            Dictionary<string, CSharpCompilation> built, HashSet<string> visiting,
            HashSet<string> usedTrees, CancellationToken token)
        {
            if (built.TryGetValue(project.Path, out CSharpCompilation previous)) return previous;
            if (!visiting.Add(project.Path)) return null; // Broken/cyclic graphs must not hang the editor.
            token.ThrowIfCancellationRequested();
            var references = new List<MetadataReference>(Core.SystemTools.Roslyn.References());
            var referenceNames = new HashSet<string>(references.Select(r => Path.GetFileName(r.Display)), StringComparer.OrdinalIgnoreCase);
            foreach (string referencePath in project.ReferencePaths())
            {
                token.ThrowIfCancellationRequested();
                if (!File.Exists(referencePath) || !referenceNames.Add(Path.GetFileName(referencePath))) continue;
                try { references.Add(MetadataReference.CreateFromFile(referencePath)); }
                catch (BadImageFormatException) { }
                catch (IOException) { }
            }
            foreach (string projectPath in project.ProjectReferences())
            {
                if (!projects.TryGetValue(projectPath, out Project referencedProject)) continue;
                CSharpCompilation dependency = BuildProject(referencedProject, activePath, projects, built, visiting, usedTrees, token);
                if (dependency == null) continue;
                if (referenceNames.Add(dependency.AssemblyName)) references.Add(dependency.ToMetadataReference());
                // Transitive references are needed when a public member exposes a dependency type.
                foreach (MetadataReference reference in dependency.References)
                {
                    string name = reference is CompilationReference cr ? cr.Compilation.AssemblyName : Path.GetFileName(reference.Display);
                    if (referenceNames.Add(name)) references.Add(reference);
                }
            }

            var sourceTrees = new List<SyntaxTree>();
            foreach (string path in project.SourceFiles())
            {
                token.ThrowIfCancellationRequested();
                if (string.Equals(path, activePath, StringComparison.OrdinalIgnoreCase)) continue;
                try
                {
                    var info = new FileInfo(path);
                    if (info.Length > 50L * 1024 * 1024) continue;
                    usedTrees.Add(path);
                    if (!trees.TryGetValue(path, out CachedTree cached) || cached.Stamp != info.LastWriteTimeUtc ||
                        cached.Length != info.Length || !cached.Tree.Options.Equals(project.ParseOptions))
                    {
                        cached = new CachedTree(info.LastWriteTimeUtc, info.Length,
                            CSharpSyntaxTree.ParseText(File.ReadAllText(path), project.ParseOptions, path, cancellationToken: token));
                        trees[path] = cached;
                    }
                    sourceTrees.Add(cached.Tree);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            string usings = project.GlobalUsings();
            if (usings.Length > 0)
                sourceTrees.Add(CSharpSyntaxTree.ParseText(usings, project.ParseOptions, project.Path + ".xte.g.cs", cancellationToken: token));

            var compilation = CSharpCompilation.Create(project.Property("AssemblyName", System.IO.Path.GetFileNameWithoutExtension(project.Path)),
                sourceTrees, references, new CSharpCompilationOptions(project.OutputKind,
                    allowUnsafe: project.IsTrue("AllowUnsafeBlocks"),
                    nullableContextOptions: project.NullableOptions));
            visiting.Remove(project.Path);
            built[project.Path] = compilation;
            return compilation;
        }

        private static Project FindOwner(string sourcePath, string workspacePath)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // An explicitly opened solution also resolves linked files outside its directory.
            if (!string.IsNullOrEmpty(workspacePath))
                foreach (string path in ReadProjects(workspacePath))
                {
                    Project project = TryOwner(path, sourcePath, seen);
                    if (project != null) return project;
                }

            for (string directory = Path.GetDirectoryName(sourcePath); !string.IsNullOrEmpty(directory); directory = Path.GetDirectoryName(directory))
            {
                foreach (string path in Files(directory, "*.csproj"))
                {
                    Project project = TryOwner(path, sourcePath, seen);
                    if (project != null) return project;
                }
                foreach (string solution in Files(directory, "*.sln").Concat(Files(directory, "*.slnx")))
                {
                    try
                    {
                        foreach (string path in ReadProjects(solution))
                        {
                            Project project = TryOwner(path, sourcePath, seen);
                            if (project != null) return project;
                        }
                    }
                    catch (IOException) { }
                    catch (System.Xml.XmlException) { }
                }
                if (Directory.Exists(System.IO.Path.Combine(directory, ".git")) || File.Exists(System.IO.Path.Combine(directory, ".git"))) break;
            }
            return null;
        }

        private static Project TryOwner(string path, string sourcePath, HashSet<string> seen)
        {
            if (!seen.Add(path) || !File.Exists(path)) return null;
            try
            {
                Project project = Project.Read(path);
                return project.Contains(sourcePath) ? project : null;
            }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
            catch (System.Xml.XmlException) { return null; }
        }

        private static string Resolve(string directory, string path) => System.IO.Path.GetFullPath(System.IO.Path.Combine(directory, path.Replace('\\', System.IO.Path.DirectorySeparatorChar)));

        private static string[] Files(string directory, string pattern)
        {
            try { return Directory.GetFiles(directory, pattern).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray(); }
            catch (IOException) { return Array.Empty<string>(); }
            catch (UnauthorizedAccessException) { return Array.Empty<string>(); }
        }

        private static IEnumerable<string> WalkSources(string directory)
        {
            var pending = new Stack<string>();
            pending.Push(directory);
            while (pending.Count > 0)
            {
                string current = pending.Pop();
                foreach (string file in Files(current, "*.cs")) yield return file;
                string[] children;
                try { children = Directory.GetDirectories(current); }
                catch (IOException) { continue; }
                catch (UnauthorizedAccessException) { continue; }
                foreach (string child in children)
                {
                    string name = System.IO.Path.GetFileName(child);
                    if (IgnoredDirectory(name)) continue;
                    try { if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) != 0) continue; }
                    catch (IOException) { continue; }
                    catch (UnauthorizedAccessException) { continue; }
                    pending.Push(child);
                }
            }
        }

        private static bool IgnoredDirectory(string name) => name.StartsWith(".", StringComparison.Ordinal) ||
            name.Equals("bin", StringComparison.OrdinalIgnoreCase) || name.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("packages", StringComparison.OrdinalIgnoreCase) || name.Equals("node_modules", StringComparison.OrdinalIgnoreCase);

        internal sealed class Snapshot
        {
            internal string ProjectPath { get; }
            internal CSharpParseOptions ParseOptions { get; }
            private readonly CSharpCompilation compilation;
            internal Snapshot(string projectPath, CSharpParseOptions parseOptions, CSharpCompilation compilation)
            {
                ProjectPath = projectPath;
                ParseOptions = parseOptions;
                this.compilation = compilation;
            }
            internal CSharpCompilation WithActiveTree(SyntaxTree tree, string sourceText)
            {
                var result = compilation.AddSyntaxTrees(tree);
                if (Core.SystemTools.Roslyn.GetNuGetPackageReferences(sourceText).Count == 0) return result;
                var existing = new HashSet<string>(result.References.Select(r => r.Display), StringComparer.OrdinalIgnoreCase);
                return result.AddReferences(Core.SystemTools.Roslyn.References(tree.FilePath, sourceText).Where(r => existing.Add(r.Display)));
            }
        }

        private sealed class CachedTree
        {
            internal DateTime Stamp { get; }
            internal long Length { get; }
            internal SyntaxTree Tree { get; }
            internal CachedTree(DateTime stamp, long length, SyntaxTree tree) { Stamp = stamp; Length = length; Tree = tree; }
        }

        private sealed class Project
        {
            internal string Path { get; private set; }
            private string directory;
            private XDocument document;
            private readonly List<XDocument> documents = new List<XDocument>();
            internal readonly List<string> InputFiles = new List<string>();
            private List<string> sourceFiles;
            private List<string> referencePaths;
            private readonly Dictionary<string, string> properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            internal CSharpParseOptions ParseOptions { get; private set; }
            internal OutputKind OutputKind => Property("OutputType").Equals("WinExe", StringComparison.OrdinalIgnoreCase) ? OutputKind.WindowsApplication :
                Property("OutputType").Equals("Exe", StringComparison.OrdinalIgnoreCase) ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary;
            internal NullableContextOptions NullableOptions => Property("Nullable").ToLowerInvariant() switch
            {
                "enable" => NullableContextOptions.Enable,
                "warnings" => NullableContextOptions.Warnings,
                "annotations" => NullableContextOptions.Annotations,
                _ => NullableContextOptions.Disable
            };

            internal static Project Read(string path)
            {
                var project = new Project { Path = path, directory = System.IO.Path.GetDirectoryName(path), document = XDocument.Load(path) };
                project.properties["Configuration"] = "Debug";
                project.properties["Platform"] = "AnyCPU";
                project.properties["MSBuildProjectDirectory"] = project.directory;
                project.properties["MSBuildProjectName"] = System.IO.Path.GetFileNameWithoutExtension(path);
                project.AddDirectoryBuildFile("Directory.Build.props");
                project.documents.Add(project.document);
                project.InputFiles.Add(path);
                project.AddDirectoryBuildFile("Directory.Build.targets");
                foreach (XDocument doc in project.documents) project.ReadProperties(doc);
                if (project.Property("TargetFramework").Length == 0)
                    project.properties["TargetFramework"] = project.Property("TargetFrameworks").Split(';')[0];
                foreach (XDocument doc in project.documents) project.ReadProperties(doc);
                var symbols = new HashSet<string>(project.Property("DefineConstants").Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()).Where(SyntaxFacts.IsValidIdentifier), StringComparer.Ordinal);
                symbols.Add("DEBUG");
                symbols.Add("TRACE");
                string tfm = project.Property("TargetFramework");
                Match net = Regex.Match(tfm, @"^net(\d+)\.(\d+)");
                if (net.Success)
                {
                    symbols.Add("NET"); symbols.Add("NETCOREAPP");
                    symbols.Add("NET" + net.Groups[1].Value + "_" + net.Groups[2].Value);
                    for (int version = 5; version <= int.Parse(net.Groups[1].Value); version++) symbols.Add("NET" + version + "_0_OR_GREATER");
                }
                if (tfm.IndexOf("-windows", StringComparison.OrdinalIgnoreCase) >= 0) symbols.Add("WINDOWS");
                LanguageVersion language = LanguageVersion.Latest;
                if (LanguageVersionFacts.TryParse(project.Property("LangVersion"), out LanguageVersion configured)) language = configured;
                project.ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(language).WithPreprocessorSymbols(symbols);
                return project;
            }

            private void AddDirectoryBuildFile(string name)
            {
                for (string current = directory; !string.IsNullOrEmpty(current); current = System.IO.Path.GetDirectoryName(current))
                {
                    string path = System.IO.Path.Combine(current, name);
                    if (!File.Exists(path)) continue;
                    InputFiles.Add(path);
                    documents.Add(XDocument.Load(path));
                    return;
                }
            }

            internal string Property(string name, string fallback = "") => properties.TryGetValue(name, out string value) ? value : fallback;
            internal bool IsTrue(string name) => Property(name).Equals("true", StringComparison.OrdinalIgnoreCase) || Property(name).Equals("enable", StringComparison.OrdinalIgnoreCase);
            private string Expand(string value) => Regex.Replace(value ?? "", @"\$\(([^)]+)\)", m => Property(m.Groups[1].Value, m.Value));

            private void ReadProperties(XDocument doc)
            {
                foreach (XElement group in doc.Root.Elements().Where(e => e.Name.LocalName == "PropertyGroup" && Enabled(e)))
                    foreach (XElement property in group.Elements().Where(Enabled)) properties[property.Name.LocalName] = Expand(property.Value.Trim());
            }

            private bool Enabled(XElement element)
            {
                string condition = Expand((string)element.Attribute("Condition")).Trim();
                if (condition.Length == 0) return true;
                Match comparison = Regex.Match(condition, "^'([^']*)'\\s*(==|!=)\\s*'([^']*)'$");
                if (!comparison.Success) return false; // Do not combine mutually exclusive configurations.
                bool equal = comparison.Groups[1].Value.Equals(comparison.Groups[3].Value, StringComparison.OrdinalIgnoreCase);
                return comparison.Groups[2].Value == "==" ? equal : !equal;
            }

            private IEnumerable<XElement> Items(string name) => documents.SelectMany(d => d.Root.Elements())
                .Where(e => e.Name.LocalName == "ItemGroup" && Enabled(e)).SelectMany(e => e.Elements())
                .Where(e => e.Name.LocalName == name && Enabled(e));

            private IEnumerable<string> Specs(string value) => Expand(value).Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim()).Where(p => p.Length > 0 && !p.Contains("$(", StringComparison.Ordinal));

            private bool Matches(string value, string path)
            {
                foreach (string spec in Specs(value))
                {
                    string pattern = Resolve(directory, spec).Replace('\\', '/');
                    string regex = Regex.Escape(pattern).Replace(@"\*\*/", "(?:.*/)?").Replace(@"\*\*", ".*").Replace(@"\*", "[^/]*").Replace(@"\?", "[^/]");
                    if (Regex.IsMatch(path.Replace('\\', '/'), "^" + regex + "$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) return true;
                }
                return false;
            }

            internal bool Contains(string path)
            {
                if (!System.IO.Path.GetExtension(path).Equals(".cs", StringComparison.OrdinalIgnoreCase)) return false;
                bool included = false;
                bool sdk = document.Root.Attribute("Sdk") != null || document.Root.Elements().Any(e => e.Name.LocalName == "Sdk" || e.Attribute("Sdk") != null);
                if (sdk && !Property("EnableDefaultItems").Equals("false", StringComparison.OrdinalIgnoreCase) &&
                    !Property("EnableDefaultCompileItems").Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    string relative = System.IO.Path.GetRelativePath(directory, path);
                    included = !System.IO.Path.IsPathRooted(relative) && !relative.Split('\\', '/').Any(IgnoredDirectory) &&
                        !Matches(Property("DefaultItemExcludes") + ";" + Property("DefaultExcludesInProjectFolder"), path);
                }
                foreach (XElement item in Items("Compile"))
                {
                    if (Matches((string)item.Attribute("Include"), path) && !Matches((string)item.Attribute("Exclude"), path)) included = true;
                    if (Matches((string)item.Attribute("Remove"), path)) included = false;
                }
                return included;
            }

            internal IEnumerable<string> SourceFiles()
            {
                if (sourceFiles != null) return sourceFiles;
                var candidates = new HashSet<string>(WalkSources(directory), StringComparer.OrdinalIgnoreCase);
                foreach (XElement item in Items("Compile"))
                    foreach (string spec in Specs((string)item.Attribute("Include")))
                    {
                        string fullPath = Resolve(directory, spec);
                        int wildcard = fullPath.IndexOfAny(new[] { '*', '?' });
                        if (wildcard < 0) { if (File.Exists(fullPath)) candidates.Add(fullPath); }
                        else
                        {
                            string root = System.IO.Path.GetDirectoryName(fullPath.Substring(0, wildcard));
                            if (!string.IsNullOrEmpty(root)) candidates.UnionWith(WalkSources(root));
                        }
                    }
                sourceFiles = candidates.Where(Contains).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
                return sourceFiles;
            }

            internal IEnumerable<string> ProjectReferences() => Items("ProjectReference")
                .Where(e => !string.Equals((string)e.Attribute("ReferenceOutputAssembly") ??
                    e.Elements().FirstOrDefault(c => c.Name.LocalName == "ReferenceOutputAssembly")?.Value, "false", StringComparison.OrdinalIgnoreCase))
                .SelectMany(e => Specs((string)e.Attribute("Include"))).Select(p => Resolve(directory, p));

            internal string GlobalUsings()
            {
                var usings = new HashSet<string>(StringComparer.Ordinal);
                if (IsTrue("ImplicitUsings"))
                {
                    usings.UnionWith(new[] { "System", "System.Collections.Generic", "System.IO", "System.Linq", "System.Net.Http", "System.Threading", "System.Threading.Tasks" });
                    if (IsTrue("UseWindowsForms")) { usings.Add("System.Drawing"); usings.Add("System.Windows.Forms"); }
                }
                var result = new StringBuilder();
                foreach (XElement item in Items("Using"))
                {
                    usings.Remove((string)item.Attribute("Remove") ?? "");
                    string name = (string)item.Attribute("Include");
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    string alias = (string)item.Attribute("Alias");
                    string prefix = string.Equals((string)item.Attribute("Static"), "true", StringComparison.OrdinalIgnoreCase) ? "static " : "";
                    usings.Add(prefix + (string.IsNullOrEmpty(alias) ? name : alias + " = " + name));
                }
                foreach (string name in usings) result.Append("global using ").Append(name).AppendLine(";");
                return result.ToString();
            }

            internal IEnumerable<string> ReferencePaths()
            {
                if (referencePaths != null) return referencePaths;
                var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (XElement item in Items("Reference"))
                {
                    string hint = item.Elements().FirstOrDefault(e => e.Name.LocalName == "HintPath")?.Value;
                    foreach (string spec in Specs(hint)) result.Add(Resolve(directory, spec));
                }
                string assetsPath = System.IO.Path.Combine(directory, "obj", "project.assets.json");
                if (File.Exists(assetsPath))
                {
                    try
                    {
                        using JsonDocument assets = JsonDocument.Parse(File.ReadAllText(assetsPath));
                        JsonElement root = assets.RootElement;
                        var targets = root.GetProperty("targets").EnumerateObject().ToList();
                        var target = targets.FirstOrDefault(t => t.Name.Equals(Property("TargetFramework"), StringComparison.OrdinalIgnoreCase));
                        if (target.Value.ValueKind == JsonValueKind.Undefined) target = targets.FirstOrDefault();
                        if (target.Value.ValueKind == JsonValueKind.Object)
                            foreach (JsonProperty library in target.Value.EnumerateObject())
                            {
                                if (!library.Value.TryGetProperty("compile", out JsonElement compile) ||
                                    !root.GetProperty("libraries").TryGetProperty(library.Name, out JsonElement metadata) ||
                                    !metadata.TryGetProperty("path", out JsonElement packagePath)) continue;
                                foreach (JsonProperty folder in root.GetProperty("packageFolders").EnumerateObject())
                                    foreach (JsonProperty assembly in compile.EnumerateObject())
                                        if (assembly.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                                            result.Add(Resolve(folder.Name, packagePath.GetString() + "/" + assembly.Name));
                            }
                    }
                    catch (JsonException) { }
                    catch (KeyNotFoundException) { }
                    catch (IOException) { }
                }
                referencePaths = result.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
                return referencePaths;
            }
        }
    }
}
