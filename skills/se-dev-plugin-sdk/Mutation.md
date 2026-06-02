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

Mutate the contents in place, then **call `NotifyChanged`** with the name of
the property whose value changed. That raises `PropertyChanged` explicitly, so
the change is observed even though the setter never ran.

`NotifyChanged` is a public method on `PluginConfig`. It notifies
unconditionally — there is no equality check, because you are stating that the
structured value changed.

## Correct patterns

```csharp
// Add to a list
config.Names.Add("added");
config.NotifyChanged(nameof(config.Names));

// Update a dictionary entry
config.Counters["foo"] = 1;
config.NotifyChanged(nameof(config.Counters));

// Modify a struct field
var bounds = config.Bounds;
bounds.Max = 10;
config.Bounds = bounds;   // struct value type: reassign as usual
```

A struct is a value type, so editing a *scalar field* still means copy / edit /
reassign — and that reassignment already notifies through the setter. Use
`NotifyChanged` when the in-place edit is on a **reference** held by the option
(a list or dictionary), including ones nested inside a struct (see below).

## Incorrect — silently ignored

```csharp
config.Names.Add("added");      // mutates in place, but no NotifyChanged: no notification
config.Counters["foo"] = 1;     // mutates in place, but no NotifyChanged: no notification
```

The mutation itself is fine; forgetting the follow-up `NotifyChanged` is the
bug.

## Nested mutation

When a struct contains a list or dictionary, the struct copy you read back
shares the *same* collection reference, so you can mutate the inner collection
in place and then raise a single notification for the top-level property.

```csharp
config.Bounds.Tags.Add(42);              // inner list is shared with the option
config.NotifyChanged(nameof(config.Bounds));
```

One `NotifyChanged` for the top-level property is enough — there is no
per-level notification to fire.

## Why this design

The constraint is what makes change detection cheap and explicit:

- No collection proxies, no `ObservableCollection<T>`, no surprises about
  which mutations notify and which do not.
- The plugin states exactly when a structured value changed, by calling
  `NotifyChanged` — change detection never has to guess.
- "Did this option change?" reduces to "did the setter fire, or did the plugin
  call `NotifyChanged`?", so the host can stream diffs to Quasar by simply
  watching `PropertyChanged`.
