using System.Collections.Generic;
using HarmonyLib;
using Magnetar.Legacy.Integration;
using Magnetar.Legacy.Loader;
using Pulsar.Legacy.Loader;
using Sandbox.Engine.Networking;
using VRage.Game;
using VRage.GameServices;

namespace Magnetar.Legacy.Patch;

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

[HarmonyPatchCategory("Early")]
[HarmonyPatch(typeof(MyWorkshop), "UpdateMod")]
internal static class Patch_MyWorkshop_UpdateMod
{
    public static void Postfix(MyWorkshopItem mod, ref MyGameServiceCallResult __result)
    {
        if (
            __result != MyGameServiceCallResult.OK
            && LegacyWorkshopDownload.TryDownload(mod, __result)
        )
            __result = MyGameServiceCallResult.OK;
    }
}
