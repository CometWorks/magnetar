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
must match; numeric spellings such as `1`, `1.0` and `1e0` compare by exact decimal value. Other schema/value differences fail admission. Assembly ownership
comes from loader metadata and ambiguous ownership is refused.

For multiple configuration types, use `schemaVersion: 2` and replace the plugin's
singular fields with:

```json
"configurations": [
  { "configType": "PluginNamespace.FirstConfig", "configuration": { "schema": {}, "defaults": {}, "values": {} } },
  { "configType": "PluginNamespace.SecondConfig", "configuration": { "schema": {}, "defaults": {}, "values": {} } }
]
```

Each type must be unique and carry its complete envelope. Do not mix the array with
singular fields. Schema 2 also accepts singular fields for plugins with one config;
schema 1 remains readable. Older SDKs reject schema 2, so deploy the matching SDK
before activating a revision using it. Roll back by reactivating the previous verified
package and canonical revision; do not rewrite a live canonical file.

Readiness inspects every public `PluginConfig` instance property and every live object
returned by managed `ConfigStorage` loads. Configs kept in private fields or static
fields therefore work without cluster-specific plugin code. `ConfigStorage.GetLoadedConfigurations(Assembly)`
exposes weakly tracked live SDK load results to Agent in both standalone and managed
processes; observations must not keep those objects alive after taking a snapshot. Multiple types each need
an explicit canonical envelope; an omitted type fails closed. Weak references avoid
keeping discarded configuration objects alive. Current instances must retain canonical
values; plugins that intentionally mutate them need a separate runtime-state object.

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
constructor fixture, private/static multiple configurations, schema-version/duplicate rejection,
exact numeric equality, canonical reload, divergent save rejection and sticky drift.
