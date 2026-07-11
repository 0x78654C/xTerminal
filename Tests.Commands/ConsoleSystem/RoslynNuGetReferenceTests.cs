using Core.SystemTools;
using FluentAssertions;
using System;
using System.IO;
using System.Linq;
using Xunit;
using RoslynHelper = Core.SystemTools.Roslyn;

namespace Tests.Commands.ConsoleSystem;

public class RoslynNuGetReferenceTests
{
    [Fact]
    public void GetNuGetPackageReferences_ParsesCommentAndScriptDirectives()
    {
        string source = @"// nuget: Newtonsoft.Json 13.0.3
#r ""nuget: Humanizer, 2.14.1""
// nuget: Newtonsoft.Json 12.0.1
using System;";

        var packages = RoslynHelper.GetNuGetPackageReferences(source).ToList();

        packages.Should().HaveCount(2);
        packages[0].Id.Should().Be("Newtonsoft.Json");
        packages[0].Version.Should().Be("13.0.3");
        packages[1].Id.Should().Be("Humanizer");
        packages[1].Version.Should().Be("2.14.1");
    }

    [Fact]
    public void TryCreateNuGetPackageDirective_RejectsInvalidPackageId()
    {
        bool success = RoslynHelper.TryCreateNuGetPackageDirective(
            @"..\bad",
            "1.0.0",
            out string directive,
            out string error);

        success.Should().BeFalse();
        directive.Should().BeEmpty();
        error.Should().Contain("Invalid");
    }

    [Fact]
    public void IsNuGetCertificateValidationError_DetectsX509ChainFailures()
    {
        string message = "The SSL connection could not be established. The remote certificate is invalid because of errors in the certificate chain: UntrustedRoot. X.509 chain validation failed.";

        RoslynHelper.IsNuGetCertificateValidationError(message).Should().BeTrue();
        RoslynHelper.NuGetCertificateValidationMessage(message).Should().Contain(":nuget restore");
    }

    [Fact]
    public void IsNuGetPackageNotFoundError_DetectsWrongPackageNameFailures()
    {
        string message = "error: NU1101: Unable to find package NotA.Real.Package. No packages exist with this id in source(s): nuget.org";

        RoslynHelper.IsNuGetPackageNotFoundError(message).Should().BeTrue();
        RoslynHelper.NuGetPackageNotFoundMessage(
                new NuGetPackageReference("NotA.Real.Package", string.Empty),
                message)
            .Should()
            .Contain("NuGet package not found: NotA.Real.Package");
    }

    [Fact]
    public void TryResolveNuGetPackageReferencePaths_UsesLocalPackageCache()
    {
        string packageRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string originalNuGetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");

        try
        {
            Environment.SetEnvironmentVariable("NUGET_PACKAGES", packageRoot);
            string packageDll = CreateFakePackage(packageRoot, "Example.Package", "1.2.0");

            bool success = RoslynHelper.TryResolveNuGetPackageReferencePaths(
                new NuGetPackageReference("Example.Package", "1.2.0"),
                out var paths,
                out string error);

            success.Should().BeTrue(error);
            paths.Should().ContainSingle(path => string.Equals(path, packageDll, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Environment.SetEnvironmentVariable("NUGET_PACKAGES", originalNuGetPackages);
            if (Directory.Exists(packageRoot))
                Directory.Delete(packageRoot, recursive: true);
        }
    }

    private static string CreateFakePackage(string packageRoot, string id, string version)
    {
        string packageDir = Path.Combine(packageRoot, id.ToLowerInvariant(), version);
        string libDir = Path.Combine(packageDir, "lib", "netstandard2.0");
        Directory.CreateDirectory(libDir);

        string packageDll = Path.Combine(libDir, id + ".dll");
        File.Copy(typeof(RoslynNuGetReferenceTests).Assembly.Location, packageDll);
        File.WriteAllText(
            Path.Combine(packageDir, id.ToLowerInvariant() + ".nuspec"),
            @"<?xml version=""1.0""?>
<package>
  <metadata>
    <id>" + id + @"</id>
    <version>" + version + @"</version>
  </metadata>
</package>");

        return packageDll;
    }
}
