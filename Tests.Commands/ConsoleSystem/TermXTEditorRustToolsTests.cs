using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

public partial class TermXTEditorSyntaxTests
{
    [Fact]
    public void RustCompletion_DiscoversNewestInstalledVsCodeServer()
    {
        string root = Path.Combine(Path.GetTempPath(), "xte-rust-tools-" + Guid.NewGuid());
        try
        {
            foreach (string version in new[] { "0.3.99", "0.3.100", "0.3.101" })
            {
                string server = Path.Combine(root, "rust-lang.rust-analyzer-" + version + "-win32-x64", "server");
                Directory.CreateDirectory(server);
                if (version != "0.3.101") File.WriteAllText(Path.Combine(server, "rust-analyzer.exe"), "fixture");
            }
            InvokePrivateStatic<string>("FindBundledRustAnalyzer", root).Should().Be(
                Path.Combine(root, "rust-lang.rust-analyzer-0.3.100-win32-x64", "server", "rust-analyzer.exe"));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Theory]
    [InlineData("default", "stable-x86_64-pc-windows-gnu")]
    [InlineData("environment", "nightly-x86_64-pc-windows-msvc")]
    [InlineData("project", "nightly-x86_64-pc-windows-msvc")]
    [InlineData("legacy", "nightly-x86_64-pc-windows-msvc")]
    [InlineData("override", "nightly-x86_64-pc-windows-msvc")]
    [InlineData("uninstalled", null)]
    public void RustCompletion_DiscoversToolchainWithoutPathAndHonorsOverrides(string selection, string? expected)
    {
        string root = Path.Combine(Path.GetTempPath(), "xte-rust-tools-" + Guid.NewGuid());
        string rustup = Path.Combine(root, "rustup");
        string project = Path.Combine(root, "project");
        Directory.CreateDirectory(project);
        try
        {
            foreach (string toolchain in new[] { "stable-x86_64-pc-windows-gnu", "nightly-x86_64-pc-windows-msvc" })
            {
                string bin = Path.Combine(rustup, "toolchains", toolchain, "bin");
                Directory.CreateDirectory(bin);
                File.WriteAllText(Path.Combine(bin, "cargo.exe"), "fixture");
                File.WriteAllText(Path.Combine(bin, "rustc.exe"), "fixture");
            }
            string settings = "default_toolchain = \"stable-x86_64-pc-windows-gnu\"\ndefault_host_triple = \"x86_64-pc-windows-msvc\"\n";
            if (selection == "override") settings += "[overrides]\n" + System.Text.Json.JsonSerializer.Serialize(project) + " = \"nightly-x86_64-pc-windows-msvc\"\n";
            File.WriteAllText(Path.Combine(rustup, "settings.toml"), settings);
            if (selection == "project" || selection == "uninstalled")
                File.WriteAllText(Path.Combine(project, "rust-toolchain.toml"), "[toolchain]\nchannel = '" + (selection == "project" ? "nightly" : "1.0.0") + "' # pinned\n");
            if (selection == "legacy") File.WriteAllText(Path.Combine(project, "rust-toolchain"), "nightly\n");
            string? environment = selection == "environment" ? "nightly" : null;
            InvokePrivateStatic<string?>("FindInstalledRustToolchain", project, rustup, environment!).Should().Be(
                expected == null ? null : Path.Combine(rustup, "toolchains", expected, "bin"));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void RustCompletion_RustupProxyDoesNotHideAnInstalledServer()
    {
        string root = Path.Combine(Path.GetTempPath(), "xte-rust-tools-" + Guid.NewGuid());
        string cargoBin = Path.Combine(root, "cargo", "bin");
        string toolchain = Path.Combine(root, "toolchain", "bin");
        string extensions = Path.Combine(root, "extensions");
        string bundled = Path.Combine(extensions, "rust-lang.rust-analyzer-0.3.100-win32-x64", "server", "rust-analyzer.exe");
        try
        {
            Directory.CreateDirectory(cargoBin);
            Directory.CreateDirectory(toolchain);
            Directory.CreateDirectory(Path.GetDirectoryName(bundled)!);
            File.WriteAllText(Path.Combine(cargoBin, "rustup.exe"), "fixture");
            File.WriteAllText(Path.Combine(cargoBin, "rust-analyzer.exe"), "fixture");
            File.WriteAllText(bundled, "fixture");
            InvokePrivateStatic<string>("FindInstalledRustAnalyzer", new[] { cargoBin }, cargoBin, toolchain, new[] { extensions })
                .Should().Be(bundled);
            File.WriteAllText(Path.Combine(toolchain, "rust-analyzer.exe"), "fixture");
            InvokePrivateStatic<string>("FindInstalledRustAnalyzer", new[] { cargoBin }, cargoBin, toolchain, new[] { extensions })
                .Should().Be(Path.Combine(toolchain, "rust-analyzer.exe"));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void RustCompletion_AutomaticDotReportsAnalyzerFailure()
    {
        var editor = RustEditorAtMarker("client.$$ ");
        InstallRustProjectResult(editor, "[]", 7);
        var task = GetPrivateField<Task>(editor, "_rustProjectCompletionTask");
        object result = task.GetType().GetProperty("Result")!.GetValue(task)!;
        result.GetType().GetField("Error")!.SetValue(result, "Rust tools could not be loaded");
        result.GetType().GetField("Notify")!.SetValue(result, true);
        InvokePrivate<bool>(editor, "CheckRustCompletionOnIdle").Should().BeTrue();
        GetPrivateField<string>(editor, "_bottomStatus").Should().Contain("Rust tools could not be loaded");
    }
}
