using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace PluginSdk.Stats
{
    /// <summary>
    /// Reflection-based schema for a stats POCO, plus the capture routines that
    /// project live POCO instances into the serializable
    /// <see cref="StatInstance"/> / <see cref="StatGroup"/> shapes. It is the
    /// stats counterpart of <c>ConfigSchema</c>: a plugin declares a plain class
    /// whose properties carry <see cref="StatAttribute"/>s (and optionally one
    /// <see cref="StatLabelAttribute"/>), and this type turns that declaration
    /// into a <see cref="StatsSchemaData"/> a consumer can render, and into
    /// per-snapshot value arrays.
    ///
    /// <para>
    /// Build is cached per POCO type, so the reflection runs once and each
    /// publish interval only walks the cached <see cref="PropertyInfo"/>s.
    /// </para>
    /// </summary>
    public sealed class StatsSchema
    {
        private static readonly ConcurrentDictionary<Type, StatsSchema> Cache
            = new ConcurrentDictionary<Type, StatsSchema>();

        private readonly PropertyInfo labelProp;     // null when no [StatLabel]
        private readonly PropertyInfo[] valueProps;  // parallel to Data.Fields

        /// <summary>The serializable schema description for a consumer.</summary>
        public StatsSchemaData Data { get; }

        private StatsSchema(StatsSchemaData data, PropertyInfo labelProp, PropertyInfo[] valueProps)
        {
            Data = data;
            this.labelProp = labelProp;
            this.valueProps = valueProps;
        }

        /// <summary>Builds (and caches) the schema for <paramref name="pocoType"/>.</summary>
        public static StatsSchema Build(Type pocoType)
        {
            if (pocoType == null) throw new ArgumentNullException(nameof(pocoType));
            return Cache.GetOrAdd(pocoType, BuildUncached);
        }

        private static StatsSchema BuildUncached(Type pocoType)
        {
            PropertyInfo labelProp = null;
            string labelDescription = null;

            foreach (var prop in pocoType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead) continue;
                var label = prop.GetCustomAttribute<StatLabelAttribute>();
                if (label == null) continue;

                if (labelProp != null)
                    throw new InvalidOperationException(
                        $"{pocoType.Name} has more than one [StatLabel] property " +
                        $"('{labelProp.Name}' and '{prop.Name}'); only one is allowed.");
                if (prop.PropertyType != typeof(string))
                    throw new InvalidOperationException(
                        $"{pocoType.Name}.{prop.Name} is marked [StatLabel] but is {prop.PropertyType.Name}; " +
                        "the label property must be of type string.");

                labelProp = prop;
                labelDescription = label.Description;
            }

            var valueProps = new List<PropertyInfo>();
            var fields = new List<StatFieldInfo>();

            foreach (var prop in pocoType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead) continue;
                var stat = prop.GetCustomAttribute<StatAttribute>();
                if (stat == null) continue;

                valueProps.Add(prop);
                fields.Add(new StatFieldInfo
                {
                    Name = prop.Name,
                    Kind = stat.Kind,
                    Description = stat.Description,
                    Unit = stat.Unit,
                    Parent = stat.Parent,
                    AcrossInstances = stat.AcrossInstances.ToString(),
                    OverTime = stat.OverTime.ToString(),
                });
            }

            var data = new StatsSchemaData
            {
                Name = pocoType.Name,
                LabelDescription = labelDescription,
                Fields = fields,
            };

            return new StatsSchema(data, labelProp, valueProps.ToArray());
        }

        /// <summary>Captures one POCO instance into a <see cref="StatInstance"/>.
        /// Booleans and enums are converted to <c>double</c> via
        /// <see cref="Convert.ToDouble(object)"/>.</summary>
        public StatInstance Capture(object poco)
        {
            if (poco == null) throw new ArgumentNullException(nameof(poco));

            var values = new double[valueProps.Length];
            for (int i = 0; i < valueProps.Length; i++)
                values[i] = Convert.ToDouble(valueProps[i].GetValue(poco));

            return new StatInstance
            {
                Label = labelProp != null ? (string)labelProp.GetValue(poco) : null,
                Values = values,
            };
        }

        /// <summary>Captures a (possibly empty) sequence of POCO instances into a
        /// <see cref="StatGroup"/> carrying this schema.</summary>
        public StatGroup CaptureGroup(IEnumerable instances)
        {
            if (instances == null) throw new ArgumentNullException(nameof(instances));

            var captured = new List<StatInstance>();
            foreach (var instance in instances)
                captured.Add(Capture(instance));

            return new StatGroup { Schema = Data, Instances = captured };
        }
    }

    /// <summary>
    /// Serializable description of a stats schema: the POCO name, the label's
    /// description (when the schema has a <see cref="StatLabelAttribute"/>
    /// property) and the ordered field definitions. Carried inline in every
    /// <see cref="StatGroup"/> so snapshots are self-describing.
    /// </summary>
    public sealed class StatsSchemaData
    {
        public string Name { get; set; }

        /// <summary>Description of the instance label, or <c>null</c> when the
        /// schema declares no label.</summary>
        public string LabelDescription { get; set; }

        /// <summary>Field definitions, positionally parallel to
        /// <see cref="StatInstance.Values"/>.</summary>
        public List<StatFieldInfo> Fields { get; set; } = new List<StatFieldInfo>();
    }

    /// <summary>
    /// Metadata for one stat field: its name, kind and the display/aggregation
    /// hints carried from the <see cref="StatAttribute"/>. The two aggregation
    /// hints are stored as enum member names (e.g. <c>"Sum"</c>, <c>"Median"</c>)
    /// so the wire form is stable against enum renumbering.
    /// </summary>
    public sealed class StatFieldInfo
    {
        public string Name { get; set; }
        public string Kind { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public string Parent { get; set; }
        public string AcrossInstances { get; set; }
        public string OverTime { get; set; }
    }
}
