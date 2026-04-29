using HarmonyLib;
using Pulsar.Magnetar.Loader;
using Pulsar.Shared;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;
using VRage.Game;

namespace Pulsar.Magnetar.Patch;

[HarmonyPatchCategory("Early")]
internal class Patch_MySessionLoader
{
    [HarmonyPatch(typeof(MySessionLoader), "LoadMultiplayerScenarioWorld")]
    [HarmonyPrefix]
    public static void Patch_LoadMultiplayerScenarioWorld(
        MyObjectBuilder_World world,
        MyMultiplayerBase multiplayerSession
    )
    {
        if (Flags.TrustedMods)
            world.Checkpoint.Mods.RemoveAll(SteamMods.IsModUntrusted);
    }

    [HarmonyPatch(typeof(MySessionLoader), "LoadMultiplayerSession")]
    [HarmonyPrefix]
    public static void Patch_LoadMultiplayerSession(
        MyObjectBuilder_World world,
        MyMultiplayerBase multiplayerSession
    )
    {
        if (Flags.TrustedMods)
            world.Checkpoint.Mods.RemoveAll(SteamMods.IsModUntrusted);
    }
}
