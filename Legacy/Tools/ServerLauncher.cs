using System.Threading;
using Pulsar.Shared.Arguments;

namespace Magnetar.Legacy;

/// <summary>
/// Single-instance guard for the Magnetar launcher. A separate mutex from
/// Pulsar's (Pulsar.Legacy / Pulsar.Modern) so a Pulsar game client and a
/// Magnetar server can coexist on the same machine. Multi-server hosts pass
/// -multiInstance (each server on its own -path / -config).
/// </summary>
internal static class ServerLauncher
{
    private const string MutexName = "Magnetar.Legacy";

    private static Mutex mutex;
    private static bool ownsMutex;

    public static bool IsOtherMagnetarRunning()
    {
        if (Flags.Current.MultiInstance)
            return false;

#if NETFRAMEWORK
        mutex = new Mutex(false, MutexName);
#else
        NamedWaitHandleOptions options = new()
        {
            CurrentUserOnly = true,
            CurrentSessionOnly = false,
        };
        mutex = new Mutex(false, MutexName, options);
#endif

        try
        {
            ownsMutex = mutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            ownsMutex = true;
        }

        return !ownsMutex;
    }

    public static void ReleaseInstanceLock()
    {
        if (ownsMutex)
            mutex.ReleaseMutex();

        mutex?.Dispose();
        mutex = null;
        ownsMutex = false;
    }
}
