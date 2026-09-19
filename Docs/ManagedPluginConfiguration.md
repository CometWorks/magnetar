# Managed plugin configuration

Managed clusters install one canonical configuration revision before plugin construction
or preload hooks. Existing plugins keep using `ConfigStorage.LoadXml<T>` or `LoadJson<T>`;
both return the approved values, including when called from a constructor. Standalone
storage behavior is unchanged.

The loader reads `CLUSTER_PLUGIN_CONFIGURATION` and verifies its bytes against
`CLUSTER_PLUGIN_CONFIGURATION_SHA256`. The JSON schema is:

```json
{
  "schemaVersion": 1,
  "revision": "deployment revision",
  "plugins": [
    {
      "id": "plugin-id",
      "assemblySha256": "64 hexadecimal characters",
      "configType": "PluginNamespace.Configuration",
      "configuration": { "schema": {}, "defaults": {}, "values": {} }
    }
  ]
}
```

Include every selected plugin and implicit compatibility assembly. Omit `configType`
and `configuration` for plugins without SDK configuration. Use the complete envelope
reported by `ConfigStorage.SaveJson`, not values alone. Schema/default/value read-back
must match; unknown fields or normalization differences fail admission. Assembly ownership
comes from loader metadata and ambiguous ownership is refused.

The loader binds owners before preload hooks and construction, then records successful
initialization and verifies the whole expected inventory. Preloader-only assemblies
need no runtime instance. The runtime calls `ManagedPluginConfiguration.Observe()` on
the game thread to detect changes, including direct collection mutation. A drift failure
is sticky for that process; the Registry decides draining/replacement. Matching
`SaveXml` calls are no-ops, divergent local saves are refused, and reloads continue to
return canonical values. Agent is not required for these checks.

This governs SDK-managed configuration. Private serializers, arbitrary files and config
objects used before an SDK load need an explicit adapter. No instrumentation freezes
arbitrary plugin fields or undoes side effects. `Observe` is diagnostic proof, not a
permission for plugins to claim cluster-wide durable state; that opt-in SDK extension
is a separate required integration stage.

Validation: `dotnet test PluginSdkTests/PluginSdkTests.csproj` includes an unchanged-plugin
constructor fixture, canonical reload, divergent save rejection and sticky drift.
