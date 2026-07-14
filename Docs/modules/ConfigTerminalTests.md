# Module: ConfigTerminalTests

**Project:** `ConfigTerminalTests` · **Files:** 11 · **Source lines:** 1766

## Purpose

xUnit test suite for the MagnetarConfig TUI (ConfigTerminal). It specifies and guards the invariants of the option registry, the surgical round-trip behavior of the config/profile/sources documents, the process/pid/atomic-file and world-creation layer, the protobuf hub-catalog reader, the Steam Workshop resolver, and a headless Terminal.Gui view-tree smoke test. Interop tests prove the tool's XML is accepted by Magnetar's own serializers, and a live end-to-end test drives the real create/start/ready/stop flow against an installed dedicated server.

## Role in Magnetar

This is the verification harness for the ConfigTerminal (MagnetarConfig) tool. Unit tests exercise ConfigTerminal.Model/Io/Process/Logs/Ui in isolation with temp directories and fakes; interop tests reflect against the deployed Magnetar.Shared.dll to confirm cross-compatibility with Pulsar.Shared's Profile/SourcesConfig/ProfilesConfig serializers; and live tests (gated by MAGNETAR_LIVE=1) plus Magnetar-install-gated interop tests validate the tool against a real Magnetar/DS deployment. It ships fixtures (a captured MagnetarHub protobuf catalog) and runs UI tests single-threaded via Terminal.Gui's FakeDriver.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `RegistryTests` | class | [`ConfigTerminalTests/RegistryTests.cs`](../descriptions/ConfigTerminalTests/RegistryTests.cs.md) | Guards OptionRegistry invariants: unique ids/xml-names, enum choices, preserved Keen typos, exact enum orderings. |
| `DocumentTests` | class | [`ConfigTerminalTests/DocumentTests.cs`](../descriptions/ConfigTerminalTests/DocumentTests.cs.md) | Config-document round-trips: unknown-element preservation, enum normalization, unset-to-default, admins/password/mods serialization. |
| `PluginConfigTests` | class | [`ConfigTerminalTests/PluginConfigTests.cs`](../descriptions/ConfigTerminalTests/PluginConfigTests.cs.md) | Plugin/profile/sources upsert and MagnetarPlugins facade: idempotent enable/disable, sibling preservation, hub/dev-folder registration. |
| `PluginInteropTests` | class | [`ConfigTerminalTests/PluginInteropTests.cs`](../descriptions/ConfigTerminalTests/PluginInteropTests.cs.md) | Reflection interop proving tool-written profiles/sources deserialize and Validate() through the deployed Magnetar.Shared.dll. |
| `ProcessAndFileTests` | class | [`ConfigTerminalTests/ProcessAndFileTests.cs`](../descriptions/ConfigTerminalTests/ProcessAndFileTests.cs.md) | Process/pid/atomic-file and world-creation behavior: PBKDF2 hash, LastSession, LaunchSpec args, PidFileReader states, WorldCreator. |
| `WorkshopResolverTests` | class | [`ConfigTerminalTests/WorkshopResolverTests.cs`](../descriptions/ConfigTerminalTests/WorkshopResolverTests.cs.md) | WorkshopResolver id extraction, JSON parsing, single-mod/collection resolution, warnings, via a canned IHttpFetcher fake. |
| `ProfileCatalogTests` | class | [`ConfigTerminalTests/ProfileCatalogTests.cs`](../descriptions/ConfigTerminalTests/ProfileCatalogTests.cs.md) | Named-profile management: key sanitization, save/load/update/rename/delete, active-match tracking, reserved Current handling. |
| `HubCatalogTests` | class | [`ConfigTerminalTests/HubCatalogTests.cs`](../descriptions/ConfigTerminalTests/HubCatalogTests.cs.md) | HubCatalog protobuf reader decoding a real MagnetarHub catalog fixture and empty/missing-input handling. |
| `UiSmokeTests` | class | [`ConfigTerminalTests/UiSmokeTests.cs`](../descriptions/ConfigTerminalTests/UiSmokeTests.cs.md) | Headless Terminal.Gui FakeDriver tests: the AppShell view-tree smoke test plus log-viewer coverage of highlighting, tail-on-open, search (with options), clear, and highlighted-line navigation. |
| `LiveEndToEndTests` | class | [`ConfigTerminalTests/LiveEndToEndTests.cs`](../descriptions/ConfigTerminalTests/LiveEndToEndTests.cs.md) | MAGNETAR_LIVE-gated real create/start/Game-ready/stop flow against an installed DS + patched launcher, restoring state after. |
| `LogHighlightTests` | class | [`ConfigTerminalTests/LogHighlightTests.cs`](../descriptions/ConfigTerminalTests/LogHighlightTests.cs.md) | Unit tests for LogHighlight.Classify: the two markers, None/null cases, Exception precedence, and case-sensitivity. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`ConfigTerminalTests/DocumentTests.cs`](../descriptions/ConfigTerminalTests/DocumentTests.cs.md) | 123 | xUnit tests for the config-document round-trip layer — `DedicatedConfigDocument` and `WorldConfigDocument` — proving edits are surgical and format-faithful. |
| [`ConfigTerminalTests/HubCatalogTests.cs`](../descriptions/ConfigTerminalTests/HubCatalogTests.cs.md) | 46 | xUnit tests for `HubCatalog`, the protobuf-net reader that decodes a MagnetarHub catalog cache (`PluginData[]`) into browsable `HubPluginInfo` rows. |
| [`ConfigTerminalTests/LiveEndToEndTests.cs`](../descriptions/ConfigTerminalTests/LiveEndToEndTests.cs.md) | 156 | Live end-to-end test of the real create → start → "Game ready" → stop flow against an installed dedicated server plus a patched Magnetar launcher, exercising the exact model/process code paths the New-World wizard drives. |
| [`ConfigTerminalTests/LogHighlightTests.cs`](../descriptions/ConfigTerminalTests/LogHighlightTests.cs.md) | 34 | Unit tests for `LogHighlight.Classify`, the log-viewer line classifier. |
| [`ConfigTerminalTests/PluginConfigTests.cs`](../descriptions/ConfigTerminalTests/PluginConfigTests.cs.md) | 254 | Comprehensive xUnit suite for the plugin/profile/sources model and the `MagnetarPlugins` facade, proving that enabling/disabling plugins is a surgical upsert that never clobbers unmanaged siblings. |
| [`ConfigTerminalTests/PluginInteropTests.cs`](../descriptions/ConfigTerminalTests/PluginInteropTests.cs.md) | 237 | Interop tests proving that profile/sources XML written by this tool is accepted by Magnetar's own serializers. |
| [`ConfigTerminalTests/ProcessAndFileTests.cs`](../descriptions/ConfigTerminalTests/ProcessAndFileTests.cs.md) | 187 | xUnit tests for the process/pid/atomic-file and world-creation layer. |
| [`ConfigTerminalTests/ProfileCatalogTests.cs`](../descriptions/ConfigTerminalTests/ProfileCatalogTests.cs.md) | 129 | xUnit tests for `ProfileCatalog`, which manages named plugin profiles derived from the active `Current` set. |
| [`ConfigTerminalTests/RegistryTests.cs`](../descriptions/ConfigTerminalTests/RegistryTests.cs.md) | 66 | xUnit tests asserting the structural invariants of `OptionRegistry` — the static table of dedicated/session config options the TUI edits. |
| [`ConfigTerminalTests/UiSmokeTests.cs`](../descriptions/ConfigTerminalTests/UiSmokeTests.cs.md) | 356 | Headless UI tests that build the `AppShell` view tree against Terminal.Gui's `FakeDriver` and pump a few main-loop iterations, catching constructor/layout exceptions without a real terminal — plus focused coverage of the log viewer's behaviour. |
| [`ConfigTerminalTests/WorkshopResolverTests.cs`](../descriptions/ConfigTerminalTests/WorkshopResolverTests.cs.md) | 178 | xUnit tests for `WorkshopResolver`, which turns Steam Workshop ids/URLs into mod names via the Steam Web API. |

