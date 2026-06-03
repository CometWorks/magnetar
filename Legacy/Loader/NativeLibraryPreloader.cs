using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Pulsar.Legacy.Loader;

/// <summary>
/// Linux native-library bootstrap. Runs once at the very top of Main() and
/// is the single place that:
///   * dlopens every bundled lib*.so* next to the launcher with an absolute
///     path and RTLD_GLOBAL, so subsequent lookups never go to disk;
///   * resolves the Windows-style DLL names declared in Magnetar's bundled
///     Steamworks.NET against the preloaded handles.
///
/// Centralising here means plugins loaded into custom AssemblyLoadContexts
/// (Magnetar's .pl5 cache directories) no longer need their own resolver
/// registration: the ResolvingUnmanagedDll hook installed below fires for
/// every ALC (existing and future).
///
/// The dedicated server runs headless: no SDL, no DXVK, no FFmpeg.
/// EOS is needed even with Steam networking because
/// MySteamService.UpdateNetworkThread drives MyEOSNetworking on the network
/// thread (EOS_Initialize on the very first tick), and the bundled VRage.EOS
/// has a [DllImport("EOSSDK-Shipping.dll")] that has to resolve to the Linux
/// libEOSSDK-Linux-Shipping.so. The native physics wrappers (Havok /
/// RecastDetour / VRage.Native) are PE-loader replacements for the Windows
/// DLLs of the same names; the se-linux-compat plugin still does an explicit
/// Init call on each, but their DllImport sites also need the alias here so
/// the runtime can resolve them from any AssemblyLoadContext.
/// </summary>
internal static class NativeLibraryPreloader
{
    // libname asked by [DllImport(...)] -> handle of the underlying .so.
    // Filled in two passes: (1) preload populates the canonical filename,
    // (2) Aliases below add Windows / short / versioned aliases.
    private static readonly Dictionary<string, IntPtr> Handles =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<AssemblyLoadContext> HookedContexts = new();

    // Names that managed bindings request -> canonical Linux filename we
    // preloaded. The right-hand side is looked up in Handles after preload,
    // so every alias on the left gets the same dlopen handle.
    private static readonly (string Alias, string Target)[] Aliases =
    {
        // Steamworks (Magnetar's bundled Steamworks.NET.dll).
        ("steam_api64",     "libsteam_api.so"),
        ("steam_api64.dll", "libsteam_api.so"),

        // Epic Online Services SDK (VRage.EOS / Epic.OnlineServices).
        // Required even though the DS uses Steam networking — MyEOSNetworking
        // is initialized from MySteamService.UpdateNetworkThread.
        ("EOSSDK-Shipping",     "libEOSSDK-Linux-Shipping.so"),
        ("EOSSDK-Shipping.dll", "libEOSSDK-Linux-Shipping.so"),

        // se-linux-compat PE-loader wrappers for the Havok / RecastDetour /
        // VRage.Native Windows DLLs.
        ("Havok.dll",        "libHavok.so"),
        ("RecastDetour.dll", "libRecastDetour.so"),
        ("VRage.Native.dll", "libVRageNative.so"),
    };

    private const int RTLD_NOW    = 0x2;
    private const int RTLD_GLOBAL = 0x100;

    [DllImport("libdl.so.2", EntryPoint = "dlopen")]
    private static extern IntPtr dlopen(string filename, int flags);

    public static void Initialize(string baseDir)
    {
        if (!OperatingSystem.IsLinux())
            return;

        // 1. Preload every bundled lib*.so* with absolute path + RTLD_GLOBAL.
        //    Absolute path bypasses ld.so search order; RTLD_GLOBAL exposes
        //    symbols to subsequent dlopen calls.
        PreloadBundled(baseDir);

        // 2. Materialise the alias table. Done after preload so every alias
        //    that points to a successfully loaded library gets cached too.
        foreach (var (alias, target) in Aliases)
        {
            if (Handles.TryGetValue(target, out var handle) && !Handles.ContainsKey(alias))
                Handles[alias] = handle;
        }

        // 3. Hook the resolver on every existing and future ALC. The
        //    AppDomain.AssemblyLoad event fires for loads in any ALC (incl.
        //    Magnetar's plugin ALCs whose default DllImport search paths do
        //    not contain baseDir), so newly-created plugin contexts get
        //    hooked the moment their first assembly is loaded.
        foreach (var alc in AssemblyLoadContext.All)
            HookContext(alc);
        AppDomain.CurrentDomain.AssemblyLoad += (_, args) =>
        {
            var alc = AssemblyLoadContext.GetLoadContext(args.LoadedAssembly);
            if (alc != null) HookContext(alc);
        };
    }

    private static void PreloadBundled(string baseDir)
    {
        if (!Directory.Exists(baseDir))
            return;

        foreach (var path in Directory.EnumerateFiles(baseDir, "lib*.so*"))
        {
            var fileName = Path.GetFileName(path);
            if (Handles.ContainsKey(fileName))
                continue;

            var handle = dlopen(path, RTLD_NOW | RTLD_GLOBAL);
            if (handle == IntPtr.Zero)
            {
                Console.WriteLine($"[Magnetar] dlopen failed: {path}");
                continue;
            }

            Handles[fileName] = handle;

            // Also alias under the unversioned name (e.g. libsteam_api.so.1
            // resolves to libsteam_api.so). First wins.
            var unversioned = StripVersionSuffix(fileName);
            if (unversioned != fileName && !Handles.ContainsKey(unversioned))
                Handles[unversioned] = handle;
        }
    }

    // libfoo.so.62.28.100 -> libfoo.so   (anything after the first ".so." is
    // soname version metadata that no DllImport site ever spells out).
    private static string StripVersionSuffix(string fileName)
    {
        int idx = fileName.IndexOf(".so.", StringComparison.Ordinal);
        return idx < 0 ? fileName : fileName.Substring(0, idx + 3);
    }

    private static void HookContext(AssemblyLoadContext alc)
    {
        if (!HookedContexts.Add(alc))
            return;
        alc.ResolvingUnmanagedDll += Resolve;
    }

    // Fires only after the runtime's default native-probing fails, so the
    // common case (loading a bundled lib via its real name in a context
    // whose probe dirs contain baseDir) doesn't go through here.
    private static IntPtr Resolve(Assembly assembly, string libraryName)
    {
        return Handles.TryGetValue(libraryName, out var handle) ? handle : IntPtr.Zero;
    }
}
