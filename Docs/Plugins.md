# Plugins

Static plugin `Rewrite` methods are registered before the dedicated server loads
its initial world. Plugin constructors and `Init` still run at the normal game
initialization stage; rewriters must not depend on either having run. Prepare
any compiler references and whitelists in the plugin preloader's `Finish` hook.
Registration belongs to `PluginInstance` owner creation, not dependency injection;
an owner disabled by an early rewrite failure is not constructed during later
initialization.

The shared owner/dispatcher regression check can be run without a game install:
`dotnet run --project Pulsar/Tests/RewriterLifecycle/RewriterLifecycle.csproj -c Release`.

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