## Public API surface

- `RegistryTests (option-registry invariants)`
- `DocumentTests (config-document round-trips)`
- `PluginConfigTests (profile/sources upsert + facade)`
- `PluginInteropTests (Magnetar.Shared serializer interop)`
- `ProcessAndFileTests (pid/atomic-file/world-creation)`
- `WorkshopResolverTests (Steam resolver via fake fetcher)`
- `ProfileCatalogTests (named-profile management)`
- `HubCatalogTests (protobuf catalog reader)`
- `UiSmokeTests (FakeDriver view-tree smoke)`
- `LiveEndToEndTests (MAGNETAR_LIVE end-to-end)`

## Dependencies

**Uses modules:** [ConfigTerminal.Io](ConfigTerminal.Io.md), [ConfigTerminal.Logs](ConfigTerminal.Logs.md), [ConfigTerminal.Model](ConfigTerminal.Model.md), [ConfigTerminal.Process](ConfigTerminal.Process.md), [ConfigTerminal.Ui](ConfigTerminal.Ui.md)  
**Used by modules:** _none_  
**External systems:** Steam Web API (live); System.Security.Cryptography; System.Xml.Serialization; Terminal.Gui (FakeDriver); deployed Magnetar.Shared.dll (Pulsar.Shared); live SE Dedicated Server + patched Magnetar launcher (live); xUnit

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
