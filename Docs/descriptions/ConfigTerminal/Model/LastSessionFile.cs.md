# ConfigTerminal/Model/LastSessionFile.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** sealed class · **Lines:** 111

## Summary
Read/write model for `Saves/LastSession.sbl` (`MyObjectBuilder_LastSession`), which selects the world the DS loads next. Despite the extension it is plain uncompressed XML; the DS checks `RelativePath` first (keeps saves portable), then `Path`.

## Types
### LastSessionFile — sealed class, internal
- **Fields:** `Path`, `RelativePath`, `GameName` (strings); `IsContentWorlds`/`IsOnline`/`IsLobby` (bool); `ServerPort` (int — written as 0 like a fresh DS file, preserved on read).
- **Methods:**
  - `PathFor(string savesPath)` (static) — `Saves/LastSession.sbl`.
  - `Read(string sblPath)` (static) — returns null when absent or unreadable; otherwise parses `Path`/`RelativePath`/`GameName`/flags/`ServerPort` (bools via `ConfigDocumentBase.ParseBool`).
  - `ForWorld(WorldInfo world, string savesPath)` (static) — builds a LastSession pointing at a world, computing `RelativePath` when the world lives under `Saves/`, defaulting `GameName` to the session or folder name.
  - `Write(AtomicFile writer, string sblPath)` — serializes `<MyObjectBuilder_LastSession>` (with xsi/xsd namespaces) via `XmlOut.ToXmlString`; omits `<RelativePath>` when empty.
  - `TryGetRelativePath(savesPath, worldPath)` (private static) — returns the relative path only when the world is genuinely under `Saves/` (not `..`-rooted / absolute), else null.

## Cross-references
- **Uses:** `ConfigDocumentBase`/`WorldInfo` (this module); `AtomicFile`/`XmlOut`/`PlatformPaths` (`ConfigTerminal/Io/`); `System.Xml.Linq`, `System.IO`.
- **Used by:** [DsInstance.cs](DsInstance.cs.md), [NewWorldWizard.cs](../Ui/NewWorldWizard.cs.md), [WorldsView.cs](../Ui/WorldsView.cs.md), [LiveEndToEndTests.cs](../../ConfigTerminalTests/LiveEndToEndTests.cs.md), [ProcessAndFileTests.cs](../../ConfigTerminalTests/ProcessAndFileTests.cs.md)
