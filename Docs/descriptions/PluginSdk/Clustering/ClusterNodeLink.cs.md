# PluginSdk/Clustering/ClusterNodeLink.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Clustering` · **Kind:** static class · **Lines:** 31

## Summary

Provides the process-wide rendezvous for one `IClusterNodeLink` provider. The
transport registers its service, ClusterRuntime reads `Current`, and disposal
can only unregister the same instance. Atomic operations prevent two transport
owners from silently replacing each other.

## Types

### `ClusterNodeLink` — static class, public

- `Current` returns the registered provider or `null`.
- `Register(IClusterNodeLink)` atomically installs the first provider, rejects a
  competing provider, and throws for `null`.
- `Unregister(IClusterNodeLink)` clears the provider only when the exact same
  instance is still registered.

## Cross-references

- **Uses:** `IClusterNodeLink`, `Interlocked`, `Volatile`
- **Used by:** [ClusterNodeLinkTests.cs](../../PluginSdkTests/ClusterNodeLinkTests.cs.md)
