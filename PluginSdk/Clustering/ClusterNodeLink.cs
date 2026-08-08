using System;
using System.Threading;

namespace PluginSdk.Clustering
{
    /// <summary>
    /// Process-wide rendezvous between the transport plugin and cluster runtime.
    /// </summary>
    public static class ClusterNodeLink
    {
        private static IClusterNodeLink current;

        public static IClusterNodeLink Current => Volatile.Read(ref current);

        public static bool Register(IClusterNodeLink service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            return Interlocked.CompareExchange(ref current, service, null) == null;
        }

        public static bool Unregister(IClusterNodeLink service)
        {
            if (service == null)
                return false;

            return ReferenceEquals(Interlocked.CompareExchange(ref current, null, service), service);
        }
    }
}
