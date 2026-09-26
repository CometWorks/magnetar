# Cluster Views: Players, Positions, and Where an Entity Lives

Three read-only views on `PluginClusterClient` answer the questions a plugin otherwise could only
answer for its own process: who is online anywhere, where they are, and whether this process may
change a given entity. Together with `ResolveOwnerAsync` and `Context.OwnedPartitions`
([SharedState.md](SharedState.md)) they cover "where is it, and who may touch it".

```csharp
var cluster = PluginCluster.ForPlugin("my.plugin");

foreach (var player in cluster.OnlinePlayers() ?? Array.Empty<PluginPlayerInfo>())
    Log.Info($"{player.Name} on {player.Node}");

var placement = cluster.LocateEntity(gridId);
if (placement?.Residence == PluginEntityResidence.Local)
    Rename(gridId);                                  // this process owns it
else if (placement?.Residence == PluginEntityResidence.Absent)
{
    // Not here. Ask the node that has it, e.g. through a request to its partition owner, or
    // BroadcastAsync (Broadcast.md) when the partition is not known.
}
```

| Call | Cluster | Plain server |
|---|---|---|
| `OnlinePlayers()` | Every player on every node, with `Node`; the World Authority's merged list as this process last received it (about 1-2 s behind). | This process's players, `Node = "standalone"`. |
| `PlayerPositions()` | Every online player: the controlled entity, else the character, else the player position; same staleness. Join with `OnlinePlayers()` on `IdentityId` for the node. | Local players. |
| `LocateEntity(id)` | This process's view of the entity: see below. | `Local` when the entity exists, else `Absent`; `Partition` is 0. |

All three are **game-thread calls**. They return **null** only in a cluster process whose cluster build
offers no views: that means *unknown*, never "nobody" - an empty list is the real answer "no players".
Bots are not players; cameras that keep an area awake are not positions.

## `LocateEntity`

Pass any entity id of a grid (a block's or subgrid's resolves to the top-level grid) or a character.
`EntityId` in the answer is the top-level entity.

| `Residence` | Meaning | May the plugin change it? |
|---|---|---|
| `Local` | In a partition this node owns and simulates. | Yes. `Partition` names it; `ResolveOwnerAsync` on it gives the fence for a fenced write. |
| `Frozen` | Owned here but mid-handover (leaving, or arriving and not yet committed). | Not now: the change may be lost with the copy that loses. Retry shortly. |
| `Unowned` | Exists here but in no partition this node owns (just spawned and not yet swept into one, or a copy). | No persisted changes. |
| `Absent` | Not in this process. On a cluster it is on another node, or nowhere. | No. |

The World Authority simulates no grids, so everything is `Absent` or `Unowned` there. There is no
cluster-wide "which node has entity X" lookup: a node knows only its own partitions. For players, use
the `Node` from `OnlinePlayers()`; for a known partition, `ResolveOwnerAsync`.
