# Shared Directory: One Folder per Plugin, Local or Cluster-Wide

`PluginSdk.Storage.PluginStorage` hands a plugin its own directory for files that every process of
the server must see. It is the file-based sibling of [SharedState.md](SharedState.md): use shared
state for small, fenced records and this directory for files and bulk data.

```csharp
using PluginSdk.Storage;

string dir = PluginStorage.GetSharedDirectory("my.plugin");   // your own id; <root>/plugins/my.plugin-<hash>, created
string tmp = Path.Combine(dir, "index.json.tmp");
File.WriteAllText(tmp, json);
File.Move(tmp, Path.Combine(dir, "index.json"), overwrite: true);   // atomic replace
```

## Where the root is

| Process | `PluginStorage.Root` | `IsClusterShared` |
|---|---|---|
| Plain dedicated server under Magnetar | `<Magnetar config dir>/PluginShared` (set by the launcher) | `false` |
| No launcher configured it | `<LocalApplicationData>/Magnetar/PluginShared` | `false` |
| Cluster node or World Authority | `CLUSTER_SHARED_ROOT`, the same path on every node | `true` |
| Cluster process without `CLUSTER_SHARED_ROOT` | `null`: `GetSharedDirectory` throws | `true` |

A cluster process is one with `CLUSTER_NODE_ID` or `CLUSTER_GATEWAY_REGISTRY` set, the same test as
`PluginCluster.IsClusterProcess`. The same call works in all four cases, so a plugin needs no cluster
code and loads on a plain server unchanged.

## Rules

- **The plugin id is your own.** Pass the id the loader bound your assembly to, the same one
  `PluginCluster.ForPlugin` takes; any other id throws `InvalidOperationException`, so a plugin cannot
  open another plugin's folder. Every id becomes a file-safe prefix of itself plus `-` and 16 hex digits
  of the SHA-256 of the exact id (`PluginStorage.FolderName(id)`). The folder never leaves
  `<root>/plugins`, and two ids never share one: not an id that spells another id's folder, and not
  ids that differ only in case on a case-insensitive filesystem. Use `FolderName` rather than
  building the path yourself.
- **Nothing locks the folder.** In a cluster, the nodes and the World Authority are separate
  processes writing the same files. Write under a temporary name in the same directory and rename
  into place. Give each file one writer, e.g. name it by the node id from `PluginCluster.Current.Context`,
  or coordinate through the plugin's own messages.
- **Cluster-wide means one filesystem.** The cluster launcher points `CLUSTER_SHARED_ROOT` at a
  directory under its run root. A cluster spread over several hosts needs that path to be the same
  shared mount on every host; the SDK cannot detect a per-host directory posing as a shared one.
- **A cluster without shared storage refuses** rather than handing out a directory only one node
  sees, which would split the plugin's state silently.
