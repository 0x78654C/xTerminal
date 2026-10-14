using System.Reflection;
using System.Runtime.Versioning;
using System.Xml.Linq;
using Commands.TerminalCommands.ConsoleSystem;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

[SupportedOSPlatform("windows")]
public class FsMonTests
{
    private const string Root = @"C:\fsmon-test";
    private const string FilePath = Root + @"\temporary.tmp";
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Type ResolverType = typeof(FsMon).Assembly.GetType("Commands.TerminalCommands.ConsoleSystem.FsMonIdentityResolver")!;

    [Theory]
    [InlineData(WatcherChangeTypes.Created)]
    [InlineData(WatcherChangeTypes.Deleted)]
    public void TempFileAlreadyGoneShowsLabelledParentOwner(WatcherChangeTypes change)
    {
        using var resolver = Resolver(p => p == Root ? @"PC\parent" : null);
        Capture(resolver, FilePath, change).Should().Be(@"PC\parent [parent owner]");
    }

    [Fact]
    public void DeletedFileKeepsCapturedOwnerAndDoesNotReadAReplacementFile()
    {
        string? owner = @"PC\original";
        using var resolver = Resolver(p => p == FilePath ? owner : null);
        Capture(resolver, FilePath, WatcherChangeTypes.Created).Should().Be(@"PC\original [owner]");
        owner = @"PC\replacement";
        Capture(resolver, FilePath, WatcherChangeTypes.Deleted).Should().Be(@"PC\original [cached owner]");
        Capture(resolver, FilePath, WatcherChangeTypes.Created).Should().Be(@"PC\replacement [owner]");
    }

    [Fact]
    public void RecreatedPathCannotInheritAnOldCachedOwner()
    {
        string? owner = @"PC\original";
        using var resolver = Resolver(p => p == FilePath ? owner : null);
        Capture(resolver, FilePath, WatcherChangeTypes.Created);
        owner = null;
        Capture(resolver, FilePath, WatcherChangeTypes.Created).Should().Be("unknown");
    }

    [Fact]
    public void RenameTransfersOwnerAndClearsTheOldPath()
    {
        string? owner = @"PC\original";
        using var resolver = Resolver(p => p == FilePath ? owner : null);
        Capture(resolver, FilePath, WatcherChangeTypes.Created);
        owner = null;
        string renamed = Root + @"\renamed.tmp";
        Capture(resolver, renamed, WatcherChangeTypes.Renamed, FilePath).Should().Be(@"PC\original [cached owner]");
        Capture(resolver, renamed, WatcherChangeTypes.Deleted).Should().Be(@"PC\original [cached owner]");
        Capture(resolver, FilePath, WatcherChangeTypes.Deleted).Should().Be("unknown");
    }

    [Fact]
    public void VanishedDirectoryUsesCachedParentOwnershipWithoutCallingItTheFileOwner()
    {
        string? owner = @"PC\parent";
        using var resolver = Resolver(p => p == Root ? owner : null);
        Capture(resolver, FilePath, WatcherChangeTypes.Created);
        owner = null;
        Capture(resolver, FilePath, WatcherChangeTypes.Deleted).Should().Be(@"PC\parent [cached parent owner]");
    }

    [Fact]
    public void DelayedAuditOverridesParentFallbackAfterTheFileHasGone()
    {
        using var resolver = Resolver(p => p == Root ? @"PC\parent" : null);
        var observed = DateTime.UtcNow;
        string fallback = Capture(resolver, FilePath, WatcherChangeTypes.Created);
        Resolve(resolver, FilePath, WatcherChangeTypes.Created, observed, fallback).Should().Be(fallback);

        Audit(resolver, FilePath, observed, "writer", "0x2");
        Resolve(resolver, FilePath, WatcherChangeTypes.Created, observed, fallback).Should().Be(@"DOMAIN\writer (brave.exe)");
    }

    [Fact]
    public void CreatorIsNeverReusedAsTheDeleterAndReadsDoNotOverrideWrites()
    {
        using var resolver = Resolver();
        var observed = DateTime.UtcNow;
        Audit(resolver, FilePath, observed.AddMilliseconds(-100), "writer", "0x2");
        Audit(resolver, FilePath, observed, "reader", "0x1");
        Resolve(resolver, FilePath, WatcherChangeTypes.Changed, observed).Should().Be(@"DOMAIN\writer (brave.exe)");
        Resolve(resolver, FilePath, WatcherChangeTypes.Deleted, observed).Should().Be("unknown");

        Audit(resolver, FilePath, observed, "deleter", "0x10000");
        Resolve(resolver, FilePath, WatcherChangeTypes.Deleted, observed).Should().Be(@"DOMAIN\deleter (brave.exe)");
    }

    [Theory]
    [InlineData(Root + @"\other\temporary.tmp")]
    [InlineData(Root + @"-other\temporary.tmp")]
    public void AuditMustMatchTheFullPath(string otherPath)
    {
        using var resolver = Resolver();
        var observed = DateTime.UtcNow;
        Audit(resolver, otherPath, observed, "other", "0x2");
        Resolve(resolver, FilePath, WatcherChangeTypes.Created, observed).Should().Be("unknown");
    }

    [Fact]
    public void FullPathMatchingIgnoresCaseAndAcceptsApostrophes()
    {
        using var resolver = Resolver();
        var observed = DateTime.UtcNow;
        string path = Root + @"\user's file.tmp";
        Audit(resolver, path.ToUpperInvariant(), observed, "writer", "0x2");
        Resolve(resolver, path, WatcherChangeTypes.Created, observed).Should().Be(@"DOMAIN\writer (brave.exe)");
    }

