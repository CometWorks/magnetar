# Cluster Node-Link Service

`PluginSdk.Clustering` is the narrow process-local boundary between the plugin
that owns Gateway transport and the plugin that owns clustered game behavior.
Normal server plugins do not need it.

The transport provider registers one `IClusterNodeLink` instance:

```csharp
using PluginSdk.Clustering;

IClusterNodeLink service = new MyTransportNodeLink();
if (!ClusterNodeLink.Register(service))
    throw new InvalidOperationException("A node-link provider already exists");
```

It must unregister the same instance during disposal:

```csharp
ClusterNodeLink.Unregister(service);
```

The cluster runtime resolves the provider after its transport dependency has
loaded:

```csharp
IClusterNodeLink link = ClusterNodeLink.Current
    ?? throw new InvalidOperationException("Cluster transport is unavailable");

link.ConnectionChanged += connected => UpdateGatewayHealth(connected);
link.MessageReceived += ReceiveGlobalState;
link.ClientDetached += RemovePlayerBinding;
link.LifecycleAcknowledged += ReceiveLifecycleAcknowledgement;
link.AttachValidator = ValidateWorldAuthorityBinding;
link.Send(serializedGlobalMessage);
```

## Contract

| Member | Meaning |
|---|---|
| `IsConnected` | A live authenticated Gateway data link currently exists. |
| `MessageReceived` | A global-state payload arrived from the Gateway. |
| `ConnectionChanged` | The authenticated Gateway link connected or disconnected. |
| `ClientDetached` | A relayed client binding was removed. |
| `LifecycleAcknowledged` | A typed, correlated Gateway/Registry lifecycle result arrived. |
| `AttachValidator` | Runtime callback that accepts a Gateway client only when its World Authority binding is current. |
| `Send(byte[])` | Sends a non-empty global-state payload; returns `false` without a live link. |
| `SendLifecycleRequest(ClusterLifecycleRequest)` | Sends a dedicated lifecycle frame; transport enqueue is not Registry acceptance. |

Callbacks run on the transport thread. Keep them short and marshal game-state
mutation onto the server update thread where required. The registry accepts one
provider and never replaces it implicitly; this makes competing transport
owners visible instead of producing split routing.

## Cluster lifecycle request/ack

Cluster nodes must not honor plugin or in-game restart/quit requests locally,
because doing so bypasses registry drain and fencing. The cluster runtime can
register one process-wide asynchronous provider:

```csharp
using PluginSdk;
using PluginSdk.Clustering;
using System.Threading;
using System.Threading.Tasks;

sealed class LifecycleProvider : IClusterLifecycleProvider
{
    public Task<ClusterLifecycleAcknowledgement> RequestAsync(
        ClusterLifecycleRequest request,
        CancellationToken cancellationToken)
        => SendToGatewayAndAwaitMatchingAck(request, cancellationToken);
}

var provider = new LifecycleProvider();
if (!ClusterLifecycle.Register(provider))
    throw new InvalidOperationException("A cluster lifecycle provider already exists");
```

Unregister the exact same delegate during disposal:

```csharp
ClusterLifecycle.Unregister(provider);
```

The acknowledgement identifies the request and reports `Accepted`, `Rejected`,
`AlreadyApplied`, or locally `Unavailable`, plus a stable reason code, operation
id, message, and node state. Completion means the Gateway/Registry replied;
transport enqueue alone is not success. A throwing, disconnected, cancelled, or
timed-out provider fails closed. With no provider registered, Magnetar keeps
normal standalone behavior.

The provider covers calls through `ServerControl` and Magnetar's built-in
`!quit`, `!stop`, and `!restart` commands. Built-in commands wait asynchronously
and show the authoritative result. It does not cover OS signals or the dedicated
server's internal exit path; those remain available to the process executor.
