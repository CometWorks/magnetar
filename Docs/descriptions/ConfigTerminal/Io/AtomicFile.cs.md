# ConfigTerminal/Io/AtomicFile.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Io` · **Kind:** sealed class · **Lines:** 83

## Summary
Crash-safe text file writer: content is written to a temp file in the same directory, flushed to disk, then atomically renamed over the target, so the destination is never observed half-written or truncated. On any failure the temp file is removed and the target is left untouched. Before the first overwrite of an existing file in this process a `.bak` copy is made (Magnetar's existing backup convention), once per path per session rather than on every save.

## Types
### AtomicFile — sealed class, internal
Instance holds the set of paths already backed up this session.

- **Fields:**
  - `Utf8NoBom` (static `UTF8Encoding`) — UTF-8 without BOM, used for all writes.
  - `backedUp` (`HashSet<string>`) — paths backed up at least once this session, keyed by `PlatformPaths.PathComparer` so backup happens once per file.
- **Methods:**
  - `WriteText(path, content)` — ensures the target directory exists, calls `BackupOnce`, writes `content` to a hidden GUID-named `.tmp` in the same directory via `FileStream` (`CreateNew`, `FileShare.None`) + `StreamWriter` with `Utf8NoBom`, flushes the stream to disk (`stream.Flush(true)`), then `File.Move(..., overwrite: true)` (atomic on the same filesystem). On any exception it deletes the temp and rethrows.
  - `BackupOnce(path)` (private) — first time a path is seen this session (`backedUp.Add`), copies an existing target to `path + ".bak"`; a failed backup is swallowed so it can't block the real write (the atomic rename is what protects integrity).
  - `TryDelete(path)` (static, private) — best-effort delete of the temp file, swallowing exceptions.

## Cross-references
- **Uses:** `PlatformPaths.PathComparer` (this module); `System.IO` (`FileStream`, `StreamWriter`, `File.Move`/`Copy`, `Path`, `Directory`); `System.Text.UTF8Encoding`; `System.Guid`.
- **Used by:** [ConfigDocumentBase.cs](../Model/ConfigDocumentBase.cs.md), [EditSession.cs](../Model/EditSession.cs.md), [LastSessionFile.cs](../Model/LastSessionFile.cs.md), [MagnetarPlugins.cs](../Model/MagnetarPlugins.cs.md), [PluginProfileDocument.cs](../Model/PluginProfileDocument.cs.md), [PluginSourcesDocument.cs](../Model/PluginSourcesDocument.cs.md), [ProfileCatalog.cs](../Model/ProfileCatalog.cs.md), [ToolSettings.cs](../State/ToolSettings.cs.md), [AccessListView.cs](../Ui/AccessListView.cs.md), [AppShell.cs](../Ui/AppShell.cs.md), [HubPluginsView.cs](../Ui/HubPluginsView.cs.md), [ModListView.cs](../Ui/ModListView.cs.md), [OptionFormView.cs](../Ui/OptionFormView.cs.md), [PasswordDialog.cs](../Ui/PasswordDialog.cs.md), [PluginSourcesView.cs](../Ui/PluginSourcesView.cs.md), [PluginsView.cs](../Ui/PluginsView.cs.md), [ProfilesView.cs](../Ui/ProfilesView.cs.md), [DocumentTests.cs](../../ConfigTerminalTests/DocumentTests.cs.md), [LiveEndToEndTests.cs](../../ConfigTerminalTests/LiveEndToEndTests.cs.md), [PluginConfigTests.cs](../../ConfigTerminalTests/PluginConfigTests.cs.md), [PluginInteropTests.cs](../../ConfigTerminalTests/PluginInteropTests.cs.md), [ProcessAndFileTests.cs](../../ConfigTerminalTests/ProcessAndFileTests.cs.md), [ProfileCatalogTests.cs](../../ConfigTerminalTests/ProfileCatalogTests.cs.md)
