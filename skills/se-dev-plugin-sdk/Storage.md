# Storage: XML on Disk, JSON over the Wire

`PluginSdk.Config.ConfigStorage` is a small static façade for both formats. A
plugin never serializes by hand.

## XML — local config file

```csharp
ConfigStorage.SaveXml(config, path);
var loaded = ConfigStorage.LoadXml<MyConfig>(path);
```

Properties of the XML format:

- **Sparse.** Only properties whose current value differs from the default
  (per a deep value comparison) are written. The result is a small,
  human-friendly file that does not churn when defaults change in a new
  plugin version.
- **Atomic write.** The serializer writes to `path + ".tmp"` and renames over
  `path`. A crash mid-write cannot leave a truncated config behind.
- **Forgiving load.** Missing elements leave the corresponding property at
  its default (the value the parameterless constructor produced).
  Unrecognized elements are skipped, so removing an option in a newer version
  does not break older XML files.
- **Missing file → defaults.** `LoadXml` with a non-existent path returns
  `new T()` rather than throwing.
- **Enums are written by member name** (e.g. `<Quality>High</Quality>`), not
  by their underlying integer value, so renumbering an enum in a later
  version cannot silently change stored values.

## JSON — Quasar wire format

```csharp
var json = ConfigStorage.SaveJson(config);
var loaded = ConfigStorage.LoadJson<MyConfig>(json);
```

The JSON document is a three-part envelope:

```json
{
  "schema":   { "layout": [...], "properties": [...], "structs": { ... }, "enums": { ... } },
  "defaults": { /* every option at its default value */ },
  "values":   { /* every option at its current value */ }
}
```

- **`schema`** — the layout tree, per-option metadata, struct definitions
  (`{ "members": [...], "captionMember": "<name>"? }` per struct, where
  `captionMember` is the `[StructCaption]` field name when set) and enum value
  tables produced by `ConfigSchema.Build(typeof(MyConfig))`. Rebuilt on every
  save; the plugin never writes it by hand.
- **`defaults`** — a full serialization of `new MyConfig()`, so a Quasar
  client can offer "reset to default" without round-tripping to the server.
- **`values`** — a full serialization of the current `config`. **Every option
  is present**, including those at their default. This is the opposite of the
  XML format and is deliberate: Quasar needs a complete picture to render
  every editor.
- **Enums are emitted as their member name string** (e.g. `"quality": "High"`),
  matching the XML format and the names listed in the `schema.enums`
  table.

`LoadJson` reads only the `values` section. A flat values-only document is
also accepted as a backward-compatibility fallback.

## When to use which

| Need | Format |
|---|---|
| Persist plugin config across restarts | XML |
| Hand a config to Quasar or accept one back | JSON |
| Migrate values from an older plugin version | XML (sparse + forgiving load) |
| Snapshot the schema for tooling or docs | JSON (`schema` section) |

## When the plugin reacts to a remote update

Quasar sends a values-only JSON document for a single config. A typical host
flow is:

1. `var updated = ConfigStorage.LoadJson<MyConfig>(receivedJson);`
2. Copy properties from `updated` onto the live `config` instance one by one
   so each `SetField` fires `PropertyChanged` for the plugin to react to.

Step 2 is the host's responsibility — `ConfigStorage` does not merge into an
existing instance; it always deserializes a fresh one.

## What is *not* persisted

- Private fields without a corresponding annotated public property.
- Properties without a `*OptionAttribute` (those are invisible to the
  schema, XML, and JSON pipelines alike).
- Computed read-only properties (`get`-only) — they are excluded from option
  discovery.
- Any runtime state on the config class outside the declared options.

If the plugin needs runtime caches or derived values, put them outside the
config class or behind a non-annotated property.
