// Parts of this file were contributed by yurei-so (PR #63) under the MIT License:
//
// MIT License
//
// Copyright (c) 2026 SpaceGT
// Copyright (c) 2026 Viktor Ferenczi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Magnetar.Legacy;
using Pulsar.Legacy.Extensions;
using Pulsar.Shared;
using Pulsar.Shared.Config;
using Pulsar.Shared.Data;
using Sandbox.Game.World;
using VRage.Game;
using VRage.Scripting;

namespace Pulsar.Legacy.Patch;

[HarmonyPatchCategory("Late")]
[HarmonyPatch(typeof(MyScriptManager), "LoadData")]
public static class Patch_MyScriptManager
{
    private static readonly Action<MyScriptManager, string, MyModContext> loadScripts;
    private static readonly FieldInfo conditionalSymbols;
    private const string ConditionalSymbol = CompilationSymbols.MagnetarSymbol;

    private static HashSet<string> ConditionalSymbols =>
        (HashSet<string>)conditionalSymbols.GetValue(MyScriptCompiler.Static);

    static Patch_MyScriptManager()
    {
        loadScripts =
            (Action<MyScriptManager, string, MyModContext>)
                Delegate.CreateDelegate(
                    typeof(Action<MyScriptManager, string, MyModContext>),
                    typeof(MyScriptManager).GetMethod(
                        "LoadScripts",
                        BindingFlags.Instance | BindingFlags.NonPublic
                    )
                );
        conditionalSymbols = typeof(MyScriptCompiler).GetField(
            "m_conditionalCompilationSymbols",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
    }

    public static void Postfix(MyScriptManager __instance)
    {
        try
        {
            HashSet<ulong> currentMods;
            if (MySession.Static.Mods is not null)
                currentMods = [.. MySession.Static.Mods.Select(x => x.PublishedFileId)];
            else
                currentMods = [];

            PluginList list = ConfigManager.Instance.List;
            Profile current = ConfigManager.Instance.Profiles.Current;
            HashSet<string> conditionalSymbols = ConditionalSymbols;
            bool symbolAdded = conditionalSymbols.Add(ConditionalSymbol);

            try
            {
                Patch_MyDefinitionErrors.RedirectModLogging(true);

                foreach (ModPlugin mod in list.GetModPlugins(current, currentMods))
                {
                    LogFile.WriteLine("Loading client mod scripts for " + mod.WorkshopId);
                    loadScripts(__instance, mod.ModLocation, mod.GetModContext());
                }
            }
            finally
            {
                Patch_MyDefinitionErrors.RedirectModLogging(false);
                if (symbolAdded)
                    conditionalSymbols.Remove(ConditionalSymbol);
            }
        }
        catch (Exception e)
        {
            LogFile.Error("An error occured while loading client mods: " + e);
            throw;
        }
    }
}
