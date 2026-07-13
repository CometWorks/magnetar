# ConfigTerminal/Ui/DesktopBackground.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** sealed class · **Lines:** 31

## Summary
The classic Turbo Vision blue desktop backdrop: a non-focusable `View` that fills its bounds with the `▒` shade glyph and sits behind all content windows.

## Types
### DesktopBackground — sealed class, internal (`View`)
Fills the toplevel with the desktop pattern.

- **Methods:**
  - `DesktopBackground()` — fills the parent, disables focus, and applies `TurboVisionTheme.Desktop`.
  - `Redraw(Rect)` — sets the desktop attribute and writes the `▒` rune across every cell of its bounds.

## Cross-references
- **Uses:** Terminal.Gui `View`/`Dim`/`Rect`/`Driver`; `TurboVisionTheme` (this module).
- **Used by:** [AppShell.cs](AppShell.cs.md)
