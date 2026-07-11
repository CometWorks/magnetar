using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;
using Magnetar.ConfigTerminal.Process;
using Xunit;

namespace Magnetar.ConfigTerminal.Tests;

public class ProcessAndFileTests : IDisposable
{
    private readonly string dir;

    public ProcessAndFileTests()
    {
        dir = Path.Combine(Path.GetTempPath(), "mcproc_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(dir, true); } catch { }
    }

    [Fact]
    public void Password_matches_the_DS_pbkdf2_formula()
    {
        // The tool must produce a hash the DS accepts: PBKDF2/SHA1, 10000 iters,
        // 16-byte salt, 20-byte key. Re-derive from the emitted salt and compare.
        PasswordHasher.HashedPassword h = PasswordHasher.Hash("secret");
        byte[] salt = Convert.FromBase64String(h.Salt);
        byte[] hash = Convert.FromBase64String(h.Hash);
        Assert.Equal(16, salt.Length);
        Assert.Equal(20, hash.Length);

#pragma warning disable SYSLIB0060
        using var pbkdf2 = new Rfc2898DeriveBytes("secret", salt, 10000);
#pragma warning restore SYSLIB0060
        Assert.Equal(pbkdf2.GetBytes(20), hash);
    }

    [Fact]
    public void LastSession_computes_relative_path_under_saves()
    {
        string saves = Path.Combine(dir, "Saves");
        string world = Path.Combine(saves, "Red Ship");
        Directory.CreateDirectory(world);

        var info = new WorldInfo { FolderName = "Red Ship", FolderPath = world, SessionName = "Red Ship", HasCheckpoint = true };
        LastSessionFile sbl = LastSessionFile.ForWorld(info, saves);
        Assert.Equal("Red Ship", sbl.RelativePath);
        Assert.Equal("Red Ship", sbl.GameName);

        sbl.Write(new AtomicFile(), LastSessionFile.PathFor(saves));
        string xml = File.ReadAllText(LastSessionFile.PathFor(saves));
        Assert.Contains("<RelativePath>Red Ship</RelativePath>", xml);
        Assert.Contains("<MyObjectBuilder_LastSession", xml);
        Assert.Contains("<IsContentWorlds>false</IsContentWorlds>", xml);
    }

    [Fact]
    public void LaunchSpec_builds_expected_args_and_rejects_conflicts()
    {
        var binding = new InstanceBinding
        {
            DataDir = "/data", MagnetarConfigDir = "/cfg", Ds64Dir = "/ds64", MagnetarExePath = "/exe",
        };
        var spec = new LaunchSpec { Binding = binding, IgnoreLastSession = true };
        var args = spec.BuildArgs().ToList();

        Assert.Contains("-daemon", args);
        Assert.Equal("/data", args[args.IndexOf("-path") + 1]);
        Assert.Equal("/cfg", args[args.IndexOf("-config") + 1]);
        Assert.Contains("-ignorelastsession", args);

        spec.ExtraArgs = new[] { "-ignorelastsession" };
        Assert.NotNull(spec.RejectionReason());
        spec.ExtraArgs = new[] { "-session:foo" };
        Assert.NotNull(spec.RejectionReason());
        spec.ExtraArgs = new[] { "-noconsent" };
        Assert.Null(spec.RejectionReason());
    }

    [Fact]
    public void PidFileReader_reports_stale_for_a_dead_pid()
    {
        string pidPath = Path.Combine(dir, "magnetar.pid");
        // A pid that is essentially certain to be dead.
        File.WriteAllText(pidPath, "999999\n/data\n");
        var reader = new PidFileReader(pidPath, "/data");
        ServerStatus s = reader.Query();
        Assert.Equal(ServerState.StalePidFile, s.State);
    }

    [Fact]
    public void PidFileReader_reports_not_running_when_absent()
    {
        var reader = new PidFileReader(Path.Combine(dir, "nope.pid"), "/data");
        Assert.Equal(ServerState.NotRunning, reader.Query().State);
    }

    [Fact]
    public void AtomicFile_backs_up_once_and_writes_content()
    {
        string path = Path.Combine(dir, "f.txt");
        File.WriteAllText(path, "original");
        var writer = new AtomicFile();
        writer.WriteText(path, "first");
        Assert.Equal("original", File.ReadAllText(path + ".bak")); // backup is the pre-edit state
        writer.WriteText(path, "second");
        Assert.Equal("original", File.ReadAllText(path + ".bak")); // still the first backup
        Assert.Equal("second", File.ReadAllText(path));
    }
}
