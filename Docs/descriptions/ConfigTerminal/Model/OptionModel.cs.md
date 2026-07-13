# ConfigTerminal/Model/OptionModel.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** enum, record · **Lines:** 82

## Summary
Declares the small value types that describe one editable config option: the scope/kind/liveness enums, an `EnumChoice` record for one enum member, and the `OptionDefinition` record that is the declarative metadata driving the editor UI, serialization, validation and liveness hints. These are the atoms the `OptionRegistry` builds and the config-document/edit-session layers consume.

## Types
### OptionScope — enum, internal
Which DS file/scope an option lives in: `DedicatedRoot` (root of `SpaceEngineers-Dedicated.cfg` / `MyConfigDedicated`) or `Session` (the cfg's `<SessionSettings>` template or a world's `<Settings>`).

### OptionKind — enum, internal
Editor widget / codec selection: `Bool`, `Int`, `UInt`, `Long`, `Float`, `Double`, `Text`, `MultilineText`, `Enum`, `UlongList`, `StringList`, `BlockTypeLimits`, `Password`.

### Liveness — enum, internal
Whether a change applies to a running server via SIGHUP reload (`LiveViaReload`) or needs a restart (`RestartRequired`).

### EnumChoice — record, internal (sealed)
One member of an enum-typed option: `Value` (int), `XmlName` (exact XML name written to disk) and `Label` (human label).

### OptionDefinition — record, internal (sealed)
Declarative metadata for one config option — the single source of truth per §6 of the design doc.
- **Properties (positional):** `Id`, `Scope`, `XmlName`, `Kind`, `Category`, `Label`, `Help`, `Default`; optional `Min`/`Max`/`Step` (`double?`), `Choices` (`EnumChoice[]`), `Liveness` (default `RestartRequired`), `Hidden`, `Experimental`, `ExperimentalRule`.
- **Methods:**
  - `NormalizeEnum(string raw)` — for an enum option, maps a raw value (matched case-insensitively against `XmlName`, `Label`, or the integer value) to the canonical `XmlName`; returns the trimmed input if unmatched, or the raw value unchanged for non-enum kinds / null input.

## Cross-references
- **Uses:** `System` (`StringComparison`); referenced throughout `ConfigTerminal.Model` (`OptionRegistry`, `ConfigDocumentBase`, `EditSession`).
- **Used by:** [Diagnostics.cs](../Diagnostics.cs.md), [ConfigDocumentBase.cs](ConfigDocumentBase.cs.md), [DedicatedConfigDocument.cs](DedicatedConfigDocument.cs.md), [EditSession.cs](EditSession.cs.md), [OptionRegistry.cs](OptionRegistry.cs.md), [WorldConfigDocument.cs](WorldConfigDocument.cs.md), [DashboardView.cs](../Ui/DashboardView.cs.md), [OptionFormView.cs](../Ui/OptionFormView.cs.md), [DocumentTests.cs](../../ConfigTerminalTests/DocumentTests.cs.md), [LiveEndToEndTests.cs](../../ConfigTerminalTests/LiveEndToEndTests.cs.md), [RegistryTests.cs](../../ConfigTerminalTests/RegistryTests.cs.md)
