using System;
using System.IO;
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

        Application.Run(dlg);

        if (dlg.Canceled || dlg.FilePaths == null || dlg.FilePaths.Count == 0)
            return null;

        string picked = dlg.FilePaths[0];
        return string.IsNullOrEmpty(picked) ? null : picked;
    }
}
