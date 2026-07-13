# Module: ConfigTerminal.Model

**Project:** `ConfigTerminal` · **Files:** 24 · **Source lines:** 3862

## Purpose

The pure data layer of the MagnetarConfig TUI, with no Terminal.Gui dependency. It holds the declarative option registry (hand-transcribed from the decompiled DS config types), XDocument upsert wrappers for the DS config files (SpaceEngineers-Dedicated.cfg, world Sandbox_config.sbc, LastSession.sbl), world/template/mod models with a template-copy world creator, edit-session dirty tracking and validation, and the plugin profile/source/hub-catalog/workshop-resolver models. Every file writer preserves unknown elements and hand edits so the tool coexists with the DS's own saves and newer game versions.

## Role in Magnetar

This module is consumed by the ConfigTerminal UI layer (Terminal.Gui windows) to read, validate and save an instance's dedicated-server, world and Magnetar plugin configuration entirely from outside the running game. It mirrors the on-disk formats and semantics of the Space Engineers DS and of Magnetar (Pulsar.Shared) — password hashing, LastSession precedence, profile/source schemas, protobuf-net hub caches — without referencing those runtime assemblies, and delegates atomic/backup file writes to the ConfigTerminal.Io layer (AtomicFile, XmlOut, PlatformPaths).

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `OptionDefinition` | record | [`ConfigTerminal/Model/OptionModel.cs`](../descriptions/ConfigTerminal/Model/OptionModel.cs.md) | Declarative metadata for one config option driving the editor, serialization, validation and liveness. |
| `OptionRegistry` | static class | [`ConfigTerminal/Model/OptionRegistry.cs`](../descriptions/ConfigTerminal/Model/OptionRegistry.cs.md) | Single source of truth for every editable dedicated-root and session-settings option. |
| `ConfigDocumentBase` | class | [`ConfigTerminal/Model/ConfigDocumentBase.cs`](../descriptions/ConfigTerminal/Model/ConfigDocumentBase.cs.md) | Abstract XDocument base with per-element upsert get/set/unset and tolerant scalar parsing. |
| `DedicatedConfigDocument` | class | [`ConfigTerminal/Model/DedicatedConfigDocument.cs`](../descriptions/ConfigTerminal/Model/DedicatedConfigDocument.cs.md) | Wrapper for SpaceEngineers-Dedicated.cfg with typed accessors for access lists, password and world selection. |
| `WorldConfigDocument` | class | [`ConfigTerminal/Model/WorldConfigDocument.cs`](../descriptions/ConfigTerminal/Model/WorldConfigDocument.cs.md) | Wrapper for a world's Sandbox_config.sbc: session settings and the per-world mod list. |
| `EditSession` | class | [`ConfigTerminal/Model/EditSession.cs`](../descriptions/ConfigTerminal/Model/EditSession.cs.md) | Content-snapshot dirty tracking plus validation and atomic save for one open document. |
| `DsInstance` | class | [`ConfigTerminal/Model/DsInstance.cs`](../descriptions/ConfigTerminal/Model/DsInstance.cs.md) | Aggregate root binding cfg, worlds, templates and last-session, resolving the active world non-throwingly. |
| `WorldCatalog` | class | [`ConfigTerminal/Model/WorldCatalog.cs`](../descriptions/ConfigTerminal/Model/WorldCatalog.cs.md) | Enumerates worlds under Saves/ into WorldInfo display metadata. |
| `WorldCreator` | static class | [`ConfigTerminal/Model/WorldCreator.cs`](../descriptions/ConfigTerminal/Model/WorldCreator.cs.md) | Creates a world by staging a copy of a DS template and stamping its Sandbox_config.sbc. |
| `MagnetarPlugins` | class | [`ConfigTerminal/Model/MagnetarPlugins.cs`](../descriptions/ConfigTerminal/Model/MagnetarPlugins.cs.md) | Facade over Magnetar's plugin profile and sources: local DLLs, dev folders, hub catalog and sources. |
| `PluginProfileDocument` | class | [`ConfigTerminal/Model/PluginProfileDocument.cs`](../descriptions/ConfigTerminal/Model/PluginProfileDocument.cs.md) | Wrapper for a Magnetar plugin profile editing the enabled-set collections. |
| `PluginSourcesDocument` | class | [`ConfigTerminal/Model/PluginSourcesDocument.cs`](../descriptions/ConfigTerminal/Model/PluginSourcesDocument.cs.md) | Wrapper for Sources/sources.xml editing dev-folder, remote-hub, remote-plugin and local-hub sources. |
| `ProfileCatalog` | class | [`ConfigTerminal/Model/ProfileCatalog.cs`](../descriptions/ConfigTerminal/Model/ProfileCatalog.cs.md) | Manages named plugin profile presets against the active Current.xml. |
| `HubCatalog` | static class | [`ConfigTerminal/Model/HubCatalog.cs`](../descriptions/ConfigTerminal/Model/HubCatalog.cs.md) | Parses Magnetar's cached protobuf-net plugin-catalog .bin blobs offline via ProtoReader. |
| `WorkshopResolver` | class | [`ConfigTerminal/Model/WorkshopResolver.cs`](../descriptions/ConfigTerminal/Model/WorkshopResolver.cs.md) | Resolves Steam Workshop mod names and expands collections via keyless ISteamRemoteStorage endpoints. |
| `MiniJson` | static class | [`ConfigTerminal/Model/Json/MiniJson.cs`](../descriptions/ConfigTerminal/Model/Json/MiniJson.cs.md) | Dependency-free JSON reader producing a lenient JsonValue tree for the workshop resolver. |
| `PasswordHasher` | static class | [`ConfigTerminal/Model/PasswordHasher.cs`](../descriptions/ConfigTerminal/Model/PasswordHasher.cs.md) | DS-identical PBKDF2-SHA1 server-password hashing. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`ConfigTerminal/Model/CheckpointReader.cs`](../descriptions/ConfigTerminal/Model/CheckpointReader.cs.md) | 76 | Reads only the handful of header fields needed for display from a `Sandbox.sbc` checkpoint, which may be GZip-compressed. |
| [`ConfigTerminal/Model/ConfigDocumentBase.cs`](../descriptions/ConfigTerminal/Model/ConfigDocumentBase.cs.md) | 99 | Base for the `XDocument`-backed DS config wrappers, implementing per-element upsert editing: unknown elements, comments and the ordering of untouched elements are preserved so the tool coexists with hand edits, the DS's own saves and newer game versions. |
| [`ConfigTerminal/Model/DedicatedConfigDocument.cs`](../descriptions/ConfigTerminal/Model/DedicatedConfigDocument.cs.md) | 176 | `XDocument` wrapper for `SpaceEngineers-Dedicated.cfg` (root `MyConfigDedicated`). |
| [`ConfigTerminal/Model/DefaultHttpFetcher.cs`](../descriptions/ConfigTerminal/Model/DefaultHttpFetcher.cs.md) | 40 | The live HTTP transport for `WorkshopResolver`: a plain `HttpClient` with a short (15s) timeout and a friendly user agent. |
| [`ConfigTerminal/Model/DsInstance.cs`](../descriptions/ConfigTerminal/Model/DsInstance.cs.md) | 121 | The aggregate root that binds a DS instance's cfg, worlds, templates and last-session together for the session. |
| [`ConfigTerminal/Model/EditSession.cs`](../descriptions/ConfigTerminal/Model/EditSession.cs.md) | 190 | Dirty-tracking plus validation for one open config document. |
| [`ConfigTerminal/Model/HubCatalog.cs`](../descriptions/ConfigTerminal/Model/HubCatalog.cs.md) | 179 | Reads Magnetar's cached plugin catalogs — the protobuf-net blobs Magnetar downloads into `Sources/Hubs/*.bin` (a `PluginData[]`) and `Sources/Plugins/*.bin` (a single-element `PluginData[]`). |
| [`ConfigTerminal/Model/Json/MiniJson.cs`](../descriptions/ConfigTerminal/Model/Json/MiniJson.cs.md) | 223 | A tiny, self-contained JSON reader — enough to parse the Steam Web API responses the Workshop resolver consumes, with zero third-party dependencies. |
| [`ConfigTerminal/Model/LastSessionFile.cs`](../descriptions/ConfigTerminal/Model/LastSessionFile.cs.md) | 111 | Read/write model for `Saves/LastSession.sbl` (`MyObjectBuilder_LastSession`), which selects the world the DS loads next. |
| [`ConfigTerminal/Model/MagnetarPlugins.cs`](../descriptions/ConfigTerminal/Model/MagnetarPlugins.cs.md) | 399 | Facade over Magnetar's plugin config for one instance: the active profile (the enabled set) and the dev-folder sources, joined into UI-ready view rows. |
| [`ConfigTerminal/Model/ModList.cs`](../descriptions/ConfigTerminal/Model/ModList.cs.md) | 50 | The per-world mod list model: a `ModItem` value type for one workshop mod and an ordered `ModList` with reorder and validation. |
| [`ConfigTerminal/Model/OptionModel.cs`](../descriptions/ConfigTerminal/Model/OptionModel.cs.md) | 82 | Declares the small value types that describe one editable config option: the scope/kind/liveness enums, an `EnumChoice` record for one enum member, and the `OptionDefinition` record that is the declarative metadata driving the editor UI, serialization, validation and liveness hints. |
| [`ConfigTerminal/Model/OptionRegistry.cs`](../descriptions/ConfigTerminal/Model/OptionRegistry.cs.md) | 431 | The single declarative source of truth for every editable DS config option, hand-transcribed from the decompiled `MyConfigDedicatedData` and `MyObjectBuilder_SessionSettings` (build 1.209.024) and cross-checked against Quasar's metadata. |
| [`ConfigTerminal/Model/PasswordHasher.cs`](../descriptions/ConfigTerminal/Model/PasswordHasher.cs.md) | 50 | Reproduces the DS server-password hashing exactly, so a password set by this tool actually admits players: PBKDF2 (SHA1), 16-byte random salt, 10000 iterations, 20-byte derived key, both stored base64 as `ServerPasswordHash` / `ServerPasswordSalt`. |
| [`ConfigTerminal/Model/PluginManifest.cs`](../descriptions/ConfigTerminal/Model/PluginManifest.cs.md) | 92 | Reads the display metadata a dev-folder plugin declares in its manifest XML — a `GitHubPlugin` serialized as `PluginData` (namespace `Pulsar.Shared.Data`). |
| [`ConfigTerminal/Model/PluginProfileDocument.cs`](../descriptions/ConfigTerminal/Model/PluginProfileDocument.cs.md) | 249 | `XDocument` wrapper for a Magnetar plugin profile (`Profiles/<key>.xml`, root `Profile`), with `Current.xml` the active set the server loads. |
| [`ConfigTerminal/Model/PluginSourcesDocument.cs`](../descriptions/ConfigTerminal/Model/PluginSourcesDocument.cs.md) | 299 | `XDocument` wrapper for `Sources/sources.xml` (root `SourcesConfig`), the registry of plugin catalog sources. |
| [`ConfigTerminal/Model/ProfileCatalog.cs`](../descriptions/ConfigTerminal/Model/ProfileCatalog.cs.md) | 147 | Manages the instance's plugin *profiles* — named presets of enabled plugins stored as `Profiles/<Key>.xml`, with `Current.xml` the active set the server loads. |
| [`ConfigTerminal/Model/ProtoReader.cs`](../descriptions/ConfigTerminal/Model/ProtoReader.cs.md) | 117 | A tiny, forward-only reader for the Protocol Buffers wire format — just enough to walk Magnetar's protobuf-net hub-catalog cache (`Sources/Hubs/*.bin`, `Sources/Plugins/*.bin`) by field number, without referencing `Shared`/protobuf-net or loading any Magnetar type. |
| [`ConfigTerminal/Model/WorkshopResolver.cs`](../descriptions/ConfigTerminal/Model/WorkshopResolver.cs.md) | 259 | Looks up Steam Workshop mod metadata (friendly names, collection members) so the per-world mod-list editor can accept a Workshop URL or id and fill the name in automatically. |
| [`ConfigTerminal/Model/WorldCatalog.cs`](../descriptions/ConfigTerminal/Model/WorldCatalog.cs.md) | 104 | Enumerates the worlds under a `Saves/` directory, building `WorldInfo` display metadata for each folder that holds a checkpoint and/or world config, sorted by last-save time descending. |
| [`ConfigTerminal/Model/WorldConfigDocument.cs`](../descriptions/ConfigTerminal/Model/WorldConfigDocument.cs.md) | 166 | `XDocument` wrapper for a world's `Sandbox_config.sbc` (`MyObjectBuilder_WorldConfiguration`). |
| [`ConfigTerminal/Model/WorldCreator.cs`](../descriptions/ConfigTerminal/Model/WorldCreator.cs.md) | 88 | Creates a new world by copying a DS world template (`Content/CustomWorlds/…`) into `Saves/` and stamping the chosen name into its `Sandbox_config.sbc` — no server start required. |
| [`ConfigTerminal/Model/WorldTemplateCatalog.cs`](../descriptions/ConfigTerminal/Model/WorldTemplateCatalog.cs.md) | 114 | Enumerates the world templates the DS ships under `<ContentPath>/CustomWorlds/`, where ContentPath is the `Content/` folder sibling to `DedicatedServer64/`. |

