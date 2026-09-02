# Plugins

Plugins are registered on
[PluginHub-DS](https://github.com/viktor-ferenczi/PluginHub-DS/). Adding other
hubs is possible but extends the trust boundary — plugins run unsandboxed native
code.

For authoring plugins, read the
**[`se-dev-plugin-sdk`](../skills/se-dev-plugin-sdk/SKILL.md)** handbook — the
plugin-author guide for `PluginSdk` (config, commands, logging, paths).

Source plugins compile against the Dedicated Server assemblies, Magnetar's
runtime libraries, `PluginSdk`, and host-provided `Magnetar.Protocol` from the
instance `Local` directory. This lets a source plugin implement Quasar companion
contracts without bundling a private protocol assembly.
