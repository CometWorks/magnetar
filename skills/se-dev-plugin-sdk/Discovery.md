# Exposing the Config to Quasar

The config object belongs to your plugin. Quasar discovers public configuration
properties and, with current SDKs, live instances returned by `ConfigStorage`.
The public-property convention below also works with older SDKs and Agents.

## The rule

For the property-based path, expose the live config through a **public,
readable, non-indexed instance property whose declared type is — or derives
from — `PluginSdk.Config.PluginConfig`.**

```csharp
using PluginSdk.Config;
using VRage.Plugins;

public class Plugin : IPlugin
{
    private static MyPluginConfig config;

    // Discovered by the Quasar agent. The declared property type must be
    // assignable to PluginConfig. The name "PluginConfig" also gives it
    // priority when more than one property qualifies.
    public MyPluginConfig PluginConfig => config;

    public void Init(object gameInstance)
    {
        config = ConfigStorage.LoadXml<MyPluginConfig>(path);
    }
    // ...
}
```

That property is sufficient for discovery. The Quasar agent runs inside the
dedicated server process, enumerates the loaded plugins, and for each one scans
its public instance properties for the first whose type is assignable to
`PluginConfig`. It then serializes that instance with `ConfigStorage.SaveJson`
(schema + defaults + values) and registers it, so Quasar can render the editor.
When several properties qualify, one named `PluginConfig` wins; otherwise the
choice is alphabetical by property name.

The same property drives the reverse direction. When an admin edits values in
Quasar, the agent reads the instance, deserializes the incoming values document,
and copies each option onto your live config through its public setters — firing
`PropertyChanged` so your plugin reacts (see
[Storage.md](Storage.md#when-the-plugin-reacts-to-a-remote-update)).

## What makes a config invisible

Each of the following compiles, round-trips to XML, and yet shows up as **no
editor at all** in Quasar:

- **Exposed only through an interface type.** A property typed as your own
  `IMyConfig` interface is *not* assignable to `PluginConfig`, even when the
  runtime object is a `PluginConfig`. Discovery tests the **declared property
  type**, not the runtime value, so the property is skipped. If your patches
  consume the config through an interface, keep that property *and* add one that
  exposes the concrete config type (or `PluginConfig`).

  ```csharp
  public IMyConfig Config => config;          // for your own code — NOT discovered
  public MyPluginConfig PluginConfig => config; // for Quasar — discovered
  ```

- **Held only in a private or static field without an SDK load.** Modern SDKs expose
  `ConfigStorage.GetLoadedConfigurations(Assembly)` for live objects returned by
  `LoadXml`/`LoadJson`, including private/static storage and multiple config types.
  Consumers can reflect this optional method for older SDK compatibility. Results are
  weakly tracked; take a snapshot without retaining instances. Older Agents and SDKs
  only discover public instance properties. Arbitrary private objects constructed or
  deserialized outside `ConfigStorage` still need an explicit adapter.

- **The property returns `null` when Quasar polls.** Construct the config in
  `Init` (before the first snapshot) or have the getter create it lazily.

## The explicit path (rarely needed)

A plugin may instead implement `IQuasarConfigProvider` (`PluginId`,
`GetConfigJson`, `ApplyConfigJson`) and take full control of serialization. That
interface lives in the host's `Magnetar.Protocol` assembly, not in PluginSdk, so
implementing it pulls a host reference into your plugin. Use it only when your
config is *not* a PluginSdk `PluginConfig` (e.g. a hand-rolled format). For a
normal PluginSdk plugin, the property-based path above is the intended mechanism
and keeps the plugin dependent on `PluginSdk` alone.
