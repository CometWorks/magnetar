using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Pulsar.Legacy.Compiler;
using Pulsar.Legacy.Launcher;
using Pulsar.Legacy.Loader;
using Pulsar.Legacy.Patch;
using Pulsar.Shared;
using Pulsar.Shared.Config;
using SharedLauncher = Pulsar.Shared.Launcher;
using SharedLoader = Pulsar.Shared.Loader;
#if NETCOREAPP
using System.Runtime.InteropServices;
#endif

namespace Pulsar.Legacy;

static class Program
{
    class ExternalTools : IExternalTools
    {
        public void OnMainThread(Action action) => Game.RunOnGameThread(action);
    }

    private const string PulsarRepo = "SpaceGT/Pulsar";
    private const string OldLauncher = "SpaceEngineersDedicated.exe";
    private const string StatsServer = "https://magnetarstats.ferenczi.eu";

    static void Main(string[] args)
    {
        // Capture the original launch state before the launcher mutates the
        // working directory or environment, so a restart can reproduce it.
        ServerControl.CaptureLaunchState(
            Environment.GetCommandLineArgs(),
            Environment.CurrentDirectory,
            Environment.GetEnvironmentVariables());

#if NETCOREAPP

        string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string libraryDir = Path.Combine(baseDir, "Libraries", "Interim");
        string runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();

        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver([libraryDir, runtimeDir]);

        MagnetarMain(args);
    }

    static void MagnetarMain(string[] args)
    {
#endif
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        Tools.InstallNativeCrashHandler("Magnetar");

        if (Flags.ExternalDebug)
            Debugger.Launch();

        Assembly currentAssembly = Assembly.GetExecutingAssembly();
        string baseDir = Path.GetDirectoryName(currentAssembly.Location);

        SetupCoreData(baseDir);
        Updater updater = TryUpdate(baseDir);
        SetupGameData(updater);
        CheckCanStart(updater);
        SetupSteam();
        SetupPlugins(baseDir);
        SetupGame(args);
    }

    private static void SetupCoreData(string baseDir)
    {
        Environment.CurrentDirectory = baseDir;

        var asmName = Assembly.GetExecutingAssembly().GetName();
        string pulsarDir = GetConfigOverride(baseDir);

        if (pulsarDir is not null)
        {
            Directory.CreateDirectory(pulsarDir);
        }
        else
        {
            pulsarDir = Path.Combine(baseDir, asmName.Name);

            if (!Directory.Exists(pulsarDir))
                pulsarDir = Path.Combine(baseDir, "Legacy");
        }

        LogFile.Init(pulsarDir);
        LogFile.WriteLine($"Starting Magnetar v{asmName.Version.ToString(3)}");

        Flags.LogFlags();

        ConfigManager.EarlyInit(pulsarDir);
    }

    private static string GetConfigOverride(string baseDir)
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.FindIndex(
            args,
            arg => arg.Equals("-config", StringComparison.OrdinalIgnoreCase)
        );

        if (index < 0 || index >= args.Length - 1)
            return null;

        string path = args[index + 1];
        if (!Path.IsPathRooted(path))
            path = Path.Combine(baseDir, path);

