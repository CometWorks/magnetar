using System;
using System.Threading;
using PluginSdk.Stats;

namespace PluginSdk.Clustering
{
    public sealed class PluginServiceDiagnostics
    {
        [StatLabel] public string Node { get; set; }
        [Discrete(AcrossInstances = StatAggregation.None)] public long Incarnation { get; set; }
        [Discrete(AcrossInstances = StatAggregation.None)] public bool Available { get; set; }
        [Gauge(AcrossInstances = StatAggregation.None)] public int Pending { get; set; }
        [Counter(AcrossInstances = StatAggregation.None)] public long Conflicts { get; set; }
        [Counter(AcrossInstances = StatAggregation.None)] public long Failures { get; set; }
        private long conflicts, failures;
        private DateTime next;
        public PluginResult Observe(PluginResult result)
        {
            if (result.Code == PluginResultCode.Conflict) Interlocked.Increment(ref conflicts);
            else if (result.Code != PluginResultCode.Success && result.Code != PluginResultCode.NotFound) Interlocked.Increment(ref failures);
            return result;
        }
        public void Publish(NodeContext context, int pending)
        {
            if (DateTime.UtcNow < next) return;
            next = DateTime.UtcNow.AddSeconds(1);
            Node = context.Node; Incarnation = context.Incarnation; Available = context.Available; Pending = pending;
            Conflicts = Interlocked.Read(ref conflicts); Failures = Interlocked.Read(ref failures);
            PluginStats.Publish("cluster-plugin-services", new StatsSnapshot { UtcTimestamp = DateTime.UtcNow,
                Groups = { StatsSchema.Build(typeof(PluginServiceDiagnostics)).CaptureGroup(new[] { this }) } });
        }
    }
}
