using System;
using System.IO;
using System.Text;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>
/// Creates a new world by copying a DS world template (Content/CustomWorlds/…)
/// into <c>Saves/</c> and stamping the chosen name into its
/// <c>Sandbox_config.sbc</c> — no server start required.
///
/// The DS loads a dedicated world by reading <c>Sandbox.sbc</c> and then
/// overriding Settings/Mods/SessionName from <c>Sandbox_config.sbc</c>
/// (<c>MyLocalCache.LoadCheckpoint</c>: "Sandbox world configuration file found,
/// overriding checkpoint settings"). So a folder copy plus a patched
/// <c>Sandbox_config.sbc</c> is a complete, immediately editable world — the
/// large (often gzipped) checkpoint is never touched. The only DS behaviour a
/// copy skips is creation-time generation (RandomizeSeed / procedural asteroid
/// generation); the stock templates already ship their content, so this is
/// faithful for them.
/// </summary>
internal static class WorldCreator
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Materializes <paramref name="worldName"/> under <paramref name="savesPath"/>
    /// from <paramref name="template"/>. Returns the created world folder path.
    /// The copy is assembled in a hidden staging folder and moved into place, so a
    /// failure never leaves a half-populated world in the list.
    /// </summary>
    public static string CreateFromTemplate(WorldTemplate template, string worldName, string savesPath)
    {
        if (template == null) throw new ArgumentNullException(nameof(template));
        if (string.IsNullOrWhiteSpace(worldName)) throw new ArgumentException("World name is required.", nameof(worldName));

        string targetDir = Path.Combine(savesPath, worldName);
        if (Directory.Exists(targetDir))
            throw new IOException($"A world folder already exists: {targetDir}");
        if (!Directory.Exists(template.FolderPath))
            throw new DirectoryNotFoundException($"Template folder not found: {template.FolderPath}");

        Directory.CreateDirectory(savesPath);

        string stagingDir = Path.Combine(savesPath, "." + worldName + ".creating");
        if (Directory.Exists(stagingDir))
            Directory.Delete(stagingDir, recursive: true);

        try
        {
            CopyDirectory(template.FolderPath, stagingDir);
            StampWorldConfig(stagingDir, worldName);
            Directory.Move(stagingDir, targetDir);
            return targetDir;
        }
        catch
        {
            try { if (Directory.Exists(stagingDir)) Directory.Delete(stagingDir, recursive: true); } catch { }
            throw;
        }
    }

    /// <summary>Sets SessionName (and a fresh save time) in the copied Sandbox_config.sbc, synthesizing it from the checkpoint if the template had none.</summary>
    private static void StampWorldConfig(string worldDir, string worldName)
    {
        string configPath = Path.Combine(worldDir, "Sandbox_config.sbc");
        bool hadConfig = File.Exists(configPath);

        WorldConfigDocument config = WorldConfigDocument.Open(configPath);
        if (!hadConfig)
            config.SeedFrom(CheckpointReader.TryRead(Path.Combine(worldDir, "Sandbox.sbc")));
        config.SessionName = worldName;
        config.RefreshLastSaveTime();

        // Write directly (no AtomicFile .bak): the whole folder is still staging and
        // is moved into place atomically once complete.
        File.WriteAllText(configPath, config.ToCanonicalString(), Utf8NoBom);
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (string file in Directory.EnumerateFiles(sourceDir))
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: false);
        foreach (string sub in Directory.EnumerateDirectories(sourceDir))
            CopyDirectory(sub, Path.Combine(destDir, Path.GetFileName(sub)));
    }
}
