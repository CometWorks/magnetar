using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using HarmonyLib;
using PluginSdk.Commands;
using Pulsar.Legacy.Commands;
using Pulsar.Legacy.Paths;
using Pulsar.Shared;
using Pulsar.Shared.Config;
using Pulsar.Shared.Data;
using Sandbox.Game.World;
using VRage.Plugins;
using SharedLoader = Pulsar.Shared.Loader;

namespace Pulsar.Legacy.Loader;

public class PluginLoader : IHandleInputPlugin
{
    public static PluginLoader Instance;

    private bool init;
    private readonly List<PluginInstance> plugins = [];
    public List<PluginInstance> Plugins => plugins;

    /// <summary>
    /// Chat-command pipeline, built once when plugins are initialized and
    /// installed as the host's <see cref="ServerCommands.Registrar"/>. Plugins
    /// register their command modules through the <see cref="ServerCommands"/>
    /// facade. Null until plugins are initialized.
    /// </summary>
    public CommandService Commands { get; private set; }

    public PluginLoader()
    {
        Instance = this;
        AppDomain.CurrentDomain.FirstChanceException += OnException;
    }

    public bool TryGetPluginInstance(string id, out PluginInstance instance)
    {
        instance = null;
        if (!init)
            return false;

        foreach (PluginInstance p in plugins)
        {
            if (p.Id == id)
            {
                instance = p;
                return true;
            }
        }

        return false;
    }

    public void RegisterSessionComponents()
    {
        foreach (PluginInstance plugin in plugins)
            plugin.RegisterSessionComponents(MySession.Static);
    }

    public void RegisterEntityComponents()
    {
        foreach (PluginInstance plugin in plugins)
            plugin.RegisterEntityComponents(MyScriptManager.Static);
    }

    public void Init(object gameInstance)
    {
        StringBuilder debugCompileResults = new();
        Assembly currentAssembly = Assembly.GetExecutingAssembly();
        new Harmony(currentAssembly.GetName().Name + ".Late").PatchCategory("Late");

        if (ConfigManager.Instance.SafeMode)
        {
            plugins.Clear();
            LogFile.Warn("Skipping plugin instantiation");
        }
        else
        {
            InstantiatePlugins();
            LogFile.WriteLine($"Initializing {plugins.Count} plugins");

            if (Flags.CheckAllPlugins)
                debugCompileResults.Append("Plugins that failed to Init:").AppendLine();

            // Install the host's command registrar before plugins initialize.
            // Ownership is taken from each plugin's assembly, so plugins may
            // register from Init() or at any later point.
            Commands = new CommandService();
            ServerCommands.Registrar = Commands;

            // Register Magnetar's built-in !save / !restart / !quit before
            // plugins initialize. Last-registration-wins, so a plugin may
            // override any of them by registering the same prefix later.
            Commands.Register(typeof(SaveCommand).Assembly,
                typeof(SaveCommand), typeof(RestartCommand), typeof(QuitCommand), typeof(StopCommand));

            // Bind the SDK PathResolver facade to the LinuxCompat case-insensitive
            // path cache before plugins initialize, so a plugin may already use it
            // from its own Init(); otherwise the pass-through shim stays active
            // (Windows). Binding is reflection-only (GetType / GetMethod /
            // CreateDelegate) and never invokes a static member, so it triggers no
            // type initializers — the LinuxCompat cache cctor runs lazily on the
            // first actual resolve call, not here.
            PathResolverBinder.Bind();

            for (int i = plugins.Count - 1; i >= 0; i--)
            {
                PluginInstance p = plugins[i];
                if (!p.Init(gameInstance))
                {
                    plugins.RemoveAtFast(i);
                    if (Flags.CheckAllPlugins)
                        debugCompileResults
                            .Append(p.FriendlyName ?? "(null)")
                            .Append(" - ")
                            .Append(p.Id ?? "(null)")
                            .Append(" by ")
                            .Append(p.Author ?? "(null)")
                            .AppendLine();
                }
            }
        }

        init = true;

        if (Flags.CheckAllPlugins)
        {
            LogFile.WriteLine("All plugins compiled, opening log file");
            LogFile.WriteLine(debugCompileResults.ToString());
            LogFile.Open();
        }

        PluginList list = ConfigManager.Instance.List;
        Profile current = ConfigManager.Instance.Profiles.Current;

        IEnumerable<ulong> steamIDs = list.GetModPlugins(current, []).Select(x => x.WorkshopId);
        SteamMods.Update(steamIDs);
    }

    public void Update()
    {
        if (!init)
            return;

        for (int i = plugins.Count - 1; i >= 0; i--)
        {
            PluginInstance p = plugins[i];
            if (!p.Update())
                plugins.RemoveAtFast(i);
        }
    }

    public void HandleInput()
    {
        if (!init)
            return;

        for (int i = plugins.Count - 1; i >= 0; i--)
        {
            PluginInstance p = plugins[i];
            if (!p.HandleInput())
                plugins.RemoveAtFast(i);
        }
    }

    public void Dispose()
    {
        foreach (PluginInstance p in plugins)
            p.Dispose();
        plugins.Clear();

        ServerCommands.Registrar = null;
        Commands = null;

        LogFile.Dispose();
        Instance = null;
    }

    private void OnException(object sender, FirstChanceExceptionEventArgs e)
    {
        try
        {
            MemberAccessException accessException =
                e.Exception as MemberAccessException
                ?? e.Exception?.InnerException as MemberAccessException;
            if (accessException is not null)
            {
                foreach (PluginInstance plugin in plugins)
                {
                    if (plugin.ContainsExceptionSite(accessException))
                        return;
                }
            }
        }
        catch { } // Do NOT throw exceptions inside this method!
    }

    private void InstantiatePlugins()
    {
        foreach (var (data, assembly) in SharedLoader.Instance.Plugins)
            if (PluginInstance.TryGet(data, assembly, out PluginInstance instance))
                plugins.Add(instance);

        for (int i = plugins.Count - 1; i >= 0; i--)
        {
            PluginInstance p = plugins[i];
            if (!p.Instantiate())
                plugins.RemoveAtFast(i);
        }
    }
}
