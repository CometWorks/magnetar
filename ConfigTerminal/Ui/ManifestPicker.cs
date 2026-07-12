using System;
using System.IO;
using System.Reflection;
using NStack;
using Terminal.Gui;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// Quasar-style dev-folder picker: browse the filesystem and select a plugin's
/// <c>.xml</c> manifest. Opens at the last-visited folder so adding several
/// plugins in a row is frictionless.
/// </summary>
internal static class ManifestPicker
{
    /// <summary>Returns the picked manifest path, or null on cancel.</summary>
    public static string Pick(string initialFolder)
    {
        var dlg = new OpenDialog("Add dev-folder plugin", "Select the plugin's .xml manifest file")
        {
            CanChooseFiles = true,
            CanChooseDirectories = false,
            AllowsMultipleSelection = false,
            AllowedFileTypes = new[] { ".xml" },
            ColorScheme = TurboVisionTheme.Dialog,
        };

        string start = !string.IsNullOrEmpty(initialFolder) && Directory.Exists(initialFolder)
            ? initialFolder
            : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        dlg.DirectoryPath = ustring.Make(start);

        QuietDirectoryWatcher(dlg);

        Application.Run(dlg);

        if (dlg.Canceled || dlg.FilePaths == null || dlg.FilePaths.Count == 0)
            return null;

        string picked = dlg.FilePaths[0];
        return string.IsNullOrEmpty(picked) ? null : picked;
    }

    /// <summary>
    /// Silences the <see cref="FileSystemWatcher"/> that Terminal.Gui's file
    /// dialog attaches to the browsed folder. That watcher uses an extremely
    /// broad <c>NotifyFilter</c> (LastAccess, LastWrite, Size, Attributes…), so
    /// in an actively-built dev/plugin folder it fires constantly; each event
    /// triggers <c>DirListView.Reload()</c>, which snaps the selection back to
    /// the top entry — the "cursor jumps every second" symptom.
    ///
    /// Terminal.Gui 1.19.0 exposes no public knob for this, so we reach in via
    /// reflection and disable the watcher. <c>Reload()</c> builds a fresh, enabled
    /// watcher on every folder change, so we re-silence it from the public
    /// <c>DirectoryChanged</c> hook (invoked after the new watcher exists).
    /// Best-effort: if the pinned internals ever change shape we fall back to
    /// stock behavior rather than crash the picker.
    /// </summary>
    private static void QuietDirectoryWatcher(OpenDialog dlg)
    {
        try
        {
            View dirList = FindView(dlg, "DirListView");
            if (dirList == null)
                return;

            Type type = dirList.GetType();
            FieldInfo watcherField = type.GetField("watcher", BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo directoryChanged = type.GetProperty("DirectoryChanged", BindingFlags.Instance | BindingFlags.Public);
            if (watcherField == null || directoryChanged == null)
                return;

            void Silence()
            {
                if (watcherField.GetValue(dirList) is FileSystemWatcher watcher)
                    watcher.EnableRaisingEvents = false;
            }

            // Chain, don't replace: the dialog's own handler keeps the path/name
            // fields in sync, and ours quiets the freshly-created watcher after.
            var previous = (Action<ustring>)directoryChanged.GetValue(dirList);
            directoryChanged.SetValue(dirList, (Action<ustring>)(dir =>
            {
                previous?.Invoke(dir);
                Silence();
            }));

            Silence();
        }
        catch
        {
            // Reflection is best-effort against Terminal.Gui internals.
        }
    }

    private static View FindView(View root, string typeName)
    {
        if (root.GetType().Name == typeName)
            return root;

        foreach (View child in root.Subviews)
        {
            View found = FindView(child, typeName);
            if (found != null)
                return found;
        }

        return null;
    }
}
