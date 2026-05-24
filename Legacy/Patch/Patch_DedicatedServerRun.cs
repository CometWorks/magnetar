using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Pulsar.Legacy.Patch;

// Magnetar never runs the dedicated server as a Windows Service, so the
// service launch branch is stripped from DedicatedServer.Run. That branch is
// the only reference to VRage.Dedicated.WindowsService / VRage.Service.MyServiceBase
// reachable on startup; leaving it in forces the JIT to load
// System.ServiceProcess, which is not part of the .NET (Core) runtime.
//
// The Windows Service types are matched by name on purpose: a typeof() here
// would itself drag in System.ServiceProcess when this patch is compiled.
[HarmonyPatchCategory("Early")]
[HarmonyPatch("VRage.Dedicated.DedicatedServer, VRage.Dedicated", "Run")]
internal static class Patch_DedicatedServerRun
{
    private const string WindowsService = "VRage.Dedicated.WindowsService";
    private const string MyServiceBase = "VRage.Service.MyServiceBase";

    public static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions
    )
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.operand is MethodBase method && IsWindowsService(method.DeclaringType))
            {
                // Neutralize in place so any branch targeting this instruction
                // (the else of the UserInteractive check) still resolves and
                // falls through to the method's return.
                instruction.opcode = OpCodes.Nop;
                instruction.operand = null;
            }

            yield return instruction;
        }
    }

    private static bool IsWindowsService(System.Type declaringType)
    {
        string name = declaringType?.FullName;
        return name == WindowsService || name == MyServiceBase;
    }
}
