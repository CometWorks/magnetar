# Shared/Config/CoreConfig.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Config` · **Kind:** class · **Lines:** 78

## Summary
`CoreConfig` persists the fundamental installation-level settings to `config.xml` in the Pulsar/Magnetar data directory. It carries the anonymous install ID used for plugin statistics (replacing Steam client IDs, which are unavailable on a dedicated server), network preferences, and the user's data-handling consent state. It is the first config file loaded during `EarlyInit` because it is required before any network call is made.

## Types

### CoreConfig — class, public

XML-serialisable bag of per-installation settings. Load/Save are symmetric: `Load` deserialises from `<pulsarDir>/config.xml` (creating a default instance if the file is absent or corrupt), `Save` replaces the file atomically by deleting then re-writing.

- **Fields:** `fileName` — constant file name (`config.xml`); `filePath` — private field set after load to store the resolved path for subsequent `Save` calls.
- **Properties:** `VotesServerBaseUrl` — read-only base URL for the statistics server (value comes from the constructor, not persisted); `InstallId` — anonymous GUID string, lazy-created by `ConfigManager.GetOrCreateInstallId()`; `DataHandlingConsent` — whether the user accepted telemetry; `DataHandlingConsentDate` — ISO-8601 string recording when consent was given; `AllowIPv6` — enables IPv6 for HTTP clients (default `true`); `NetworkTimeout` — milliseconds for HTTP requests (default `5000`); `GameVersion` — `[XmlIgnore]` runtime `Version` object; `GameVersionString` — `[XmlElement("GameVersion")]` string bridge for XML serialisation, converts to/from `Version`.
- **Methods:** `Save()` — serialises the instance to `filePath` via `XmlSerializer`, logs and swallows exceptions; `Load(mainDirectory)` — static factory that deserialises from `<mainDirectory>/config.xml` or returns a default instance if the file is missing or unreadable.

## Cross-references
- **Uses:** `Shared/LogFile.cs` (via `LogFile` static), `System.Xml.Serialization`
- **Used by:** [GitHub.cs](../Network/GitHub.cs.md), [ConfigManager.cs](ConfigManager.cs.md), [Program.cs](../../Legacy/Program.cs.md), [Loader.cs](../Loader.cs.md)