## Public API surface

- `OptionRegistry.DedicatedOptions / SessionOptions / All / ById(id) / Categories(scope)`
- `DedicatedConfigDocument.Open(filePath) / SetPassword(plaintext) / SetAdministrators/SetBanned/SetReserved`
- `WorldConfigDocument.Open(filePath) / ReadMods() / WriteMods(list) / RefreshLastSaveTime()`
- `ConfigDocumentBase.Get/Set/Unset/IsSet(def) / Save(writer)`
- `EditSession.IsDirty / Validate() / Save(writer) / Rebase()`
- `DsInstance.Open(binding) / Reload() / ActiveWorld`
- `WorldCatalog.Scan() / WorldCreator.CreateFromTemplate(template, name, savesPath)`
- `LastSessionFile.Read(path) / ForWorld(world, savesPath) / Write(writer, path)`
- `MagnetarPlugins.LocalDlls() / HubCatalogPlugins() / SetHubPluginEnabled(id, on) / AddDevFolderFromManifest(path)`
- `ProfileCatalog.NamedProfiles() / SaveCurrentAs(name) / Load(key) / Rename(key, name) / Delete(key)`
- `WorkshopResolver.ExtractIds(text) / Resolve(ids)`
- `HubCatalog.ReadFile(binPath, label)`

## Dependencies

**Uses modules:** [ConfigTerminal.Io](ConfigTerminal.Io.md)  
**Used by modules:** [ConfigTerminal.App](ConfigTerminal.App.md), [ConfigTerminal.Io](ConfigTerminal.Io.md), [ConfigTerminal.Logs](ConfigTerminal.Logs.md), [ConfigTerminal.Process](ConfigTerminal.Process.md), [ConfigTerminal.Ui](ConfigTerminal.Ui.md), [ConfigTerminalTests](ConfigTerminalTests.md)  
**External systems:** Steam ISteamRemoteStorage Web API; System.IO.Compression; System.Net.Http; System.Security.Cryptography; System.Xml.Linq; protobuf-net wire format (read-only, no library)

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
