# ConfigTerminalTests/PluginInteropTests.cs

**Project:** ConfigTerminalTests · **Namespace:** `Magnetar.ConfigTerminal.Tests` · **Kind:** class · **Lines:** 237

## Summary
Interop tests proving that profile/sources XML written by this tool is accepted by Magnetar's own serializers. They load the deployed `Magnetar.Shared.dll` by reflection, deserialize the tool-produced `Current.xml`/`sources.xml` with `XmlSerializer` exactly as Magnetar's `ProfilesConfig.Load` does, and assert `Validate()` passes and the enabled sets (Local, DevFolder, GitHub, RemoteHub) round-trip. Every test is gated on `Magnetar.Shared.dll` being present (defaults to the standard `~/.local/share/Magnetar/Bin` install, overridable via `MAGNETAR_SHARED`) and returns early — skipping — when it is not.

## Types
### PluginInteropTests — class, public
Reflection-driven cross-serializer tests against Magnetar's shipped types, resolving Shared's dependencies from its own directory via an `AssemblyResolve` handler.

- **Methods:**
  - `SharedDllPath()` (private static) — resolves `Magnetar.Shared.dll` from `MAGNETAR_SHARED` or the default install path.
  - `Profile_round_trips_through_Magnetar_serializer()` — writes a profile with a local DLL + dev-folder plugin plus a pre-existing GitHub entry, deserializes via `XmlSerializer(Pulsar.Shared.Data.Profile)`, asserts `Validate()` true and that `Local` contains `Essentials.dll` and `DevFolder` contains the `my-plugin`/`Manifest.xml` entry.
  - `Hub_edits_round_trip_through_Magnetar_serializers()` — `EnableGitHub` + `AddRemoteHub`, then deserializes both `Pulsar.Shared.Data.Profile` (GitHub id present, `Validate()` true) and `Pulsar.Shared.Config.SourcesConfig` (RemoteHub `Repo` present).
  - `Named_profile_loads_through_Magnetar_ProfilesConfig()` — saves a named profile via `ProfileCatalog.SaveCurrentAs`, then calls Magnetar's `ProfilesConfig.Load(dir)` and asserts the named "Survival Preset" and the active `Current` both load and pass `Validate()`.

## Cross-references
- **Uses:** `PluginProfileDocument`, `PluginSourcesDocument`, `ProfileCatalog` (`ConfigTerminal/Model/`); `AtomicFile` (`ConfigTerminal/Io/`); deployed `Magnetar.Shared.dll` types `Pulsar.Shared.Data.Profile`, `Pulsar.Shared.Config.SourcesConfig`/`ProfilesConfig` (via reflection); `System.Xml.Serialization.XmlSerializer`, `System.Reflection`, `AppDomain.AssemblyResolve`; xUnit.
- **Used by:** _none within the repository_