    [Theory]
    [InlineData(-3)]
    [InlineData(2)]
    public void UnrelatedAuditTimesDoNotIdentifyTheActor(int seconds)
    {
        using var resolver = Resolver();
        var observed = DateTime.UtcNow;
        Audit(resolver, FilePath, observed.AddSeconds(seconds), "other", "0x2");
        Resolve(resolver, FilePath, WatcherChangeTypes.Created, observed).Should().Be("unknown");
    }

    [Fact]
    public void ClosestOperationWinsEvenIfAuditDeliveryIsOutOfOrder()
    {
        using var resolver = Resolver();
        var observed = DateTime.UtcNow;
        Audit(resolver, FilePath, observed.AddMilliseconds(-50), "closest", "0x2");
        Audit(resolver, FilePath, observed.AddSeconds(-1), "older", "0x2");
        Resolve(resolver, FilePath, WatcherChangeTypes.Created, observed).Should().Be(@"DOMAIN\closest (brave.exe)");
    }

    [Fact]
    public void RenameCanMatchAuditAtTheOldPath()
    {
        using var resolver = Resolver();
        var observed = DateTime.UtcNow;
        Audit(resolver, FilePath, observed, "renamer", "0x10000");
        Resolve(resolver, Root + @"\renamed.tmp", WatcherChangeTypes.Renamed, observed, oldPath: FilePath)
            .Should().Be(@"DOMAIN\renamer (brave.exe)");
    }

    [Fact]
    public void AnEarlierWriteAtTheOldPathDoesNotIdentifyTheRenamer()
    {
        using var resolver = Resolver();
        var observed = DateTime.UtcNow;
        Audit(resolver, FilePath, observed, "writer", "0x2");
        Resolve(resolver, Root + @"\renamed.tmp", WatcherChangeTypes.Renamed, observed, oldPath: FilePath)
            .Should().Be("unknown");
    }

    [Fact]
    public void HandleRequestsAndMalformedRecordsDoNotIdentifyAnActor()
    {
        using var resolver = Resolver();
        var observed = DateTime.UtcNow;
        Audit(resolver, FilePath, observed, "requester", "0x10000", eventId: 4656);
        Call(resolver, "RecordAudit", "<broken");
        Call(resolver, "RecordAudit", "<Event />");
        Resolve(resolver, FilePath, WatcherChangeTypes.Deleted, observed).Should().Be("unknown");
    }

    [Fact]
    public void NonRecursiveSessionRejectsNestedAudits()
    {
        using var resolver = Resolver(recursive: false);
        var observed = DateTime.UtcNow;
        string nested = Root + @"\nested\file.tmp";
        Audit(resolver, nested, observed, "writer", "0x2");
        Resolve(resolver, nested, WatcherChangeTypes.Created, observed).Should().Be("unknown");
    }

    [Fact]
    public void RealFileOwnerSurvivesDeletionAndSessionsDoNotShareState()
    {
        string directory = Path.Combine(Path.GetTempPath(), "fsmon-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "short-lived.tmp");
            using var resolver = Resolver(root: directory);
            File.WriteAllText(path, "temporary");
            string owner = Capture(resolver, path, WatcherChangeTypes.Created);
            owner.Should().EndWith(" [owner]");
            File.Delete(path);
            Capture(resolver, path, WatcherChangeTypes.Deleted).Should().Be(owner.Replace("[owner]", "[cached owner]"));

            using var fresh = Resolver(root: directory);
            Capture(fresh, path, WatcherChangeTypes.Deleted).Should().EndWith(" [parent owner]");
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static IDisposable Resolver(Func<string, string?>? owner = null, bool recursive = true, string root = Root)
        => (IDisposable)Activator.CreateInstance(ResolverType, Members, null, new object?[] { root, recursive, owner }, null)!;

    private static object? Call(object resolver, string method, params object?[] args)
        => ResolverType.GetMethod(method, Members)!.Invoke(resolver, args);

    private static string Capture(object resolver, string path, WatcherChangeTypes change, string? oldPath = null)
        => (string)Call(resolver, "CaptureFallback", path, change, oldPath)!;

    private static string Resolve(object resolver, string path, WatcherChangeTypes change, DateTime observed,
        string fallback = "unknown", string? oldPath = null)
        => (string)Call(resolver, "Resolve", path, change, observed, fallback, oldPath)!;

    private static void Audit(object resolver, string path, DateTime time, string user, string mask, int eventId = 4663)
    {
        XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events/event";
        XElement Data(string name, string value) => new(ns + "Data", new XAttribute("Name", name), value);
        // Deliberately use a different order from Windows' current schema to
        // verify that account/process fields are read by name, not by index.
        var xml = new XElement(ns + "Event",
            new XElement(ns + "System", new XElement(ns + "EventID", eventId),
                new XElement(ns + "TimeCreated", new XAttribute("SystemTime", time.ToString("O")))),
            new XElement(ns + "EventData", Data("ProcessName", @"C:\Program Files\Brave\brave.exe"),
                Data("ObjectName", path), Data("SubjectDomainName", "DOMAIN"), Data("AccessMask", mask),
                Data("ObjectType", "File"), Data("SubjectUserName", user)));
        Call(resolver, "RecordAudit", xml.ToString());
    }
}
