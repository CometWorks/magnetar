using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using HarmonyLib;
using Pulsar.Legacy.Compiler;
using Pulsar.Legacy.Launcher;
using Pulsar.Legacy.Loader;
using Pulsar.Legacy.Patch;
using Pulsar.Shared;
using Pulsar.Shared.Config;
using Pulsar.Shared.Votes;
using SharedLauncher = Pulsar.Shared.Launcher;
using SharedLoader = Pulsar.Shared.Loader;

namespace Pulsar.Legacy;

static class Program
{
    class ExternalTools : IExternalTools
    {
        public void OnMainThread(Action action) => Game.RunOnGameThread(action);
    }

    private const string PulsarRepo = "SpaceGT/Pulsar";
    private const string OldLauncher = "SpaceEngineersDedicated.exe";
    private const string VotesServer = "https://magnetarstats.ferenczi.eu";

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
        string libraryDir = Path.Combine(baseDir, "Libraries", "MagnetarInterim");
        string runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();

        // -help just prints usage and exits, so skip loading the bundled native
        // libraries (which register process lifecycle handlers that emit noise).
        // Detected from the raw args rather than Flags so Main pulls in no
        // Magnetar.Shared type before the resolver below is installed.
        bool helpRequested = args.Any(a =>
            a.Equals("-h", StringComparison.OrdinalIgnoreCase)
            || a.Equals("-help", StringComparison.OrdinalIgnoreCase)
            || a.Equals("--help", StringComparison.OrdinalIgnoreCase));

        // On Linux, preload every bundled native .so and register a single
        // DllImport resolver covering every present and future ALC. Must run
        // before any [DllImport] site fires (Steamworks.NET, etc.). On Windows
        // the native dependencies resolve through the normal DLL search path.
        if (OperatingSystem.IsLinux() && !helpRequested)
            NativeLibraryPreloader.Initialize(baseDir);

        // Probe baseDir too: on Linux the managed Steamworks.NET.dll is staged
        // next to the executable (not in Libraries/). Resolving it via this
        // generic handler avoids referencing any Magnetar.Shared type from Main
        // — doing so would force the CLR to load Magnetar.Shared while JIT-ing
        // Main, before this resolver is installed, and the .NET 10 launcher keeps
        // its managed dependencies under Libraries/ rather than next to the exe.
        // The Windows DS-dir copy of Steamworks.NET is handled later in SetupSteam.
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver([libraryDir, runtimeDir, baseDir]);

