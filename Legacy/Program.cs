using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using HarmonyLib;
using Magnetar.Legacy.Arguments;
using Magnetar.Legacy.Launcher;
using Magnetar.Legacy.Loader;
using Magnetar.Legacy.Patch;
using Magnetar.Legacy.Stats;
using Pulsar.Compiler;
using Pulsar.Interface;
using Pulsar.Protocol.Interface;
using Pulsar.Shared;
using Pulsar.Shared.Arguments;
using Pulsar.Shared.Config;
using Pulsar.Shared.Data;
using SharedLauncher = Pulsar.Shared.Launcher;
using SharedLoader = Pulsar.Shared.Loader;

namespace Magnetar.Legacy;

static class Program
{
    class ExternalTools : IExternalTools
    {
        public void OnMainThread(Action action) => Game.RunOnGameThread(action);
    }

    private const string MagnetarRepo = "CometWorks/magnetar";
    private const string OldLauncher = "SpaceEngineersDedicated.exe";
    private const string VotesServer = "https://magnetarstats.ferenczi.eu";

    static void Main(string[] args)
    {
        // Capture the original launch state before the launcher mutates the
        // working directory or environment, so a restart can reproduce it.
        ServerControl.CaptureLaunchState(
            Environment.GetCommandLineArgs(),
            Environment.CurrentDirectory,
            Environment.GetEnvironmentVariables()
        );

#if NETCOREAPP

        string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string libraryDir = Path.Combine(baseDir, "Libraries", "MagnetarInterim");
        string runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();

        // -help/-version just print and exit, so skip loading the bundled
        // native libraries (which register process lifecycle handlers that emit
        // noise). ServerFlags lives in this assembly and its detection path
        // references no Pulsar.Shared type, so it is safe to consult before
        // the resolver below is installed.
        bool fastPath = ServerFlags.Help || ServerFlags.Version;

        // On Linux, preload every bundled native .so and register a single
        // DllImport resolver covering every present and future ALC. Must run
        // before any [DllImport] site fires (Steamworks.NET, etc.). On Windows
        // the native dependencies resolve through the normal DLL search path.
        if (OperatingSystem.IsLinux() && !fastPath)
            NativeLibraryPreloader.Initialize(libraryDir, baseDir);

        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver(
            [libraryDir, runtimeDir, baseDir]
        );

        MagnetarMain(args);
    }

