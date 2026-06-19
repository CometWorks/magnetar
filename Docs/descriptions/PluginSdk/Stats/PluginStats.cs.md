# PluginSdk/Stats/PluginStats.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Stats` · **Kind:** static class · **Lines:** 90

## Summary

The process-wide publish/subscribe hub for plugin statistics: a producer publishes a self-describing `StatsSnapshot` under a provider name, and a consumer reads the latest snapshot by name, lists the active providers, or subscribes to `Updated` to receive every publication as it happens. It is the generic transport every telemetry producer shares; it has no knowledge of what the numbers mean — the schema carried inside each snapshot describes them. The API is intentionally `public static` with no `InternalsVisibleTo` coupling, so a plugin calls it directly. Only the most recent snapshot per provider is kept, in a `ConcurrentDictionary`, so publishing and reading are thread-safe without external locking. There is no in-tree consumer yet; the intended one is the external Quasar Agent.

## Types

### `PluginStats` — static class, public

Holds the latest `StatsSnapshot` for each provider plus a change event. Both publication and reads go through a `ConcurrentDictionary<string, StatsSnapshot>`.

- **Fields (private static readonly):**
  - `Log` (`Logger`) — `Logger.Create("PluginStats")`; used only to report a faulting `Updated` subscriber
  - `Current` (`ConcurrentDictionary<string, StatsSnapshot>`) — most recent snapshot per provider name

- **Events:**
  - `Updated` (`event Action<string, StatsSnapshot>`, public static) — raised after every `Publish`, synchronously on the publishing thread, with the provider name and the new snapshot; each subscriber on the invocation list is called inside its own try/catch, so one that throws is logged via `Log.Error` and blocks neither the other subscribers nor the publisher

- **Properties (public static):**
  - `Providers` (`IReadOnlyList<string>`) — a point-in-time copy of the provider names that currently hold a snapshot, safe to enumerate while publishing continues

- **Methods (public static):**
  - `Publish(string provider, StatsSnapshot snapshot)` — stores `snapshot` as the current one for `provider`, replacing any previous, then notifies `Updated`; throws `ArgumentException` when `provider` is null/empty and `ArgumentNullException` when `snapshot` is null
  - `TryGetSnapshot(string provider, out StatsSnapshot snapshot) → bool` — point-in-time read of the latest snapshot for one provider; `false` when the provider has published nothing or was cleared
  - `Clear(string provider)` — removes one provider's snapshot (e.g. when its plugin is disabled at runtime); a null/empty name is a no-op, the removal is idempotent, and it does not raise `Updated`

## Cross-references

- **Uses:** `PluginSdk/Stats/StatsSnapshot.cs` (`StatsSnapshot`, the published payload); `PluginSdk/Logging/Logger.cs` (`Logger`, to report a faulting `Updated` subscriber); `System.Collections.Concurrent` (`ConcurrentDictionary`)
- **Used by:** _none within the repository_
