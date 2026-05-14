using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Pulsar.Legacy.Patch;

// Magnetar is configured by an external interface (Web UI), so the dedicated
// server never shows its built-in Telerik/WinForms configuration UI and never
// runs as a Windows Service. Both branches of DedicatedServer.Run are stripped:
//
//   if (!ProcessArgs(args))
//   {
//       if (Environment.UserInteractive)
//           ...; MyConfigurator.Start(...);  // SelectInstanceForm + ConfigForm
//       else
//           MyServiceBase.Run(new WindowsService());
//   }
//
// The whole body is replaced with the headless path that the UI ultimately
// reaches anyway: selecting the "Local / Console" instance ("Continue to
// server configuration") and pressing "Save && Start" launches the server with
// RunMain on the default instance. RunMain loads the most recent world recorded
// in LastSession.sbl (see MySandboxGame world-load logic), which is exactly what
// we want for an externally configured server.
//
// ProcessArgs is still honoured so explicit flags (-console, -noconsole, -path,
// -session:, -ignorelastsession, ...) keep working; when it has already handled
// the launch we skip the implicit RunMain.
//
// Members are resolved by reflection off the patched type so this file pulls in
// no compile-time reference to VRage.Dedicated (RunMain is internal and the
// InitializeServices setter is private), and never touches the Windows Service
// types that would otherwise drag in System.ServiceProcess.
[HarmonyPatchCategory("Early")]
[HarmonyPatch("VRage.Dedicated.DedicatedServer, VRage.Dedicated", "Run")]
internal static class Patch_DedicatedServerRun
{
    public static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator,
        MethodBase original
    )
    {
        Type dedicatedServer = original.DeclaringType;
        MethodInfo setInitializeServices = AccessTools.PropertySetter(
            dedicatedServer,
            "InitializeServices"
        );
        MethodInfo processArgs = AccessTools.Method(dedicatedServer, "ProcessArgs");
        MethodInfo runMain = AccessTools.Method(dedicatedServer, "RunMain");
        FieldInfo defaultInstanceName = AccessTools.Field(dedicatedServer, "DefaultInstanceName");

        Label skipRun = generator.DefineLabel();
        CodeInstruction ret = new CodeInstruction(OpCodes.Ret);
        ret.labels.Add(skipRun);

        // InitializeServices = initializeServices;
        yield return new CodeInstruction(OpCodes.Ldarg_1);
        yield return new CodeInstruction(OpCodes.Call, setInitializeServices);

        // if (ProcessArgs(args)) return;
        yield return new CodeInstruction(OpCodes.Ldarg_0);
        yield return new CodeInstruction(OpCodes.Call, processArgs);
        yield return new CodeInstruction(OpCodes.Brtrue, skipRun);

        // RunMain(DefaultInstanceName, null, isService: false, showConsole: true, checkAlive: false);
        yield return new CodeInstruction(OpCodes.Ldsfld, defaultInstanceName);
        yield return new CodeInstruction(OpCodes.Ldnull);
        yield return new CodeInstruction(OpCodes.Ldc_I4_0);
        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
        yield return new CodeInstruction(OpCodes.Ldc_I4_0);
        yield return new CodeInstruction(OpCodes.Call, runMain);

        yield return ret;
    }
}
