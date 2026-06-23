# Mission Screens: Client Popups from Server Plugins

`PluginSdk.MissionScreens` lets a server-side plugin ask a client to open Space
Engineers' built-in mission-screen popup (`ShowMissionScreen`). This is useful
for command help, longer reports, changelogs, rule text, and any response that
does not fit cleanly in chat.

## Required world mod

Mission screens are local client UI. A dedicated server cannot open them by
calling the ModAPI directly. Magnetar therefore ships a companion world mod in
`MagnetarMod/`:

```text
MagnetarMod/Data/Scripts/MagnetarMod/MagnetarModSession.cs
```

Enable that folder as a Space Engineers world mod. The dedicated server sends a
secure mod message on channel `48731`; the client mod receives it and calls
`MyAPIGateway.Utilities.ShowMissionScreen` locally.

If the world does not list `MagnetarMod`, `MissionScreens` returns `false` so
callers can fall back to chat or another response.

## Usage

```csharp
using PluginSdk;
using PluginSdk.Commands;
using VRage.Game.ModAPI;

[CommandRoot("rules", "Rules", "server rules")]
public sealed class RulesCommands : CommandModule
{
    [Command("", "Shows server rules")]
    [Permission(MyPromoteLevel.None)]
    public void ShowRules()
    {
        MissionScreens.ShowToPlayer(
            Context.Caller.IdentityId,
            "Server Rules",
            null,
            "Rules",
            "1. No offline raiding\n2. Keep chat civil\n3. Report exploits",
            "Close");
    }
}
```

## API

| Call | Target |
|---|---|
| `MissionScreens.ShowToPlayer(identityId, ...)` | One player by identity id. Best fit for command handlers. |
| `MissionScreens.ShowToSteam(steamId, ...)` | One player by Steam id. Useful when plugin code already tracks Steam ids. |
| `MissionScreens.ShowToAll(...)` | Every connected player. Use sparingly; this opens a blocking UI popup. |
| `MissionScreens.IsHostSenderAvailable` | True when Magnetar host has bound the server-side sender. This does not prove the client mod is loaded. |

All calls return `false` when no host sender is bound, the `MagnetarMod` world
mod is not present, the target is invalid, or the payload is empty. They return
`true` once the server-side packet has been queued/sent.

Callbacks are intentionally not part of this API. The SE mission screen callback
exists only on the client where the UI is opened. If a plugin needs an OK/Cancel
response later, add a dedicated request/response packet for that workflow.

## Built-in command help

Magnetar's generated `!help`, `!prefix`, and `!prefix help <command>` output
uses mission screens automatically when `MissionScreens` is bound. If the host
sender is unavailable, the command system falls back to the existing chat
replies.
