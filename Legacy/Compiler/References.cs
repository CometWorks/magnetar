using System.Collections.Generic;
using Pulsar.Shared;

namespace Pulsar.Legacy.Compiler;

internal static class References
{
    private static readonly string[] baseEnvironment =
    [
        "Microsoft.CSharp",
        "0Harmony",
        "Newtonsoft.Json",
        "Mono.Cecil",
        "NLog",
    ];

    private static readonly string[] includeGlobs =
    [
        "SpaceEngineers*.dll",
        "VRage*.dll",
        "Sandbox*.dll",
        "ProtoBuf*.dll",
    ];

    private static readonly string[] excludeGlobs = ["VRage.Native.dll"];

    public static IEnumerable<string> GetReferences(string exeLocation)
    {
        foreach (string name in Tools.GetFiles(exeLocation, includeGlobs, excludeGlobs))
            yield return name;

        foreach (string name in baseEnvironment)
            yield return name;
    }
}