    static void MagnetarMain(string[] args)
    {
#endif
        if (ServerFlags.Help)
        {
            ServerFlags.PrintHelp();
            return;
        }

        if (ServerFlags.Version)
        {
            ServerFlags.PrintVersion();
            return;
        }

        // Populate Pulsar's flags from a filtered argv: value-taking
        // Magnetar/DS option pairs are stripped (Pulsar's normalizer could
        // otherwise rewrite their values into Pulsar flags) and the appended
        // defaults force headless behaviour (no splash, no dialogs, never
        // launch or block on the Steam client). The dedicated server itself
        // still receives the original args.
        Parser.Initialize(ServerFlags.PulsarParserArgs(args), se1: true);

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        CrashHandler.InstallNative("Magnetar");

        if (Flags.Current.ExternalDebug)
            Debugger.Launch();

        Assembly currentAssembly = Assembly.GetExecutingAssembly();
        string baseDir = Path.GetDirectoryName(currentAssembly.Location);

        // The Pulsar Interface (splash / dialog) process is not shipped with
        // Magnetar; with the forced -noSplash/-noPrompt flags it is never
        // started, and Tools.ShowMessageBox degrades to a log-only no-op.
        string guiPath = Path.Combine(
            baseDir,
            "Libraries",
            "Interface",
            "Interface" + Tools.ExecutableExtension
        );

        using InterfaceClient interfaceClient = new(guiPath);
        Tools.EarlyInit(interfaceClient);

        // Unlike the game client there is deliberately no single-instance
        // mutex: multi-server hosts run several Magnetar processes, each on
        // its own -path/-config pair, and the pid file identifies each one.

        SetupCoreData(baseDir);

        // -withdraw-consent is a one-shot maintenance action: erase server-side
        // data, record the denial, and exit without starting the server.
        if (ServerFlags.Consent == ConsentChoice.Withdraw)
        {
            ConsentManager.Withdraw(VotesServer);
            return;
        }

        ConsentManager.Resolve();

        // Detach from the parent (typically Quasar) before the heavy startup work
        // so the parent terminating cannot take the dedicated server down with it.
        if (ServerFlags.Daemon)
            Daemon.Detach();

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
        string magnetarDir = GetConfigOverride(baseDir) ?? GetConfigDir(baseDir, asmName);

        if (!Directory.Exists(magnetarDir))
            Directory.CreateDirectory(magnetarDir);

        // Preserve the previous launch's log before Pulsar's LogFile overwrites
        // it; servers restart often and operators need the history.
        LogRotation.RotatePrevious(magnetarDir);

        LogFile.Init(magnetarDir);
        LogFile.WriteLine($"Starting Magnetar v{asmName.Version.ToString(3)}");
        LogFile.WriteLine($"Flavour: {asmName.Name}");
        LogFile.WriteLine($"Platform: {Tools.Platform}");
        LogFile.WriteLine($"Runtime: {Tools.Runtime}");

        Parser.LogChanged();
        ServerFlags.LogFlags();

        if (!string.IsNullOrWhiteSpace(ServerFlags.GitHubToken))
            LogFile.Warn(
                "-github-token / MAGNETAR_GITHUB_TOKEN is not supported by the "
                    + "Pulsar-based network layer yet and has no effect."
            );

        // MAGNETAR_SAFE_MODE only disables the preloader patches (a one-off
        // recovery knob); Pulsar's -safeMode flag is the way to start with
        // user plugins disabled. Kept separate from ConfigManager.SafeMode,
        // which Pulsar's Loader uses to drop every non-core plugin.
        preloaderDisabled = Environment.GetEnvironmentVariable("MAGNETAR_SAFE_MODE") == "1";
        if (preloaderDisabled)
            LogFile.Warn("MAGNETAR_SAFE_MODE=1 set. No preloader patches will be applied!");

        ConfigManager.EarlyInit(magnetarDir);
    }

    private static bool preloaderDisabled;

    // Magnetar is portable, like Pulsar: config, logs and caches live in a
    // folder named after the launcher, next to the binary. -useHome moves
    // that folder under the user's app-data directory instead (%APPDATA% on
    // Windows, ~/.config on Linux), and -config overrides it entirely.
    private static string GetConfigDir(string baseDir, AssemblyName asmName)
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string dataDir = Flags.Current.UseHome ? Path.Combine(appData, "Magnetar") : baseDir;

        string named = Path.Combine(dataDir, asmName.Name);
        if (Directory.Exists(named))
            return named;

        // Both launchers share the MagnetarLegacy folder until a named one
        // exists, so switching between them keeps the configuration.
        string fallback = Path.Combine(dataDir, "MagnetarLegacy");
        return Directory.Exists(fallback) ? fallback : named;
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
        Updater updater = new(MagnetarRepo);

        // Auto-update stays disabled: Magnetar's release archives are not in
        // the layout Pulsar's updater expects (it validates the target folder
        // against the Pulsar launcher names before replacing it). The Updater
        // object is still used for the bitrot and game-update prompts.
        // updater.TryUpdate();

        string checkFile = Path.Combine(baseDir, "checksum.txt");
        string libraryDir = Path.Combine(baseDir, "Libraries");

