# Offline Blockers: Keep a Partition Online While the Plugin Needs It

A cluster takes a partition offline once no player is within `offline.quietMeters` and its grids are
at rest. Nothing simulates there until someone comes back or it is loaded on demand. A static grid
with a job that must go on, like a shipyard building a ship for a player who left, has to say so.

```csharp
var cluster = PluginCluster.ForPlugin("my.plugin");
IDisposable blocker = cluster.RegisterOfflineBlocker(
    entityId => Yard.HasJob(entityId) ? "job" : null
);
```

The node asks the blocker about each member of a partition it could take offline: top-level grids,
floating objects, inventory bags and characters, by entity id. A reason, any string that isn't null or
empty, keeps the whole partition online. Asking again is how it ends: the first sweep that gets only
nulls lets the partition go after the usual `offline.graceSeconds`. Disposing the registration has the
same effect for all of them.

The reasons show on the node's `/offline` status and sweep summary as `<plugin>/<reason>`, counted per
partition. Keep the set of reasons small: it isn't a place for ids.

## Rules

- **Game thread, and fast.** It runs for every member of every partition a node hosts, every few
  seconds, so answer from what you already hold in memory.
- **An exception counts as a reason.** The partition stays online and the exception is logged once per
  registration. Staying online costs memory; going offline would stop a job.
- **One blocker per plugin.** Registering a second while the first is live throws; dispose the first.
- **Register early, and register on every node.** The cluster keeps nothing. Each node asks only
  the blockers registered in its own process, so the answer has to be right on whichever node holds
  the grid: after a node restart the plugin has to register again and know its jobs, and after a
  partition moves the new node's blocker is the one asked. Keep the jobs in a store that outlives both,
  such as [SharedState.md](SharedState.md) records, or carry them with [GridHandover.md](GridHandover.md).
- **It only holds a partition that is online.** A partition already offline stays there until a player
  or an explicit load brings it back. A node restart keeps nothing from going offline before the blocker
  is back only through the usual `offline.minOnlineSeconds` and `offline.graceSeconds`.
- **A cluster stop takes every partition offline regardless**, which is how a stop saves the world.

## Plain dedicated server and the World Authority

There is no offline sweep, so the blocker never fires. `RegisterOfflineBlocker` still succeeds on a plain
server and on the World Authority (a plugin needs no special case). In a cluster process it returns null
when the cluster build has no blocker support, or before the provider has started: wait for
`PluginCluster.Current` before taking null as "no support", and treat that as "partitions may go offline
under this plugin".

## A cluster or Magnetar without the call

The method is new in PluginSdk contract 1.1 (Magnetar after v2.4.3.8), and a cluster implements it from
the release that requires that contract. A plugin that has to run on older installs too looks it up with
reflection, since its source doesn't compile against an older PluginSdk, and picks its own fallback when
the method is missing or returns null.
