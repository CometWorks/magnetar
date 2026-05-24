using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
namespace Pulsar.Updater;

internal static class Writer
{
    private const string Magnetar = "Magnetar";
    private const int MaxFiles = 15;

    // Magnetar ships a single Linux launcher (Interim.dll/.exe), no Modern
    // or Legacy variants. "Modern" stays in Preserve only for backward
    // compatibility with old install layouts.
    private static readonly HashSet<string> Preserve = ["Legacy", "Interim", "Modern"];
    private static readonly HashSet<string> Check =
    [
        "Interim.dll",
        "LICENSE",
    ];

    public static void Update(ZipArchive source, string destination)
    {
        if (!Validate(destination))
            Environment.Exit(1);

        CleanFolder(destination, Preserve);
        source.ExtractToDirectory(destination);
    }

    private static void CleanFolder(string folder, HashSet<string> exclude)
    {
        string updater = Assembly.GetExecutingAssembly().Location;
        if (IsUpdaterFolder(updater, folder))
            exclude.Add(Path.GetFileName(updater));

        foreach (string file in Directory.EnumerateFiles(folder))
            if (!exclude.Contains(Path.GetFileName(file)))
                File.Delete(file);

        foreach (string dir in Directory.EnumerateDirectories(folder))
            if (!exclude.Contains(Path.GetFileName(dir)))
                Directory.Delete(dir, recursive: true);
    }

    private static bool IsUpdaterFolder(string updater, string folder)
    {
        string updaterFolder = Path.GetDirectoryName(updater);
        return Path.GetFullPath(updaterFolder) == Path.GetFullPath(folder);
    }

    private static bool Validate(string folder)
    {
        if (!Directory.Exists(folder))
            return false;

        folder = Path.GetFullPath(folder);

        // Honour XDG_DATA_HOME, fall back to ~/.local/share/Magnetar.
        // Matches the $(Magnetar) default in Directory.Build.props.
        string xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        string dataHome = string.IsNullOrWhiteSpace(xdg)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local", "share"
            )
            : xdg;
        string defaultPath = Path.Combine(dataHome, Magnetar);

        if (folder == Path.GetFullPath(defaultPath))
            return true;

        bool isPulsarInstall = Check.All(name => File.Exists(Path.Combine(folder, name)));
        bool hasOtherFiles = Directory.GetFiles(folder).Length > MaxFiles;
        if (isPulsarInstall && !hasOtherFiles)
            return true;

        return ContinuePrompt(folder);
    }

    private static bool ContinuePrompt(string folder)
    {
        // Headless server: there is no operator to confirm, so refuse to
        // clean a directory we don't recognise as a Magnetar install.
        string message =
            "The installation folder could not be validated and will NOT be touched:\n"
            + folder + "\n"
            + "Re-run the updater pointing at the actual Magnetar install directory.";

        Console.Error.WriteLine(
            $"[Magnetar Updater] {message}".Replace("\r\n", "\n").Replace("\n", Environment.NewLine)
        );
        return false;
    }
}
