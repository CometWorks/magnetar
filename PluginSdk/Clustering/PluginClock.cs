#nullable disable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Sandbox.ModAPI;

namespace PluginSdk.Clustering
{
    /// <summary>The server's time as this process sees it (T-0256).</summary>
    public sealed class PluginClusterTime
    {
        /// <summary>Game time (MySession.ElapsedGameTime): the same on every node, and what the sun follows.
        /// An admin time-of-day change steps it.</summary>
        public TimeSpan GameTime { get; set; }
        /// <summary>The in-game date: 2081-01-01 plus <see cref="GameTime"/>.</summary>
        public DateTime GameDateTime => new DateTime(2081, 1, 1, 0, 0, 0, DateTimeKind.Utc) + GameTime;
        /// <summary>A monotonic millisecond clock that never steps (not even for time-of-day changes); in a
        /// cluster it is shared by every node, so stamps from different nodes compare. Only differences mean
        /// anything.</summary>
        public double ClockMilliseconds { get; set; }
        /// <summary>This process follows the cluster clock (always true on a plain server and on the WA; false
        /// on a node until its first clock sample, when its times are its own).</summary>
        public bool Synchronized { get; set; }
        /// <summary>This process defines the time: the World Authority, or a plain server.</summary>
        public bool Authoritative { get; set; }
    }

    /// <summary>Optional provider capability: the cluster clock.</summary>
    public interface IPluginClusterClockProvider
    {
        PluginClusterTime Time();
    }

    internal static class PluginLocalClock
    {
        private static readonly Stopwatch Uptime = Stopwatch.StartNew();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static PluginClusterTime Time()
        {
            var session = MyAPIGateway.Session;
            TimeSpan game = session == null ? TimeSpan.Zero : session.GameDateTime - new DateTime(2081, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return new PluginClusterTime { GameTime = game, ClockMilliseconds = Uptime.Elapsed.TotalMilliseconds,
                Synchronized = true, Authoritative = true };
        }
    }
}
