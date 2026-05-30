# Mutation Patterns — Critical Reading

`PluginConfig` raises `PropertyChanged` from the **top-level property setter**
only. Lists, dictionaries, and structs cannot observe in-place mutations of
their contents, so any change that bypasses the setter is invisible to
everything downstream — XML save, JSON envelope, Quasar push, plugin
listeners.

This is the single most common mistake when writing plugins against
`PluginSdk`. Read this page before writing code that edits collection or
struct values.

## The rule

To make a change observable, **reassign the whole property**. Build a new
collection or copy the struct, mutate the copy, then assign it back.

The default equality check in `SetField` is `EqualityComparer<T>.Default`,
which for collections is reference equality — so a fresh instance always
counts as "changed" and the notification fires.

## Correct patterns

```csharp
// Add to a list
config.Names = new List<string>(config.Names) { "added" };

// Update a dictionary entry
var counters = new SerializableDictionary<string, int>(config.Counters);
counters["foo"] = 1;
config.Counters = counters;

// Modify a struct field
var bounds = config.Bounds;
bounds.Max = 10;
config.Bounds = bounds;
```

## Incorrect — silently ignored

```csharp
config.Names.Add("added");      // mutates in place: no notification
config.Counters["foo"] = 1;     // mutates in place: no notification

config.Bounds.Max = 10;
// Won't even compile for a struct property — the compiler enforces the
// copy/reassign pattern in that case. Lists and dicts give you no such
// safety net.
```

## Nested mutation

When a struct contains a list, a dictionary, or another struct, **every level
must be rebuilt and reassigned**. There is no propagation: mutating an inner
collection or modifying a nested struct field in place is invisible at every
level. Only the top-level property setter raises `PropertyChanged`.

```csharp
var outer = config.Bounds;                          // copy struct
var inner = new List<int>(outer.Tags) { 42 };       // rebuild list
outer.Tags = inner;                                 // assign into struct copy
config.Bounds = outer;                              // assign into property
```

## Why this design

The constraint is what makes change detection cheap and explicit:

- No collection proxies, no `ObservableCollection<T>`, no surprises about
  which mutations notify and which do not.
- Round-tripping through `EqualityComparer<T>.Default` is enough to gate
  notifications, regardless of value shape.
- "Did this option change?" reduces to "did the top-level setter fire?", so
  the host can stream diffs to Quasar by simply watching `PropertyChanged`.

## A small helper if you find yourself doing this often

```csharp
static void UpdateList<T>(PluginConfig owner, Expression<Func<List<T>>> prop, Action<List<T>> mutate)
{ ... }
```

A few plugins have written wrappers like the above. There is no
sanctioned helper in `PluginSdk` — keep the rebuild-and-reassign pattern
visible at call sites unless your config sees heavy edits.