        if (Flags.Current.MakeCheckFile && Directory.Exists(libraryDir))
        {
            // The freshly written checksum trivially matches; skip the verify.
            UTF8Encoding encoding = new();
            File.WriteAllText(checkFile, Tools.GetFolderHash(libraryDir), encoding);
        }
        else if (File.Exists(checkFile) && Directory.Exists(libraryDir))
        {
            string checkSum = File.ReadAllText(checkFile);
            if (Tools.GetFolderHash(libraryDir) != checkSum)
            {
                // The prompt itself is suppressed by the forced -noPrompt, so
                // name the reason for the exit here.
                ShowStartupError(
                    "The Libraries folder does not match checksum.txt (corrupted install). "
                        + "Reinstall Magnetar, or regenerate the checksum with -mkCheck."
                );
                updater.ShowBitrotPrompt();
            }
        }

        return updater;
    }

    private static void SetupGameData(Updater updater)
    {
        string ds64Dir = Folder.GetDS64();
        if (ds64Dir is null)
        {
            ShowStartupError(
                $"{OldLauncher} not found!\nYou can specify a custom location with \"-ds64\""
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
            "..",
            "..",
            "..",
            "workshop",
            "content",
            Steam.AppIdSe1.ToString()
        );

        Version seVersion = Game.GetGameVersion(ds64Dir);
        if (seVersion is null) // Prevent NRE from Keen updates
        {
            ShowStartupError(
                "Unable to read the game version from the dedicated server binaries. "
                    + "A Space Engineers update may have changed their layout; "
                    + "check for a Magnetar update."
            );
            updater.ShowBitrotPrompt();
        }

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
            {
                // Pulsar's Updater.GameUpdatePrompt is a Yes/No dialog that
                // exits the process unless confirmed — under the forced
                // -noPrompt it would silently Exit(0) on every launch after a
                // game update. A server just logs the change and clears the
                // compiled-plugin caches so everything rebuilds for the new
                // game version.
                string change = (seVersion > oldSeVersion ? "up" : "down") + "graded";
                LogFile.WriteLine(
                    $"Space Engineers has been {change} "
                        + $"({oldSeVersion.ToString(3)} -> {seVersion.ToString(3)}); "
                        + "plugins will be rebuilt for the new game version."
                );
                GitHubPlugin.ClearGitHubCache();
                LocalFolderPlugin.ClearDevFolderCache();
            }

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
        {
            ShowStartupError(
                "The launcher's .exe.config is missing next to MagnetarLegacy.exe "
                    + "(required because the dedicated server ships one). Reinstall Magnetar."
            );
            updater.ShowBitrotPrompt();
        }
#endif

        if (!launcher.CanStart())
        {
            // CanStart's own dialog is suppressed by the forced -noPrompt.
            ShowStartupError(
                "Refusing to start (a conflicting Space Engineers process is running, "
                    + "or an unsupported argument was passed). Use -multiInstance to run "
                    + "several servers on this machine."
            );
            Environment.Exit(1);
        }
    }

    private static void SetupSteam()
    {
        // Register a resolver for the DS-shipped Steamworks.NET so workshop
        // calls bind at world-load time. Narrow on purpose: the broad DS64
        // resolver is installed later (SetupGameResolver), after the launcher
        // dependencies have priority. Do NOT initialize the Steam client API
        // here (Pulsar's Steam.Init): the dedicated server runs the Steam
        // game-server API itself, and starting the client API in the same
        // process corrupts game-server registration, making the server
        // invisible in the browser and unjoinable.
        string ds64Dir = ConfigManager.Instance.GameDir;
        AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) =>
        {
            if (new AssemblyName(eventArgs.Name).Name != "Steamworks.NET")
                return null;

            string path = Path.Combine(ds64Dir, "Steamworks.NET.dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
    }

    private static void SetupPlugins(string baseDir)
    {
        var asmName = Assembly.GetExecutingAssembly().GetName();
        string dependencyDir = Path.Combine(baseDir, "Libraries", asmName.Name);
        string compilerPath = Path.Combine(
            baseDir,
            "Libraries",
            "Compiler",
            "Compiler" + Tools.ExecutableExtension
        );

        string magnetarDir = ConfigManager.Instance.PulsarDir;
        string ds64Dir = ConfigManager.Instance.GameDir;

        string[] runtimeDirs = CompilerFactory.GetRuntimeDirectories();
        string[] probeDirs = [.. runtimeDirs, ds64Dir, dependencyDir];
        string[] references = [.. References.GetReferences(ds64Dir)];

        using (
            CompilerFactory compiler = new(
                compilerPath,
                references,
                probeDirs,
                LogFile.FilePath,
                [.. Tools.GetCompilationSymbols(trusted: true)]
            )
        )
        {
            string[] corePlugins = GetCorePlugins();
            Tools.Init(new ExternalTools(), compiler);
            SharedLoader.Instance = new SharedLoader(VotesServer, corePlugins);
            UsageStats.ReportEnabledPlugins(VotesServer, corePlugins);
        }

        Preloader preloader = new(SharedLoader.Instance.Plugins.Select(x => x.Value));
        if (preloader.HasPatches && !preloaderDisabled)
        {
            string preloadDir = Path.Combine(magnetarDir, "Preloader");

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

        // Recompiled dedicated server builds have built-in compatibility
        bool isGameFramework = Tools.GetFiles(ds64Dir, ["*.config"], []).Any();
        if (!isGameFramework)
            return [];

        // se-dotnet-compat lets the .NET Framework dedicated server run under
        // CoreCLR (the Interim/.NET 10 launcher). se-linux-compat additionally
        // wraps the Windows-native libraries with their Linux .so equivalents.
        return Tools.IsWindows() ? ["se-dotnet-compat"] : ["se-dotnet-compat", "se-linux-compat"];
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

    private static void ShowStartupError(string message)
    {
        Console.Error.WriteLine($"[Magnetar] Error: {message}");
        LogFile.Error(message);
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
        Game.ShowIntroVideo(Flags.Current.GameIntroVideo);
        Game.RegisterPlugin(new PluginLoader());

        IEnumerable<string> symbols = Tools.GetCompilationSymbols(trusted: false);
        Game.ConfigureCompiler(symbols, Flags.Current.DebugMods);

        // Install POSIX signal handlers and bind the plugin SDK facade before
        // the server starts. Safe this early — handlers tolerate a null session.
        ServerControl.InstallSignalHandlers();

        // Validate -path (this can Exit(1)) BEFORE publishing the pid file, so
        // an aborted startup never leaves a stale magnetar.pid behind.
        string[] finalArgs = EnsureDataPathApplied(args, ds64Dir);

        // Publish this instance's pid so an external tool (MagnetarConfig) can
        // discover and verify it. Written after the daemon detach (the pid is
        // final) and removed by ServerControl.FlushAll on every clean exit.
        PidFile.Write(ConfigManager.Instance.PulsarDir, ResolveDataDir(args, ds64Dir));

        Game.StartDedicatedServer(finalArgs);
    }

    // Resolves the DS data directory (the "-path" value) the same way the DS's
    // ProcessArgs does — combined against the DS binaries' folder so an absolute
    // path passes through unchanged. Returns null when "-path" is absent (the DS
    // then uses its default instance).
    private static string ResolveDataDir(string[] args, string ds64Dir)
    {
        int index = Array.FindIndex(
            args,
            arg => arg.Equals("-path", StringComparison.OrdinalIgnoreCase)
        );

        if (index < 0 || index + 1 >= args.Length)
            return null;

        return Path.GetFullPath(Path.Combine(ds64Dir, args[index + 1].Trim('"')));
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
            ShowStartupError(
                $"-path directory does not exist: {dataPath}\n"
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

        LogFile.WriteLine(
            $"Applying -path \"{dataPath}\" by enabling the server's console launch mode (-console)."
        );
        return [.. args, "-console"];
    }
}
