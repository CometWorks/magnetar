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

    // Builds a minimal DS world template folder (Content/CustomWorlds/<name>).
    private WorldTemplate MakeTemplate(string folderName, string sessionName, string extraFileName = null)
    {
        string tplDir = Path.Combine(dir, "Content", "CustomWorlds", folderName);
        Directory.CreateDirectory(tplDir);
        File.WriteAllText(Path.Combine(tplDir, "Sandbox.sbc"), "<MyObjectBuilder_Checkpoint><SessionName>" + sessionName + "</SessionName></MyObjectBuilder_Checkpoint>");
        File.WriteAllText(Path.Combine(tplDir, "Sandbox_config.sbc"),
            "<?xml version=\"1.0\"?>\n<MyObjectBuilder_WorldConfiguration>\n  <Settings><GameMode>Survival</GameMode></Settings>\n  <SessionName>" + sessionName + "</SessionName>\n  <LastSaveTime>2001-01-01T00:00:00.0000000</LastSaveTime>\n</MyObjectBuilder_WorldConfiguration>");
        if (extraFileName != null)
            File.WriteAllText(Path.Combine(tplDir, extraFileName), "payload");
        return new WorldTemplate
        {
            FolderName = folderName,
            FolderPath = tplDir,
            DisplayName = folderName,
            HasCheckpoint = true,
            HasWorldConfig = true,
        };
    }

    [Fact]
    public void WorldCreator_copies_template_and_stamps_the_name()
    {
        string saves = Path.Combine(dir, "Saves");
        WorldTemplate tpl = MakeTemplate("Empty World", "{LOCG:CustomWorld_Empty}", extraFileName: "thumb.jpg");

        string created = WorldCreator.CreateFromTemplate(tpl, "My Server World", saves);

        Assert.Equal(Path.Combine(saves, "My Server World"), created);
        Assert.True(File.Exists(Path.Combine(created, "Sandbox.sbc")));   // checkpoint copied verbatim
        Assert.True(File.Exists(Path.Combine(created, "thumb.jpg")));      // all template files copied

        // SessionName is stamped into Sandbox_config.sbc; the template's Settings survive.
        var cfg = WorldConfigDocument.Open(Path.Combine(created, "Sandbox_config.sbc"));
        Assert.Equal("My Server World", cfg.SessionName);

        // The catalog now lists it under the chosen name.
        var catalog = new WorldCatalog(saves);
        catalog.Scan();
        Assert.Contains(catalog.Worlds, w => w.FolderName == "My Server World" && w.SessionName == "My Server World");

        // No staging folder is left behind.
        Assert.DoesNotContain(Directory.EnumerateDirectories(saves), d => Path.GetFileName(d).StartsWith("."));
    }

    [Fact]
    public void WorldCreator_rejects_an_existing_world_folder()
    {
        string saves = Path.Combine(dir, "Saves");
        Directory.CreateDirectory(Path.Combine(saves, "Taken"));
        WorldTemplate tpl = MakeTemplate("Empty World", "Empty");

        Assert.Throws<IOException>(() => WorldCreator.CreateFromTemplate(tpl, "Taken", saves));
    }

    [Fact]
    public void WorldCreator_synthesizes_config_when_template_has_only_a_checkpoint()
    {
        string saves = Path.Combine(dir, "Saves");
        // Template with just Sandbox.sbc (no Sandbox_config.sbc).
        string tplDir = Path.Combine(dir, "Content", "CustomWorlds", "Bare");
        Directory.CreateDirectory(tplDir);
        File.WriteAllText(Path.Combine(tplDir, "Sandbox.sbc"), "<MyObjectBuilder_Checkpoint><SessionName>Bare</SessionName></MyObjectBuilder_Checkpoint>");
        var tpl = new WorldTemplate { FolderName = "Bare", FolderPath = tplDir, DisplayName = "Bare", HasCheckpoint = true, HasWorldConfig = false };

        string created = WorldCreator.CreateFromTemplate(tpl, "Fresh", saves);

        var cfg = WorldConfigDocument.Open(Path.Combine(created, "Sandbox_config.sbc"));
        Assert.Equal("Fresh", cfg.SessionName); // synthesized config carries the chosen name
    }
}
