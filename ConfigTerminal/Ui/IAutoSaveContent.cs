using System.Collections.Generic;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// A hosted panel that persists its edits automatically. The shell drives it on
/// a ~1s tick, on panel switch, and on quit, so there is no explicit save step.
/// </summary>
internal interface IAutoSaveContent
{
    /// <summary>
    /// Persists pending valid changes if the panel is dirty; a no-op when clean.
    /// Called on the ~1s tick, on panel switch (before dispose), and on quit.
    /// Must never block or pop a dialog.
    /// </summary>
    void FlushPendingSave();

    /// <summary>
    /// Labels of fields currently holding invalid values that were therefore not
    /// saved; empty when everything is valid. Used to warn on leaving the panel.
    /// </summary>
    IReadOnlyList<string> InvalidFields { get; }
}
