using System.Runtime.Versioning;
using System.Text.Json;
using Commands;
using Core;
using Core.SystemTools;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

[SupportedOSPlatform("Windows")]
public sealed class CommandRepositoryEnvironmentCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _aliasFile;
    private readonly string _savedAliasFile;

    public CommandRepositoryEnvironmentCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "xt_alias_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);

        _aliasFile = Path.Combine(_tempDir, "alias.json");
        _savedAliasFile = GlobalVariables.aliasFile;

        GlobalVariables.aliasFile = _aliasFile;
        GlobalVariables.aliasParameters = string.Empty;
        GlobalVariables.aliasRunFlag = false;
        GlobalVariables.aliasInParameter.Clear();
    }

    public void Dispose()
    {
        GlobalVariables.aliasFile = _savedAliasFile;
        GlobalVariables.aliasParameters = string.Empty;
        GlobalVariables.aliasRunFlag = false;
        GlobalVariables.aliasInParameter.Clear();

        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public void GetCommand_UnresolvedCommand_ReturnsEnvironmentCommand()
    {
        var command = CommandRepository.GetCommand("where cmd");

        command.Should().NotBeNull();
        command!.Name.Should().Be("where");
        GlobalVariables.aliasParameters.Should().BeEmpty();
    }

    [Fact]
    public void GetCommand_AliasWithSameNameAsEnvironmentCommand_UsesAlias()
    {
        File.WriteAllText(_aliasFile, JsonSerializer.Serialize(new[]
        {
            new AliasC { CommandName = "where", Command = "echo alias-ran" }
        }));

        var command = CommandRepository.GetCommand("where cmd");

        command.Should().NotBeNull();
        command!.Name.Should().Be("echo");
        GlobalVariables.aliasParameters.Should().Be("echo alias-ran");
    }
}
