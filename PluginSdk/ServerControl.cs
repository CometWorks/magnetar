using System;
using System.Runtime.CompilerServices;

// Only the host launcher may Bind the facade to its implementation.
[assembly: InternalsVisibleTo("MagnetarInterim")]
[assembly: InternalsVisibleTo("PluginSdkTests")]

namespace PluginSdk
{
    /// <summary>
    /// Plugin-facing surface for controlling the dedicated server's lifecycle:
    /// saving the world, reloading the dedicated config, and quitting or
    /// restarting the process. These mirror the operations the host also
    /// triggers from POSIX signals (SIGTERM/SIGINT → <see cref="SaveAndQuit"/>,
    /// SIGHUP → <see cref="ReloadConfig"/>).
    ///
    /// <para>
    /// The host binds the real implementations at startup via the internal
    /// <see cref="Bind"/>. Before binding (or in a non-hosted context such as a
    /// unit test) the calls are safe no-ops: the <c>bool</c>-returning ones
    /// report <c>false</c> and the others do nothing.
    /// </para>
    ///
    /// <para>
    /// All calls are thread-safe. The host marshals world access to the game's
    /// update thread and null-guards when no session is loaded, so plugins may
    /// invoke these from any thread, including from a chat-command handler
    /// running on the update thread.
    /// </para>
    /// </summary>
    public static class ServerControl
    {
        private static Func<bool> saveWorld = () => false;
        private static Func<bool> reloadConfig = () => false;
        private static Action saveAndQuit = () => { };
        private static Action saveAndRestart = () => { };
        private static Action quitWithoutSaving = () => { };
        private static Action restartWithoutSaving = () => { };

        /// <summary>Saves the world without quitting. Returns <c>false</c> when
        /// no session is loaded or the host has not bound an implementation.</summary>
        public static bool SaveWorld() => saveWorld();

        /// <summary>Saves the world, then re-reads the dedicated config and
        /// applies the settings that are safe to change at runtime (the MOTD in
        /// particular). Does not quit. Returns <c>false</c> when no session is
        /// loaded or the host has not bound an implementation.</summary>
        public static bool ReloadConfig() => reloadConfig();

        /// <summary>Saves the world, then quits the process with exit code 0.</summary>
        public static void SaveAndQuit() => saveAndQuit();

        /// <summary>Saves the world, then replaces the process with a fresh
        /// instance launched with the original command line, environment and
        /// working directory captured when the server first started.</summary>
        public static void SaveAndRestart() => saveAndRestart();

        /// <summary>Quits the process immediately with exit code 0, without
        /// saving.</summary>
        public static void QuitWithoutSaving() => quitWithoutSaving();

        /// <summary>Restarts the process immediately (original command line,
        /// environment and working directory), without saving.</summary>
        public static void RestartWithoutSaving() => restartWithoutSaving();

        /// <summary>Host-only: installs the real implementations. Called once at
        /// launcher startup.</summary>
        internal static void Bind(
            Func<bool> saveWorld,
            Func<bool> reloadConfig,
            Action saveAndQuit,
            Action saveAndRestart,
            Action quitWithoutSaving,
            Action restartWithoutSaving)
        {
            ServerControl.saveWorld = saveWorld ?? (() => false);
            ServerControl.reloadConfig = reloadConfig ?? (() => false);
            ServerControl.saveAndQuit = saveAndQuit ?? (() => { });
            ServerControl.saveAndRestart = saveAndRestart ?? (() => { });
            ServerControl.quitWithoutSaving = quitWithoutSaving ?? (() => { });
            ServerControl.restartWithoutSaving = restartWithoutSaving ?? (() => { });
        }
    }
}
