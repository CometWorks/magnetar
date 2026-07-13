# ConfigTerminalTests/HubCatalogTests.cs

**Project:** ConfigTerminalTests · **Namespace:** `Magnetar.ConfigTerminal.Tests` · **Kind:** class · **Lines:** 46

## Summary
xUnit tests for `HubCatalog`, the protobuf-net reader that decodes a MagnetarHub catalog cache (`PluginData[]`) into browsable `HubPluginInfo` rows. It parses a real captured `magnetar-hub.bin` fixture and verifies that a known GitHub plugin's fields decode correctly off the wire, plus that missing/empty/null inputs degrade to an empty list rather than throwing.

## Types
### HubCatalogTests — class, public
Fact suite loading the `Fixtures/magnetar-hub.bin` catalog blob from the test output directory.

- **Methods:**
  - `FixturePath(name)` (private static) — resolves `Fixtures/<name>` under `AppContext.BaseDirectory`.
  - `Parses_real_magnetar_hub_catalog()` — `HubCatalog.ReadFile(..., "MagnetarHub")` returns ≥5 plugins, each with non-empty `Id`/`FriendlyName`; the "Block Limits" entry decodes as `HubPluginKind.GitHub`, `RepoId="CometWorks/block-limits"`, `Author="OwendB"`, a tooltip containing "block limit", `SourceLabel="MagnetarHub"`, and a non-blank stable `Id`.
  - `Missing_file_returns_empty()` — `ReadFile` of a nonexistent path, `Parse(null)`, and `Parse(empty array)` all return empty.

## Cross-references
- **Uses:** `HubCatalog`, `HubPluginInfo`, `HubPluginKind` (`ConfigTerminal/Model/`); `Fixtures/magnetar-hub.bin` (captured protobuf-net blob); xUnit; `System.IO.Path`, `AppContext`.
- **Used by:** _none within the repository_
