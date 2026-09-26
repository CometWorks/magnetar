# Grid Handover: Keep Per-Grid Plugin State When a Grid Changes Node

In a cluster, a grid moves between nodes whenever its partition is handed over. The grid, its
blocks and its mod storage travel with it, but anything a plugin holds in memory about the grid stays
behind on the old node. `RegisterGridHandover` tells the plugin when a grid leaves and arrives, and
carries a small state blob per grid from the old node to the new one.

```csharp
sealed class Tracker : IPluginGridHandover
{
    public byte[] Leaving(long gridId) =>                      // source, while the grid is frozen
        Missions.TryGetValue(gridId, out var mission) ? mission.Serialize() : null;

    public void Left(long gridId) => Missions.Remove(gridId);  // source: it is on another node now
    public void Stayed(long gridId) { }                        // source: the move was called off

    public void Arrived(long gridId, byte[] state)            // target: live here now
    {
        if (state != null) Missions[gridId] = Mission.Deserialize(state);
    }
}

var cluster = PluginCluster.ForPlugin("my.plugin");
IDisposable hooks = cluster.RegisterGridHandover(new Tracker());
```

## Lifecycle of one move

| Node | Call | When |
|---|---|---|
| Source | `Leaving(grid)` | The grid is frozen for capture. Return its state, or null. |
| Source | `Left(grid)` | The target committed; the grid has been removed here. |
| Source | `Stayed(grid)` | The move was abandoned (capture failed or the handover aborted); the grid resumes here. |
| Target | `Arrived(grid, state)` | The grid is committed and live here; `state` is what `Leaving` returned. |

Every `Leaving` is followed by exactly one `Left` or `Stayed` on the same node. `Arrived` fires only
on a committed move; an import the target discards produces no call there. The grid keeps its
entity id, so the id is the key on both nodes.

## Rules

- **Game thread, and fast.** `Leaving` runs inside the capture, which holds the game thread: build the
  blob from what you already have in memory.
- **At most 16 KB per plugin per grid** (`PluginGridHandover.MaxStateBytes`); a larger state is dropped
  and logged, and the grid arrives with null. All plugins together get 1 MB per handover.
- **Null is a normal answer in `Arrived`.** The plugin gave none, or the handover was recovered after a
  node restart: the state travels with the live handover only, it is not saved. Durable per-grid data
  belongs in the grid itself (mod storage) or in [SharedState.md](SharedState.md) records.
- **Top-level grids only.** Each grid of a connected group (rotor, piston, connector) is its own call.
  Characters and floating objects are not reported.
- **An exception** in a hook is logged and skipped; it never fails the handover, and other plugins
  still get their calls.
- **One handler per plugin.** Registering a second while the first is live throws; dispose the first.

## Plain dedicated server and the World Authority

Grids never change process, so the hooks never fire. `RegisterGridHandover` still succeeds on a plain
server (a plugin needs no special case). In a cluster process whose cluster build has no hook support,
it returns null.

## How it works

The source asks each registered plugin at capture and appends the states, as JSON, to the handover's
network-state string, which the gateway forwards to the target unchanged. The target strips them before
importing the network ids and hands them out at commit. The capture frame, the gateway and the plugin
protocol are unchanged.
