# PluginSdk/Clustering/ClusterLifecycle.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Clustering` · **Kind:** contract + rendezvous

## Summary

Defines the typed, asynchronous authority boundary for plugin/chat lifecycle requests in a
cluster node. `ClusterLifecycle` admits one process-wide `IClusterLifecycleProvider`; without one,
`ServerControl` preserves standalone local behavior. Once registered, provider errors,
cancellation, invalid acknowledgements, and link failures become typed `Unavailable` results and
never authorize local fallback.

## Types

- `ClusterLifecycleOrigin` — distinguishes plugin calls from built-in chat commands.
- `ClusterLifecycleDisposition` — `Accepted`, `Rejected`, `AlreadyApplied`, or local
  `Unavailable`.
- `ClusterLifecycleRequest` — correlated request ID, shutdown/restart kind, origin, save
  preference, optional caller ID, and bounded reason.
- `ClusterLifecycleAcknowledgement` — request/operation IDs, disposition, stable reason code,
  operator message, and current node state.
- `IClusterLifecycleProvider` — asynchronous provider whose completion represents an
  authoritative acknowledgement rather than transport enqueue.
- `ClusterLifecycle` — exact-owner registration and fail-closed `TryRequest` routing.

## Cross-references

- **Uses:** `ServerTerminationKind`, `System.Threading`, `System.Threading.Tasks`, `VRage.Utils.MyLog`
- **Used by:** [MagnetarCommands.cs](../../Legacy/Commands/MagnetarCommands.cs.md), [IClusterNodeLink.cs](IClusterNodeLink.cs.md), [ServerControl.cs](../ServerControl.cs.md), [ClusterLifecycleTests.cs](../../PluginSdkTests/ClusterLifecycleTests.cs.md), [ClusterNodeLinkTests.cs](../../PluginSdkTests/ClusterNodeLinkTests.cs.md)
