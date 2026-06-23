# Chat Commands: Declarative Server Commands

`PluginSdk.Commands` lets a plugin add server-side chat commands — the
`!something` lines players type in chat — by writing attribute-decorated
methods, with no parsing, dispatching or reply boilerplate. It mirrors the
ergonomics of Torch's command system but is namespaced per plugin and runs on
Magnetar's host.

Every command a plugin adds lives under one short **prefix** (the "root"), so a
plugin called Essentials might own `!ess`:

```
!ess               → overview of the plugin's commands
!ess help [cmd]    → usage for the root, or one command
!ess save          → a command
!ess tp Bob 10     → a command with arguments
```

The plugin **registers its command modules explicitly** (usually from `Init()`)
— there is no silent assembly discovery. This mirrors calling
`Harmony.PatchAll()`: the host intercepts global chat and dispatches matching
`!`-lines (suppressing them from normal chat so other players never see the
command text), but the set of command modules is whatever the plugin hands
over. See [Registration](#registration) below.

## A command module

```csharp
using PluginSdk.Commands;
using VRage.Game.ModAPI;   // MyPromoteLevel
using VRageMath;           // Color

[CommandRoot("ess", "Essentials", "core admin tools")]
public sealed class EssentialsCommands : CommandModule
{
    [Command("save", "Saves the world")]
    [Permission(MyPromoteLevel.Admin)]
    public string Save()
    {
        // ... trigger a save ...
        return "World saved.";          // returned text is sent to the caller
    }

    [Command("tp", "Teleport to a player")]
    [Permission(MyPromoteLevel.Moderator)]
    public CommandReply Teleport(string target, int distance = 5)
    {
        // Context exposes who called and the parsed args.
        if (string.IsNullOrEmpty(target))
            return CommandReply.Error("No target given.");
        return CommandReply.Ok($"Teleported {Context.Caller.Name} to {target}.");
    }

    [Command("broadcast", "Announce to everyone")]
    [Permission(MyPromoteLevel.Admin)]
    public CommandReply Announce(params string[] words)
        => CommandReply.Announce(string.Join(" ", words), Color.Yellow);
}
```

This exposes `!ess`, `!ess help`, `!ess save`, `!ess tp` and `!ess broadcast`.
A plugin may declare several modules sharing one prefix; their commands merge
under the same root.

## Registration

Command modules are **registered explicitly** through the static
`ServerCommands` facade — analogous to calling `Harmony.PatchAll()`. You always
pass your **own assembly**; the host uses it to attribute ownership of the
registered prefixes, so you never pass an id:

```csharp
using System.Reflection;   // Assembly
using PluginSdk.Commands;
using VRage.Plugins;       // IPlugin

public sealed class EssentialsPlugin : IPlugin
{
    public void Init(object gameInstance)
    {
        // Scan this plugin's assembly for [CommandRoot] modules:
        ServerCommands.Register(Assembly.GetExecutingAssembly());

        // ...or name the module types explicitly (no scan, compiler-checked):
        // ServerCommands.Register(Assembly.GetExecutingAssembly(),
        //                         typeof(EssentialsCommands), typeof(InfoCommands));
    }

    public void Update() { }
    public void Dispose() { }
}
```

| Call | What it does |
|---|---|
| `ServerCommands.Register(assembly)` | Registers every `[CommandRoot]` `CommandModule` in the assembly. The usual one-liner. |
| `ServerCommands.Register(assembly, params Type[])` | Registers the named module types only — prefer this when you want the set to be obvious and compiler-checked. |

Registration is **explicit**: the host never silently discovers command
classes. Because ownership comes from the assembly you pass — not from a host
time window — you may register from `Init()` or at any later point (e.g. when a
feature is enabled). `Init()` is the natural place, since it runs once at
startup and the registry persists for the host's lifetime. Calling `Register`
with no host present (no registrar installed) throws
`InvalidOperationException`.

## The pieces

| Type | Role |
|---|---|
| `[CommandRoot(prefix, title?, description?)]` | Class attribute on a `CommandModule`. Chooses the `!prefix` and the overview title (shown as the sender of the built-in overview/help/error replies). |
| `[Command("path", description?, helpText?)]` | Method attribute. The path is split on spaces into a nested command (`"grid list"` → `!ess grid list`). |
| `[Permission(MyPromoteLevel)]` | Minimum level to run the command. **Absent ⇒ `Admin`** — forgetting it fails safe, not open. |
| `CommandModule` | Base class. A fresh instance is created per call; `Context` is set before the handler runs. Keep modules stateless. |
| `CommandContext Context` | Per-call environment: `Caller`, `Args`, `RawArgs`, `Prefix`, and `Respond(...)`. |
| `CommandReply` | A reply with optional `Color`, `Font`, `Author`, and a `Broadcast` flag. |

## Command paths and sub-namespaces

`[Command("grid list")]` and `[Command("grid delete")]` create a `grid`
sub-namespace automatically. `!ess grid list` resolves the deepest matching
path; remaining words become arguments. Intermediate words with no handler of
their own simply act as grouping.

## Arguments

Handler parameters are bound positionally from the words after the command
path:

- Supported types: `string`, the integer and floating-point primitives,
  `bool` (`true/false`, `yes/no`, `on/off`, `1/0`), and any `enum`
  (case-insensitive by name).
- Parameters with a C# default value are **optional**.
- A trailing `params string[]` captures **all** remaining words.
- Quote `"multi word"` arguments to keep spaces; `\` escapes the next char.

If a required argument is missing or a value does not parse, the caller
automatically gets an error with the generated usage line, e.g.
`!ess tp <target> [distance]` — the handler is not called.

## Replying

The simplest reply is the **return value**:

| Return type | Effect |
|---|---|
| `void` | Sends nothing. Call `Context.Respond(...)` yourself if needed. |
| `string` | Sent privately to the caller (white). |
| `CommandReply` | Full control: colour, font, author, broadcast. |
| `IEnumerable<string>` | One private line per item. |

Or reply explicitly through the context at any point:

```csharp
Context.Respond("plain line to the caller");
Context.Respond("warning", Color.Orange);
Context.Respond(CommandReply.Announce("server restarting", Color.Red));
```

### Formatting

Space Engineers chat supports a **font** and an arbitrary **RGB colour** per
message, plus the **author** label shown as the sender.

- `CommandReply.Ok` (white), `CommandReply.Info` (blue) and `CommandReply.Error`
  (red) set the font; the client renders the matching colour.
- Set an explicit `Color` (via `CommandReply.Announce(text, color)` or
  `WithColor`) for any RGB value.
- `Font` accepts a `VRage.Game.MyFontEnum` constant (`White`, `Red`, `Green`,
  `Blue`, `DarkBlue`, ...).
- `Author` is the sender label. When you don't set it, your handler's replies
  default to the **prefix** (e.g. `ess`), while the built-in overview/help/error
  replies use the root **title** (e.g. `Essentials`). Override per reply with
  `WithAuthor`. If no author is set at all, the host shows `Server`.

`Broadcast` replies go to every player; otherwise the reply is private to the
caller. Build replies fluently:

```csharp
return CommandReply.Ok("done").WithColor(Color.LightGreen).WithAuthor("Ess");
```

## Permissions and visibility

Levels are Keen's `MyPromoteLevel`: `None`, `Scripter`, `Moderator`,
`SpaceMaster`, `Admin`, `Owner`. A caller below a command's level gets a short
"no permission" reply, and the command is **hidden** from that caller's
`!prefix` overview and `help` — so permission also controls discoverability.

## What the host does for you

- Registers the modules you hand it (see [Registration](#registration)),
  attributing ownership to the assembly you pass.
- Intercepts global chat, parses the `!prefix ...` line, checks permission,
  binds arguments, runs the handler on the game thread, and sends replies.
- Suppresses recognised command lines from being broadcast to other players.
- Generates `!prefix` overview and `!prefix help [cmd]` automatically — do not
  define a `help` command yourself (the name is reserved). When the
  `MissionScreens` host sender is available, generated help opens as a mission
  screen through `MagnetarMod`; otherwise it falls back to chat.
- Several modules — even from different plugins — may share one prefix; their
  commands merge under the same root. If two commands resolve to the same path,
  the **last registration wins** (it overwrites the earlier one), so a plugin
  can deliberately override another's command.

## Notes and limits

- Commands are accepted on the **global** chat channel only.
- Modules are instantiated per invocation; persistent state belongs in your
  plugin, reachable via static members the plugin sets up.
- The whole pipeline (`CommandRegistry` → `CommandDispatcher`) is independent of
  the game session, so command logic can be unit-tested against a fake
  `ICommandResponder`.
