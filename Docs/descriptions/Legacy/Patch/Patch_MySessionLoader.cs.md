# Legacy/Patch/Patch_MySessionLoader.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Patch` · **Kind:** class, internal · **Lines:** 38

## Summary
Contains two Harmony Prefix patches on `MySessionLoader.LoadMultiplayerScenarioWorld` and `MySessionLoader.LoadMultiplayerSession`. Both apply the MagnetarMod client companion to the world checkpoint before the session loads, and — when the `-hardened` flag is active (`Flags.TrustedMods`) — strip any mod that is not locally installed via Steam from the checkpoint, enforcing a "trusted mods" security policy.

## Types

### Patch_MySessionLoader — class, internal
Applied in the `"Early"` patch category. Hosts two independent Harmony Prefix patches on the two multiplayer-session load entry points; both share identical logic. Each prefix first calls `MagnetarClientMod.ApplyToCheckpoint(world?.Checkpoint)` to inject (or strip) the MagnetarMod client companion in the checkpoint's mod list. Then, when `Flags.TrustedMods` is `true`, it calls `world.Checkpoint.Mods.RemoveAll(SteamMods.IsModUntrusted)` to remove any `ModItem` that is not a locally installed Steam item. The patches return `void`, so the original methods continue executing with the now-adjusted mod list.

- **Methods:** `Patch_LoadMultiplayerScenarioWorld(MyObjectBuilder_World world, MyMultiplayerBase multiplayerSession) — Harmony Prefix on MySessionLoader.LoadMultiplayerScenarioWorld; applies the MagnetarMod companion and strips untrusted mods when hardened mode is active` · `Patch_LoadMultiplayerSession(MyObjectBuilder_World world, MyMultiplayerBase multiplayerSession) — Harmony Prefix on MySessionLoader.LoadMultiplayerSession; applies the MagnetarMod companion and strips untrusted mods when hardened mode is active`

## Cross-references
- **Uses:** `Legacy/Loader/MagnetarClientMod.cs` (`ApplyToCheckpoint`), `Legacy/Loader/SteamMods.cs` (`SteamMods.IsModUntrusted`), `Shared/Flags.cs` (`Flags.TrustedMods`), `Sandbox.Game.World.MySessionLoader.LoadMultiplayerScenarioWorld` and `LoadMultiplayerSession` (patched targets), `Sandbox.Engine.Multiplayer.MyMultiplayerBase`, `VRage.Game.MyObjectBuilder_World`
- **Used by:** _none within the repository_
