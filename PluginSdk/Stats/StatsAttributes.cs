using System;

namespace PluginSdk.Stats
{
    /// <summary>
    /// How a stat value is combined <b>across the instances</b> of a group at a
    /// single point in time. A group is the (dynamically sized) set of POCOs a
    /// provider publishes for one schema — e.g. one row per connected client.
    /// </summary>
    public enum StatAggregation
    {
        /// <summary>Not meaningfully combinable across instances (e.g. an id or
        /// an epoch counter); a consumer should keep the per-instance values.</summary>
        None,

        /// <summary>Add the values (e.g. per-client byte rates sum to the server total).</summary>
        Sum,

        /// <summary>Average the values across instances.</summary>
        Mean,

        /// <summary>Smallest value across instances.</summary>
        Min,

        /// <summary>Largest value across instances.</summary>
        Max,
    }

    /// <summary>
    /// How a stat value is combined <b>over the time axis</b> — i.e. how a
    /// consumer should fold the successive snapshots of the same instance into a
    /// single number for a reporting bucket.
    /// </summary>
    public enum TimeAggregation
    {
        /// <summary>Keep the most recent sample; older samples are discarded
        /// (e.g. a configuration flag, a tick rate, a connection epoch).</summary>
        Last,

        /// <summary>Add the samples (e.g. an event count accumulated per interval).</summary>
        Sum,

        /// <summary>Arithmetic mean of the samples in the bucket.</summary>
        Mean,

        /// <summary>Smallest sample in the bucket.</summary>
        Min,

        /// <summary>Largest sample in the bucket.</summary>
        Max,

        /// <summary>Middle sample (50th percentile). Meaningful for discrete
        /// values where the mean is not.</summary>
        Median,

        /// <summary>The consumer should retain a percentile distribution
        /// (p50/p90/p99/...) rather than a single folded value.</summary>
        Percentiles,
    }

    /// <summary>
    /// Base class for the attributes that mark a numeric property of a stats
    /// POCO as a published value. It mirrors <c>ConfigOptionAttribute</c>: the
    /// concrete kinds (<see cref="CounterAttribute"/>, <see cref="GaugeAttribute"/>,
    /// <see cref="DiscreteAttribute"/>) set sensible per-kind aggregation
    /// defaults in their constructor, which any property can override through the
    /// named-argument syntax:
    ///
    /// <code>
    /// [Gauge("Server → client rate", Unit = "B/s")]
    /// public double DownBytesPerSec { get; set; }
    ///
    /// [Gauge(AcrossInstances = StatAggregation.Mean)]
    /// public double DownConfidence { get; set; }
    /// </code>
    ///
    /// <para>
    /// The two aggregation properties are non-nullable enums because attribute
    /// arguments cannot be <c>Nullable&lt;TEnum&gt;</c>; the per-kind default is
    /// applied in the constructor and the named-argument assignment (which runs
    /// after the constructor) overrides it when present.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class StatAttribute : Attribute
    {
        /// <summary>Human-readable description shown by a consumer's UI.</summary>
        public string Description { get; set; }

        /// <summary>Unit of the value, e.g. <c>"B/s"</c>, <c>"ms"</c>, <c>"s"</c>.
        /// Free-form; <c>null</c> when dimensionless.</summary>
        public string Unit { get; set; }

        /// <summary>Optional grouping hint for a consumer's UI (e.g. a section
        /// name). Independent of the schema/group structure.</summary>
        public string Parent { get; set; }

        /// <summary>How to combine this value across the instances of a group.</summary>
        public StatAggregation AcrossInstances { get; set; }

        /// <summary>How to combine this value over the time axis.</summary>
        public TimeAggregation OverTime { get; set; }

        protected StatAttribute(string description)
        {
            Description = description;
        }

        /// <summary>Stat kind discriminator: <c>counter</c>, <c>gauge</c> or
        /// <c>discrete</c>. Surfaced in the schema so a consumer can choose a
        /// default rendering.</summary>
        public abstract string Kind { get; }
    }

    /// <summary>
    /// A monotonically increasing count of events (e.g. packets sent). Counts add
    /// up both across instances and over time, so the defaults are
    /// <see cref="StatAggregation.Sum"/> / <see cref="TimeAggregation.Sum"/>.
    /// </summary>
    public sealed class CounterAttribute : StatAttribute
    {
        public CounterAttribute(string description = null) : base(description)
        {
            AcrossInstances = StatAggregation.Sum;
            OverTime = TimeAggregation.Sum;
        }

        public override string Kind => "counter";
    }

    /// <summary>
    /// An instantaneous measured quantity (e.g. a byte rate or a queue depth).
    /// Such quantities add across instances but are averaged over time, so the
    /// defaults are <see cref="StatAggregation.Sum"/> /
    /// <see cref="TimeAggregation.Mean"/>.
    /// </summary>
    public sealed class GaugeAttribute : StatAttribute
    {
        public GaugeAttribute(string description = null) : base(description)
        {
            AcrossInstances = StatAggregation.Sum;
            OverTime = TimeAggregation.Mean;
        }

        public override string Kind => "gauge";
    }

    /// <summary>
    /// A discrete or categorical value for which averaging is meaningless (e.g.
    /// an enum-like mode, a connection epoch, a boolean flag). It is not summed
    /// across instances by default and is folded with the median over time, where
    /// min/max/percentiles also remain meaningful. Defaults are
    /// <see cref="StatAggregation.None"/> / <see cref="TimeAggregation.Median"/>.
    /// </summary>
    public sealed class DiscreteAttribute : StatAttribute
    {
        public DiscreteAttribute(string description = null) : base(description)
        {
            AcrossInstances = StatAggregation.None;
            OverTime = TimeAggregation.Median;
        }

        public override string Kind => "discrete";
    }

    /// <summary>
    /// Marks the single <c>string</c> property of a stats POCO whose value labels
    /// each captured instance (e.g. <c>"server"</c>, or a client's Steam id). At
    /// most one property per POCO may carry this attribute; when absent, captured
    /// instances have a <c>null</c> label.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class StatLabelAttribute : Attribute
    {
        /// <summary>Human-readable description of what the label identifies.</summary>
        public string Description { get; set; }

        public StatLabelAttribute(string description = null)
        {
            Description = description;
        }
    }
}
