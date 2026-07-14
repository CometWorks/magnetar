namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// Quasar-style dev-folder picker: browse the filesystem and select a plugin's
/// <c>.xml</c> manifest. Opens at the last-visited folder so adding several
/// plugins in a row is frictionless.
/// </summary>
internal static class ManifestPicker
{
    /// <summary>Returns the picked manifest path, or null on cancel.</summary>
    public static string Pick(string initialFolder) =>
        FileDialogs.PickFile(
            "Add dev-folder plugin",
            "Select the plugin's .xml manifest file",
            initialFolder,
            new[] { ".xml" });
}
