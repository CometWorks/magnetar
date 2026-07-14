# ConfigTerminal/Model/OptionRegistry.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** static class · **Lines:** 431

## Summary
The single declarative source of truth for every editable DS config option, hand-transcribed from the decompiled `MyConfigDedicatedData` and `MyObjectBuilder_SessionSettings` (build 1.209.024) and cross-checked against Quasar's metadata. Field names (including Keen's typos such as `AutoRestatTimeInMin` and `AFKTimeountMin`), defaults, and enum XML names are exact so they match the game byte-for-byte. It lazily builds two `OptionDefinition` lists — dedicated-root options and session-settings options — grouped into UI categories, and exposes lookup/enumeration helpers over them. See Docs/ConfigTerminal.md §6.

## Types
### OptionRegistry — static class, internal
Holds the option catalog and builds it on first access.
- **Fields:**
  - Enum-choice tables (`static readonly EnumChoice[]`): `GameMode`, `OnlineMode`, `EnvironmentHostility`, `BlockLimits`, `LimitBlocksBy` — values verified against the decompiled enums and serialized by name.
  - `dedicated`, `session` (`List<OptionDefinition>`) — lazily-built backing lists.
- **Properties:**
  - `DedicatedOptions` / `SessionOptions` (`IReadOnlyList<OptionDefinition>`) — lazy `??=` over `BuildDedicated()` / `BuildSession()`.
  - `All` (`IEnumerable<OptionDefinition>`) — the two lists concatenated.
- **Methods:**
  - `ById(string id)` — first option whose `Id` matches, or null.
  - `Categories(OptionScope scope)` — distinct category names for a scope, in declaration order.
  - `BuildDedicated()` / `BuildSession()` (private) — construct the full option lists via the nested `Builder`, one `Cat(...)` call per category; encode ranges, liveness (`LiveViaReload` for a handful of identity/anti-spam options) and experimental rules.
  - `Humanize(string xml)` (private static) — inserts spaces before internal capitals so `MaxFloatingObjects` reads "Max Floating Objects".
### OptionRegistry.Builder — class, private (sealed)
Compact fluent builder that humanizes labels and tracks the current category.
- **Properties/Fields:** `Options` (`List<OptionDefinition>`), `scope`, `category` (defaults "General").
- **Methods:** `Cat(name)` sets the current category; typed helpers `Bool`, `NullableBool`, `Int` (two overloads — numeric range vs. Liveness), `Short` (clamps to `short` range), `UInt`, `Long`, `Float`, `Double`, `Text`, `Multiline`, `StringList`, `BlockLimits`, `Enum` each `Add(...)` an `OptionDefinition` with `Id` `"Dedicated.<xml>"` / `"Session.<xml>"` and a humanized label.

## Cross-references
- **Uses:** `OptionDefinition`/`EnumChoice`/`OptionScope`/`OptionKind`/`Liveness` (this module); `System.Linq`, `System.Text.StringBuilder`.
- **Used by:** [Diagnostics.cs](../Diagnostics.cs.md), [EditSession.cs](EditSession.cs.md), [AppShell.cs](../Ui/AppShell.cs.md), [DashboardView.cs](../Ui/DashboardView.cs.md), [WorldsView.cs](../Ui/WorldsView.cs.md), [DocumentTests.cs](../../ConfigTerminalTests/DocumentTests.cs.md), [LiveEndToEndTests.cs](../../ConfigTerminalTests/LiveEndToEndTests.cs.md), [RegistryTests.cs](../../ConfigTerminalTests/RegistryTests.cs.md)
