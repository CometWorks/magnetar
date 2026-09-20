# Preparing a managed deployment

Introduced in **Magnetar 2.4.2.2**, `MagnetarInterim -prepareManaged <directory>` compiles and exports a selected plugin
profile without starting a temporary dedicated server. This command currently targets
Linux/CoreCLR. Use a separate preparation configuration directory; normal source-list
loading, compiler caches and logs are written there.

Consumers must also check that `-help` advertises `-prepareManaged` before invoking
it: older launchers ignore unknown options and may start the dedicated server.
Use the PluginSdk binary shipped in the actual Magnetar release when rebuilding
the cluster runtime package. Managed admission compares its exact SHA-256; matching
SDK source or API versions do not imply identical DLL bytes across builds. Rebuild
the cluster package against that released SDK and refresh its recorded SDK hash
before activating a deployment prepared with the new release.

```sh
./MagnetarInterim.bin -config /srv/preparation/config \
  -ds64 /srv/SpaceEngineers/DedicatedServer64 \
  -profile Managed -prepareManaged /srv/preparation/export
```

The output directory must not exist. The command publishes it atomically after all
plugins export successfully, and exits nonzero on failure. It resolves the selected
profile, its transitive declared dependencies, implicit compatibility plugins, and
`direct-transport`. Missing plugins, unsupported runtime/platforms and failed builds
abort the export. Workshop mods, development source folders, cluster role plugins,
and unpinned source references are unsupported. `-daemon`, `-bare`, `-safeMode`,
`-debugCompileAll`, and execution inside a managed cluster process are refused.

Preparation uses the existing source compiler, NuGet restore and asset resolver.
It does not invoke Steam initialization, native-library bootstrap, preloader
constructors/hooks, `IPlugin` constructors or `Init`, or the game entry point. Plugin
assemblies are loaded for reflection. Configuration constructors run to obtain SDK
defaults, just as they do during `ConfigStorage.SaveJson`; plugins remain trusted code.
No usage telemetry is sent by the preparation path.

## Export format

The resulting directory contains:

* `CommonPlugins/<id>/<id>.dll`, `<id>.xml`, dependencies and local assets.
* `DirectTransport/DirectTransport.dll`, `DirectTransport.xml`,
  `DirectTransport.dll.xml`, dependencies and local assets.
* `preparation.json`, the machine-readable result below.

Both remote source plugins and metadata-backed local binary plugins must declare a
repository and exact 40-character source commit. Alternate profile versions resolve
to their actual repository and commit. Each local plugin must occupy a separate
directory containing its DLL, dependencies and `PluginData`/`GitHubPlugin` XML metadata
next to the DLL. The DLL must be named `plugin.dll` or match its directory name
(for example `Local/quasar-agent/quasar-agent.dll` with `quasar-agent.xml`); the
metadata's ID is the profile's `Local` entry. Local provenance is the supplied metadata's assertion; preparation
does not reconstruct or attest the original local build. Development-folder builds
are refused because the manifest commit does not prove the working tree's contents.

Binary directories are copied completely, preserving runtime dependencies and native
libraries. Named assets outside that directory are copied under `Assets/<name>`.
`source-metadata.xml` preserves the source manifest, including original asset pins;
an existing local bundle's source manifest is retained. The result also records
`magnetarVersion` and `runtimeIdentifier` for build provenance.
Exported loader metadata uses relative local asset paths and contains no asset download URLs.
Symbolic links and unsafe plugin/asset names are refused. Plugin source caches remain
rebuildable caches; the exported file hashes identify the frozen deployment bytes.

```json
{
  "schemaVersion": 1,
  "gameVersion": "1208015",
  "pluginSdkSha256": "64 hexadecimal characters",
  "pluginConfigurations": {
    "example": {
      "configType": "Example.Configuration",
      "configuration": { "schema": {}, "defaults": {}, "values": {} }
    },
    "without-config": { "configurations": [] }
  },
  "plugins": [
    {
      "id": "example",
      "repository": "owner/repository",
      "commit": "40 hexadecimal characters",
      "source": "github",
      "assemblySha256": "64 hexadecimal characters"
    }
  ],
  "files": {
    "CommonPlugins/example/example.dll": {
      "sha256": "64 hexadecimal characters",
      "bytes": 1234
    }
  }
}
```

`gameVersion` is the dedicated server's numeric build-version value, expressed as a
string. `source` is `github` or `local-metadata`. `files` hashes every exported bundle
file; it excludes `preparation.json` itself. Consumers must verify hashes and copy the
bundles into their approved immutable package before deployment.

Multiple configuration types use a `configurations` array of objects containing
`configType` and the complete `configuration` envelope. The consumer combines this
map with the frozen assembly hashes and deployment revision to create the canonical
[managed plugin configuration](ManagedPluginConfiguration.md).

## Configuration discovery and limits

For each plugin's single concrete `IPlugin` entry point, preparation inspects public
readable instance properties declared as concrete `PluginConfig` subclasses. It
constructs those configuration types directly and calls the SDK's own `SaveJson`.
It never calls plugin property getters. Multiple properties sharing the same type
produce one envelope. Fresh defaults become initial approved values; existing XML
configuration files are not imported.

Every concrete SDK configuration type declared by the plugin assembly must be exposed
through such a property. Private/static-only configurations, unused configuration
types, abstract/base-typed properties, foreign-assembly configurations and defaults
requiring game initialization produce an explicit error. This deliberately avoids
claiming runtime configuration discovery from a server that never initialized.
Plugins using arbitrary serializers remain outside the SDK configuration guarantee.

Preparation proves compilation and captures default schemas; runtime managed readiness
still verifies actual loaded configuration instances and values. A plugin that changes
its defaults during initialization can therefore fail readiness. Configuration types
loaded dynamically from dependencies also require a separately verified deployment.

Validation: `dotnet test PluginSdkTests/PluginSdkTests.csproj --filter
FullyQualifiedName~PreparationDefaultsTests` covers multiple complete envelopes,
constructor/getter exclusion, ambiguous/private configuration refusal and unavailable
game-dependent defaults.
