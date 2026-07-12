using System;
using System.Linq;
using Magnetar.ConfigTerminal.Model;
using Magnetar.ConfigTerminal.Process;

namespace Magnetar.ConfigTerminal;

/// <summary>
/// Headless, read-only report of an instance's state (-diag). Exercises the same
/// model/process layers the UI uses without starting Terminal.Gui — handy for
/// ops scripts, CI smoke tests, and confirming an instance resolves.
/// </summary>
internal static class Diagnostics
{
    public static int Run(InstanceBinding binding)
    {
        Console.WriteLine("MagnetarConfig diagnostics");
        Console.WriteLine($"  data dir   : {binding.DataDir}");
        Console.WriteLine($"  config dir : {binding.MagnetarConfigDir}");
        Console.WriteLine($"  launcher   : {binding.MagnetarExePath}");
        Console.WriteLine($"  ds64       : {binding.Ds64Dir ?? "(not found)"}");
        Console.WriteLine();

        DsInstance instance = DsInstance.Open(binding);

        Console.WriteLine($"cfg exists          : {instance.Config?.ExistsOnDisk}");
        if (instance.Config != null)
        {
            Console.WriteLine($"  ServerName        : {Val(instance, "Dedicated.ServerName")}");
            Console.WriteLine($"  ServerPort        : {Val(instance, "Dedicated.ServerPort")}");
            Console.WriteLine($"  NetworkType       : {Val(instance, "Dedicated.NetworkType")}");
            Console.WriteLine($"  IgnoreLastSession : {instance.Config.IgnoreLastSession}");
            Console.WriteLine($"  LoadWorld         : '{instance.Config.LoadWorld}'");
            Console.WriteLine($"  Premade path      : '{instance.Config.PremadeCheckpointPath}'");
            Console.WriteLine($"  Password set      : {instance.Config.HasPassword}");
        }

        Console.WriteLine();
        Console.WriteLine($"Worlds ({instance.Worlds.Worlds.Count}):");
        foreach (WorldInfo w in instance.Worlds.Worlds)
            Console.WriteLine($"  {(w.IsActive ? "*" : " ")} {w.SessionName,-30} mods={w.ModCount} cfg={(w.HasWorldConfig ? "ok" : "missing")} saved={w.LastSaveTime:yyyy-MM-dd HH:mm}");

        Console.WriteLine();
        Console.WriteLine($"Active world : {instance.ActiveWorld?.SessionName ?? "(none)"}");

        Console.WriteLine();
        Console.WriteLine($"Templates ({instance.Templates.Templates.Count}):");
        foreach (WorldTemplate t in instance.Templates.Templates)
            Console.WriteLine($"    {t.DisplayName}  ({t.FolderName})");

        Console.WriteLine();
        try
        {
            var plugins = new MagnetarPlugins(binding.MagnetarConfigDir, new Io.AtomicFile());
            var dlls = plugins.LocalDlls();
            Console.WriteLine($"Local DLLs ({dlls.Count(d => d.Enabled)} enabled of {dlls.Count}):");
            foreach (LocalDllInfo d in dlls)
                Console.WriteLine($"  {(d.Enabled ? "[x]" : "[ ]")} {d.FileName}");

            var devs = plugins.DevFolderPlugins();
            Console.WriteLine($"Registered dev folders ({devs.Count(p => p.Enabled)} enabled of {devs.Count}):");
            foreach (DevFolderPlugin p in devs)
                Console.WriteLine($"  {(p.Enabled ? "[x]" : "[ ]")} {p.Id}  {p.Folder ?? "(no folder)"}{(p.SourceMissing ? " !missing" : "")}");

            var hub = plugins.HubCatalogPlugins();
            Console.WriteLine($"Hub plugins ({hub.Count(h => h.Enabled)} enabled of {hub.Count} in catalog):");
            foreach (HubPluginView h in hub)
                Console.WriteLine($"  {(h.Enabled ? "[x]" : "[ ]")} {h.Info.FriendlyName,-28} {h.Info.RepoId ?? h.Info.Id}");

            Console.WriteLine($"Plugin sources: {plugins.RemoteHubs().Count} hub, " +
                $"{plugins.RemotePlugins().Count} plugin, {plugins.LocalHubs().Count} local");

            var catalog = new ProfileCatalog(binding.MagnetarConfigDir, new Io.AtomicFile());
            var namedProfiles = catalog.NamedProfiles();
            string activeMatch = catalog.ActiveMatchKey();
            Console.WriteLine($"Profiles ({namedProfiles.Count} saved; active set matches: {activeMatch ?? "none"}):");
            foreach (ProfileInfo pr in namedProfiles)
                Console.WriteLine($"  {(pr.MatchesActive ? "->" : "  ")} {pr.Name}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Plugins: could not read ({e.Message})");
        }

        Console.WriteLine();
        var proc = new MagnetarProcess(binding);
        ServerStatus status = proc.Query();
        Console.WriteLine($"Server status : {status}  {status.Detail}");

        if (instance.Problems.Any)
        {
            Console.WriteLine();
            Console.WriteLine("Problems:");
            foreach (string p in instance.Problems.Messages)
                Console.WriteLine("  • " + p);
        }

        return 0;
    }

    private static string Val(DsInstance instance, string id)
    {
        OptionDefinition def = OptionRegistry.ById(id);
        return def == null ? "" : instance.Config.Get(def);
    }
}
