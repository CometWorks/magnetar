namespace Magnetar.ConfigTerminal.Ui;

internal static class HelpDialog
{
    private const string Text =
        "MagnetarConfig — configure and operate one Magnetar-managed\n" +
        "Space Engineers 1 Dedicated Server instance.\n\n" +
        "Keys:\n" +
        "  F1  Help        F3  Worlds       F4  Logs\n" +
        "  F5  Start/Stop  F7  Settings     F8  Plugins\n" +
        "  F10 Quit        (F6 activates the selected world in Worlds)\n\n" +
        "Changes save automatically — there is no Save key.\n\n" +
        "Files edited (in place, atomically, with .bak backups):\n" +
        "  • SpaceEngineers-Dedicated.cfg  (global config)\n" +
        "  • Saves/<world>/Sandbox_config.sbc  (per-world settings + mods)\n" +
        "  • Saves/LastSession.sbl  (which world loads next)\n" +
        "  • Profiles/*.xml, Sources/sources.xml  (plugins + sources)\n\n" +
        "New worlds are created by copying a DS template folder into\n" +
        "Saves/ and stamping the name into its Sandbox_config.sbc — the\n" +
        "world is ready immediately, no server start required.";

    // Left-aligned body: the text carries bullet lists and key tables, both of
    // which MessageBox's per-line centering would mangle.
    public static void Show() => Dialogs.QueryDetails("About MagnetarConfig", null, Text, error: false, "Close");
}
