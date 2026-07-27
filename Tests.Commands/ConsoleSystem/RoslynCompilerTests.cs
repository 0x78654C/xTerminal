using System.Reflection;
using System.Runtime.Versioning;
using Commands.TerminalCommands.Roslyn;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

[SupportedOSPlatform("Windows")]
public class RoslynCompilerTests : IDisposable
{
    private readonly string _tempDir;

    public RoslynCompilerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ccs_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
        }
    }

    [Fact]
    public void CompileAndRun_ParameterlessMain_RunsProgram()
    {
        string path = Path.Combine(_tempDir, "parameterless.cs");
        string outputPath = Path.Combine(_tempDir, "parameterless.txt");
        File.WriteAllText(
            path,
            """
            using System.IO;

            public static class Program
            {
                public static void Main()
                {
                    File.WriteAllText(@"__OUTPUT_PATH__", "parameterless main");
                }
            }
            """.Replace("__OUTPUT_PATH__", outputPath.Replace("\"", "\"\"")));

        RunCompiler(path);

        File.ReadAllText(outputPath).Should().Be("parameterless main");
    }

    [Fact]
    public void CompileAndRun_MainWithArguments_StillPassesCommandLineArguments()
    {
        string path = Path.Combine(_tempDir, "with_arguments.cs");
        string outputPath = Path.Combine(_tempDir, "with_arguments.txt");
        File.WriteAllText(
            path,
            """
            using System.IO;

            public static class Program
            {
                public static void Main(string[] args)
                {
                    File.WriteAllText(@"__OUTPUT_PATH__", string.Join(",", args));
                }
            }
            """.Replace("__OUTPUT_PATH__", outputPath.Replace("\"", "\"\"")));

        RunCompiler(path, "first second");

        File.ReadAllText(outputPath).Should().Be("first,second");
    }

    private static void RunCompiler(string path, string parameters = "")
    {
        var compiler = new Compiler();
        MethodInfo method = typeof(Compiler).GetMethod(
            "CompileAndRun",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        method.Invoke(compiler, new object[] { path, parameters, false });
    }
}
