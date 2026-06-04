---
name: se-dev-plugin-sdk
description: Handbook for plugin developers using Magnetar's PluginSdk to declare configuration variables, the UI layout Quasar renders remotely, server-side chat commands, case-insensitive path resolution that works on both Windows and Linux, and to log through one environment-agnostic Logger.
license: MIT
---

# Magnetar PluginSdk — Developer Handbook

> **C# 14 syntax.** All examples in this handbook use the C# 14 `field`
> contextual keyword in property accessors (no explicit private backing
> field). `PluginSdk.csproj` sets `<LangVersion>latest</LangVersion>`; consumer
> plugins should do the same. The library still targets `netstandard2.0` — the
> `field` keyword is a compile-time feature and does not change runtime
> compatibility.

`PluginSdk` is the small .NET Standard 2.0 library a Magnetar plugin uses to
declare its configuration. The same declaration drives three things at once:

1. **Local XML config** — sparse, on-disk, only non-default values.
2. **Remote JSON envelope** — schema + defaults + current values, consumed by
   the Quasar control plane.
3. **Web UI layout** — Quasar renders the editor from the schema; the plugin
   never ships any UI code.

You only write a `PluginConfig`-derived class with attribute-decorated
properties. Everything else (validation hints, UI tree, change notifications,
storage) is derived from those attributes by reflection.

The library also lets a plugin declare **server-side chat commands**
(`!prefix cmd args`) with attribute-decorated methods — no parsing or
dispatch boilerplate. See [Commands.md](Commands.md).

The library also gives a plugin a single **`Logger`** that writes to the game
log when running standalone and to structured JSON when managed by Quasar —
the plugin logs the same way in both. See [Logging.md](Logging.md).

The library also exposes a **`PathResolver`** facade so file-handling code
resolves paths case-insensitively on Linux and stays a cheap no-op on Windows —
the plugin writes one code path that works on both. See [Paths.md](Paths.md).

The library also exposes a **`ServerControl`** facade so a plugin can drive the
dedicated server's lifecycle — save the world, reload the dedicated config, and
quit or restart the process — through static calls. See
[ServerControl.md](ServerControl.md).

## When to read what

| Document | When you need it |
|---|---|
| [Config.md](Config.md) | Writing the config class itself — base class contract, property pattern, change notification (incl. the list/dict/struct in-place mutation pitfall). |
| [Options.md](Options.md) | Picking the right attribute for a value (bool, ranges, strings, lists, dicts, structs). |
| [Layout.md](Layout.md) | Grouping options into tabs, sections and columns for the Web UI. |
| [Storage.md](Storage.md) | Loading and saving — XML on disk, JSON over the wire. |
| [Mutation.md](Mutation.md) | **Critical reading** before editing a list, dictionary or struct option at runtime — why in-place mutations need `NotifyChanged`. |
| [Example.md](Example.md) | Complete annotated config class to copy-paste from. |
| [Commands.md](Commands.md) | Adding server chat commands (`!prefix cmd`) with `[CommandRoot]` / `[Command]` modules. |
| [Logging.md](Logging.md) | Logging through one environment-agnostic `Logger` — game log when standalone, JSON when managed by Quasar. |
| [Paths.md](Paths.md) | Resolving filesystem paths case-insensitively via `PathResolver` so file handling works on both Windows and Linux. |
| [ServerControl.md](ServerControl.md) | Driving the server lifecycle — save, reload config, quit, restart — via the static `ServerControl` facade. |

## Minimal example

```csharp
using PluginSdk.Config;

public class MyPluginConfig : PluginConfig
{
    [BoolOption("Enable the feature")]
    public bool Enabled { get; set => SetField(ref field, value); } = true;

    [IntOption(1, 240, "Ticks per second")]
    public int TickRate { get; set => SetField(ref field, value); } = 60;
}
```

That is enough for Quasar to render a usable editor with a checkbox and a
bounded integer field, and for `ConfigStorage` to round-trip the values.

## What PluginSdk does *not* do

- It does not load, save, or watch files on its own — the plugin host calls
  `ConfigStorage.SaveXml` / `LoadXml` at appropriate moments.
- It does not push changes to Quasar — the host transports the JSON envelope.
- It does not render UI. The schema is metadata; the UI lives elsewhere.
- It does not expose a fluent builder or runtime registration API.
  Configuration is declared statically with attributes; reflection at
  `ConfigSchema.Build(typeof(MyPluginConfig))` produces the schema.
