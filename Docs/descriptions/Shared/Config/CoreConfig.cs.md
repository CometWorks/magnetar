# Shared/Config/CoreConfig.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Config` · **Kind:** class · **Lines:** 74

## Summary
`CoreConfig` persists the fundamental installation-level settings to `config.xml` in the Pulsar/Magnetar data directory. It carries network preferences and the user's data-handling consent state. It is the first config file loaded during `EarlyInit` because it is required before any network call is made. The anonymous statistics identity is no longer stored here — it lives in the standalone `instance.id` file managed by [`ConfigManager`](ConfigManager.cs.md) — so `CoreConfig` only records the *decision* (a tri-state consent flag), not the identifier.

## Types

### CoreConfig — class, public

XML-serialisable bag of per-installation settings. Load/Save are symmetric: `Load` deserialises from `<pulsarDir>/config.xml` (creating a default instance if the file is absent or corrupt), `Save` replaces the file atomically by deleting then re-writing.

- **Fields:** `fileName` — constant file name (`config.xml`); `filePath` — private field set after load to store the resolved path for subsequent `Save` calls.
- **Properties:** `VotesServerBaseUrl` — read-only base URL for the statistics server (value comes from the constructor, not persisted); `DataHandlingConsent` — nullable `bool?` tri-state: `null` = undecided (eligible for prompting), `true` = telemetry accepted, `false` = declined; `DataHandlingConsentDate` — ISO-8601 string recording when the decision was made; `AllowIPv6` — enables IPv6 for HTTP clients (default `true`); `NetworkTimeout` — milliseconds for HTTP requests (default `5000`); `GameVersion` — `[XmlIgnore]` runtime `Version` object; `GameVersionString` — `[XmlElement("GameVersion")]` string bridge for XML serialisation, converts to/from `Version`.
- **Methods:** `Save()` — serialises the instance to `filePath` via `XmlSerializer`, logs and swallows exceptions; `Load(mainDirectory)` — static factory that deserialises from `<mainDirectory>/config.xml` or returns a default instance if the file is missing or unreadable.

> **Note.** The former `InstallId` property has been removed; the anonymous per-install identifier is now the random UUID in `instance.id` (its first 20 hex chars become the server-side `PlayerHash`). `DataHandlingConsent` changed from `bool` to `bool?` so that "undecided" is distinct from "declined", which is what drives the one-time interactive consent prompt.

## Cross-references
- **Uses:** `Shared/LogFile.cs` (via `LogFile` static), `System.Xml.Serialization`
- **Used by:** [Program.cs](../../Legacy/Program.cs.md), [ConfigManager.cs](ConfigManager.cs.md), [Loader.cs](../Loader.cs.md), [GitHub.cs](../Network/GitHub.cs.md), [ConsentManager.cs](../Votes/ConsentManager.cs.md)
