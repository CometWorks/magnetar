# ConfigTerminalTests/DocumentTests.cs

**Project:** ConfigTerminalTests · **Namespace:** `Magnetar.ConfigTerminal.Tests` · **Kind:** class · **Lines:** 123

## Summary
xUnit tests for the config-document round-trip layer — `DedicatedConfigDocument` and `WorldConfigDocument` — proving edits are surgical and format-faithful. It verifies unknown XML elements survive an upsert, enum values normalize to their DS XML name, unsetting an option removes the element so the registry default applies, and administrators/passwords/mods serialize in the exact shapes the dedicated server reads. Uses a per-test temp directory (`IDisposable` cleanup).

## Types
### DocumentTests — class, public, implements `IDisposable`
Each test opens a document in a fresh temp dir and saves through an `AtomicFile`.

- **Fields:** `dir` — unique temp directory created in the ctor, recursively deleted in `Dispose`.
- **Methods:**
  - `Upsert_preserves_unknown_elements()` — editing `Dedicated.ServerName` keeps a `FutureUnknownField` element and its text, writes `<ServerName>new</ServerName>`, and produces a `.bak` backup.
  - `Enum_value_is_normalized_to_xml_name()` — `Set` of `Session.OnlineMode` accepts either a label ("Public") or an int ("1") and both `Get` back as `PUBLIC`.
  - `Unset_removes_the_element_so_the_default_applies()` — after `Unset`, `IsSet` is false and `Get` returns the registry `Default`.
  - `Administrators_serialize_as_unsignedLong_items()` — `SetAdministrators` emits `<Administrators>` with `<unsignedLong>` items and reads back via the `Administrators` property.
  - `Password_writes_hash_and_salt_and_can_clear()` — `SetPassword` writes `ServerPasswordHash`/`ServerPasswordSalt` and sets `HasPassword`; `SetPassword(null)` clears it.
  - `Mods_round_trip_in_load_order_with_sbm_name()` — `WorldConfigDocument.WriteMods`/`ReadMods` preserve load order, emit `<Name>{id}.sbm</Name>` and `FriendlyName`, and round-trip `PublishedFileId` and `IsDependency`.

## Cross-references
- **Uses:** `DedicatedConfigDocument`, `WorldConfigDocument`, `OptionRegistry`, `OptionDefinition`, `ModList`, `ModItem` (`ConfigTerminal/Model/`); `AtomicFile` (`ConfigTerminal/Io/`); xUnit; `System.IO`.
- **Used by:** _none within the repository_
