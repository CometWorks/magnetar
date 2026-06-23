# Shared/Data/LegacyWorkshopArchive.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Data` · **Kind:** static helper · **Lines:** 212

## Summary
`LegacyWorkshopArchive` locates and expands early Space Engineers Workshop packages that Steam stores as a single `*_legacy.bin` ZIP archive instead of loose mod files. It extracts into the existing Workshop item folder so downstream DS code can read the normal `Data/` tree.

## Types
### LegacyWorkshopArchive — static class, public
Shared helper for repairing legacy Workshop mod folders.

- **Constants:** `LegacyArchivePattern` (`*_legacy.bin`), `DataDirectory` (`Data`), and `MarkerFile` (`.magnetar-legacy-extract`).
- **Methods:**
  - `FindLegacyArchive(modFolder)` — returns the newest top-level `*_legacy.bin` file in a mod folder, or null.
  - `TryRepair(workshopId, modFolder)` — checks whether a folder is already usable, finds a legacy archive when `Data/` is absent or stale, and extracts it when needed.
  - `TryExtract(workshopId, legacyArchive, targetFolder)` — extracts to a unique temp folder, copies files into the target folder, writes a marker keyed by archive path/size/timestamp, logs success or failure, and returns whether `Data/` exists afterward.
  - `IsMarkerCurrent(markerPath, legacyArchive)` / `GetMarkerContent(legacyArchive)` — avoid repeated extraction while the archive is unchanged.
  - `ExtractZip(zipFile, targetFolder)` — opens the `.bin` as a ZIP archive and extracts entries after path validation.
  - `NormalizeEntryPath(entryName)` — rejects absolute, parent-traversal, and drive-colon paths to prevent zip-slip writes.
  - `CopyDirectory(sourceFolder, targetFolder)` — overlays extracted files into the Workshop item folder.
  - `TerminatePath(path)` and `TryDeleteDirectory(directory)` — path/callback helpers.

## Cross-references
- **Uses:** `LogFile` (Shared/LogFile.cs); BCL `System.IO`, `System.IO.Compression`, `System.Linq`.
- **Used by:** [ModPlugin.cs](ModPlugin.cs.md), [SteamMods.cs](../../Legacy/Loader/SteamMods.cs.md)
