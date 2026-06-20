# Shared/Config/ProfilesConfig.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Config` · **Kind:** class · **Lines:** 156

## Summary
`ProfilesConfig` manages the on-disk lifecycle of named plugin-enable profiles. Each profile is an XML file under `<pulsarDir>/Profiles/` (one file per profile, named by the profile's sanitised key). The special file `Current.xml` tracks the currently active profile. `ProfilesConfig` handles loading, saving, adding, removing, and renaming profiles, and provides automatic backup-and-reset behaviour when the current profile file is corrupt.

## Types

### ProfilesConfig — class, public

Stateful manager for the directory of profile XML files. It is primary-constructor-based, taking the folder path at construction time. The internal store is a `Dictionary<string, Profile>` keyed by `Profile.Key`.

- **Fields:** `currentKey` — constant (`"Current"`) for the reserved current-profile filename; `profiles` — private `Dictionary<string, Profile>` of all non-current named profiles; `folderPath` — injected via primary constructor, path to the Profiles directory.
- **Properties:** `Current` — the active `Profile` instance; `Profiles` — `IEnumerable<Profile>` over all named profiles.
- **Methods:** `Save(key?)` — serialises the profile identified by `key` (or `Current` when `key` is `null`) to `<key>.xml` in the profiles folder; `Exists(key)` — returns `true` if the key matches a loaded profile or the reserved `"Current"` key; `Add(profile)` — registers and immediately saves a new profile; `Remove(key)` — removes a profile from the dictionary and deletes its file; `Rename(key, newName)` — deletes the old file, updates `Name`, re-keys the dictionary entry under the new sanitised key, and saves; `Load(mainDirectory)` — static factory that reads all `*.xml` files from `<mainDirectory>/Profiles/` (excluding `.bak` and `Current.xml`), deserialises each as a `Profile`, validates it, then separately loads `Current.xml`; on corrupt current profile it creates an empty default and renames the bad file to `Current.xml.bak[N]` then calls `Tools.ShowMessage` to alert the operator.

## Cross-references
- **Uses:** `Shared/Data/Profile.cs`, `Shared/LogFile.cs`, `Shared/Tools.cs` (via `Tools.CleanFileName`, `Tools.ShowMessage`)
- **Used by:** [ConfigManager.cs](ConfigManager.cs.md), [Loader.cs](../Loader.cs.md), [PluginList.cs](../PluginList.cs.md)
