# PluginSdk/Clustering/IClusterNodeLink.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Clustering` · **Kind:** interface · **Lines:** 21

## Summary

Defines the transport-independent Gateway data-link contract shared by the
DirectTransport provider and ClusterRuntime consumer. It exposes connection
state, global messages, client detach notifications, World Authority attach
validation, dedicated lifecycle request/ack frames, and global-message sending
without exposing LiteNetLib types.

## Types

### `IClusterNodeLink` — interface, public

- `IsConnected` reports whether the authenticated Gateway link is live.
- `MessageReceived` carries Gateway global-state payloads.
- `ConnectionChanged` reports link transitions.
- `ClientDetached` reports removal of relayed client bindings.
- `LifecycleAcknowledged` carries a typed, correlated Gateway/Registry result.
- `AttachValidator` lets ClusterRuntime validate World Authority binding data.
- `Send(byte[])` sends a global-state payload and reports whether it was queued.
- `SendLifecycleRequest(ClusterLifecycleRequest)` sends a dedicated lifecycle
  request and reports transport submission only.

Callbacks execute on the transport thread and must return promptly.

## Cross-references

- **Uses:** `System.Action`, `System.Func`
- **Used by:** [ClusterNodeLink.cs](ClusterNodeLink.cs.md), [ClusterNodeLinkTests.cs](../../PluginSdkTests/ClusterNodeLinkTests.cs.md)
