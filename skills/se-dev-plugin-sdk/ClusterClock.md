# Cluster Clock: One Game Time and One Clock for Every Node

`PluginClusterClient.Time()` tells a plugin what time it is on the server and whether it can rely on
that time matching the other nodes.

```csharp
var time = PluginCluster.ForPlugin("my.plugin").Time();
if (time != null && time.Synchronized)
{
    DateTime inGame = time.GameDateTime;            // same on every node; the sun follows it
    double stamp = time.ClockMilliseconds;           // compare with stamps taken on other nodes
}
```

| Field | Meaning |
|---|---|
| `GameTime` / `GameDateTime` | `MySession.ElapsedGameTime` and 2081-01-01 plus it. Every node follows the World Authority's, so the sun, day/night and anything derived from game time agree across nodes. An admin time-of-day change steps it. |
| `ClockMilliseconds` | A clock that never steps, not even for a time-of-day change, shared by all nodes: stamps from different nodes compare (within the sync error, tens of ms). Only differences are meaningful. Use it for timeouts, ordering and expiry that cross nodes. |
| `Synchronized` | This process follows the cluster clock. A node is unsynchronized for a moment after start, until its first clock sample; its times are then its own. |
| `Authoritative` | This process defines the time: the World Authority, or a plain server. |

Call on the game thread. Null means a cluster process whose cluster build has no clock support.

## Plain dedicated server

`GameTime` is the session's game time (zero before a session loads), `ClockMilliseconds` is the process
uptime clock, and both flags are true.

## Changing the time of day

Do not set `MySession.ElapsedGameTime` or `IMySession.GameDateTime` on a cluster node: the node's game
time is not its own, and the next clock update undoes the change. The admin time-of-day setting goes
through the World Authority, which applies it to the whole cluster once.
