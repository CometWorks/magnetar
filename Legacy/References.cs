using System.Collections.Generic;
using Pulsar.Shared;

namespace Pulsar.Legacy;

/// <summary>
/// Reference names handed to the out-of-process Roslyn compiler for building
/// plugins from source. Server plugins compile against the dedicated server's
/// assembly set plus the loader environment and the PluginSdk; there is no
/// WinForms/WPF block here (unlike Pulsar's client build) because the server
/// is headless on every platform.
/// </summary>
internal static class References
{
    private static readonly string[] common =
    [
        "Microsoft.CSharp",
        "0Harmony",
        "Newtonsoft.Json",
        "Mono.Cecil",
        "NLog",
        "PluginSdk",
    ];

    private static readonly string[] game =
    [
        "SpaceEngineers*.dll",
        "VRage*.dll",
        "Sandbox*.dll",
        "ProtoBuf*.dll",
        "protobuf*.dll", // the DS ships a lowercase protobuf-net.dll; Linux globs are case-sensitive
    ];

    private static readonly string[] excludeGlobs = ["VRage.Native.dll"];

    public static IEnumerable<string> GetReferences(string ds64)
    {
        foreach (string name in Tools.GetFiles(ds64, game, excludeGlobs))
            yield return name;

        foreach (string name in common)
            yield return name;
    }
}
