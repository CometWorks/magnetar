using Pulsar.Shared.Data;
using VRage.Game;
using VRage.GameServices;

namespace Pulsar.Legacy.Extensions;

internal static class ModPluginExtensions
{
    public static MyObjectBuilder_Checkpoint.ModItem GetModItem(this ModPlugin modPlugin)
    {
        var modItem = new MyObjectBuilder_Checkpoint.ModItem(modPlugin.WorkshopId, "Steam");
        modItem.SetModData(new WorkshopItem(modPlugin.ModLocation));
        return modItem;
    }

    class WorkshopItem : MyWorkshopItem
    {
        public WorkshopItem(string folder)
        {
            Folder = folder;
        }
    }

    public static MyModContext GetModContext(this ModPlugin modPlugin)
    {
        MyModContext modContext = new();
        modContext.Init(modPlugin.GetModItem());
        modContext.Init(modPlugin.WorkshopId.ToString(), null, modPlugin.ModLocation);
        return modContext;
    }
}
