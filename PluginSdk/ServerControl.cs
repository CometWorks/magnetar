using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PluginSdk.Clustering;
using VRage.Utils;

// Only the host launcher may Bind the facade to its implementation.
[assembly: InternalsVisibleTo("MagnetarInterim")]
[assembly: InternalsVisibleTo("MagnetarLegacy")]
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
    /// <summary>
    /// Why the dedicated server is tearing the current process down, reported by
    /// <see cref="ServerControl.Terminating"/>. <see cref="Restart"/> means the
    /// server will come back (a fresh instance is launched / the image is
    /// replaced); <see cref="Shutdown"/> means it is meant to stay down.
    /// </summary>
    public enum ServerTerminationKind
    {
        /// <summary>An admin asked the server to stop and stay down, via the
        /// in-game <c>!quit</c> / <c>!stop</c> command.</summary>
        Shutdown,

        /// <summary>The server is restarting and will come back up
        /// (e.g. the <c>!restart</c> command).</summary>
        Restart,
    }

    public static class ServerControl
    {
        private static Func<bool> saveWorld = () => false;
        private static Func<bool> reloadConfig = () => false;
        private static Action saveAndQuit = () => { };
        private static Action saveAndRestart = () => { };
        private static Action quitWithoutSaving = () => { };
        private static Action restartWithoutSaving = () => { };

        /// <summary>
        /// Raised once when an admin drives the server's lifecycle from in-game —
        /// the <c>!quit</c>/<c>!stop</c> and <c>!restart</c> commands — carrying
        /// the intent (<see cref="ServerTerminationKind.Shutdown"/> vs
        /// <see cref="ServerTerminationKind.Restart"/>). It does NOT fire for OS
        /// signals (SIGTERM/SIGINT) or SE's internal exit path, which are not
        /// admin lifecycle intents. It fires on the thread performing the
        /// termination, while plugins are still loaded and the session is intact,
        /// and before any save or plugin disposal — so a handler may run a short
        /// blocking task (e.g. flush a network message) to convey the intent
        /// elsewhere. Handlers must return promptly: teardown continues once they
        /// have all run, and an exception thrown by one handler does not stop the
        /// others or the shutdown.
        /// </summary>
        public static event Action<ServerTerminationKind> Terminating;

        /// <summary>Saves the world without quitting. Returns <c>false</c> when
        /// no session is loaded or the host has not bound an implementation.</summary>
        public static bool SaveWorld() => saveWorld();

        /// <summary>Saves the world, then re-reads the dedicated config and
        /// applies the settings that are safe to change at runtime (the MOTD in
        /// particular). Does not quit. Returns <c>false</c> when no session is
        /// loaded or the host has not bound an implementation.</summary>
        public static bool ReloadConfig() => reloadConfig();

        /// <summary>Saves the world, then quits the process with exit code 0.
        /// A registered cluster lifecycle provider routes the request and fails closed.</summary>
        public static void SaveAndQuit()
        {
            RequestOrRun(ServerTerminationKind.Shutdown, true, saveAndQuit);
        }

        /// <summary>Saves the world, then replaces the process with a fresh
        /// instance launched with the original command line, environment and
        /// working directory captured when the server first started. A registered
        /// cluster lifecycle provider routes the request and fails closed.</summary>
        public static void SaveAndRestart()
        {
            RequestOrRun(ServerTerminationKind.Restart, true, saveAndRestart);
        }

        /// <summary>Quits the process immediately with exit code 0, without
        /// saving. A registered cluster lifecycle provider routes the
        /// request.</summary>
        public static void QuitWithoutSaving()
        {
            RequestOrRun(ServerTerminationKind.Shutdown, false, quitWithoutSaving);
        }

        /// <summary>Restarts the process immediately (original command line,
        /// environment and working directory), without saving. A registered
        /// cluster lifecycle provider routes the request and fails closed.</summary>
        public static void RestartWithoutSaving()
        {
            RequestOrRun(ServerTerminationKind.Restart, false, restartWithoutSaving);
        }

        private static void RequestOrRun(ServerTerminationKind kind, bool saveFirst, Action standaloneAction)
        {
            var request = new ClusterLifecycleRequest(Guid.NewGuid(), kind,
                ClusterLifecycleOrigin.Plugin, saveFirst, reason: "PluginSdk ServerControl");
            if (!ClusterLifecycle.TryRequest(request, out Task<ClusterLifecycleAcknowledgement> acknowledgement))
            {
                standaloneAction();
                return;
            }

            _ = ObserveClusterAcknowledgement(acknowledgement);
        }

        private static async Task ObserveClusterAcknowledgement(
            Task<ClusterLifecycleAcknowledgement> pending)
        {
            ClusterLifecycleAcknowledgement acknowledgement = await pending.ConfigureAwait(false);
            if (acknowledgement.Disposition is ClusterLifecycleDisposition.Rejected
                or ClusterLifecycleDisposition.Unavailable)
                MyLog.Default?.WriteLine($"Cluster lifecycle request denied: {acknowledgement.ReasonCode}: {acknowledgement.Message}");
        }

        /// <summary>Host-only: raises <see cref="Terminating"/>, isolating each
        /// subscriber so a faulting or slow handler cannot derail the shutdown
        /// for the others.</summary>
        internal static void RaiseTerminating(ServerTerminationKind kind)
        {
            var handler = Terminating;
            if (handler == null)
                return;

            foreach (var subscriber in handler.GetInvocationList())
            {
                try
                {
                    ((Action<ServerTerminationKind>)subscriber)(kind);
                }
                catch(Exception e)
                {
                    // A plugin handler must never block or break teardown.
                    MyLog.Default.Error("Exception in Terminating handler", e);
                }
            }
        }

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
