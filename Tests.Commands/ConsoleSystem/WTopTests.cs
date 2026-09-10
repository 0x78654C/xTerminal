using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Core.SystemTools;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

[SupportedOSPlatform("windows")]
public class WTopTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;
    private static readonly Type SnapshotType = typeof(ProcessListingUI).GetNestedType("ProcessSnapshot", Private)!;

    [Fact]
    public void LoadingFrameDoesNotWaitForASample()
    {
        var ui = new ProcessListingUI();
        Frame(ui).Should().Contain("Loading processes");
        Publish(ui);
        Frame(ui).Should().Contain("No processes available").And.NotContain("Loading processes");
    }

    [Fact]
    public void RefreshAndSortKeepTheSameProcessSelected()
    {
        var ui = new ProcessListingUI();
        Publish(ui, Row(10, "alpha", memory: 20, cpu: 3), Row(20, "beta", memory: 10, cpu: 9));
        Call(ui, "SelectIndex", 1);

        Key(ui, ConsoleKey.C);
        SelectedId(ui).Should().Be(20);
        Field<int>(ui, "_selectedIndex").Should().Be(0);
        Publish(ui, Row(30, "aardvark"), Row(10, "alpha", cpu: 10), Row(20, "beta", cpu: 1));
        Call(ui, "SortedProcesses");
        SelectedId(ui).Should().Be(20);

        Key(ui, ConsoleKey.M);
        SelectedId(ui).Should().Be(20);
        Key(ui, ConsoleKey.N);
        SelectedId(ui).Should().Be(20);
        Field<int>(ui, "_selectedIndex").Should().Be(2);
    }

    [Fact]
    public void RemovedProcessClampsSelectionAndEmptyListClearsIt()
    {
        var ui = new ProcessListingUI();
        Publish(ui, Row(1, "alpha"), Row(2, "beta"));
        Call(ui, "SelectIndex", 1);
        Publish(ui, Row(1, "alpha"));
        Call(ui, "SortedProcesses");
        SelectedId(ui).Should().Be(1);
        Publish(ui);
        Call(ui, "SortedProcesses");
        Call(ui, "SelectedProcess").Should().BeNull();
        Key(ui, ConsoleKey.End);
        Field<int>(ui, "_selectedIndex").Should().Be(0);
    }

    [Fact]
    public void KillTargetIsTheDisplayedSnapshotEvenWhenANewerSampleArrives()
    {
        var ui = new ProcessListingUI();
        var original = Row(2, "beta", started: 20);
        Publish(ui, Row(1, "alpha"), original);
        Call(ui, "SelectIndex", 1);
        Publish(ui, Row(3, "aardvark"), Row(1, "alpha"), Row(2, "replacement", started: 40));

        Call(ui, "SelectedProcess").Should().BeSameAs(original);
        var captured = Call(ui, "SelectedProcess");
        Call(ui, "SortedProcesses");
        captured.Should().BeSameAs(original);
        Property<long>(captured!, "StartTimeTicks").Should().Be(20);
    }

    [Fact]
    public void KillRefusesAReusedPidWithoutTerminatingTheProcess()
    {
        using var process = Process.GetCurrentProcess();
        var ui = new ProcessListingUI();
        var stale = Row(process.Id, process.ProcessName, started: process.StartTime.ToUniversalTime().Ticks - 1);
        Call(ui, "KillSelected", stale);

        Field<string>(ui, "_status").Should().Contain("identity cannot be verified");
        Field<bool>(ui, "_killPending").Should().BeFalse();
        process.HasExited.Should().BeFalse();
    }

    [Fact]
    public void KillTerminatesOnlyTheCapturedTestProcessAndRequestsRefresh()
    {
        using var child = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"),
            Arguments = "-NoLogo -NoProfile -NonInteractive -Command Start-Sleep -Seconds 30",
            UseShellExecute = false,
            CreateNoWindow = true
        })!;
        try
        {
            using var wake = new AutoResetEvent(false);
            var ui = new ProcessListingUI();
            SetField(ui, "_sampleWake", wake);
            Call(ui, "KillSelected", Row(child.Id, child.ProcessName, started: child.StartTime.ToUniversalTime().Ticks));
            child.WaitForExit(5000).Should().BeTrue();
            Field<string>(ui, "_status").Should().StartWith("Killed");
            wake.WaitOne(0).Should().BeTrue();
        }
        finally
        {
            if (!child.HasExited) child.Kill();
        }
    }

    [Fact]
    public void UsernameLookupsAreBoundedAndOldIdentitiesAreRemovedFromCache()
    {
        var ui = new ProcessListingUI();
        var old = Row(10, "old", started: 100);
        object identity = old.GetType().GetProperty("Identity")!.GetValue(old)!;
        object pending = Field<object>(ui, "_pendingUserLookups");
        pending.GetType().GetMethod("Add")!.Invoke(pending, new[] { identity });
        object second = Row(20, "other");
        pending.GetType().GetMethod("Add")!.Invoke(pending, new[] { second.GetType().GetProperty("Identity")!.GetValue(second)! });

        Call(ui, "CachedUser", Row(30, "waiting")).Should().Be("…");
        Property<int>(pending, "Count").Should().Be(2);
        var cache = Field<IDictionary>(ui, "_userCache");
        cache.Add(identity, "previous owner");
        Publish(ui, Row(10, "replacement", started: 200));
        cache.Count.Should().Be(0);
    }

    [Fact]
    public void SearchMatchesNameOrExactPidAndRepeatsWithWraparound()
    {
        var ui = new ProcessListingUI();
        Publish(ui, Row(10, "alpha"), Row(20, "worker"), Row(30, "WorkerHelper"));
        Call(ui, "JumpToProcess", "WORKER", false);
        SelectedId(ui).Should().Be(20);
        Key(ui, ConsoleKey.F3);
        SelectedId(ui).Should().Be(30);
        Key(ui, ConsoleKey.F3);
        SelectedId(ui).Should().Be(20);
        Call(ui, "JumpToProcess", "10", false);
        SelectedId(ui).Should().Be(10);
        Call(ui, "JumpToProcess", "1", false);
        Field<string>(ui, "_status").Should().Be("No match for 1");
    }

    [Fact]
    public void SearchCancelAndEmptyEnterPreserveSearchState()
    {
        var ui = new ProcessListingUI();
        Publish(ui, Row(10, "worker"), Row(20, "workerTwo"));
        Key(ui, ConsoleKey.Oem2, '/');
        foreach (char c in "worker") Key(ui, ConsoleKey.A, c);
        Key(ui, ConsoleKey.Enter, '\r');
        SelectedId(ui).Should().Be(10);
        Key(ui, ConsoleKey.Oem2, '/');
        Key(ui, ConsoleKey.Enter, '\r');
        SelectedId(ui).Should().Be(20);
        Key(ui, ConsoleKey.Oem2, '/');
        Key(ui, ConsoleKey.Escape, '\x1b');
        Field<bool>(ui, "_exitRequested").Should().BeFalse();
        Field<bool>(ui, "_inSearchMode").Should().BeFalse();
    }

    [Fact]
    public void SearchLengthIsBoundedAndControlCAlwaysExits()
    {
        var ui = new ProcessListingUI();
        Key(ui, ConsoleKey.Oem2, '/');
        for (int i = 0; i < 400; i++) Key(ui, ConsoleKey.A, 'a');
        Field<string>(ui, "_searchQuery").Length.Should().Be(256);
        Key(ui, ConsoleKey.C, '\x03', control: true);
        Field<bool>(ui, "_exitRequested").Should().BeTrue();
    }

    [Fact]
    public void RefreshKeyWakesTheSampler()
    {
        using var wake = new AutoResetEvent(false);
        var ui = new ProcessListingUI();
        SetField(ui, "_sampleWake", wake);
        Key(ui, ConsoleKey.R);
        wake.WaitOne(0).Should().BeTrue();
    }

    [Fact]
    public void SortingIsCachedAndUsesDeterministicTies()
    {
        var ui = new ProcessListingUI();
        Publish(ui, Row(3, "beta", memory: 100, cpu: 20), Row(2, "Alpha", memory: 100, cpu: 20), Row(1, "alpha", memory: 100, cpu: 20));
        var view = Call(ui, "SortedProcesses");
        Call(ui, "SortedProcesses").Should().BeSameAs(view);
        Ids(view!).Should().Equal(1, 2, 3);
        Key(ui, ConsoleKey.M);
        var memoryView = Call(ui, "SortedProcesses");
        memoryView.Should().NotBeSameAs(view);
        Ids(memoryView!).Should().Equal(1, 2, 3);
        Key(ui, ConsoleKey.C);
        Ids(Call(ui, "SortedProcesses")!).Should().Equal(1, 2, 3);
    }

    [Theory]
    [InlineData(60, 12)]
    [InlineData(80, 24)]
    [InlineData(120, 30)]
    public void FrameFitsTerminalAndPreservesSnapshotValues(int width, int height)
    {
        var ui = new ProcessListingUI();
        // Inaccessible identities exercise rendering without starting owner lookups.
        Publish(ui, Row(123, new string('x', 180), memory: 104857600, cpu: 25, started: 0));
        SetField(ui, "_inSearchMode", true);
        SetField(ui, "_searchQuery", new string('a', 256));
        string frame = (string)Call(ui, "BuildFrame", width, height)!;
        frame.Should().Contain("123").And.Contain("25.0").And.Contain("100.0");

        var moves = Regex.Matches(frame, "\x1b\\[(\\d+);(\\d+)H");
        for (int i = 0; i < moves.Count; i++)
        {
            int start = moves[i].Index + moves[i].Length;
            int end = i + 1 == moves.Count ? frame.Length : moves[i + 1].Index;
            string visible = Regex.Replace(frame[start..end], "\x1b\\[[0-9;]*[mK]", "");
            int column = int.Parse(moves[i].Groups[2].Value);
            (visible.Length + column - 1).Should().BeLessThanOrEqualTo(width, $"frame segment {i} must not wrap");
        }
    }

    [Theory]
    [InlineData(10000000L, 20000000L, 1.0, 4, 25.0)]
    [InlineData(10000000L, 20000000L, 2.0, 4, 12.5)]
    [InlineData(20000000L, 10000000L, 1.0, 4, 0.0)]
    [InlineData(0L, 100000000L, 1.0, 1, 100.0)]
    [InlineData(0L, 10000000L, 0.0, 4, 0.0)]
    public void ProcessCpuUsesActualElapsedTime(long previous, long current, double seconds, int processors, double expected)
    {
        Call(null, "CalculateProcessCpu", previous, current, seconds, processors).Should().Be(expected);
    }

    [Fact]
    public void SystemCpuSubtractsIdleFromKernelAndUserDeltas()
    {
        object[] values = { 100L, 200L, 300L, 150L, 260L, 340L, 0.0 };
        typeof(ProcessListingUI).GetMethod("TryCalculateSystemCpu", Private)!.Invoke(null, values).Should().Be(true);
        values[6].Should().Be(50.0);
    }

    [Fact]
    public void CaptureReadsLiveProcessesAndReleasesHandlesWithoutWaitingForGc()
    {
        var ui = new ProcessListingUI();
        using var current = Process.GetCurrentProcess();
        Call(ui, "CaptureProcesses"); // Warm framework caches before measuring.
        current.Refresh();
        int before = current.HandleCount;
        Array rows = Array.Empty<object>();
        for (int i = 0; i < 5; i++) rows = (Array)Call(ui, "CaptureProcesses")!;
        current.Refresh();
        current.HandleCount.Should().BeLessThan(before + 40);
        var self = rows.Cast<object>().Single(row => Property<int>(row, "Id") == current.Id);
        Property<string>(self, "Name").Should().Be(current.ProcessName);
        Property<int>(self, "ThreadCount").Should().BeGreaterThan(0);
        Property<long>(self, "StartTimeTicks").Should().Be(current.StartTime.ToUniversalTime().Ticks);
    }

    [Fact]
    public void UsernameLookupVerifiesIdentityAndDoesNotRequireFullProcessAccess()
    {
        using var current = Process.GetCurrentProcess();
        var identity = Row(current.Id, current.ProcessName, started: current.StartTime.ToUniversalTime().Ticks)
            .GetType().GetProperty("Identity")!;
        var row = Row(current.Id, current.ProcessName, started: current.StartTime.ToUniversalTime().Ticks);
        Call(null, "GetUser", identity.GetValue(row)!).Should().Be(Environment.UserName);
        var stale = Row(current.Id, current.ProcessName, started: 1);
        Call(null, "GetUser", identity.GetValue(stale)!).Should().Be("—");
    }

    [Fact]
    public async Task SamplerPublishesAndStopsPromptlyWhenWoken()
    {
        var ui = new ProcessListingUI();
        using var wake = new AutoResetEvent(false);
        SetField(ui, "_sampleWake", wake);
        var sampler = Task.Run(() => Call(ui, "SampleLoop"));
        try
        {
            await Task.Run(() => SpinWait.SpinUntil(() => Field<bool>(ui, "_hasSample"), 5000));
            Field<bool>(ui, "_hasSample").Should().BeTrue();
        }
        finally
        {
            SetField(ui, "_exitRequested", true);
            wake.Set();
            await sampler.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    private static object Row(int id, string name, long memory = 0, double cpu = 0, long started = 100)
        => Activator.CreateInstance(SnapshotType, id, name, memory, 7, started, cpu)!;

    private static void Publish(ProcessListingUI ui, params object[] rows)
    {
        Array snapshots = Array.CreateInstance(SnapshotType, rows.Length);
        for (int i = 0; i < rows.Length; i++) snapshots.SetValue(rows[i], i);
        Call(ui, "PublishProcesses", snapshots);
    }

    private static string Frame(ProcessListingUI ui) => (string)Call(ui, "BuildFrame", 120, 30)!;
    private static int SelectedId(ProcessListingUI ui) => Property<int>(Call(ui, "SelectedProcess")!, "Id");
    private static IEnumerable<int> Ids(object rows) => ((IEnumerable)rows).Cast<object>().Select(row => Property<int>(row, "Id"));
    private static T Property<T>(object target, string name) => (T)target.GetType().GetProperty(name)!.GetValue(target)!;
    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private)!.GetValue(target)!;
    private static void SetField(object target, string name, object value) => target.GetType().GetField(name, Private)!.SetValue(target, value);
    private static object? Call(object? target, string name, params object[] args) => typeof(ProcessListingUI).GetMethod(name, Private)!.Invoke(target, args);
    private static void Key(ProcessListingUI ui, ConsoleKey key, char value = '\0', bool control = false)
        => Call(ui, "HandleKey", new ConsoleKeyInfo(value, key, false, false, control));
}
