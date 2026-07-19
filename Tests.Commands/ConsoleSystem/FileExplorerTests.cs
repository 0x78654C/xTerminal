using Core.DirFiles;
using FluentAssertions;
using System.Reflection;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

public class FileExplorerTests
{
    [Fact]
    public void BuildFilePreview_ReturnsNormalizedTextLines()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");

        try
        {
            File.WriteAllText(path, "alpha\tbeta\r\nsecond");

            List<string> preview = BuildFilePreview(path, new FileInfo(path).Length, 5);

            preview.Should().Equal("  alpha    beta", "  second");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void BuildFilePreview_SkipsBinaryFiles()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".bin");

        try
        {
            File.WriteAllBytes(path, new byte[] { 0x41, 0x00, 0x42, 0x03 });

            List<string> preview = BuildFilePreview(path, new FileInfo(path).Length, 5);

            preview.Should().Equal("  (binary preview skipped)");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void BuildFilePreview_ReportsWhenByteLimitTruncatesFile()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");

        try
        {
            File.WriteAllText(path, new string('x', 70 * 1024));

            List<string> preview = BuildFilePreview(path, new FileInfo(path).Length, 2);

            preview.Should().HaveCount(2);
            preview[1].Should().Contain("preview truncated at 64.0 KB");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static List<string> BuildFilePreview(string path, long totalBytes, int maxLines)
    {
        MethodInfo method = typeof(FileExplorer).GetMethod(
            "BuildFilePreview",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        return (List<string>)method.Invoke(null, new object[] { path, totalBytes, maxLines })!;
    }
}
