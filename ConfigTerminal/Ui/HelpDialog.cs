namespace Magnetar.ConfigTerminal.Ui;

internal static class HelpDialog
{
    private const string Text =
        "MagnetarConfig — configure and operate one Magnetar-managed\n" +
        "Space Engineers 1 Dedicated Server instance.\n\n" +
        "Keys:\n" +
        "  F1  Help        F3  Worlds       F4  Logs\n" +
        "  F5  Start/Stop  F7  Settings     F10 Quit\n" +
        "  F2  Save the current document\n\n" +
        "Files edited (in place, atomically, with .bak backups):\n" +
        "  • SpaceEngineers-Dedicated.cfg  (global config)\n" +
        "  • Saves/<world>/Sandbox_config.sbc  (per-world settings + mods)\n" +
        "  • Saves/LastSession.sbl  (which world loads next)\n\n" +
        "New worlds are created the DS-faithful way: a template is staged\n" +
        "(PremadeCheckpointPath + SessionSettings) and the DS materializes\n" +
        "the world on a -ignorelastsession start, then reaches 'Game ready'.";

    // Left-aligned body: the text carries bullet lists and key tables, both of
    // which MessageBox's per-line centering would mangle.
    public static void Show() => Dialogs.QueryDetails("About MagnetarConfig", null, Text, error: false, "Close");
}
