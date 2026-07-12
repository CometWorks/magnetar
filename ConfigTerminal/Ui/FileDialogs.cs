using System;
using System.IO;
using System.Reflection;
using NStack;
using Terminal.Gui;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// Filesystem browse dialogs shared by the instance picker (path fields) and the
/// dev-folder manifest picker. Wraps Terminal.Gui's <see cref="OpenDialog"/> and
/// quiets its noisy folder watcher (see <see cref="QuietDirectoryWatcher"/>).
/// </summary>
internal static class FileDialogs
{
    /// <summary>Browse for a directory. Returns the picked path, or null on cancel.</summary>
    public static string PickDirectory(string title, string prompt, string initial)
    {
        var dlg = new OpenDialog(title, prompt)
        {
            CanChooseFiles = false,
            CanChooseDirectories = true,
            AllowsMultipleSelection = false,
            ColorScheme = TurboVisionTheme.Dialog,
        };
        return Run(dlg, initial);
    }

    /// <summary>Browse for a file. Returns the picked path, or null on cancel.</summary>
    public static string PickFile(string title, string prompt, string initial, string[] allowedTypes = null)
    {
        var dlg = new OpenDialog(title, prompt)
        {
            CanChooseFiles = true,
            CanChooseDirectories = false,
            AllowsMultipleSelection = false,
            ColorScheme = TurboVisionTheme.Dialog,
        };
        if (allowedTypes != null)
            dlg.AllowedFileTypes = allowedTypes;
        return Run(dlg, initial);
    }

    private static string Run(OpenDialog dlg, string initial)
    {
        Seed(dlg, initial);
        QuietDirectoryWatcher(dlg);
        Application.Run(dlg);

        if (dlg.Canceled || dlg.FilePaths == null || dlg.FilePaths.Count == 0)
            return null;

        string picked = dlg.FilePaths[0];
        return string.IsNullOrEmpty(picked) ? null : picked;
    }

    /// <summary>
    /// Points the dialog at the seed path. When the seed exists, open its *parent*
    /// with the seed itself pre-selected (<see cref="FileDialog.FilePath"/>) — the
    /// Open button is then active immediately and re-opening highlights the current
    /// value, instead of dropping the user inside the folder with nothing selected
    /// (which leaves Open disabled). Falls back to the parent, then home.
    /// </summary>
    private static void Seed(OpenDialog dlg, string initial)
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string full = null;
        try { if (!string.IsNullOrEmpty(initial)) full = Path.GetFullPath(initial); } catch { }
        if (string.IsNullOrEmpty(full))
        {
            dlg.DirectoryPath = ustring.Make(home);
            return;
        }

        bool exists = Directory.Exists(full) || File.Exists(full);
        string parent = null;
        try { parent = Path.GetDirectoryName(full.TrimEnd('/', '\\')); } catch { }
        bool hasParent = !string.IsNullOrEmpty(parent) && Directory.Exists(parent);

        if (exists && hasParent)
        {
            dlg.DirectoryPath = ustring.Make(parent);
            dlg.FilePath = ustring.Make(full);
        }
        else if (Directory.Exists(full))
        {
            dlg.DirectoryPath = ustring.Make(full);
        }
        else
        {
            dlg.DirectoryPath = ustring.Make(hasParent ? parent : home);
        }
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
    public static void QuietDirectoryWatcher(OpenDialog dlg)
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
