# ConfigTerminal/Model/CheckpointReader.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** sealed class, static class · **Lines:** 76

## Summary
Reads only the handful of header fields needed for display from a `Sandbox.sbc` checkpoint, which may be GZip-compressed. It uses a forward-only `XmlReader` and stops early so it never materializes the (possibly huge) grid/entity data — a fallback for worlds missing `Sandbox_config.sbc`.

## Types
### CheckpointInfo — sealed class, internal
Display-only info pulled from a checkpoint without loading the entity tree.
- **Fields:** `SessionName` (string, defaults empty); `LastSaveTime` (`DateTime?`).
### CheckpointReader — static class, internal
- **Methods:**
  - `TryRead(string sandboxSbcPath)` — returns null when the file is absent or on any exception; otherwise reads elements forward-only, capturing `SessionName` and bailing out once a heavy section (`Settings`/`AppVersion`/`SectorEncounters`/`Factions`) is reached.
  - `Decompress(Stream raw)` (private static) — sniffs the GZip magic (`0x1F 0x8B`), rewinds, and wraps in a `GZipStream` when compressed, else returns the raw stream.

## Cross-references
- **Uses:** `System.IO`, `System.IO.Compression.GZipStream`, `System.Xml.XmlReader`.
- **Used by:** [WorldCatalog.cs](WorldCatalog.cs.md), [WorldConfigDocument.cs](WorldConfigDocument.cs.md), [WorldCreator.cs](WorldCreator.cs.md), [WorldTemplateCatalog.cs](WorldTemplateCatalog.cs.md)
