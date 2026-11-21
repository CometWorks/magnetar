using System;
using System.IO;
using System.Linq;
using Magnetar.Legacy;
using Magnetar.Legacy.Arguments;
using Xunit;

namespace PluginSdkTests;

public class DedicatedLogRetentionTests : IDisposable
{
    private const long MB = 1024 * 1024;
    private static readonly DateTime Now = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);
    private readonly string dir = Path.Combine(
        Path.GetTempPath(),
        "magnetar-log-retention-" + Guid.NewGuid().ToString("N")
    );

    public DedicatedLogRetentionTests() => Directory.CreateDirectory(dir);

    public void Dispose() => Directory.Delete(dir, recursive: true);

    private string Log(string name, double ageDays, long bytes)
    {
        string path = Path.Combine(dir, name);
        using (var stream = File.Create(path))
            stream.SetLength(bytes);
        File.SetLastWriteTimeUtc(path, Now.AddDays(-ageDays));
        return path;
    }

    private string[] Left() =>
        Directory
            .GetFiles(dir)
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    [Fact]
    public void LaunchWithOldAndOversizedLogsKeepsTheActiveOneAndTheNewestThatFit()
    {
        // The active log is the oldest and biggest on purpose: it is kept anyway.
        string active = Log("SpaceEngineersDedicated_active.log", 30, 3 * MB);
        Log("SpaceEngineersDedicated_d01.log", 1, 1 * MB);
        Log("SpaceEngineersDedicated_d02.log", 2, 1 * MB);
        Log("SpaceEngineersDedicated_d03.log", 3, 1 * MB); // over the 6 MB total
        Log("SpaceEngineersDedicated_d20.log", 20, 1); // older than 14 days
        Log("info.log", 40, 1 * MB); // not a dedicated server log

        var deleted = DedicatedLogRetention.Prune(
            dir,
            "SpaceEngineersDedicated_*.log",
            active,
            maxAgeDays: 14,
            maxTotalBytes: 5 * MB + MB / 2,
            Now
        );

        Assert.Equal(
            new[] { "SpaceEngineersDedicated_d03.log", "SpaceEngineersDedicated_d20.log" },
            deleted.OrderBy(name => name, StringComparer.Ordinal)
        );
        Assert.Equal(
            new[]
            {
                "SpaceEngineersDedicated_active.log",
                "SpaceEngineersDedicated_d01.log",
                "SpaceEngineersDedicated_d02.log",
                "info.log",
            },
            Left()
        );
    }

    [Fact]
    public void ZeroLimitsDeleteNothing()
    {
        string active = Log("SpaceEngineersDedicated_active.log", 0, 1);
        Log("SpaceEngineersDedicated_old.log", 400, 50 * MB);

        Assert.Empty(
            DedicatedLogRetention.Prune(dir, "SpaceEngineersDedicated_*.log", active, 0, 0, Now)
        );
        Assert.Equal(2, Left().Length);
    }

    [Fact]
    public void OptionsDefaultAndParse()
    {
        var defaults = ServerArguments.Parse([]);
        Assert.Equal(14, defaults.LogMaxAgeDays);
        Assert.Equal(1024, defaults.LogMaxSizeMB);

        var set = ServerArguments.Parse(["-logMaxAgeDays", "3", "-logmaxsizemb=0"]);
        Assert.Equal(3, set.LogMaxAgeDays);
        Assert.Equal(0, set.LogMaxSizeMB);
        Assert.DoesNotContain(set.PulsarArguments, arg => arg.Contains("log"));

        Assert.Throws<ArgumentException>(() => ServerArguments.Parse(["-logMaxAgeDays", "-1"]));
        Assert.Throws<ArgumentException>(() => ServerArguments.Parse(["-logMaxSizeMB", "lots"]));
    }
}
