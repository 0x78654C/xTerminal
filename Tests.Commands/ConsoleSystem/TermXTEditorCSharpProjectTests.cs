using Core.DirFiles;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

public partial class TermXTEditorSyntaxTests
{
    [Theory]
    [InlineData(".sln")]
    [InlineData(".slnx")]
    [InlineData(".csproj")]
    public void CSharpProject_RawCommandLineOpensAndSavesMetadataEvenWhenMalformed(string extension)
    {
        using var project = new CSharpProjectFixture();
        string path = project.Write("My Project" + extension, "<unfinished project metadata");
        var factory = typeof(global::Commands.TerminalCommands.ScriptingLanguage.TermXTEditorCommand).GetMethod(
            "CreateEditorFromArguments", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var editor = (TermXTEditor)factory.Invoke(null, new object[] { "-raw \"" + path + "\"", Path.GetDirectoryName(path)! })!;

        GetPrivateField<string>(editor, "_path").Should().Be(path);
        GetPrivateField<TermXTEditorSyntax>(editor, "_syntax").Should().Be(TermXTEditorSyntax.PlainText);
        Lines(editor).Should().ContainSingle().Which.Should().Be("<unfinished project metadata");
        Diagnostics(editor).Should().BeEmpty();
        Lines(editor)[0] = "repaired metadata";
        InvokePrivate<bool>(editor, "Save", false).Should().BeTrue();
        File.ReadAllLines(path).Should().Equal("repaired metadata");
    }

    [Theory]
    [InlineData(".sln")]
    [InlineData(".slnx")]
    [InlineData(".csproj")]
    public void CSharpProject_ProjectEditAndRawOpenPreserveSourceAndCanReopenWorkspace(string extension)
    {
        using var project = new CSharpProjectFixture();
        project.Write("App/App.csproj", CSharpProjectFixture.SdkProject);
        string sourcePath = project.Write("App/Program.cs", "class Program {}");
        string workspacePath = extension == ".csproj" ? project.PathFor("App/App.csproj") : project.Solution(extension, "App/App.csproj");
        var editor = new TermXTEditor(workspacePath);
        string[] metadata = File.ReadAllLines(workspacePath);

        InvokePrivate(editor, "ExecuteEditorCommand", "project edit");
        GetPrivateField<string>(editor, "_path").Should().Be(workspacePath);
        Lines(editor).Should().Equal(metadata);
        GetPrivateField<TermXTEditorSyntax>(editor, "_syntax").Should().Be(TermXTEditorSyntax.PlainText);
        Diagnostics(editor).Should().BeEmpty();
        Lines(editor).Add("");
        InvokePrivate<bool>(editor, "Save", false).Should().BeTrue();
        File.ReadAllText(sourcePath).Should().Be("class Program {}");

        InvokePrivate(editor, "ExecuteEditorCommand", "e \"" + workspacePath + "\"");
        GetPrivateField<string>(editor, "_path").Should().Be(sourcePath);
        GetPrivateField<TermXTEditorSyntax>(editor, "_syntax").Should().Be(TermXTEditorSyntax.CSharp);
        InvokePrivate(editor, "ExecuteEditorCommand", "e -raw \"" + workspacePath + "\"");
        GetPrivateField<string>(editor, "_path").Should().Be(workspacePath);
        GetPrivateField<TermXTEditorSyntax>(editor, "_syntax").Should().Be(TermXTEditorSyntax.PlainText);
    }

    [Fact]
    public void CSharpProject_ProjectEditFollowsOwningProjectAfterLeavingExplicitSolution()
    {
        using var project = new CSharpProjectFixture();
        project.Write("App/App.csproj", CSharpProjectFixture.SdkProject);
        project.Write("App/Program.cs", "class Program {}");
        string solution = project.Solution(".slnx", "App/App.csproj");
        string otherProject = project.Write("Other/Other.csproj", CSharpProjectFixture.SdkProject);
        string otherSource = project.Write("Other/Program.cs", "class Other {}");
        var editor = new TermXTEditor(solution);
        InvokePrivate(editor, "OpenExplorerFile", otherSource);
        InvokePrivate(editor, "ExecuteEditorCommand", "project edit");
        GetPrivateField<string>(editor, "_path").Should().Be(otherProject);
    }

    [Fact]
    public void CSharpProject_RawTextDoesNotActivateCSharpCompletionOrDiagnostics()
    {
        using var project = new CSharpProjectFixture();
        string path = project.Write("App.csproj", "using System;\nConsole.");
        var editor = new TermXTEditor(path, TermXTEditorSyntax.CSharp, openAsText: true);
        SetPrivateField(editor, "_cursorLine", 1);
        SetPrivateField(editor, "_cursorCol", 8);
        SetPrivateEnumField(editor, "_mode", "Insert");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', ConsoleKey.Spacebar, false, false, true));
        GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
        Diagnostics(editor).Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CSharpProject_LoadsHintPathsAndRestoredPackageCompileAssets(bool packageAssets)
    {
        using var project = new CSharpProjectFixture();
        string assemblyPath = project.PathFor("packages/sample/1.0.0/lib/net10.0/Sample.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(assemblyPath)!);
        var compilation = CSharpCompilation.Create("Sample", new[] { CSharpSyntaxTree.ParseText("public class PackageType { public void PackageMember() {} }") },
            Core.SystemTools.Roslyn.References(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        compilation.Emit(assemblyPath).Success.Should().BeTrue();
        project.Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>" +
            (packageAssets ? "" : "<ItemGroup><Reference Include=\"Sample\"><HintPath>../packages/sample/1.0.0/lib/net10.0/Sample.dll</HintPath></Reference></ItemGroup>") + "</Project>");
        if (packageAssets)
        {
            var assets = new
            {
                targets = new Dictionary<string, object>
                {
                    ["net9.0"] = new { },
                    ["net10.0"] = new Dictionary<string, object> { ["Sample/1.0.0"] = new { compile = new Dictionary<string, object> { ["lib/net10.0/Sample.dll"] = new { } } } }
                },
                libraries = new Dictionary<string, object> { ["Sample/1.0.0"] = new { path = "sample/1.0.0" } },
                packageFolders = new Dictionary<string, object> { [project.PathFor("packages")] = new { } }
            };
            project.Write("App/obj/project.assets.json", System.Text.Json.JsonSerializer.Serialize(assets));
        }
        var editor = project.Editor("App/Program.cs", "class Program { void Test() { new PackageType().$$ } }");
        CSharpCompletions(editor).Should().Contain(c => c.Label == "PackageMember");
    }

    [Fact]
    public void CSharpProject_LegacyItemsAndSharedConditionalPropertiesAreRespected()
    {
        using var project = new CSharpProjectFixture();
        project.Write("Directory.Build.props", "<Project><PropertyGroup><DefineConstants>SHARED</DefineConstants></PropertyGroup><PropertyGroup Condition=\"'$(Configuration)' == 'Release'\"><DefineConstants>RELEASE_ONLY</DefineConstants></PropertyGroup></Project>");
        project.Write("App/App.csproj", "<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"><ItemGroup><Compile Include=\"Program.cs\" /><Compile Include=\"Customer.cs\" /></ItemGroup></Project>");
        project.Write("App/Customer.cs", "#if SHARED\npublic class Customer { public void SharedMember() {} }\n#endif\n#if RELEASE_ONLY\npublic class ReleaseOnly {}\n#endif");
        project.Write("App/Excluded.cs", "public class Unincluded {}");
        var editor = project.Editor("App/Program.cs", "class Program { void Test() { new Customer().$$ } }");
        CSharpCompletions(editor).Should().Contain(c => c.Label == "SharedMember");
        SetMarkedCSharpBuffer(editor, "class Program { Uninc$$ }");
        CSharpCompletions(editor).Should().NotContain(c => c.Label == "Unincluded");
        SetMarkedCSharpBuffer(editor, "class Program { Relea$$ }");
        CSharpCompletions(editor).Should().NotContain(c => c.Label == "ReleaseOnly");
    }

    [Fact]
    public void CSharpProject_TransitiveReferencesResolveReturnTypesAndGlobalUsingsStayInTheirProject()
    {
        using var project = new CSharpProjectFixture();
        project.Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"../Library/Library.csproj\" /></ItemGroup></Project>");
        project.Write("Library/Library.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup><ProjectReference Include=\"../Models/Models.csproj\" /></ItemGroup></Project>");
        project.Write("Models/Models.csproj", CSharpProjectFixture.SdkProject);
        project.Write("Models/Customer.cs", "public class Customer { public void Save() {} }");
        project.Write("Library/Factory.cs", "public class Factory { public Customer Create() => new Customer(); }");
        var editor = project.Editor("App/Program.cs", "class Program { void Test() { new Factory().Create().$$ } }");
        CSharpCompletions(editor).Should().Contain(c => c.Label == "Save");
        SetMarkedCSharpBuffer(editor, "class Program { void Test() { Console.WriteLine(); } }$$");
        Diagnostics(editor).Should().Contain(d => d.Code == "CS0103");
    }

    [Fact]
    public void CSharpProject_AutomaticallyLoadsSiblingClassesAndTheirMembers()
    {
        using var project = new CSharpProjectFixture();
        project.Write("App.csproj", CSharpProjectFixture.SdkProject);
        project.Write("Models/Customer.cs", "namespace Shop; public class Customer { public string DisplayName { get; set; } public void Save() {} }");
        var editor = project.Editor("Program.cs", "using Shop; class Program { void Test() { var customer = new Customer(); customer.$$ } }");

        CSharpCompletions(editor).Should().Contain(c => c.Label == "DisplayName").And.Contain(c => c.Label == "Save");
        editor = project.Editor("Program.cs", "using Shop; class Program { Cust$$ }");
        CSharpCompletions(editor).Should().Contain(c => c.Label == "Customer");
    }

    [Theory]
    [InlineData(".sln")]
    [InlineData(".slnx")]
    [InlineData(".csproj")]
    public void CSharpProject_OpeningWorkspaceLoadsSourceAndRootsExplorer(string extension)
    {
        using var project = new CSharpProjectFixture();
        project.Write("App/App.csproj", CSharpProjectFixture.SdkProject);
        string source = project.Write("App/Program.cs", "class Program {}");
        project.Write("App/Alpha.cs", "class Alpha {}");
        string workspace = extension == ".csproj" ? project.PathFor("App/App.csproj") : project.Solution(extension, "App/App.csproj");
        var editor = new TermXTEditor(workspace);

        GetPrivateField<string>(editor, "_path").Should().Be(source);
        GetPrivateField<TermXTEditorSyntax>(editor, "_syntax").Should().Be(TermXTEditorSyntax.CSharp);
        GetPrivateField<string>(editor, "_csharpWorkspacePath").Should().Be(workspace);
        InvokePrivate<string>(editor, "GetExplorerStartDirectory").Should().Be(Path.GetDirectoryName(workspace));
        Lines(editor).Should().ContainSingle().Which.Should().Be("class Program {}");
    }

    [Theory]
    [InlineData(".sln")]
    [InlineData(".slnx")]
    public void CSharpProject_ReferencesResolveWithoutBuildingAndUnrelatedProjectsStayOut(string extension)
    {
        using var project = new CSharpProjectFixture();
        project.Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"../Library/Library.csproj\" /></ItemGroup></Project>");
        project.Write("Library/Library.csproj", CSharpProjectFixture.SdkProject);
        project.Write("Library/Customer.cs", "namespace Shop; public class Customer { public string Name => \"\"; internal void Hidden() {} }");
        project.Write("Other/Other.csproj", CSharpProjectFixture.SdkProject);
        project.Write("Other/Unrelated.cs", "namespace Shop; public class Unrelated {}");
        project.Write("App/Program.cs", "");
        string workspace = project.Solution(extension, "App/App.csproj", "Library/Library.csproj", "Other/Other.csproj");
        var editor = new TermXTEditor(workspace);
        SetMarkedCSharpBuffer(editor, "using Shop; class Program { void Test() { new Customer().$$ } }");

        CSharpCompletions(editor).Should().Contain(c => c.Label == "Name").And.NotContain(c => c.Label == "Hidden");
        SetMarkedCSharpBuffer(editor, "using Shop; class Program { Unrel$$ }");
        CSharpCompletions(editor).Should().NotContain(c => c.Label == "Unrelated");
        SetMarkedCSharpBuffer(editor, "using Shop; class Program { Customer value = new Customer(); }$$");
        Diagnostics(editor).Should().NotContain(d => d.Severity == "Error");
    }

    [Theory]
    [InlineData(".sln")]
    [InlineData(".slnx")]
    public void CSharpProject_DirectlyOpenedLinkedFileFindsOwnerThroughSolution(string extension)
    {
        using var project = new CSharpProjectFixture();
        project.Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><Compile Include=\"../Shared/**/*.cs\" /></ItemGroup></Project>");
        project.Write("App/Customer.cs", "namespace Shop; public class Customer { public void Save() {} }");
        project.Solution(extension, "App/App.csproj");
        var editor = project.Editor("Shared/Linked.cs", "namespace Shop; class Linked { void Test() { new Customer().$$ } }");

        CSharpCompletions(editor).Should().Contain(c => c.Label == "Save");
    }

    [Fact]
    public void CSharpProject_CompileItemsExcludeRemovedGeneratedAndUnincludedSources()
    {
        using var project = new CSharpProjectFixture();
        project.Write("App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include=\"Program.cs;Models/**/*.cs\" Exclude=\"Models/Excluded.cs\" /><Compile Remove=\"Models/Removed.cs\" /></ItemGroup></Project>");
        foreach (string path in new[] { "Models/Included.cs", "Models/Removed.cs", "Models/Excluded.cs", "Unincluded.cs", "obj/Generated.cs", "bin/Output.cs" })
            project.Write(path, "public class " + Path.GetFileNameWithoutExtension(path) + " {}");
        var editor = project.Editor("Program.cs", "class Program { $$ }");
        InvokePrivate(editor, "StartCSharpCompletion", true);
        var names = ActiveCSharpCompletions(editor).Select(c => c.Label);
        names.Should().Contain("Included").And.NotContain(new[] { "Removed", "Excluded", "Unincluded", "Generated", "Output" });
    }

    [Fact]
    public void CSharpProject_DiagnosticsUseProjectSettingsAndOnlyCurrentUnsavedBuffer()
    {
        using var project = new CSharpProjectFixture();
        project.Write("App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><DefineConstants>CUSTOM</DefineConstants><OutputType>Exe</OutputType></PropertyGroup></Project>");
        project.Write("Customer.cs", "#if CUSTOM\npublic class Customer { public string? Name { get; set; } }\n#endif");
        project.Write("Broken.cs", "class Broken { MissingType value; }");
        var editor = project.Editor("Program.cs", "class Old { MissingType bad; }$$");
        SetMarkedCSharpBuffer(editor, "var customer = new Customer(); Console.WriteLine(customer.Name);$$");

        Diagnostics(editor).Should().NotContain(d => d.Severity == "Error").And.NotContain(d => d.Code == "CS8632");
        File.ReadAllText(project.PathFor("Program.cs")).Should().Contain("MissingType");
    }

    [Fact]
    public void CSharpProject_ReloadPicksUpChangedAddedAndDeletedSources()
    {
        using var project = new CSharpProjectFixture();
        project.Write("App.csproj", CSharpProjectFixture.SdkProject);
        project.Write("Customer.cs", "public class Customer { public void Before() {} }");
        var editor = project.Editor("Program.cs", "class Program { void Test() { new Customer().$$ } }");
        CSharpCompletions(editor).Should().Contain(c => c.Label == "Before");
        project.Write("Customer.cs", "public partial class Customer { public void AfterReload() {} }");
        project.Write("Extra.cs", "public partial class Customer { public void Added() {} }");
        InvokePrivate(editor, "ExecuteEditorCommand", "project reload");
        CSharpCompletions(editor).Should().Contain(c => c.Label == "AfterReload").And.Contain(c => c.Label == "Added").And.NotContain(c => c.Label == "Before");
        File.Delete(project.PathFor("Extra.cs"));
        InvokePrivate(editor, "ExecuteEditorCommand", "project reload");
        CSharpCompletions(editor).Should().NotContain(c => c.Label == "Added");
    }

    [Fact]
    public void CSharpProject_SwitchingToStandaloneFileDropsProjectSymbols()
    {
        using var project = new CSharpProjectFixture();
        project.Write("App/App.csproj", CSharpProjectFixture.SdkProject);
        project.Write("App/Customer.cs", "public class Customer { public void Save() {} }");
        var editor = project.Editor("App/Program.cs", "class Program { Cust$$ }");
        CSharpCompletions(editor).Should().Contain(c => c.Label == "Customer");
        string standalone = project.Write("Standalone.cs", "class Standalone { Cust }");
        InvokePrivate(editor, "OpenExplorerFile", standalone);
        SetMarkedCSharpBuffer(editor, "class Standalone { Cust$$ }");
        CSharpCompletions(editor).Should().NotContain(c => c.Label == "Customer");
    }

    [Fact]
    public void CSharpProject_InvalidSolutionPreservesOpenBuffer()
    {
        using var project = new CSharpProjectFixture();
        var editor = project.Editor("Program.cs", "class Program {}$$");
        string invalid = project.Write("Broken.slnx", "<Solution><Project");
        InvokePrivate(editor, "OpenExplorerFile", invalid);
        GetPrivateField<string>(editor, "_path").Should().Be(project.PathFor("Program.cs"));
        Lines(editor).Should().ContainSingle().Which.Should().Be("class Program {}");
    }

    private static void SetMarkedCSharpBuffer(TermXTEditor editor, string marked)
    {
        int marker = marked.IndexOf("$$", StringComparison.Ordinal);
        string text = marked.Remove(marker, 2);
        Lines(editor).Clear();
        Lines(editor).AddRange(text.Split('\n'));
        var cursor = LineColumnAtPosition(text, marker);
        SetPrivateField(editor, "_cursorLine", cursor.line);
        SetPrivateField(editor, "_cursorCol", cursor.column);
        typeof(TermXTEditor).GetMethod("InvalidateDocumentCaches", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, new[] { typeof(bool) }, null)!.Invoke(editor, new object[] { false });
        InvokePrivate(editor, "DismissCompletion");
    }

    private sealed class CSharpProjectFixture : IDisposable
    {
        internal const string SdkProject = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>";
        private readonly string root = Path.Combine(Path.GetTempPath(), "xte-project-tests-" + Guid.NewGuid().ToString("N"));
        internal string PathFor(string path) => Path.GetFullPath(Path.Combine(root, path));
        internal string Write(string path, string text)
        {
            string full = PathFor(path);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, text);
            return full;
        }
        internal TermXTEditor Editor(string path, string marked)
        {
            Write(path, "");
            return CSharpEditorAtMarker(PathFor(path), marked);
        }
        internal string Solution(string extension, params string[] projects)
        {
            string text = extension == ".slnx"
                ? "<Solution><Folder Name=\"/Sources/\">" + string.Concat(projects.Select(p => "<Project Path=\"" + p + "\" />")) + "</Folder></Solution>"
                : "Microsoft Visual Studio Solution File, Format Version 12.00\n" + string.Join("\n", projects.Select(p => "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"App\", \"" + p.Replace('/', '\\') + "\", \"{" + Guid.NewGuid() + "}\"\nEndProject"));
            return Write("Workspace" + extension, text);
        }
        public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
