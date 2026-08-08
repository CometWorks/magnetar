# PluginSdkTests/ClusterLifecycleTests.cs

**Project:** PluginSdkTests · **Namespace:** `PluginSdk.Tests` · **Kind:** tests

## Summary

Verifies that one registered lifecycle provider receives all four `ServerControl` termination
paths exactly once with the correct kind/save preference, no local delegate runs, provider
registration is exact-owner, provider failure returns `Unavailable`, and no provider preserves
standalone behavior.

## Cross-references

- **Uses:** `ClusterLifecycle`, `IClusterLifecycleProvider`, `ServerControl`, xUnit
