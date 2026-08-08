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
| `AttachValidator` | Runtime callback that accepts a Gateway client only when its World Authority binding is current. |
| `Send(byte[])` | Sends a non-empty global-state payload; returns `false` without a live link. |

Callbacks run on the transport thread. Keep them short and marshal game-state
mutation onto the server update thread where required. The registry accepts one
provider and never replaces it implicitly; this makes competing transport
owners visible instead of producing split routing.
