using System;
using System.Collections.Generic;

namespace PluginSdk.Stats
{
    /// <summary>
    /// One captured instance of a stats schema: a label (which row this is) and
    /// the numeric values, positionally parallel to
    /// <see cref="StatsSchemaData.Fields"/>. Every value is a <c>double</c> —
    /// booleans capture as 0/1 and enums as their underlying integer — so a
    /// consumer needs only the schema to interpret them.
    /// </summary>
    public sealed class StatInstance
    {
        /// <summary>Instance label from the POCO's <see cref="StatLabelAttribute"/>
        /// property, or <c>null</c> when the schema declares no label.</summary>
        public string Label { get; set; }

        /// <summary>Field values, in the same order as
        /// <see cref="StatsSchemaData.Fields"/>.</summary>
        public double[] Values { get; set; }
    }

    /// <summary>
    /// A schema together with the (dynamically sized) set of instances captured
    /// for it at one point in time — e.g. one <see cref="StatInstance"/> per
    /// connected client. Carrying the schema inline keeps a snapshot
    /// self-describing for a consumer that picks it up without prior knowledge.
    /// </summary>
    public sealed class StatGroup
    {
        /// <summary>The schema the <see cref="Instances"/> were captured against.</summary>
        public StatsSchemaData Schema { get; set; }

        /// <summary>Captured instances; may be empty (e.g. no clients connected).</summary>
        public List<StatInstance> Instances { get; set; } = new List<StatInstance>();
    }

    /// <summary>
    /// An immutable point-in-time publication from one provider: a UTC timestamp
    /// and one or more <see cref="StatGroup"/>s (e.g. a single-instance server
    /// group plus a multi-instance per-client group). Published through
    /// <see cref="PluginStats.Publish"/>.
    /// </summary>
    public sealed class StatsSnapshot
    {
        /// <summary>When the snapshot was captured (UTC).</summary>
        public DateTime UtcTimestamp { get; set; }

        /// <summary>The groups in this snapshot.</summary>
        public List<StatGroup> Groups { get; set; } = new List<StatGroup>();
    }
}
