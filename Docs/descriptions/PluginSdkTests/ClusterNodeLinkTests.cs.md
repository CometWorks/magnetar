# PluginSdkTests/ClusterNodeLinkTests.cs

**Project:** PluginSdkTests · **Namespace:** `PluginSdk.Tests` · **Kind:** test class · **Lines:** 41

## Summary

Specifies the single-provider lifecycle of `ClusterNodeLink`: the first
provider wins, another provider cannot replace or unregister it, and the exact
provider can unregister cleanly.

## Types

### `ClusterNodeLinkTests` — class, public

- `Register_and_unregister_require_the_same_provider()` verifies registration,
  collision rejection, identity-safe unregistration, and cleanup.
- `TestNodeLink` is a no-op `IClusterNodeLink` fixture.

## Cross-references

- **Uses:** `PluginSdk.Clustering.ClusterNodeLink`, `IClusterNodeLink`, xUnit
- **Used by:** _none_
