# ConfigTerminalTests/RegistryTests.cs

**Project:** ConfigTerminalTests · **Namespace:** `Magnetar.ConfigTerminal.Tests` · **Kind:** class · **Lines:** 66

## Summary
xUnit tests asserting the structural invariants of `OptionRegistry` — the static table of dedicated/session config options the TUI edits. It guards that option ids and per-scope XML element names are unique, that every enum option carries choices, that Keen's misspelled XML names are preserved verbatim, and that specific enum orderings and gate options match the dedicated server's expectations.

## Types
### RegistryTests — class, public
Fact-only suite over `OptionRegistry.All` and `OptionRegistry.ById`.

- **Methods:**
  - `Ids_are_unique()` — no duplicate `OptionDefinition.Id` across the whole registry.
  - `XmlNames_are_unique_per_scope()` — within each of `OptionScope.DedicatedRoot` and `OptionScope.Session`, `XmlName` values are unique (so two options in one scope never write the same element).
  - `Enum_options_have_choices()` — every `OptionKind.Enum` definition has a non-null, non-empty `Choices` array.
  - `Keen_typos_are_preserved_verbatim()` — the misspelled DS XML names `AutoRestatTimeInMin` and `AFKTimeountMin` still exist, so nobody "corrects" them and breaks config compatibility.
  - `BlockLimits_enum_order_matches_DS()` — `Session.BlockLimitsEnabled` choice values 2/3 map to XML names `PER_FACTION`/`PER_PLAYER`.
  - `Online_and_hostility_enums_use_exact_xml_names()` — `Session.OnlineMode` choices ordered by value are `OFFLINE, PUBLIC, FRIENDS, PRIVATE`, and `Session.EnvironmentHostility` value 3 is `CATACLYSM_UNREAL`.
  - `MaxPlayers_is_present_for_new_world_gate()` — `Session.MaxPlayers` exists (the New-World wizard depends on it).

## Cross-references
- **Uses:** `OptionRegistry`, `OptionDefinition`, `OptionScope`, `OptionKind` (`ConfigTerminal/Model/`); xUnit (`Fact`); `System.Linq`.
- **Used by:** _none within the repository_
