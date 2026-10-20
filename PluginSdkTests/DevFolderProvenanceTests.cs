using System;
using System.Diagnostics;
using System.IO;
using Magnetar.Legacy.Preparation;
using Xunit;

namespace PluginSdkTests;

public sealed class DevFolderProvenanceTests : IDisposable
{
    private readonly string repository = Path.Combine(
        Path.GetTempPath(),
        "dev-folder-provenance-" + Guid.NewGuid().ToString("N")
    );
    private readonly string plugin;

    public DevFolderProvenanceTests()
    {
        plugin = Path.Combine(repository, "world", "plugin");
        Directory.CreateDirectory(plugin);
        File.WriteAllText(Path.Combine(plugin, "Plugin.cs"), "class Plugin {}\n");
        File.WriteAllText(Path.Combine(repository, ".gitignore"), "bin/\n");
        Git("init -q");
        Git("add -A");
        Git("-c user.name=Test -c user.email=test@example.com commit -q -m Initial");
    }

    public void Dispose()
    {
        foreach (string file in Directory.GetFiles(repository, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(repository, true);
    }

    [Fact]
    public void CleanCheckoutPinsHead()
    {
        Directory.CreateDirectory(Path.Combine(plugin, "bin"));
        File.WriteAllText(Path.Combine(plugin, "bin", "ignored.dll"), "");

        Assert.Equal(
            Git("rev-parse HEAD").Trim(),
            DevFolderProvenance.CleanHead("example", plugin)
        );
    }

    [Fact]
    public void RefusesModifiedTrackedFile()
    {
        File.AppendAllText(Path.Combine(plugin, "Plugin.cs"), "// edited\n");

        var error = Assert.Throws<InvalidDataException>(() =>
            DevFolderProvenance.CleanHead("example", plugin)
        );
        Assert.Contains("example", error.Message);
        Assert.Contains("world/plugin/Plugin.cs", error.Message);
    }

    [Fact]
    public void RefusesUntrackedFileAnywhereInRepository()
    {
        File.WriteAllText(Path.Combine(repository, "world", "notes.txt"), "");

        var error = Assert.Throws<InvalidDataException>(() =>
            DevFolderProvenance.CleanHead("example", plugin)
        );
        Assert.Contains("world/notes.txt", error.Message);
    }

    [Fact]
    public void RefusesFolderOutsideGit()
    {
        string outside = Path.Combine(
            Path.GetTempPath(),
            "dev-folder-outside-" + Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(outside);
        try
        {
            var error = Assert.Throws<InvalidDataException>(() =>
                DevFolderProvenance.CleanHead("example", outside)
            );
            Assert.Contains("git checkout", error.Message);
        }
        finally
        {
            Directory.Delete(outside);
        }
    }

    private string Git(string arguments)
    {
        var startInfo = new ProcessStartInfo("git", arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = repository,
        };
        using var process = Process.Start(startInfo);
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, "git " + arguments + ": " + error);
        return output;
    }
}
