# Global Commands: Run Once, Cluster-Wide

`ExecuteGlobalAsync` runs a plugin command on the World Authority **once per operation id**. A caller that
times out, loses its node or is retried by a player can send the same command again with the same id and
get the first outcome back, without the command running twice. Use it for grants, payouts, unique
names, one-off world events: anything where twice is a bug.

```csharp
var cluster = PluginCluster.ForPlugin("my.plugin");

// On the World Authority (and on a plain server): serve the command.
cluster.RegisterGlobalCommand("reward", payload =>
{
    var claim = Claim.Parse(payload);
    Bank.Credit(claim.Player, claim.Amount);        // runs once per operation id
    return Encoding.UTF8.GetBytes("ok");
});

// Anywhere: ask for it. Keep the id with the thing it is for, so a retry reuses it.
var result = await cluster.ExecuteGlobalAsync("reward", claimBytes, claim.OperationId, TimeSpan.FromSeconds(10));
switch (result.Code)
{
    case PluginResultCode.Success:  /* result.Payload - now or from an earlier run */ break;
    case PluginResultCode.Invalid:  /* the command threw: result.Error; retrying replays it */ break;
    case PluginResultCode.Conflict: /* this id was already used with a different payload */ break;
    default:                        /* not reached (Timeout, Unavailable...): retry with the SAME id */ break;
}
```

## Semantics

- **Where it runs:** on the World Authority's game thread, whichever node asked. The plugin must be loaded
  on the WA and have registered the command there; otherwise the answer is `Unsupported`. A registration
  on a regular node is harmless and never called.
- **Once per id:** the outcome - result or exception message - is recorded under (plugin, command,
  operation id) with a fingerprint of the payload. A repeat with the same payload gets the record; a
  repeat with another payload is `Conflict`.
- **How long it remembers:** the most recent 4096 global operations of the whole cluster (all plugins
  share the ledger with the cluster's own commands), saved in the World Authority's checkpoint, so it
  survives a WA restart that restores that checkpoint. A retry after that window runs again.
- **Right after a node starts** (the first seconds after it begins serving) the registry does not yet count
  it as ready: calls from it answer `Unavailable` or `Fenced`, and broadcasts from others leave it out. Retry.
- **Results are acknowledgements:** at most 4096 bytes (`MaxResultBytes`); a larger result fails the
  command. Put bulk data in [SharedState.md](SharedState.md) records.
- **Payload** up to 64 KB, as for requests. Topics that start with `global/` are reserved.

## Plain dedicated server

The command runs locally, once per id, remembered in memory for the process's lifetime (the 4096 most
recent). `RegisterGlobalCommand` needs the plugin's opt-in (`PluginCluster.ForPlugin`), like requests.

In a cluster process whose cluster build has no ledger, `RegisterGlobalCommand` returns null: a
command that might run twice is not offered.
