# Broadcast: One Message to Every Node

`PluginClusterClient.BroadcastAsync` sends one message to your plugin's handler for a topic on every
live process of the server: each cluster node, the World Authority, and the sending node itself. It is
the fan-out sibling of `RequestAsync` in [SharedState.md](SharedState.md). Receiving is the same
`RegisterHandler(topic, handler)`, so a subscriber does not know or care whether a message came as a
request or as a broadcast.

```csharp
using PluginSdk.Clustering;

var cluster = PluginCluster.ForPlugin("my.plugin");            // your own loader id

// Subscribe: once a provider is available (e.g. from Init, or on ContextChanged).
IDisposable subscription = cluster.RegisterHandler("prices", message =>
{
    Prices.Apply(message.Payload);                              // runs on the game thread
    return Task.FromResult<byte[]>(null);                      // optional per-node reply
});

// Publish: everyone, this node included.
var result = await cluster.BroadcastAsync("prices", payload, Guid.NewGuid(), TimeSpan.FromSeconds(5));
if (result.Code != PluginResultCode.Success)
    foreach (var (node, reply) in result.Nodes)
        if (reply.Code != PluginResultCode.Success) Log.Warning($"{node}: {reply.Code}");
```

## What you get back

`PluginBroadcastResult.Nodes` maps each node the message was addressed to onto that node's
`PluginResult`: `Success` with the handler's reply payload, or why that copy failed. `Code` is
`Success` only when every copy succeeded; otherwise it is the first failure. When nothing was sent,
`Nodes` is empty and `Code` says why:

| Code | Meaning |
|---|---|
| `Invalid` | Bad topic, payload over 64 KB, empty operation id, or a timeout outside (0, 30 s]. |
| `Unavailable` | No provider (the plugin opted in before one exists), or the registry cannot be reached. |
| `Unsupported` | The provider predates broadcast. |

Per node, `Unsupported` means that node has no handler registered for the topic (it has not
subscribed, or the plugin is not loaded there), and `Timeout` means no reply arrived in time.

## Semantics

- **Who receives it.** The registry's list of nodes that are ready at the moment of the call: registered,
  serving and holding a live lease. A node that is starting or draining is not on it and gets no copy;
  a node that joins later does not get old messages. Pass `includeWorldAuthority: false` to skip the WA.
- **At most once per node, per call.** Each copy is a fenced unicast to that node's current incarnation,
  validated by the registry before the handler runs, exactly like `RequestAsync`. A copy to a node that
  restarted meanwhile is refused (`Fenced`), not delivered to the new incarnation.
- **Retry with the same operation id.** A retry re-sends to everyone, so a handler that must not act
  twice keeps the recent `message.OperationId`s and ignores a repeat. Every copy of one broadcast
  carries the same operation id.
- **Right after a node starts** (the first seconds after it begins serving) the registry does not yet count
  it as ready: calls from it answer `Unavailable` or `Fenced`, and broadcasts from others leave it out. Retry.
- **No ordering** between broadcasts, or between the copies of one broadcast.
- **Handlers run on the game thread**, a few per frame, like request handlers; a slow handler delays
  the others, so hand heavy work to a task.

## Plain dedicated server

Without a cluster the one process is every node: `BroadcastAsync` delivers the message once to the
local handler (on the next frame) and `Nodes` holds one entry, `"standalone"`. A plugin needs no
cluster-specific code.

## How it works

The cluster provider asks the registry for the roster of ready nodes (`nodes`, protocol 1, one
`OwnerFence` per node) and sends one fenced `PluginMessage` to each over the existing node link. The
record and message protocols are unchanged; a broadcast is N ordinary requests with one roster lookup.
