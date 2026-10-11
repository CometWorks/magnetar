# Plugins

The dedicated server loads its initial world in `MySandboxGame.Initialize`, before
it initializes plugins. Magnetar defers that world load until after the plugins'
`Init`, so server plugins see the order a client plugin does: `Init` runs with no
world loaded, and the world loads with the plugins' patches applied and their
session components (`[MySessionComponentDescriptor]` classes) registered the
usual way, restored from the save.

Static plugin `Rewrite` methods are registered earlier still, when Magnetar
creates the plugin owners; rewriters must not depend on the plugin's constructor
or `Init` having run. Prepare any compiler references and whitelists in the
plugin preloader's `Finish` hook. Registration belongs to `PluginInstance` owner
creation, not dependency injection; an owner disabled by an early rewrite failure
is not constructed during later initialization.

The Magnetar owner/dispatcher regression check can be run without a game install:
`dotnet run --project Tests/RewriterLifecycle/RewriterLifecycle.csproj -c Release`.

To regression-test initial-world rewriting on Linux, start a fresh server process
with the [Sigma Draconis creative save](https://steamcommunity.com/sharedfiles/filedetails/?id=3656416777)
and its mods. Allow the first mod download to finish. Confirm the dedicated-server
log reaches `Session loaded` and `Game ready`, and that
`Storage/3580645761.sbm_Mod/Sigma Draconis Expanse2_ WeaponsInit.log` exists under
the dedicated-server data directory. The underscore proves the mod's
`Path.GetInvalidFileNameChars()` call received Windows semantics before opening
storage. An early `Server successfully started` message alone is not a pass.

Plugins are registered on the
[MagnetarHub](https://github.com/CometWorks/magnetar-hub). Adding other
hubs is possible but extends the trust boundary — plugins run unsandboxed native
code.

For authoring plugins, read the
**[`se-dev-plugin-sdk`](../skills/se-dev-plugin-sdk/SKILL.md)** handbook — the
plugin-author guide for `PluginSdk` (config, commands, logging, paths).