        MagnetarMain(args);
    }

    static void MagnetarMain(string[] args)
    {
#endif
        if (Flags.Help)
        {
            Flags.PrintHelp();
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        Tools.InstallNativeCrashHandler("Magnetar");

        if (Flags.ExternalDebug)
            Debugger.Launch();

        Assembly currentAssembly = Assembly.GetExecutingAssembly();
        string baseDir = Path.GetDirectoryName(currentAssembly.Location);

        SetupCoreData();

        // -withdraw-consent is a one-shot maintenance action: erase server-side
        // data, record the denial, and exit without starting the server.
        if (Flags.Consent == ConsentChoice.Withdraw)
        {
            ConsentManager.Withdraw(VotesServer);
            return;
        }

        ConsentManager.Resolve();

        // Detach from the parent (typically Quasar) before the heavy startup work
        // so the parent terminating cannot take the dedicated server down with it.
        if (Flags.Daemon)
            Daemon.Detach();

        Updater updater = TryUpdate(baseDir);
        SetupGameData(updater);
        CheckCanStart(updater);
        SetupSteam();
        SetupPlugins(baseDir);
        SetupGame(args);
    }

    private static void SetupCoreData()
    {
        Assembly currentAssembly = Assembly.GetExecutingAssembly();
        string baseDir = Path.GetDirectoryName(currentAssembly.Location);
        Environment.CurrentDirectory = baseDir;

        var asmName = currentAssembly.GetName();
        string pulsarDir = GetConfigOverride(baseDir) ?? GetConfigDir(baseDir, asmName);

        if (!Directory.Exists(pulsarDir))
            Directory.CreateDirectory(pulsarDir);

        LogFile.Init(pulsarDir);
        LogFile.WriteLine($"Starting Magnetar v{asmName.Version.ToString(3)}");

        Flags.LogFlags();

        if (Environment.GetEnvironmentVariable("MAGNETAR_SAFE_MODE") == "1")
            LogFile.Warn("MAGNETAR_SAFE_MODE=1 set. No preloader patches will be applied!");

        ConfigManager.EarlyInit(pulsarDir);

        if (Environment.GetEnvironmentVariable("MAGNETAR_SAFE_MODE") == "1")
            ConfigManager.Instance.SafeMode = true;
    }

    private static string GetConfigDir(string baseDir, AssemblyName asmName)
    {
#if NETCOREAPP
        // Linux: honour XDG_CONFIG_HOME, otherwise ~/.config/Magnetar.
        if (OperatingSystem.IsLinux())
        {
            string xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (!string.IsNullOrWhiteSpace(xdg))
                return Path.Combine(xdg, "Magnetar");

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".config", "Magnetar");
        }
#endif
        // Windows: config/log directory next to the binary.
        string dir = Path.Combine(baseDir, asmName.Name);
        if (!Directory.Exists(dir))
            dir = Path.Combine(baseDir, "MagnetarLegacy");
        return dir;
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

        // Auto-update disabled: Magnetar is versioned independently from the
        // upstream SpaceGT/Pulsar repo checked here, so it always sees itself as out of
        // date and would self-replace with Pulsar on every launch, exiting (code 0)
        // before the dedicated server starts. Uncomment to restore automatic updates.
        // updater.TryUpdate();

        string checkSum = null;
        string checkFile = Path.Combine(baseDir, "checksum.txt");
        string libraryDir = Path.Combine(baseDir, "Libraries");

        if (Flags.MakeCheckFile && Directory.Exists(libraryDir))
        {
            UTF8Encoding encoding = new();
            checkSum = Tools.GetFolderHash(libraryDir);
            File.WriteAllText(checkFile, checkSum, encoding);
        }
        else if (File.Exists(checkFile))
            checkSum = File.ReadAllText(checkFile);

        if (checkSum is not null && Directory.Exists(libraryDir) && Tools.GetFolderHash(libraryDir) != checkSum)
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

        // Publish the resolved game-install root to plugins running in this
        // process. The LinuxCompat preloader reads SPACE_ENGINEERS_ROOT to
        // locate native libraries (Havok, D3DCompiler shim, etc.) under
        // DedicatedServer64/ or Bin64/. Without it the plugin logs a warning
        // and skips wrapper init, which later trips a Havok load failure.
        // We point at the parent so the plugin's existing probe
        // (DedicatedServer64 -> Bin64 fallback) keeps working unchanged.
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SPACE_ENGINEERS_ROOT")))
        {
            string gameRoot = Path.GetDirectoryName(ds64Dir);
            if (!string.IsNullOrEmpty(gameRoot))
                Environment.SetEnvironmentVariable("SPACE_ENGINEERS_ROOT", gameRoot);
        }

        string modDir = Path.Combine(
            ds64Dir,
            "..", "..", "..", "workshop", "content",
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
                Repo = "CometWorks/magnetar-hub",
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
            SharedLoader.Instance = new SharedLoader(VotesServer, GetCorePlugins());
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
        if (!isGameFramework)
            return [];

        // se-dotnet-compat lets the .NET Framework dedicated server run under
        // CoreCLR (the Interim/.NET 10 launcher). se-linux-compat additionally
        // wraps the Windows-native libraries with their Linux .so equivalents.
        return OperatingSystem.IsLinux()
            ? ["se-dotnet-compat", "se-linux-compat"]
            : ["se-dotnet-compat"];
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

        Game.StartDedicatedServer(EnsureDataPathApplied(args, ds64Dir));
    }

    // The dedicated server's own "-path <dir>" argument (the instance/data
    // directory holding SpaceEngineers-Dedicated.cfg and the world saves) is
    // parsed by DedicatedServer.ProcessArgs into a *local* variable that is only
    // forwarded to RunMain inside the "-console" / "-noconsole" branches. The
    // configurator UI those branches replace is stripped by Patch_DedicatedServerRun,
    // whose fallback launch passes a null path — so a bare "-path" (without a
    // console flag) is silently dropped and the default %APPDATA% instance is used.
    //
    // Rather than make users remember to also pass "-console", append it for them
    // when "-path" is present and no console flag is. ProcessArgs then applies the
    // path through its own (cross-platform) resolution. "-console" matches the
    // console behaviour Magnetar already launches with by default, so nothing else
    // changes. A missing directory would make the server abort startup silently, so
    // validate it here and fail loudly — this is a server, so a typo'd data path
    // must be explicit rather than quietly loading the default world.
    private static string[] EnsureDataPathApplied(string[] args, string ds64Dir)
    {
        int index = Array.FindIndex(
            args,
            arg => arg.Equals("-path", StringComparison.OrdinalIgnoreCase)
        );

        if (index < 0 || index + 1 >= args.Length)
            return args;

        // Resolve the same way ProcessArgs does: combine against the DS binaries'
        // directory. Path.Combine returns a rooted second argument unchanged, so
        // absolute paths (C:\... or /srv/...) pass through on both platforms.
        string dataPath = Path.Combine(ds64Dir, args[index + 1].Trim('"'));
        if (!Directory.Exists(dataPath))
        {
            Tools.ShowMessage(
                $"Error: -path directory does not exist: {dataPath}\n"
                    + "Create it or correct the path; refusing to start on the default instance."
            );
            Environment.Exit(1);
        }

        bool hasConsoleFlag = args.Any(arg =>
            arg.Equals("-console", StringComparison.OrdinalIgnoreCase)
            || arg.Equals("-noconsole", StringComparison.OrdinalIgnoreCase)
        );
        if (hasConsoleFlag)
            return args;

        LogFile.WriteLine($"Applying -path \"{dataPath}\" by enabling the server's console launch mode (-console).");
        return [.. args, "-console"];
    }
}
