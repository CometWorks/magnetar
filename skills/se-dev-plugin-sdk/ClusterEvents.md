# Cluster Events: Nodes, the World Authority and Partition Ownership

`PluginClusterClient.ClusterEvent` reports changes in the cluster around the plugin, and
`NodesAsync()` lists the processes that make it up right now.

```csharp
var cluster = PluginCluster.ForPlugin("my.plugin");
cluster.ClusterEvent += change =>
{
    switch (change.Kind)
    {
        case PluginClusterEventKind.NodeDown: Forget(change.Node, change.Incarnation); break;
        case PluginClusterEventKind.PartitionLost: StopWorkIn(change.Partition); break;
    }
};
IReadOnlyList<PluginNodeInfo> nodes = await cluster.NodesAsync();   // node, incarnation, role
```

| Kind | Raised on | Carries | Source and latency |
|---|---|---|---|
| `NodeUp` | every subscribed process | `Node`, `Incarnation`, `Role` | Registry roster, polled every 5 s while someone subscribes. |
| `NodeDown` | every subscribed process | the incarnation that left | Same poll. A node leaves the ready set when it stops, drains, restarts or loses its lease, so a crash shows once its lease expires. |
| `WorldAuthorityChanged` | every process | `Generation` | The node's registry heartbeat (sub-second to a few seconds). |
| `PartitionAcquired` | the node that now owns it | `Partition`, `Generation` | This node's ownership, checked every 250 ms. A handover, split, merge or recovery all acquire. |
| `PartitionLost` | the node that owned it | `Partition`, the last `Generation` | Same. |

- **Game thread.** Every event is raised on the game thread; an observer that throws is skipped without
  affecting other plugins.
- **Changes only.** The first roster after subscribing is taken silently, so a subscriber does not get a
  `NodeUp` for every node already there: read `NodesAsync()` for the starting state.
- **A restart is two events:** `NodeDown` for the old incarnation and `NodeUp` for the new one. Key state
  by (node, incarnation).
- **Events are hints, not fences.** Before writing on behalf of a partition, resolve its owner fence
  ([SharedState.md](SharedState.md)); an event can arrive after the fact it reports has changed again.
- `ContextChanged` still fires on any change of this node's context; the events say what changed.

## Plain dedicated server

No event is ever raised. `NodesAsync()` returns the one process, `"standalone"`. In a cluster process
whose cluster build has no event support, or while the registry cannot be reached, `NodesAsync()`
returns null (unknown), never an empty list.
