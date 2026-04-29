using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using HarmonyLib;
using Pulsar.Magnetar.Compiler;
using Pulsar.Magnetar.Launcher;
using Pulsar.Magnetar.Loader;
using Pulsar.Magnetar.Patch;
using Pulsar.Shared;
using Pulsar.Shared.Config;
using Pulsar.Shared.Splash;
using SharedLauncher = Pulsar.Shared.Launcher;
using SharedLoader = Pulsar.Shared.Loader;

namespace Pulsar.Magnetar;

static class Program
{
    class ExternalTools : IExternalTools
    {
        public void OnMainThread(Action action) => Game.RunOnGameThread(action);
    }

    private const string PulsarRepo = "SpaceGT/Pulsar";
    private const string OldLauncher = "SpaceEngineersDedicated.exe";
    private const string StatsServer = "https://pluginstats.ferenczi.eu";

    static void Main(string[] args)
    {
        string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string libraryDir = Path.Combine(baseDir, "Libraries", "Magnetar");
        string runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();

        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver([libraryDir, runtimeDir]);

        MagnetarMain(args);
    }

    static void MagnetarMain(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        Tools.InstallNativeCrashHandler("Magnetar");

        Application.EnableVisualStyles();

        if (SharedLauncher.IsOtherPulsarRunning())
        {
            Tools.ShowMessageBox("Error: Magnetar is already running!");
            return;
        }

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
        string pulsarDir = Path.Combine(baseDir, asmName.Name);

        if (!Directory.Exists(pulsarDir))
            pulsarDir = Path.Combine(baseDir, "Magnetar");

        LogFile.Init(pulsarDir);
        LogFile.WriteLine($"Starting Magnetar v{asmName.Version.ToString(3)}");

        Flags.LogFlags();

        if (Flags.SplashType == SplashType.Pulsar)
            SplashManager.Instance = new SplashManager();

        SplashManager.Instance?.SetTitle("Magnetar");
        SplashManager.Instance?.SetText("Starting Magnetar...");

        ConfigManager.EarlyInit(pulsarDir);
    }

    private static Updater TryUpdate(string baseDir)
    {
        Updater updater = new(PulsarRepo);
        updater.TryUpdate();

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
            Tools.ShowMessageBox(
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
                Name = "PluginHub",
                Repo = "StarCpt/PluginHub",
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

        if (!launcher.CanStart())
            Environment.Exit(1);
    }

    private static void SetupSteam()
    {
        SplashManager.Instance?.SetText("Starting Steam...");
        string ds64Dir = ConfigManager.Instance.GameDir;
        AppDomain.CurrentDomain.AssemblyResolve += Steam.SteamworksResolver(ds64Dir);
        Steam.Init(Steam.AppIdSe1DS);
    }

    private static void SetupPlugins(string baseDir)
    {
        SplashManager.Instance?.SetText("Getting Plugins...");

        var asmName = Assembly.GetExecutingAssembly().GetName();
        string dependencyDir = Path.Combine(baseDir, "Libraries", asmName.Name);

        string pulsarDir = ConfigManager.Instance.PulsarDir;
        string ds64Dir = ConfigManager.Instance.GameDir;

        using (CompilerFactory compiler = new([ds64Dir, dependencyDir], ds64Dir, pulsarDir))
        {
            Tools.Init(new ExternalTools(), compiler);
            SharedLoader.Instance = new SharedLoader(StatsServer, GetCorePlugins());
        }

        Preloader preloader = new(SharedLoader.Instance.Plugins.Select(x => x.Item2));
        if (preloader.HasPatches && !ConfigManager.Instance.SafeMode)
        {
            SplashManager.Instance?.SetText("Applying Preloaders...");
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
        // se-dotnet-compat preloading disabled; user supplies a custom build via the Current profile.
        return [];
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

        Game.AddCompilationSymbols("NETCOREAPP");

        SplashManager.Instance?.SetText("Launching Dedicated Server...");
        if (Tools.IsNative())
            ProgressPollFactory().Start();

        SplashManager.Instance?.Delete();
        Game.StartDedicatedServer(args);
    }

    private static Thread ProgressPollFactory()
    {
        static void ProgressPoll()
        {
            float progress = 0;
            SplashManager splash = SplashManager.Instance;

            while (SplashManager.Instance is not null && progress < 1)
            {
                // FIXME: Does not work well with preloaded assemblies
                progress = Game.GetLoadProgress();

                if (float.IsNaN(splash.BarValue) || splash.BarValue < progress)
                    splash?.SetBarValue(progress);

                Thread.Sleep(250); // ms
            }
        }

        return new Thread(ProgressPoll) { IsBackground = true, Name = "ProgressPoll" };
    }
}
