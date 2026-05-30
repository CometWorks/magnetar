# Annotated Example

A medium-sized config class that exercises every option category and the
layout tree. Copy and prune for your plugin.

```csharp
using System.Collections.Generic;
using PluginSdk.Config;
using PluginSdk.Tools;

namespace MyPlugin
{
    // ---- Struct values used by some options -----------------------------

    public struct Range
    {
        [StructMember("Lower bound")] public int Min;
        [StructMember("Upper bound")] public int Max;
    }

    public struct PolicyNode
    {
        [StructMember] public int Id;
        [StructMember] public int ParentId;                       // <-- tree shape
        [StructMember, StructCaption] public string Label { get; set; }   // <-- row caption
    }

    // ---- Enum used by an option -----------------------------------------

    public enum LogLevel
    {
        [EnumCaption("Debug (verbose)")] Debug,
        Info,                                            // caption = "Info"
        [EnumCaption("Warning")]         Warn,
        [EnumCaption("Error")]           Error,
    }

    // ---- The config class -----------------------------------------------

    [Tab("general",  caption: "General")]
    [Tab("advanced", caption: "Advanced")]
    [Section("server",      parent: "general",  caption: "Server")]
    [Section("limits",      parent: "general",  caption: "Limits")]
    [Section("collections", parent: "advanced", caption: "Collections")]
    [Column("server-left",  parent: "server",   caption: "Left")]
    [Column("server-right", parent: "server",   caption: "Right")]
    public class MyPluginConfig : PluginConfig
    {
        // Scalars
        private bool     enabled    = true;
        private string   serverName = "Unnamed";
        private int      tickRate   = 60;
        private double   gravity    = 1.0;
        private LogLevel logLevel   = LogLevel.Info;

        // Compound
        private Range allowedPorts = new Range { Min = 27000, Max = 27100 };

        // Collections of scalars
        private List<string> tags = new List<string>();
        private SerializableDictionary<string, int> quotas =
            new SerializableDictionary<string, int>();

        // Collection of structs as a tree
        private List<PolicyNode> policy = new List<PolicyNode>();

        // ---- Scalar properties --------------------------------------------

        [BoolOption("Enable the plugin", Parent = "server-left")]
        public bool Enabled
        {
            get => enabled;
            set => SetField(ref enabled, value);
        }

        [StringOption(maxLength: 64, description: "Display name", Parent = "server-left")]
        public string ServerName
        {
            get => serverName;
            set => SetField(ref serverName, value);
        }

        [IntOption(1, 240, "Ticks per second", Parent = "server-right")]
        public int TickRate
        {
            get => tickRate;
            set => SetField(ref tickRate, value);
        }

        [DoubleOption(0.0, 4.0, "Gravity multiplier", Parent = "server-right")]
        public double Gravity
        {
            get => gravity;
            set => SetField(ref gravity, value);
        }

        // ---- Enum property -----------------------------------------------

        [EnumOption("Log verbosity", Parent = "server-right")]
        public LogLevel LogLevel
        {
            get => logLevel;
            set => SetField(ref logLevel, value);
        }

        // ---- Struct property ---------------------------------------------

        [StructOption(description: "Port range", Parent = "limits")]
        public Range AllowedPorts
        {
            get => allowedPorts;
            set => SetField(ref allowedPorts, value);
        }

        // ---- Collections --------------------------------------------------

        [ListOption(description: "Free-form tags", Parent = "collections")]
        public List<string> Tags
        {
            get => tags;
            set => SetField(ref tags, value);
        }

        [DictOption(description: "Per-player quotas", Parent = "collections")]
        public SerializableDictionary<string, int> Quotas
        {
            get => quotas;
            set => SetField(ref quotas, value);
        }

        [ListOption(description: "Policy tree",
                    TreeParentField = nameof(PolicyNode.ParentId),
                    Parent = "collections")]
        public List<PolicyNode> Policy
        {
            get => policy;
            set => SetField(ref policy, value);
        }
    }
}
```

## What Quasar sees

- Two tabs: **General** and **Advanced**.
- Inside **General**: a **Server** section split into **Left** and **Right**
  columns (Right also holds a `LogLevel` drop-down whose entries are the
  `[EnumCaption]` captions); and a **Limits** section with the port-range
  struct editor.
- Inside **Advanced**: a **Collections** section with a list-of-strings
  editor, a key/value editor for `Quotas`, and a tree editor for `Policy`.
- Each field shows its description as help text. Numeric and string
  constraints (`min`, `max`, `maxLength`) are honored by the editors.

## Editing this config at runtime

```csharp
// Single field — straightforward.
config.TickRate = 30;

// List — rebuild and reassign, see Mutation.md.
config.Tags = new List<string>(config.Tags) { "new-tag" };

// Dict — copy, mutate copy, reassign.
var q = new SerializableDictionary<string, int>(config.Quotas);
q["alice"] = 100;
config.Quotas = q;

// Struct — copy, mutate copy, reassign.
var ports = config.AllowedPorts;
ports.Max = 27200;
config.AllowedPorts = ports;
```

## Persisting

```csharp
// Local file
ConfigStorage.SaveXml(config, "config.xml");
var loaded = ConfigStorage.LoadXml<MyPluginConfig>("config.xml");

// Wire format
var json = ConfigStorage.SaveJson(config);
var fromWire = ConfigStorage.LoadJson<MyPluginConfig>(json);
```
