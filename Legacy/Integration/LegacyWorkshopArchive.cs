using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Pulsar.Shared;
using VRage.GameServices;

namespace Magnetar.Legacy.Integration;

public static class LegacyWorkshopArchive
{
    private const string LegacyArchiveSuffix = "_legacy.bin";
    private const string LegacyArchivePattern = "*" + LegacyArchiveSuffix;
    private const string DataDirectory = "Data";
    private const string MarkerFile = ".magnetar-legacy-extract";

    private static readonly MethodInfo FolderSetter = AccessTools.PropertySetter(
        typeof(MyWorkshopItem),
        nameof(MyWorkshopItem.Folder)
    );

    public static string FindLegacyArchive(string modFolder)
    {
        if (string.IsNullOrWhiteSpace(modFolder) || !Directory.Exists(modFolder))
            return null;

        return Directory
            .EnumerateFiles(modFolder, LegacyArchivePattern, SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    /// <summary>
    /// Expands the legacy archive of a Workshop mod and returns the folder to
    /// load the mod from. Steam points a legacy item at its archive file, so
    /// for those that is the archive's folder. Returns <paramref name="modPath"/>
    /// unchanged when there is nothing to expand or the expansion failed.
    /// </summary>
    public static string Repair(ulong workshopId, string modPath)
    {
        if (string.IsNullOrWhiteSpace(modPath))
            return modPath;

        string modFolder = modPath;
        string legacyArchive;
        if (modPath.EndsWith(LegacyArchiveSuffix, StringComparison.OrdinalIgnoreCase))
        {
            if (!File.Exists(modPath))
                return modPath;
            legacyArchive = modPath;
            modFolder = Path.GetDirectoryName(modPath);
        }
        else
        {
            legacyArchive = FindLegacyArchive(modPath);
            if (legacyArchive is null)
                return modPath;
        }

        string dataPath = Path.Combine(modFolder, DataDirectory);
        string markerPath = Path.Combine(modFolder, MarkerFile);
        if (Directory.Exists(dataPath) && IsMarkerCurrent(markerPath, legacyArchive))
            return modFolder;

        return TryExtract(workshopId, legacyArchive, modFolder) ? modFolder : modPath;
    }

    // Folder has a protected setter
    internal static void SetFolder(MyWorkshopItem item, string folder) =>
        FolderSetter.Invoke(item, [folder]);

    public static bool TryExtract(ulong workshopId, string legacyArchive, string targetFolder)
    {
        if (string.IsNullOrWhiteSpace(legacyArchive) || !File.Exists(legacyArchive))
            return false;
        if (string.IsNullOrWhiteSpace(targetFolder))
            return false;

        string tempFolder = Path.Combine(
            targetFolder,
            ".magnetar-legacy-extract-" + Guid.NewGuid().ToString("N")
        );
        try
        {
            Directory.CreateDirectory(targetFolder);
            Directory.CreateDirectory(tempFolder);

            ExtractZip(legacyArchive, tempFolder);
            CopyDirectory(tempFolder, targetFolder);
            File.WriteAllText(
                Path.Combine(targetFolder, MarkerFile),
                GetMarkerContent(legacyArchive)
            );

            string dataPath = Path.Combine(targetFolder, DataDirectory);
            if (!Directory.Exists(dataPath))
            {
                LogFile.Error(
                    "Expanded legacy workshop mod "
                        + workshopId
                        + " from "
                        + legacyArchive
                        + ", but no Data directory was produced."
                );
                return false;
            }

            LogFile.WriteLine(
                "Expanded legacy workshop mod "
                    + workshopId
                    + " from "
                    + legacyArchive
                    + " into "
                    + targetFolder
            );
            return true;
        }
        catch (Exception e)
        {
            LogFile.Error(
                "Failed expanding legacy workshop mod "
                    + workshopId
                    + " from "
                    + legacyArchive
                    + ": "
                    + e
            );
            return false;
        }
        finally
        {
            TryDeleteDirectory(tempFolder);
        }
    }

    private static bool IsMarkerCurrent(string markerPath, string legacyArchive)
    {
        if (!File.Exists(markerPath))
            return false;

        try
        {
            return File.ReadAllText(markerPath) == GetMarkerContent(legacyArchive);
        }
        catch
        {
            return false;
        }
    }

    private static string GetMarkerContent(string legacyArchive)
    {
        FileInfo file = new(legacyArchive);
        return file.FullName + "\n" + file.Length + "\n" + file.LastWriteTimeUtc.Ticks + "\n";
    }

    private static void ExtractZip(string zipFile, string targetFolder)
    {
        using FileStream stream = File.OpenRead(zipFile);
        using ZipArchive archive = new(stream, ZipArchiveMode.Read);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string relativePath = NormalizeEntryPath(entry.FullName);
            if (relativePath.Length == 0)
                continue;

            string targetPath = Path.GetFullPath(Path.Combine(targetFolder, relativePath));
            string targetRoot = TerminatePath(Path.GetFullPath(targetFolder));
            if (!targetPath.StartsWith(targetRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(
                    "Archive entry escapes target folder: " + entry.FullName
                );

            if (
                entry.FullName.EndsWith("/", StringComparison.Ordinal)
                || entry.FullName.EndsWith("\\", StringComparison.Ordinal)
                || string.IsNullOrEmpty(entry.Name)
            )
            {
                Directory.CreateDirectory(targetPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
            using Stream input = entry.Open();
            using FileStream output = new(
                targetPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None
            );
            input.CopyTo(output);
        }
    }

    private static string NormalizeEntryPath(string entryName)
    {
        string normalized = entryName.Replace('\\', '/');
        if (
            string.IsNullOrWhiteSpace(normalized)
            || normalized.StartsWith("/", StringComparison.Ordinal)
        )
            throw new InvalidDataException("Invalid archive entry path: " + entryName);

        List<string> segments = [];
        foreach (string segment in normalized.Split('/'))
        {
            if (segment.Length == 0 || segment == ".")
                continue;
            if (segment == ".." || segment.IndexOf(':') >= 0)
                throw new InvalidDataException("Invalid archive entry path: " + entryName);

            segments.Add(segment);
        }

        return segments.Count == 0 ? string.Empty : Path.Combine(segments.ToArray());
    }

    private static void CopyDirectory(string sourceFolder, string targetFolder)
    {
        string sourceRoot = TerminatePath(Path.GetFullPath(sourceFolder));

        foreach (
            string directory in Directory.EnumerateDirectories(
                sourceFolder,
                "*",
                SearchOption.AllDirectories
            )
        )
        {
            string relativePath = directory.Substring(sourceRoot.Length);
            Directory.CreateDirectory(Path.Combine(targetFolder, relativePath));
        }

        foreach (
            string file in Directory.EnumerateFiles(sourceFolder, "*", SearchOption.AllDirectories)
        )
        {
            string relativePath = file.Substring(sourceRoot.Length);
            string targetPath = Path.Combine(targetFolder, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
            File.Copy(file, targetPath, overwrite: true);
        }
    }

    private static string TerminatePath(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            return path;

        return path + Path.DirectorySeparatorChar;
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
        catch { }
    }
}