        return Path.GetFullPath(path);
    }

    private static Updater TryUpdate(string baseDir)
    {
        Updater updater = new(PulsarRepo);

        // Auto-update disabled: Magnetar is versioned independently (0.1.0) from the
        // upstream SpaceGT/Pulsar repo checked here, so it always sees itself as out of
        // date and would self-replace with Pulsar on every launch, exiting (code 0)
        // before the dedicated server starts. Uncomment to restore automatic updates.
        // updater.TryUpdate();

        string checkSum = null;
        string checkFile = Path.Combine(baseDir, "checksum.txt");
        string libraryDir = Path.Combine(baseDir, "Libraries");

        if (Flags.MakeCheckFile)
        {
            UTF8Encoding encoding = new();
            checkSum = Tools.GetFolderHash(libraryDir);
            File.WriteAllText(checkFile, checkSum, encoding);
        }
        else if (File.Exists(checkFile))
            checkSum = File.ReadAllText(checkFile);

        if (checkSum is not null && Tools.GetFolderHash(libraryDir) != checkSum)
            updater.ShowBitrotPrompt();

        return updater;
    }

    private static void SetupGameData(Updater updater)
    {
        string ds64Dir = Folder.GetDS64();
        if (ds64Dir is null)
        {
            Tools.ShowMessage(
                $"Error: {OldLauncher} not found!\n"
                    + "You can specify a custom location with \"-ds64\""
            );
            Environment.Exit(1);
        }

        string modDir = Path.Combine(
            ds64Dir,
            @"..\..\..\workshop\content",
            Steam.AppIdSe1.ToString()
        );

        Version seVersion = Game.GetGameVersion(ds64Dir);
        if (seVersion is null)
            updater.ShowBitrotPrompt();

        RemoteHubConfig[] defaultHubs =
        [
            new RemoteHubConfig()
            {
                Name = "MagnetarHub",
                Repo = "viktor-ferenczi/MagnetarHub",
                Branch = "main",
                Enabled = true,
                Hash = null,
                LastCheck = null,
                Trusted = true,
            },
        ];

        ConfigManager.Init(ds64Dir, modDir, seVersion, defaultHubs);

        CoreConfig coreConfig = ConfigManager.Instance.Core;
        Version oldSeVersion = coreConfig.GameVersion;
        if (seVersion != oldSeVersion)
        {
            if (oldSeVersion is not null)
                Updater.GameUpdatePrompt(oldSeVersion, seVersion, 3);

            coreConfig.GameVersion = seVersion;
            coreConfig.Save();
        }
    }

    private static void CheckCanStart(Updater updater)
    {
        string ds64Dir = ConfigManager.Instance.GameDir;
        string originalLoaderPath = Path.Combine(ds64Dir, OldLauncher);
        var launcher = new SharedLauncher(originalLoaderPath);

#if NETFRAMEWORK
        if (!launcher.VerifyConfig())
            updater.ShowBitrotPrompt();
#endif

        if (!launcher.CanStart())
            Environment.Exit(1);
    }

    private static void SetupSteam()
    {
        // Register the Steamworks.NET resolver so workshop calls bind at world-load time.
        // Do NOT initialize the Steam client API here: the dedicated server runs the Steam
        // game-server API itself, and starting the client API in the same process corrupts
        // game-server registration, making the server invisible in the browser and unjoinable.
        string ds64Dir = ConfigManager.Instance.GameDir;
        AppDomain.CurrentDomain.AssemblyResolve += Steam.SteamworksResolver(ds64Dir);
    }

    private static void SetupPlugins(string baseDir)
    {
        var asmName = Assembly.GetExecutingAssembly().GetName();
        string dependencyDir = Path.Combine(baseDir, "Libraries", asmName.Name);

        string pulsarDir = ConfigManager.Instance.PulsarDir;
        string ds64Dir = ConfigManager.Instance.GameDir;

        using (CompilerFactory compiler = new([ds64Dir, dependencyDir], ds64Dir, pulsarDir))
        {
#if NETFRAMEWORK
            if (!Tools.IsNative())
                compiler.Init();
#endif

            Tools.Init(new ExternalTools(), compiler);
            SharedLoader.Instance = new SharedLoader(StatsServer, GetCorePlugins());
        }

        Preloader preloader = new(SharedLoader.Instance.Plugins.Select(x => x.Item2));
        if (preloader.HasPatches && !ConfigManager.Instance.SafeMode)
        {
            string preloadDir = Path.Combine(pulsarDir, "Preloader");

            preloader.PreHooks();
            preloader.Patch(ds64Dir, preloadDir);
            SetupGameResolver();
            preloader.PostHooks();
        }
        else
            SetupGameResolver();
    }

    private static string[] GetCorePlugins()
    {
#if NETFRAMEWORK
        return [];
#else
        string ds64Dir = ConfigManager.Instance.GameDir;
        bool isGameFramework = Tools.GetFiles(ds64Dir, ["*.config"], []).Any();
        return isGameFramework ? ["se-dotnet-compat"] : [];
#endif
    }

    private static void SetupGameResolver()
    {
        string ds64Dir = ConfigManager.Instance.GameDir;
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver([ds64Dir]);
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        string message = $"Unhandled exception: {e.ExceptionObject}";
        Console.Error.WriteLine($"[Magnetar] {message}");
        LogFile.Error(message);
        Environment.Exit(1);
    }

    private static ResolveEventHandler AssemblyResolver(string[] probeDirs)
    {
        return (sender, args) =>
        {
            string targetName = new AssemblyName(args.Name).Name;

            foreach (string probeDir in probeDirs)
            {
                string targetPath = Path.Combine(probeDir, targetName);

                if (File.Exists(targetPath + ".dll"))
                    return Assembly.LoadFrom(targetPath + ".dll");

                if (File.Exists(targetPath + ".exe"))
                    return Assembly.LoadFrom(targetPath + ".exe");
            }

            return null;
        };
    }

    private static void SetupGame(string[] args)
    {
        string ds64Dir = ConfigManager.Instance.GameDir;
        string originalLoaderPath = Path.Combine(ds64Dir, OldLauncher);
        Patch_PrepareCrashReport.SpaceEngineersPath = originalLoaderPath;

        LogFile.GameLog = new GameLog();

        Game.SetMainAssembly(originalLoaderPath);

        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        new Harmony(assemblyName + ".Early").PatchCategory("Early");

        Game.SetupMyFakes();
        Game.ShowIntroVideo(Flags.GameIntroVideo);
        Game.RegisterPlugin(new PluginLoader());

#if NETCOREAPP
        Game.AddCompilationSymbols("NETCOREAPP");
#endif

        // Install POSIX signal handlers and bind the plugin SDK facade before
        // the server starts. Safe this early — handlers tolerate a null session.
        ServerControl.InstallSignalHandlers();

        Game.StartDedicatedServer(args);
    }
}
