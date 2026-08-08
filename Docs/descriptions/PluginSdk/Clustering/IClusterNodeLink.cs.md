# PluginSdk/Clustering/IClusterNodeLink.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Clustering` · **Kind:** interface · **Lines:** 21

## Summary

Defines the transport-independent Gateway data-link contract shared by the
DirectTransport provider and ClusterRuntime consumer. It exposes connection
state, global messages, client detach notifications, World Authority attach
validation, and global-message sending without exposing LiteNetLib types.

## Types

### `IClusterNodeLink` — interface, public

- `IsConnected` reports whether the authenticated Gateway link is live.
- `MessageReceived` carries Gateway global-state payloads.
- `ConnectionChanged` reports link transitions.
- `ClientDetached` reports removal of relayed client bindings.
- `AttachValidator` lets ClusterRuntime validate World Authority binding data.
- `Send(byte[])` sends a global-state payload and reports whether it was queued.

Callbacks execute on the transport thread and must return promptly.

## Cross-references

- **Uses:** `System.Action`, `System.Func`
- **Used by:** `ClusterNodeLink`
