using System.Collections.Generic;
using Pulsar.Shared;

namespace Magnetar.Legacy;

/// <summary>
/// Magnetar's counterpart of <see cref="Tools.GetCompilationSymbols"/>.
///
/// Pulsar yields PULSAR as its last trusted symbol to mark the loader that
/// compiled the code. Magnetar replaces it with MAGNETAR: the two are
/// mutually exclusive loader identities, not a hierarchy, so code compiled
/// here must never see PULSAR. Plugins rely on that to tell the dedicated
/// server apart from the game client — the dotnet-compat and linux-compat
/// repositories share one Shared/ tree between their client and server
/// plugins and select between them with #if MAGNETAR, down to the namespace
/// their rewriter lives in and the set of game assemblies they publicize.
///
/// The platform and runtime symbols upstream yields (NETCOREAPP, NETFRAMEWORK,
/// LINUX) are passed through unchanged, so a submodule bump that adds one is
/// picked up automatically.
///
/// The symbol is trusted-only, like PULSAR upstream: mod and in-game script
/// compilation (trusted: false) must not be able to branch on it.
/// </summary>
internal static class CompilationSymbols
{
    private const string PulsarSymbol = "PULSAR";

    /// <summary>
    /// The loader identity symbol. Also added to the game's own script
    /// compiler by Patch_MyScriptManager while client mod scripts load.
    /// </summary>
    public const string MagnetarSymbol = "MAGNETAR";

    public static IEnumerable<string> Get(bool trusted)
    {
        foreach (string symbol in Tools.GetCompilationSymbols(trusted))
            if (symbol != PulsarSymbol)
                yield return symbol;

        if (trusted)
            yield return MagnetarSymbol;
    }
}
