# Layout: Tabs, Sections, Columns

Layout is **optional**. With no layout containers declared the UI is a flat
vertical stack of options in property-declaration order — perfectly usable
for small configs.

For larger configs, declare layout containers on the class and attach each
option to a container via `Parent`. The result is a tree the UI walks.

## The three container kinds

All three are class-level attributes and share the same shape:
`Id`, `Parent`, `Caption`.

| Attribute | Behavior |
|---|---|
| `[Tab(id, parent, caption)]` | One tab in a tab strip. Tabs that share a parent become siblings of the same tab strip — only one is visible at a time. |
| `[Section(id, parent, caption)]` | A captioned group box. Sections that share a parent stack vertically. |
| `[Column(id, parent, caption)]` | A vertical column. Columns that share a parent lay out side-by-side. |

`Parent = null` (the default) makes the container a root of the layout tree.
`Caption` is what the user sees.

## How options attach

Each option attribute accepts `Parent = "<container-id>"`:

```csharp
[BoolOption("Enable", Parent = "general-left")]
public bool Enabled { ... }
```

An option whose `Parent` is `null` (or refers to no declared container) lands
at the root of the UI tree.

## Example tree

```csharp
[Tab("general",  caption: "General")]
[Tab("advanced", caption: "Advanced")]
[Section("scalars",     parent: "general",  caption: "Scalar values")]
[Section("collections", parent: "advanced", caption: "Collections")]
[Column("scalars-left",  parent: "scalars", caption: "Left")]
[Column("scalars-right", parent: "scalars", caption: "Right")]
public class MyConfig : PluginConfig
{
    [BoolOption("Flag",   Parent = "scalars-left")]  public bool Flag    { ... }
    [IntOption (0, 100,   "Count",  Parent = "scalars-left")]  public int  Count { ... }
    [StringOption(maxLength: 64, description: "Name", Parent = "scalars-right")]
                                                     public string Name  { ... }

    [ListOption(description: "Tags", Parent = "collections")]
    public List<string> Tags { ... }
}
```

The resulting layout:

```
Tab "General"
└── Section "Scalar values"
    ├── Column "Left"
    │   ├── Flag
    │   └── Count
    └── Column "Right"
        └── Name
Tab "Advanced"
└── Section "Collections"
    └── Tags
```

## Rules and gotchas

- **Container ids must be unique within the config class.** They are how
  options refer to their parent.
- **Caption is for humans; `Id` is the contract.** Renaming a caption is
  cosmetic; renaming an id will orphan every option that referenced it.
- **No code-level nesting.** The tree is encoded entirely by `Parent`
  references; the order of `[Tab]`/`[Section]`/`[Column]` attributes on the
  class is irrelevant — pick whatever reads best.
- **Container kinds can mix freely.** A `Tab` may contain `Section`s, a
  `Section` may contain `Column`s, a `Column` may contain another `Section`,
  etc. The UI honors the kind at each node.
- **Unattached options.** Any option without a `Parent` is rendered at the
  root, alongside whatever root containers exist. Useful for a "common" field
  outside the tab strip.
- **No conditional visibility.** There is no "show only when X" mechanism in
  the schema. If a field is irrelevant, leave it visible and use validation
  semantics in the plugin to ignore it.
