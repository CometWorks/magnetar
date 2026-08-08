using System;
using System.Threading;
using System.Threading.Tasks;
using VRage.Utils;

namespace PluginSdk.Clustering
{
    /// <summary>Where a local server lifecycle request originated.</summary>
    public enum ClusterLifecycleOrigin : byte
    {
        Plugin = 0,
        ChatCommand = 1,
    }

    /// <summary>Authoritative outcome of a cluster lifecycle request.</summary>
    public enum ClusterLifecycleDisposition : byte
    {
        Accepted = 0,
        Rejected = 1,
        AlreadyApplied = 2,
        Unavailable = 3,
    }

    /// <summary>A plugin or chat request to change this server process' lifecycle.</summary>
    public sealed class ClusterLifecycleRequest
    {
        public Guid RequestId { get; }
        public ServerTerminationKind Kind { get; }
        public ClusterLifecycleOrigin Origin { get; }
        public bool SaveFirst { get; }
        public ulong CallerId { get; }
        public string Reason { get; }

        public ClusterLifecycleRequest(Guid requestId, ServerTerminationKind kind,
            ClusterLifecycleOrigin origin, bool saveFirst, ulong callerId = 0, string reason = "")
        {
            if (requestId == Guid.Empty)
                throw new ArgumentException("Lifecycle request id cannot be empty", nameof(requestId));
            if (!Enum.IsDefined(typeof(ServerTerminationKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (!Enum.IsDefined(typeof(ClusterLifecycleOrigin), origin))
                throw new ArgumentOutOfRangeException(nameof(origin));
            if (reason != null && reason.Length > 512)
                throw new ArgumentException("Lifecycle reason cannot exceed 512 characters", nameof(reason));

            RequestId = requestId;
            Kind = kind;
            Origin = origin;
            SaveFirst = saveFirst;
            CallerId = callerId;
            Reason = reason ?? string.Empty;
        }
    }

    /// <summary>Correlated Gateway/Registry acknowledgement for a lifecycle request.</summary>
    public sealed class ClusterLifecycleAcknowledgement
    {
        public Guid RequestId { get; }
        public ClusterLifecycleDisposition Disposition { get; }
        public Guid OperationId { get; }
        public string ReasonCode { get; }
        public string Message { get; }
        public string NodeState { get; }

        public ClusterLifecycleAcknowledgement(Guid requestId, ClusterLifecycleDisposition disposition,
            Guid operationId, string reasonCode, string message, string nodeState = "")
        {
            if (requestId == Guid.Empty)
                throw new ArgumentException("Lifecycle acknowledgement request id cannot be empty", nameof(requestId));
            if (!Enum.IsDefined(typeof(ClusterLifecycleDisposition), disposition))
                throw new ArgumentOutOfRangeException(nameof(disposition));

            RequestId = requestId;
            Disposition = disposition;
            OperationId = operationId;
            ReasonCode = reasonCode ?? string.Empty;
            Message = message ?? string.Empty;
            NodeState = nodeState ?? string.Empty;
        }

        public static ClusterLifecycleAcknowledgement Unavailable(Guid requestId, string reasonCode, string message) =>
            new ClusterLifecycleAcknowledgement(requestId, ClusterLifecycleDisposition.Unavailable,
                Guid.Empty, reasonCode, message);
    }

    /// <summary>
    /// Cluster runtime provider. Completion means an authoritative acknowledgement arrived,
    /// not merely that a transport accepted bytes.
    /// </summary>
    public interface IClusterLifecycleProvider
    {
        Task<ClusterLifecycleAcknowledgement> RequestAsync(
            ClusterLifecycleRequest request, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Process-wide rendezvous for cluster lifecycle routing. Without a provider,
    /// <see cref="ServerControl"/> preserves standalone local behavior. Once a provider is
    /// registered, every failure remains handled and therefore fails closed.
    /// </summary>
    public static class ClusterLifecycle
    {
        private static IClusterLifecycleProvider current;

        public static bool Register(IClusterLifecycleProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            return Interlocked.CompareExchange(ref current, provider, null) == null;
        }

        public static bool Unregister(IClusterLifecycleProvider provider)
        {
            if (provider == null)
                return false;

            return ReferenceEquals(Interlocked.CompareExchange(ref current, null, provider), provider);
        }

        /// <summary>
        /// Routes through the registered provider. Returns false only when the process is standalone
        /// and no provider exists; provider errors become fail-closed unavailable acknowledgements.
        /// </summary>
        public static bool TryRequest(ClusterLifecycleRequest request,
            out Task<ClusterLifecycleAcknowledgement> acknowledgement,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            IClusterLifecycleProvider provider = Volatile.Read(ref current);
            if (provider == null)
            {
                acknowledgement = null;
                return false;
            }

            acknowledgement = RequestProtected(provider, request, cancellationToken);
            return true;
        }

        private static async Task<ClusterLifecycleAcknowledgement> RequestProtected(
            IClusterLifecycleProvider provider, ClusterLifecycleRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                Task<ClusterLifecycleAcknowledgement> pending =
                    provider.RequestAsync(request, cancellationToken);
                if (pending == null)
                    throw new InvalidOperationException("Cluster lifecycle provider returned no acknowledgement task");

                ClusterLifecycleAcknowledgement acknowledgement = await pending.ConfigureAwait(false);
                if (acknowledgement == null || acknowledgement.RequestId != request.RequestId)
                    throw new InvalidOperationException("Cluster lifecycle provider returned an invalid acknowledgement");
                return acknowledgement;
            }
            catch (OperationCanceledException)
            {
                return ClusterLifecycleAcknowledgement.Unavailable(request.RequestId,
                    "request_cancelled", "Cluster lifecycle request was cancelled.");
            }
            catch (Exception exception)
            {
                MyLog.Default?.Error("Cluster lifecycle request failed; denying local termination", exception);
                return ClusterLifecycleAcknowledgement.Unavailable(request.RequestId,
                    "provider_failure", "Cluster lifecycle provider failed.");
            }
        }
    }
}
