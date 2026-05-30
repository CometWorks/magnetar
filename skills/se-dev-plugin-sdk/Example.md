# Annotated Example

A medium-sized config class that exercises every option category and the
layout tree. Copy and prune for your plugin.

```csharp
using System.Collections.Generic;
using PluginSdk.Config;
using PluginSdk.Tools;
using VRage;
using VRageMath;

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
        // ---- Scalar properties --------------------------------------------

        [BoolOption("Enable the plugin", Parent = "server-left")]
        public bool Enabled { get; set => SetField(ref field, value); } = true;

        [StringOption(maxLength: 64, description: "Display name", Parent = "server-left")]
        public string ServerName { get; set => SetField(ref field, value); } = "Unnamed";

        [IntOption(1, 240, "Ticks per second", Parent = "server-right")]
        public int TickRate { get; set => SetField(ref field, value); } = 60;

        [DoubleOption(0.0, 4.0, "Gravity multiplier", Parent = "server-right")]
        public double Gravity { get; set => SetField(ref field, value); } = 1.0;

        // ---- Enum property -----------------------------------------------

        [EnumOption("Log verbosity", Parent = "server-right")]
        public LogLevel LogLevel { get; set => SetField(ref field, value); } = LogLevel.Info;

        // ---- Struct property ---------------------------------------------

        [StructOption(description: "Port range", Parent = "limits")]
        public Range AllowedPorts { get; set => SetField(ref field, value); }
            = new Range { Min = 27000, Max = 27100 };

        // ---- Collections --------------------------------------------------

        [ListOption(description: "Free-form tags", Parent = "collections")]
        public List<string> Tags { get; set => SetField(ref field, value); } = new List<string>();

        [DictOption(description: "Per-player quotas", Parent = "collections")]
        public SerializableDictionary<string, int> Quotas { get; set => SetField(ref field, value); }
            = new SerializableDictionary<string, int>();

        [ListOption(description: "Policy tree",
                    TreeParentField = nameof(PolicyNode.ParentId),
                    Parent = "collections")]
        public List<PolicyNode> Policy { get; set => SetField(ref field, value); } = new List<PolicyNode>();

        // ---- VRage value types -------------------------------------------

        [ColorOption(ColorFormat.Rgb, "HUD accent", Parent = "server-right")]
        public Color HudColor { get; set => SetField(ref field, value); } = Color.Cyan;

        [Vector3DOption("World offset", Parent = "limits")]
        public Vector3D WorldOffset { get; set => SetField(ref field, value); } = Vector3D.Zero;

        [PositionAndOrientationOption("Default spawn pose", Parent = "limits")]
        public MyPositionAndOrientation SpawnPose { get; set => SetField(ref field, value); }
            = new MyPositionAndOrientation(Vector3D.Zero, Vector3.Forward, Vector3.Up);
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
