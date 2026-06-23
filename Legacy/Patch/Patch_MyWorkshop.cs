using System.Collections.Generic;
using HarmonyLib;
using Pulsar.Legacy.Loader;
using Sandbox.Engine.Networking;
using VRage.Game;
using VRage.GameServices;

namespace Pulsar.Legacy.Patch;

[HarmonyPatchCategory("Early")]
[HarmonyPatch(typeof(MyWorkshop), "DownloadWorldModsBlocking")]
internal static class Patch_MyWorkshop
{
    public static void Prefix(ref List<MyObjectBuilder_Checkpoint.ModItem> mods)
    {
        MagnetarClientMod.ApplyToModList(ref mods);
    }

    public static void Postfix(
        List<MyObjectBuilder_Checkpoint.ModItem> mods,
        MyWorkshop.ResultData __result
    )
    {
        if (__result.Result == MyGameServiceCallResult.OK)
            SteamMods.RepairLegacyArchives(mods);
    }
}
